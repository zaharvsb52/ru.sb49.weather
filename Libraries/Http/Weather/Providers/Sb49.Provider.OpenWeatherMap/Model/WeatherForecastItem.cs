using Newtonsoft.Json;
using Sb49.Provider.OpenWeatherMap.Model.Core;

namespace Sb49.Provider.OpenWeatherMap.Model
{
    public class WeatherForecastItem : WeatherResponseBase
    {
        /// <summary>
        ///  Data/time of calculation, UTC.
        /// </summary>
        [JsonProperty("dt_txt")]
        public string Date { get; set; }
    }
}