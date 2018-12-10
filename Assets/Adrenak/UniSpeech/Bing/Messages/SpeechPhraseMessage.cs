namespace Adrenak.UniSpeech {

	public class SpeechPhraseMessage : MessageBase {
		public string RecognitionStatus { get; set; }
		public string DisplayText { get; set; }
		public long Offset { get; set; }
		public long Duration { get; set; }
		public NBest[] NBest { get; set; }
	}

	public class NBest {
		public double Confidence { get; set; }
		public string Lexical { get; set; }
		public string ITN { get; set; }
		public string MaskedITN { get; set; }
		public string Display { get; set; }
	}
}
