using Newtonsoft.Json;

namespace Sb49.Provider.YahooWeather.Model
{
    public class Astronomy
    {
        /// <summary>
        /// Today's sunrise time. The time is a string in a local time format of "h:mm am/pm", for example "7:02 am".
        /// </summary>
        [JsonProperty("sunrise")]
        public string Sunrise { get; set; }

        /// <summary>
        /// Today's sunset time. The time is a string in a local time format of "h:mm am/pm", for example "4:51 pm".
        /// </summary>
        [JsonProperty("sunset")]
        public string Sunset { get; set; }
    }
}