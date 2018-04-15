using System;

namespace Sb49.Provider.OpenWeatherMap
{
    //http://openweathermap.org/weather-data
    public enum WeatherProviderUnits
    {
        /// <summary>
        /// Temperature in Kelvin.
        /// </summary>
        //Default,

        /// <summary>
        /// Temperature in Fahrenheit.
        /// </summary>
        Imperial,

        /// <summary>
        /// Temperature in Celsius.
        /// </summary>
        Metric
    }

    public enum WeatherDataResponseType
    {
        Forecast,
        Current
    }

    /// <summary>
    /// Extension methods for the request parameter enumerations.
    /// </summary>
    public static class ParameterExtensions
    {
        public static string ToValue(this WeatherProviderUnits self)
        {
            switch (self)
            {
                    case WeatherProviderUnits.Imperial:
                    return "imperial";
                    case WeatherProviderUnits.Metric:
                    return "metric";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static string ToValue(this WeatherDataResponseType self)
        {
            switch (self)
            {
                case WeatherDataResponseType.Forecast:
                    return "forecast";
                case WeatherDataResponseType.Current:
                    return "weather";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}