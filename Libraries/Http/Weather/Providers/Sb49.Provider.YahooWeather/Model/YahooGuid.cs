using Newtonsoft.Json;

namespace Sb49.Provider.YahooWeather.Model
{
    public class YahooGuid
    {
        [JsonProperty("isPermaLink")]
        public string IsPermaLink { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }
    }
}