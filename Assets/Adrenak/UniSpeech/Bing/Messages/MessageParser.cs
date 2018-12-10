using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace Adrenak.UniSpeech {
	public class MessageParser {
        public string Body { get; private set; }
        public List<KeyValuePair<string, string>> Headers { get; private set; }

        /// <summary>
        /// Constructs an instance using a server response text
        /// </summary>
        /// <param name="message">The server response to be used to construct the instance</param>
        public MessageParser(string message) {
            var headers = new List<KeyValuePair<string, string>>();

            StringReader reader = new StringReader(message);
            string line = null;
            do {
                line = reader.ReadLine();

                if (line == string.Empty) 
                    break;
                else {
                    var colonIndex = line.IndexOf(':');
                    if (colonIndex > -1) {
                        headers.Add(new KeyValuePair<string, string>(line.Substring(0, colonIndex), line.Substring(colonIndex + 1)));
                    }
                }
            } while (line != null);
            
            Headers = headers;
            Body = reader.ReadToEnd();
        }

        /// <summary>
        /// Returns the text message as a C# object, derives from <see cref="MessageBase"/>
        /// </summary>
        /// <returns></returns>
        public MessageBase GetObject() {
			Type type;
            switch (Path.ToLower()) {
                case "turn.start":
                    type = typeof(TurnStartMessage);
                    break;
                case "turn.end":
                    type = typeof(TurnEndMessage);
                    break;
                case "speech.enddetected":
                    type = typeof(SpeechEndDetectedMessage);
                    break;
                case "speech.phrase":
                    type = typeof(SpeechPhraseMessage);
                    break;
                case "speech.hypothesis":
                    type = typeof(SpeechHypothesisMessage);
                    break;
                case "speech.startdetected":
                    type = typeof(SpeechStartDetectedMessage);
                    break;
                case "speech.fragment":
                    type = typeof(SpeechFragmentMessage);
                    break;
                default:
                    throw new NotSupportedException(Path);
            }
            return (MessageBase)JsonConvert.DeserializeObject(Body, type);
		}
		
        /// <summary>
        /// Gets the header with the given name
        /// </summary>
        public string GetHeader(string key) {
            foreach (var p in Headers) {
                if (string.Equals(p.Key, key, StringComparison.OrdinalIgnoreCase)) 
                    return p.Value;
            }
            return null;
        }

        public string Path {
            get { return GetHeader("path"); }
        }

        public string RequestId {
            get { return GetHeader("x-requestid"); }
        }
    }
}