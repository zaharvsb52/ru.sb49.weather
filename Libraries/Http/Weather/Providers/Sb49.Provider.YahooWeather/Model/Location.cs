using Newtonsoft.Json;

namespace Sb49.Provider.YahooWeather.Model
{
    public class Location
    {
        [JsonProperty("city")]
        public string City { get; set; }

        [JsonProperty("country")]
        public string Country { get; set; }

        [JsonProperty("region")]
        public string Region { get; set; }
    }
}