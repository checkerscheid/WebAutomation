using System;
using System.Net;
using System.Net.Http;

namespace WebAutomation.Communication {
	internal static class SharedHttpClient {
		public static readonly HttpClient Instance = CreateClient();

		private static HttpClient CreateClient() {
			// ensure TLS 1.2
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
			var handler = new HttpClientHandler();
			// Verhindere, dass sich Cookies über die Lebenszeit des Singleton-Clients ansammeln
			// (kann zu großen Cookie-Headern und HTTP 431 führen). Wenn Cookies benötigt werden,
			// muss Cookie-Management pro Anfrage oder mit einer begrenzten CookieContainer-Strategie erfolgen.
			handler.UseCookies = false;
			// default handler settings can be adjusted here if needed
			var client = new HttpClient(handler, disposeHandler: true);
			client.Timeout = TimeSpan.FromSeconds(30);
			return client;
		}
	}
}
