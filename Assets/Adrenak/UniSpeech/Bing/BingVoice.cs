using Adrenak.Unex;
using System;
using UnityEngine;

namespace Adrenak.UniSpeech {
	public class BingVoice : MonoBehaviour {
		public event Action<string> OnError;
		public event Action<string> OnHypothesis;
		public event Action<string> OnResult;

		public bool DoDebug { get; set; }
		public BingService Service { get; private set; }
		public string Key { get; private set; }

		Mic m_Mic;

		BingVoice() { }

		public static BingVoice New(string key) {
			var go = new GameObject() {
				hideFlags = HideFlags.HideAndDontSave
			};
			DontDestroyOnLoad(go);

			var instance = go.AddComponent<BingVoice>();
			instance.Key = key;
			return instance;
		}

		void Start() {
			// Setup service
			Service = new BingService {
				DoDebug = DoDebug
			};

			Service.OnError += error => OnError.TryInvoke(error.Message);
			Service.OnSpeechHypothesis += hypothesis => OnHypothesis.TryInvoke(hypothesis.Text);
			Service.OnSpeechPhrase += phrase => OnResult.TryInvoke(phrase.DisplayText);

			// Handle Mic
			m_Mic = Mic.Instance;
			m_Mic.StartRecording(16000, 20);
			m_Mic.OnSampleReady += (index, sample) => Service.Stream(sample.ToBytes());
		}

		public void StartRecording() {
			if (!Service.IsAuthorized()) {
				Service.Authenticate(Key,
					token => Service.Connect(),
					exception => OnError.TryInvoke(exception.Message)
				);
			}
			else
				Service.Connect();
		}

		public void StopRecording() {
			Service.Disconnect();
		}
	}
}
