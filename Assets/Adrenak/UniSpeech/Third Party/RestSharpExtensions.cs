using System.Net;
using System.Text;
using System;

namespace RestSharp {
	public static class RestSharpExtensions {
		public static bool IsSuccess(this HttpStatusCode responseCode) {
			var numericResponse = (int)responseCode;
			const int statusCodeOk = (int)HttpStatusCode.OK;
			const int statusCodeBadRequest = (int)HttpStatusCode.BadRequest;

			return numericResponse >= statusCodeOk && numericResponse < statusCodeBadRequest;
		}

		public static bool IsSuccess(this IRestResponse response) {
			return response.StatusCode.IsSuccess() && response.ResponseStatus == ResponseStatus.Completed;
		}

		public static Exception GetException(this IRestResponse response) {
			if (response.IsSuccess())
				return null;
			else
				return RestException.New(response);
		}
	}

	public class RestException : Exception {
		public RestException(HttpStatusCode httpStatusCode, Uri requestUri, string content, string message, Exception innerException)
			: base(message, innerException) {
			HttpStatusCode = httpStatusCode;
			RequestUri = requestUri;
			Content = content;
		}

		public HttpStatusCode HttpStatusCode { get; private set; }

		public Uri RequestUri { get; private set; }

		public string Content { get; private set; }

		public static RestException New(IRestResponse response) {

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

			return new RestException(response.StatusCode, uri, response.Content, sb.ToString(), innerException);
		}
	}
}
