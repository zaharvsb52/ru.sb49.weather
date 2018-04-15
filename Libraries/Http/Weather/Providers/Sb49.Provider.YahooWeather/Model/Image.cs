using Newtonsoft.Json;

namespace Sb49.Provider.YahooWeather.Model
{
    public class Image
    {
        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("width")]
        public string Width { get; set; }

        [JsonProperty("height")]
        public string Height { get; set; }

        [JsonProperty("link")]
        public string Link { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
    }
}