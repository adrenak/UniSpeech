using System.Net;
using System.Text;
using System;

namespace RestSharp {
	public class RestSharpException : Exception {
		public RestSharpException(HttpStatusCode httpStatusCode, Uri requestUri, string content, string message, Exception innerException)
			: base(message, innerException) {
			HttpStatusCode = httpStatusCode;
			RequestUri = requestUri;
			Content = content;
		}

		public HttpStatusCode HttpStatusCode { get; private set; }
		public Uri RequestUri { get; private set; }
		public string Content { get; private set; }
		public static RestSharpException New(IRestResponse response) {

			Exception innerException = null;

			var sb = new StringBuilder();
			var uri = response.ResponseUri;

			sb.AppendLine(string.Format("Processing request [{0}] resulted with following errors:", uri));

			if (response.StatusCode.IsSuccess() == false) {
				sb.AppendLine("- Server responded with unsuccessfull status code: " + response.StatusCode + ": " + response.StatusDescription);
				sb.AppendLine("- " + response.Content);
			}

			if (response.ErrorException != null) {
				sb.AppendLine("- An exception occurred while processing request: " + response.ErrorMessage);
				innerException = response.ErrorException;
			}

			return new RestSharpException(response.StatusCode, uri, response.Content, sb.ToString(), innerException);
		}
	}
}