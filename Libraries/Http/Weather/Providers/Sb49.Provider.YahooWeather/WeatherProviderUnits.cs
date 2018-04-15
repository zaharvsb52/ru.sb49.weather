using System;

namespace Sb49.Provider.YahooWeather
{
//developer.yahoo.com/weather/documentation.html#codes
    public enum WeatherProviderUnits
    {
        /// <summary>
        /// Temperature in Celsius.
        /// </summary>
        Si,

        /// <summary>
        /// Temperature in Fahrenheit.
        /// </summary>
        Imperial
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
                case WeatherProviderUnits.Si:
                    return "c";
                case WeatherProviderUnits.Imperial:
                    return "f";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}