using Newtonsoft.Json;

namespace Sb49.Provider.YahooWeather.Model
{
    public class Results
    {
        [JsonProperty("channel")]
        public Channel Channel { get; set; }
    }
}