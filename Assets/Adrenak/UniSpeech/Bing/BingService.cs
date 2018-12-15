using System;
using System.Linq;
using System.Collections.Generic;
using WebSocketSharp;
using Adrenak.Unex;

namespace Adrenak.UniSpeech {
	public class BingService {
		public string BaseUrl = "wss://westus.stt.speech.microsoft.com/speech/recognition/conversation/cognitiveservices/v1?format=simple&language={0}";
		public string Lang = "en-US";
		public bool DoDebug { get; set; }
		public bool Loop { get; set; }
		public BingAuthorization Auth { get; private set; }

		BingStatus m_Status;
		public BingStatus Status {
			get { return m_Status; }
			private set {
				m_Status = value;
				OnStatusChange.TryInvoke(m_Status);
			}
		}

		WebSocket m_Socket = null;
		string m_RequestId;

		// The header that needs to go with each speech segment
		byte[] m_Header;

		// The audio segment populated from individual audio samples that needs to be sent via the socket once filled
		byte[] m_Buffer = new byte[0];

		// ================================================
		// EVENTS
		// ================================================
		public event Action<BingStatus> OnStatusChange;
		public event Action<MessageBase> OnGetMessage;
		public event Action<Exception> OnError;

		public event Action<TurnStartMessage> OnTurnStart;
		public event Action<TurnEndMessage> OnTurnEnd;
		public event Action<SpeechEndDetectedMessage> OnSpeechEndDetected;
		public event Action<SpeechPhraseMessage> OnSpeechPhrase;
		public event Action<SpeechHypothesisMessage> OnSpeechHypothesis;
		public event Action<SpeechStartDetectedMessage> OnSpeechStartDetected;
		public event Action<SpeechFragmentMessage> OnSpeechFragment;

		// ================================================
		// PUBLIC
		// ================================================
		/// <summary>
		/// Creates a new instance. CALL FROM THE MAIN THREAD
		/// </summary>
		public BingService() {
			SecurityManager.Init();
			Dispatcher.Create();

			var url = String.Format(BaseUrl, Lang);
			m_Socket = new WebSocket(url);
			m_Socket.OnError += OnSocketError;
			m_Socket.OnMessage += OnSocketMessage;
		}

		~BingService() {
			Auth.Dispose();

			m_Socket.OnError -= OnSocketError;
			m_Socket.OnMessage -= OnSocketMessage;
			m_Socket = null;
		}

		/// <summary>
		/// Request authentication so that the client can communicate with the server
		/// </summary>
		/// <param name="key">The Bing Speech to Text API key</param>
		/// <param name="callback">Bool callback for whether the authentication was successful</param>
		public void Authenticate(string key, Action<string> onSuccess, Action<Exception> onFailure) {
			TryLog("Authenticating");
			Auth = new BingAuthorization(key);
			Status = BingStatus.Authenticating;

			Auth.FetchToken(
				token => {
					TryLog("Authorized: " + token);
					Status = BingStatus.Authenticated;
					m_RequestId = BingUtils.GetNewRequestID();
					m_Header = BingUtils.GetHeader(m_RequestId);
					onSuccess.TryInvoke(token);
				},
				exception => {
					TryLogError(exception);
					Status = BingStatus.Idle;
					onFailure.TryInvoke(exception);
				}
			);
		}

		/// <summary>
		/// Establishes socket connected with the server
		/// </summary>
		/// <returns>Whether the socket can attempt to connect. Usually returns true when the service has not been authenticated</returns>
		public bool Connect() {
			if (Status == BingStatus.Idle || !Auth.IsValid()) {
				TryLogError("Could not connect. Not authorized");
				return false;
			}

			m_Socket.CustomHeaders = new Dictionary<string, string> {
				{ "X-ConnectionId", BingUtils.GetNewRequestID()},
				{ "Authorization", "Bearer " + Auth.Token}
			};

			TryLog("Connecting...");
			Status = BingStatus.Connecting;
			m_Socket.ConnectAsync(
				() => {
					TryLog("Connected");
					Status = BingStatus.Connected;
				},
				exception => {
					TryLogError("Could not connect: " + exception);
					Status = BingStatus.Authenticated;
				}
			);
			return true;
		}

