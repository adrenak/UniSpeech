using UnityEngine;
using Adrenak.UniSpeech;
using Adrenak.UniMic;

public class Demo : MonoBehaviour {
	// Use this for initialization
	void Start () {
		// Setup service
		var service = new BingService();
		service.Authenticate(
			"INSERT_YOUR_BING_KEY_HERE",
			token => service.Connect(),
			exception => Debug.LogError(exception)
		);
		
		service.OnStateChange += state => Debug.Log("State changed: " + state);
		service.OnError += error => Debug.LogError(error);

		service.OnSpeechHypothesis += hypothesis => Debug.Log("Hypothesis: " + hypothesis.Text);
		service.OnSpeechPhrase += phrase => Debug.Log("Phrase: " + phrase.DisplayText);

		// Setup mic
		var mic = Mic.Instance;
		mic.StartRecording(16000, 20);
		mic.OnSampleReady += (index, sample) => {
			service.Stream(sample.ToBytes());
		};
	}
}
