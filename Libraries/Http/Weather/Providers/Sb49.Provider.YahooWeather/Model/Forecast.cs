using Newtonsoft.Json;

namespace Sb49.Provider.YahooWeather.Model
{
    public class Forecast
    {
        /// <summary>
        /// the condition code for this forecast. You could use this code to choose a text description or image for the forecast. 
        /// The possible values for this element are described in Condition Codes.
        /// </summary>
        [JsonProperty("code")]
        public int? Code { get; set; }

        /// <summary>
        /// The date to which this forecast applies. The date is in "dd Mmm yyyy" format, for example "30 Nov 2005".
        /// </summary>
        [JsonProperty("date")]
        public string Date { get; set; }

        /// <summary>
        /// Day of the week to which this forecast applies. Possible values are Mon Tue Wed Thu Fri Sat Sun.
        /// </summary>
        [JsonProperty("day")]
        public string Day { get; set; }

        /// <summary>
        /// The forecasted high temperature for this day, in the units specified by the yweather:units element.
        /// </summary>
        [JsonProperty("high")]
        public int? High { get; set; }

        /// <summary>
        /// The forecasted low temperature for this day, in the units specified by the yweather:units element.
        /// </summary>
        [JsonProperty("low")]
        public int? Low { get; set; }

        /// <summary>
        /// A textual description of conditions, for example, "Partly Cloudy".
        /// </summary>
        [JsonProperty("text")]
        public string Text { get; set; }
    }
}