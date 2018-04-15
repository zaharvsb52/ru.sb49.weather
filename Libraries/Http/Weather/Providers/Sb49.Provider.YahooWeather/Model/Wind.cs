using Newtonsoft.Json;

namespace Sb49.Provider.YahooWeather.Model
{
    public class Wind
    {
        /// <summary>
        /// Wind chill in degrees.
        /// </summary>
        [JsonProperty("chill")]
        public int? Chill { get; set; }

        /// <summary>
        /// Wind direction, in degrees.
        /// </summary>
        [JsonProperty("direction")]
        public int? Direction { get; set; }

        /// <summary>
        /// Wind speed, in the units specified in the speed attribute of the yweather:units element (mph or kph).
        /// </summary>
        [JsonProperty("speed")]
        public double? Speed { get; set; }
    }
}