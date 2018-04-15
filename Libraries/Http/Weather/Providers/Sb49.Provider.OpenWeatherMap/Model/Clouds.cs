using Newtonsoft.Json;

namespace Sb49.Provider.OpenWeatherMap.Model
{
    public class Clouds
    {
        [JsonProperty("all")]
        public int CloudinessPercent { get; set; }
    }
}