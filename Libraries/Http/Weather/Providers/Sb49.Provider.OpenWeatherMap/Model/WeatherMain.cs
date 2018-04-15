using Newtonsoft.Json;

namespace Sb49.Provider.OpenWeatherMap.Model
{
    public class WeatherMain
    {
        /// <summary>
        /// Temperature. Unit Default: Kelvin, Metric: Celsius, Imperial: Fahrenheit.
        /// </summary>
        [JsonProperty("temp")]
        public double? Temperature { get; set; }

        /// <summary>
        /// Minimum temperature at the moment of calculation. This is deviation from 'temp' that is possible for large cities and megalopolises geographically expanded 
        /// (use these parameter optionally). Unit Default: Kelvin, Metric: Celsius, Imperial: Fahrenheit.
        /// </summary>
        [JsonProperty("temp_min")]
        public double? MinTemperature { get; set; }

        /// <summary>
        /// Maximum temperature at the moment of calculation. This is deviation from 'temp' that is possible for large cities and megalopolises geographically expanded
        /// (use these parameter optionally). Unit Default: Kelvin, Metric: Celsius, Imperial: Fahrenheit.
        /// </summary>
        [JsonProperty("temp_max")]
        public double? MaxTemperature { get; set; }

        /// <summary>
        /// Atmospheric pressure on the sea level by default, hPa.
        /// </summary>
        [JsonProperty("pressure")]
        public double? Pressure { get; set; }

        /// <summary>
        /// Humidity, %.
        /// </summary>
        [JsonProperty("humidity")]
        public double? Humidity { get; set; }
    }
}