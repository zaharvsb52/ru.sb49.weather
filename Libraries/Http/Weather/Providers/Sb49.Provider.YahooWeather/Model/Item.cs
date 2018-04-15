using System.Collections.Generic;
using Newtonsoft.Json;

namespace Sb49.Provider.YahooWeather.Model
{
    public class Item
    {
        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("lat")]
        public double? Latitude { get; set; }

        [JsonProperty("long")]
        public double? Longitude { get; set; }

        [JsonProperty("link")]
        public string Link { get; set; }

        [JsonProperty("pubDate")]
        public string PubDate { get; set; }

        [JsonProperty("condition")]
        public Condition Condition { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("forecast")]
        public List<Forecast> Forecast { get; set; }

        [JsonProperty("guid")]
        public YahooGuid YahooGuid { get; set; }
    }
}