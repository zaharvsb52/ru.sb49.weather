using System.Collections.Generic;
using Newtonsoft.Json;

namespace Sb49.Provider.OpenWeatherMap.Model
{
    public class WeatherForecastResponse
    {
        [JsonProperty("city")]
        public City City { get; set; }

        //[JsonProperty("code")]
        //public string Code { get; set; }

        //[JsonProperty("message")]
        //public double Message { get; set; }

        [JsonProperty("list")]
        public List<WeatherForecastItem> Items { get; set; }
    }
}