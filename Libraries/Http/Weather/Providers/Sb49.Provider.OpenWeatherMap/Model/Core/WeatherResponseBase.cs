using System.Collections.Generic;
using Newtonsoft.Json;

namespace Sb49.Provider.OpenWeatherMap.Model.Core
{
    public class WeatherResponseBase
    {
        [JsonProperty("main")]
        public WeatherMain WeatherMain { get; set; }

        [JsonProperty("weather")]
        public List<WeatherInfo> WeatherInfos { get; set; }

        [JsonProperty("clouds")]
        public Clouds Clouds { get; set; }

        [JsonProperty("wind")]
        public Wind Wind { get; set; }
    }
}