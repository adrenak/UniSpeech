#if RSG_PROMISE
using RSG;
#endif

using System.Net;
using System;

namespace RestSharp {
	public static class RestSharpExtensions {
#if RSG_PROMISE
		public static IPromise<IRestResponse> ExecuteAsync(this RestClient client, RestRequest request) {
			Promise<IRestResponse> promise = new Promise<IRestResponse>();
			client.ExecuteAsync(request, (response, handler) => promise.Resolve(response));
			return promise;
		}

		public static IPromise<IRestResponse> ExecuteAsync(this RestClient client, RestRequest request, out RestRequestAsyncHandle handle) {
			Promise<IRestResponse> promise = new Promise<IRestResponse>();
			handle = client.ExecuteAsync(request, (response, handler) => promise.Resolve(response));
			return promise;
		}
#endif

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
				return RestSharpException.New(response);
		}
	}
}
