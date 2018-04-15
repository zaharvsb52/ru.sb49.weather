using Newtonsoft.Json;

namespace Sb49.Provider.YahooWeather.Model
{
    public class Atmosphere
    {
        /// <summary>
        ///  Humidity, in percent. 
        /// </summary>
        [JsonProperty("humidity")]
        public double? Humidity { get; set; }

        /// <summary>
        /// Barometric pressure, in the units specified by the pressure attribute of the yweather:units element (in or mb).
        /// </summary>
        [JsonProperty("pressure")]
        public double? Pressure { get; set; }

        /// <summary>
        /// State of the barometric pressure: steady (0), rising (1), or falling (2).
        /// </summary>
        [JsonProperty("rising")]
        public int Rising { get; set; }

        /// <summary>
        /// Visibility, in the units specified by the distance attribute of the yweather:units element (mi or km). 
        /// Note that the visibility is specified as the actual value * 100. For example, a visibility of 16.5 miles will be specified as 1650. 
        /// A visibility of 14 kilometers will appear as 1400.
        /// </summary>
        [JsonProperty("visibility")]
        public double Visibility { get; set; }
    }
}