namespace Adrenak.UniSpeech {
	public class SpeechFragmentMessage : MessageBase {
        public string Text { get; set; }
        public long Offset { get; set; }
        public long Duration { get; set; }
    }
}