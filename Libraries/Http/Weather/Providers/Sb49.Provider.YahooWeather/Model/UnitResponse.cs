using Newtonsoft.Json;

namespace Sb49.Provider.YahooWeather.Model
{
    public class UnitResponse
    {
        /// <summary>
        /// Units for distance, mi for miles or km for kilometers.
        /// </summary>
        [JsonProperty("distance")]
        public string Distance { get; set; }

        /// <summary>
        /// Units of barometric pressure, in for pounds per square inch or mb for millibars.
        /// </summary>
        [JsonProperty("pressure")]
        public string Pressure { get; set; }

        /// <summary>
        /// Units of speed, mph for miles per hour or kph for kilometers per hour.
        /// </summary>
        [JsonProperty("speed")]
        public string Speed { get; set; }

        /// <summary>
        /// Degree units, f for Fahrenheit or c for Celsius.
        /// </summary>
        [JsonProperty("temperature")]
        public string Temperature { get; set; }
    }
}