		/// <summary>
		/// Disconnects the instance by closing the socket connection
		/// </summary>
		/// <returns></returns>
		public bool Disconnect() {
			if (Status < BingStatus.Connected) {
				TryLogError("Can not disconnect. Not connected");
				return false;
			}

			TryLog("Disconnecting");
			Status = BingStatus.Disconnecting;
			m_Socket.CloseAsync(() => {
				TryLog("Disconnected");
				Status = BingStatus.Authenticated;
			});
			return true;
		}

		/// <summary>
		/// Adds the audio sample to the packet for streaming.
		/// </summary>
		/// <param name="sample">The audio sample to be streamed</param>
		/// <returns>True if this frame lead to completion of a packet that got sent</returns>
		public bool Stream(byte[] sample) {
			if (sample.Length == 0)
				return false;

			if (Status < BingStatus.Connected)
				return false;

			if (Status == BingStatus.Connected) {
				Status = BingStatus.Streaming;

				// Before we stream the audio, we must once send the speechConfig
				var config = BingUtils.GetConfig(m_RequestId);
				m_Socket.Send(config);
			}

			var freeBytes = 8192 - m_Header.Length;
			m_Buffer = m_Buffer.Concat(sample).ToArray();

			// If we can not add more to the buffer
			if (m_Buffer.Length + sample.Length < freeBytes)
				return true;

			// We require 8192 bytes to data. After adding header and audio data, we may still have some bytes left
			// We fill them up with empty/blank bytes and send via the socket. The blank is small enough that the service will
			// not think of it as microphone silence
			var blankLen = 8192 - m_Header.Length - m_Buffer.Length;
			var packet = m_Header.Concat(m_Buffer).Concat(new byte[blankLen]).ToArray();

			try { m_Socket.Send(packet); } catch { }
			m_Buffer = new byte[0];

			return true;
		}

		public bool IsAuthorized() {
			return Auth != null && Auth.IsValid();
		}

		// ================================================
		// SOCKET EVENTS
		// ================================================
		void OnSocketError(object sender, ErrorEventArgs e) {
			var exception = e.Exception;
			if (exception != null)
				OnError.TryInvoke(e.Exception);
			else
				OnError.TryInvoke(new Exception("Unknown socket error"));

			Status = BingStatus.Authenticated;
		}

		void OnSocketMessage(object sender, MessageEventArgs e) {
			var parser = new MessageParser(e.Data);
			var message = parser.GetObject();

			// All events are fired from the main thread
			Dispatcher.Enqueue(() => {
				OnGetMessage.TryInvoke(message);
				switch (parser.Path.ToLower()) {
					case "turn.start":
						OnTurnStart.TryInvoke(message as TurnStartMessage);
						break;
					case "turn.end":
						m_Socket.Send(BingUtils.TurnEndAcknowledgement());
						m_Buffer = new byte[0];
						OnTurnEnd.TryInvoke(message as TurnEndMessage);

						Status = BingStatus.Disconnecting;
						m_Socket.CloseAsync(() => {
							Connect();
						});
						break;
					case "speech.enddetected":
						OnSpeechEndDetected.TryInvoke(message as SpeechEndDetectedMessage);
						break;
					case "speech.phrase":
						OnSpeechPhrase.TryInvoke(message as SpeechPhraseMessage);
						break;
					case "speech.hypothesis":
						OnSpeechHypothesis.TryInvoke(message as SpeechHypothesisMessage);
						break;
					case "speech.startdetected":
						OnSpeechStartDetected.TryInvoke(message as SpeechStartDetectedMessage);
						break;
					case "speech.fragment":
						OnSpeechFragment.TryInvoke(message as SpeechFragmentMessage);
						break;
				}
			});
		}

		void TryLog(object msg) {
			if (DoDebug)
				UnityEngine.Debug.Log("[BingService]" + msg);
		}

		void TryLogError(object err) {
			if (DoDebug)
				UnityEngine.Debug.LogError("[BingService]" + err);
		}
	}
}
