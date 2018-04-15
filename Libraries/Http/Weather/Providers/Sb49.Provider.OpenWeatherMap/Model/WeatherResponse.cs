using Newtonsoft.Json;
using Sb49.Provider.OpenWeatherMap.Model.Core;

namespace Sb49.Provider.OpenWeatherMap.Model
{
    public class WeatherResponse : WeatherResponseBase
    {
        [JsonProperty("coord")]
        public Coordinates Coordinates { get; set; }

        [JsonProperty("sys")]
        public WeatherSys WeatherSys { get; set; }

        [JsonProperty("id")]
        public int? CityId { get; set; }

        [JsonProperty("name")]
        public string CityName { get; set; }

        /// <summary>
        /// Time of data calculation, unix, UTC.
        /// </summary>
        [JsonProperty("dt")]
        public long? DateUnixTime { get; set; }
    }
}