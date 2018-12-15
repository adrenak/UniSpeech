using UnityEngine;
using Adrenak.UniSpeech;

public class BingDemo : MonoBehaviour {
	BingVoice voice;

	void Start() {
		voice = BingVoice.New("ENTER_KEY_HERE");
		voice.OnHypothesis += hypo => Debug.Log(hypo);
		voice.OnResult += res => Debug.Log(res);
	}

	[ContextMenu("Start")]
	public void StartRecording() {
		voice.StartRecording();
	}

	[ContextMenu("Stop")]
	public void StopRecording() {
		voice.StopRecording();
	}
}
