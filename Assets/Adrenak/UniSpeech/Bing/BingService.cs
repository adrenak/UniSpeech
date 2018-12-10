using System;
using System.Linq;
using System.Collections.Generic;
using WebSocketSharp;

namespace Adrenak.UniSpeech {
	public class BingService {
		const string k_BaseUrl = "wss://westus.stt.speech.microsoft.com/speech/recognition/conversation/cognitiveservices/v1?format=simple&language={0}";
		const string k_Lang = "en-US";

		public BingAuthorization Auth { get; private set; }

		BingState m_State;
		public BingState State {
			get { return m_State; }
			private set {
				m_State = value;
				Dispatcher.Enqueue(() => OnStateChange.TryInvoke(m_State));
			}
		}

		WebSocket m_Socket = null;
		string m_RequestId;
		bool m_Running = false;

		// The header that needs to go with each speech segment
		byte[] m_Header;

		// The audio segment populated from individual audio samples that needs to be sent via the socket once filled
		byte[] m_Buffer = new byte[0];
		
		// ================================================
		// EVENTS
		// ================================================
		public event Action<BingState> OnStateChange;
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
		/// Creates a new instance
		/// </summary>
		public BingService() {
			UniSpeechSecurity.Init();
			Dispatcher.Create();
			var url = String.Format(k_BaseUrl, k_Lang);
			m_Socket = new WebSocket(url);
			SubscribeToSockets();
		}

		~BingService() {
			Dispose();
			UnsubscribeToSocket();
		}

		/// <summary>
		/// Request authentication so that the client can communicate with the server
		/// </summary>
		/// <param name="key">The Bing Speech to Text API key</param>
		/// <param name="callback">Bool callback for whether the authentication was successful</param>
		public void Authenticate(string key, Action<string> onSuccess, Action<Exception> onFailure) {
			Auth = new BingAuthorization(key);

			Auth.FetchToken(
				token => {
					State = BingState.Authenticated;
					m_RequestId = BingUtils.GetNewRequestID();
					m_Header = BingUtils.GetHeader(m_RequestId);
					onSuccess.TryInvoke(token);
				},
				exception => {
					State = BingState.Idle;
					onFailure.TryInvoke(exception);
				}
			);
		}

		/// <summary>
		/// Establishes socket connected with the server
		/// </summary>
		/// <returns>Whether the socket can attemp to connect.</returns>
		public bool Connect() {
			if (State != BingState.Authenticated || State == BingState.Connected) {
				UnityEngine.Debug.LogError("Invoke Authenticate before Connect");
				return false;
			}

			m_Socket.CustomHeaders = new Dictionary<string, string> {
				{ "X-ConnectionId", BingUtils.GetNewRequestID()},
				{ "Authorization", "Bearer " + Auth.Token}
			};

			m_Socket.ConnectAsync();
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

			if (State != BingState.Connected && State != BingState.StreamingStarted)
				return false;

			if (State == BingState.Connected) {
				State = BingState.StreamingStarted;

				// Before we stream the audio, we must once send the speechConfig
				var config = BingUtils.GetConfig(m_RequestId);
				m_Socket.Send(config);
			}
			var freeBytes = 8192 - m_Header.Length;
			m_Buffer = m_Buffer.Concat(sample).ToArray();

			// If we can not add more to the buffer
			//if(m_Buffer.Length + sample.Length < freeBytes)
			if (m_Buffer.Length != (freeBytes / sample.Length) * sample.Length)
				return false;

			// We require 8192 bytes to data. After adding header and audio data, we may still have some bytes left
			// We fill them up with empty/blank bytes and send via the socket. The blank is small enough that the service will
			// not think of it as microphone silence
			var blankLen = 8192 - m_Header.Length - m_Buffer.Length;
			var packet = m_Header.Concat(m_Buffer).Concat(new byte[blankLen]).ToArray();

			m_Socket.Send(packet);
			m_Buffer = new byte[0];

			return true;
		}

		/// <summary>
		/// Disposes the instance
		/// </summary>
		public void Dispose() {
			m_Running = false;
			Auth.Dispose();
			m_Socket = null;
		}

		// ================================================
		// SOCKET EVENTS
		// ================================================
		void OnSocketOpen(object sender, EventArgs e) {
			// Set state to connected so that the Stream method can work
			State = BingState.Connected;
		}

		void OnSocketError(object sender, ErrorEventArgs e) {
			var exception = e.Exception;
			if (exception != null)
				OnError.TryInvoke(e.Exception);
			else
				OnError.TryInvoke(new Exception("Unknown socket error"));

			State = BingState.Authenticated;
		}

		void OnSocketMessage(object sender, MessageEventArgs e) {
			var parser = new MessageParser(e.Data);
			var message = parser.GetObject();

			Action EndStreaming = () => {
				// Blip the state
				State = BingState.StreamingEnded;
				State = BingState.Connected;
				m_Socket.Send(BingUtils.TurnEndAcknowledgement());
				m_Buffer = new byte[0];
			};

			// All events are fired from the main thread
			Dispatcher.Enqueue(() => {
				OnGetMessage.TryInvoke(message);
				switch (parser.Path.ToLower()) {
					case "turn.start":
						OnTurnStart.TryInvoke(message as TurnStartMessage);
						break;
					case "turn.end":
						EndStreaming();
						OnTurnEnd.TryInvoke(message as TurnEndMessage);
						break;
					case "speech.enddetected":
						EndStreaming();
						OnSpeechEndDetected.TryInvoke(message as SpeechEndDetectedMessage);
						break;
					case "speech.phrase":
						EndStreaming();
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

		void OnSocketClose(object sender, CloseEventArgs e) {
			if (!m_Running) {
				m_Socket.Close();
				m_Socket = null;
			}
			State = BingState.Idle;
		}

		void SubscribeToSockets() {
			m_Socket.OnOpen += OnSocketOpen;
			m_Socket.OnError += OnSocketError;
			m_Socket.OnMessage += OnSocketMessage;
			m_Socket.OnClose += OnSocketClose;
		}

		void UnsubscribeToSocket() {
			m_Socket.OnOpen -= OnSocketOpen;
			m_Socket.OnError -= OnSocketError;
			m_Socket.OnMessage -= OnSocketMessage;
			m_Socket.OnClose -= OnSocketClose;
		}
	}
}
