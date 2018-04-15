using Newtonsoft.Json;

namespace Sb49.Provider.OpenWeatherMap.Model
{
    public class Coordinates
    {
        [JsonProperty("lon")]
        public double? Longitude { get; set; }

        [JsonProperty("lat")]
        public double? Latitude { get; set; }
    }
}