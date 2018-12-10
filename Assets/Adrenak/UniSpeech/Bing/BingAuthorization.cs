using System;
using RestSharp;
using System.Threading;
using Adrenak.UniSpeech;

namespace Adrenak.UniSpeech {
	public class BingAuthorization {
		const string k_TokenFetchUrl = "https://westus.api.cognitive.microsoft.com/sts/v1.0/issueToken";
		string m_SubscriptionKey;
		public string Token { get; private set; }
		Timer m_TokenTimer;

		bool m_Enabled;

		// We need to renew token every 10 minutes, we set the timer duration to 9
		const int k_RenewTimerDuration = 9;

		public BingAuthorization(string subscriptionKey) {
			m_SubscriptionKey = subscriptionKey;
		}

		public void FetchToken(Action<string> onSuccess = null, Action<Exception> onFailure = null) {
			m_Enabled = true;

			var client = new RestClient(k_TokenFetchUrl);
			var request = new RestRequest(Method.POST);
			request.AddHeader("Ocp-Apim-Subscription-Key", m_SubscriptionKey);

			client.ExecuteAsync(request, (response, handle) => {
				if (response.IsSuccess()) {					
					Token = response.Content;
					onSuccess.TryInvoke(Token);
				}
				else
					onFailure.TryInvoke(response.GetException());
			});
		}
		
		public void StopFetch() {
			m_Enabled = false;
		}

		public void Dispose() {
			StopFetch();
			if (m_TokenTimer != null) m_TokenTimer.Dispose();
		}
		
		void CreateTimer() {
			m_TokenTimer = new Timer(
				new TimerCallback(OnTokenExpiredCallback),
				this,
				TimeSpan.FromMinutes(k_RenewTimerDuration),
				TimeSpan.FromMilliseconds(-1)
			);
		}

		void OnTokenExpiredCallback(object stateInfo) {
			if (m_Enabled)
				FetchToken();
		}
	}
}
