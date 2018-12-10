using System;

namespace Adrenak.UniSpeech {
	public static class Extensions {
		public static void TryInvoke<T>(this Action<T> action, T t) {
			if (action != null)
				action(t);
		}

		public static void TryInvoke(this Action action) {
			if (action != null)
				action();
		}
		
		public static byte[] ToBytes(this float[] floatArray) {
			var floatLen = floatArray.Length;
			Int16[] intData = new Int16[floatLen];
			Byte[] bytesData = new Byte[floatLen * 2];
			Byte[] byteArr = new Byte[2];

			int rescaleFactor = 32767; //to convert float to Int16

			for (int i = 0; i < floatArray.Length; i++) {
				intData[i] = (short)(floatArray[i] * rescaleFactor);
				byteArr = BitConverter.GetBytes(intData[i]);
				byteArr.CopyTo(bytesData, i * 2);
			}
			return bytesData;
		}
	}
}
