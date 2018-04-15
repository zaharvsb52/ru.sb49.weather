using Newtonsoft.Json;

namespace Sb49.Provider.OpenWeatherMap.Model
{
    /// <summary>
    /// More info Weather condition codes.
    /// </summary>
    public class WeatherInfo
    {
        /// <summary>
        /// Weather condition id.
        /// </summary>
        [JsonProperty("id")]
        public int? ConditionId { get; set; }

        /// <summary>
        /// Group of weather parameters (Rain, Snow, Extreme etc.).
        /// </summary>
        [JsonProperty("main")]
        public string Condition { get; set; }

        /// <summary>
        /// Weather condition within the group.
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("icon")]
        public string Icon { get; set; }
    }
}