using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;

namespace Sb49.Http.Core
{
    public abstract class HttpClientBase : IHttpClient
    {
        public abstract Uri BaseUri { get; }
        public TimeSpan? Timeout { get; set; }

        protected virtual object GetStringAsync(string requestString, CancellationToken token)
        {
            using (var handler = new HttpClientHandler())
            {
                if (handler.SupportsAutomaticDecompression)
                    handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

                using (var client = new HttpClient(handler, false))
                {
                    client.BaseAddress = BaseUri;
                    if (Timeout.HasValue)
                        client.Timeout = Timeout.Value;
                    if (IfCancellationRequested(token))
                        return null;
                    var result = GetStringAsync(client, requestString, token);
                    return result;
                }
            }
        }

        protected object GetStringAsync(HttpClient client, string requestString, CancellationToken token)
        {
            if (!OnValidateRequest(token))
                return null;

            var response = client.GetAsync(requestString, token).Result;
            if (IfCancellationRequested(token))
                return null;
            var json = response?.Content.ReadAsStringAsync().Result;
            OnIncrementRequestCount();
            if (IfCancellationRequested(token))
                return null;
            return OnProcessResponse(response?.Headers, json, token);
        }

        protected abstract object OnProcessResponse(HttpResponseHeaders responseHeaders, string json, CancellationToken token);
        protected abstract bool OnValidateRequest(CancellationToken token);
        protected abstract void OnIncrementRequestCount();
        protected abstract bool IfCancellationRequested(CancellationToken token);
    }
}