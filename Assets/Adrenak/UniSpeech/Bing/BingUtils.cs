using System;
using System.Text;
using System.Linq;

namespace Adrenak.UniSpeech {
	public class BingUtils {
        // SPEECH CONFIG
        const string k_SpeechContext = 
@"{
	""context"":{
		""system"":{""version"":""1.0.00000""},
		""os"":{
			""platform"":""Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/59.0.3071.115 Safari/537.36"",
			""name"":""Browser"",
			""version"":""""
		},
		""device"":{""manufacturer"":""SpeechSample"",""model"":""SpeechSample"",""version"":""1.0.00000""}
	}
}";

		public static string GetConfig(string requestId) {
            StringBuilder speechConfigBuilder = new StringBuilder();
            speechConfigBuilder.Append("path:speech.config\r\n");
            speechConfigBuilder.Append("x-timestamp:" + DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffK") + "\r\n");
            speechConfigBuilder.Append("content-type:application/json\r\n");
            speechConfigBuilder.Append("x-requestId:" + requestId + "\r\n");
            speechConfigBuilder.Append("\r\n\r\n");
            speechConfigBuilder.Append(k_SpeechContext);
            return speechConfigBuilder.ToString();
        }

        // HEADER
        public static byte[] GetHeader(string requestId) {
            var headerBuilder = new StringBuilder();
            headerBuilder.Append("path:audio\r\n");
            headerBuilder.Append("x-requestid:" + requestId + "\r\n");
            headerBuilder.Append("x-timestamp:" + DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffK") + "\r\n");
            headerBuilder.Append("content-type:audio/wav; codec=audio/pcm; samplerate=16000");
            headerBuilder.Append("Accept:application/json");

			byte[] headerBytes = Encoding.ASCII.GetBytes(headerBuilder.ToString());
            byte[] headerBytesUInt16 = BitConverter.GetBytes((UInt16)headerBytes.Length);
            bool isBigEndian = !BitConverter.IsLittleEndian;
            var headerHead = !isBigEndian ? new byte[] { headerBytesUInt16[1], headerBytesUInt16[0] } : new byte[] { headerBytesUInt16[0], headerBytesUInt16[1] };

            return headerHead.Concat(headerBytes).ToArray();
        }

        // TURN END ACKNOWLEDGEMENT
        public static string TurnEndAcknowledgement() {
            StringBuilder ackBuilder = new StringBuilder();
            ackBuilder.Append("path:telemetry\r\n");
            ackBuilder.Append("x-timestamp:" + DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffK") + "\r\n");
            ackBuilder.Append("content-type:application/json\r\n");
            ackBuilder.Append("\r\n\r\n");
            ackBuilder.Append("Details");
            return ackBuilder.ToString();
        }

		public static string GetNewRequestID() {
			return Guid.NewGuid().ToString("N");
		}
	}
}
