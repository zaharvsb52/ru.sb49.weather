using Newtonsoft.Json;

namespace Sb49.Provider.OpenWeatherMap.Model
{
    public class City
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("coord")]
        public Coordinates Coord { get; set; }

        [JsonProperty("country")]
        public string Country { get; set; }
    }
}