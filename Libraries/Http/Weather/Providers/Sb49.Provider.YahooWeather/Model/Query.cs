using Newtonsoft.Json;

namespace Sb49.Provider.YahooWeather.Model
{
    public class Query
    {
        [JsonProperty("count")]
        public int Count { get; set; }

        [JsonProperty("created")]
        public string CreatedDate { get; set; }

        [JsonProperty("lang")]
        public string Lang { get; set; }

        [JsonProperty("results")]
        public Results Results { get; set; }
    }
}