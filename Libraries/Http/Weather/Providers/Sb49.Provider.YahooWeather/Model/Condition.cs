using Newtonsoft.Json;

namespace Sb49.Provider.YahooWeather.Model
{
    public class Condition
    {
        [JsonProperty("code")]
        public int? Code { get; set; }

        /// <summary>
        /// Current date and time for which this forecast applies. The date is in RFC822 Section 5 format, for example "Wed, 30 Nov 2005 1:56 pm PST".
        /// </summary>
        [JsonProperty("date")]
        public string Date { get; set; }

        [JsonProperty("temp")]
        public double? Temp { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }
    }
}