using Newtonsoft.Json;

namespace Sb49.Provider.YahooWeather.Model
{
    //https://developer.yahoo.com/weather/documentation.html#codes
    public class WeatherForecastResponse
    {
        [JsonProperty("query")]
        public Query Query { get; set; }
    }
}