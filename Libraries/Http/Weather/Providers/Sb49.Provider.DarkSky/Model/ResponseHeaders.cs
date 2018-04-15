using System.Net.Http.Headers;

namespace Sb49.Provider.DarkSky.Model
{
    public class ResponseHeaders
    {
        public long? ApiCalls { get; set; }
        public CacheControlHeaderValue CacheControl { get; set; }
        public string ResponseTime { get; set; }
    }
}