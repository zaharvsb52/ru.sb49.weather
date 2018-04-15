using Newtonsoft.Json;

namespace Sb49.Provider.OpenWeatherMap.Model
{
    public class WeatherSys
    {
        //[JsonProperty("type")]
        //public string Type { get; set; }

        //[JsonProperty("id")]
        //public string Id { get; set; }

        //[JsonProperty("message")]
        //public string Message { get; set; }

        /// <summary>
        /// Country code (GB, JP etc.).
        /// </summary>
        [JsonProperty("country")]
        public string Country { get; set; }

        /// <summary>
        /// Sunrise time, unix, UTC.
        /// </summary>
        [JsonProperty("sunrise")]
        public long? SunriseUnixTime { get; set; }

        /// <summary>
        /// Sunset time, unix, UTC.
        /// </summary>
        [JsonProperty("sunset")]
        public long? SunsetUnixTime { get; set; }
    }
}