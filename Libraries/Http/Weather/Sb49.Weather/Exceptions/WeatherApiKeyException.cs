using System;

namespace Sb49.Weather.Exceptions
{
    public class WeatherApiKeyException : WeatherExceptionBase
    {
        public WeatherApiKeyException() : base (Properties.Resources.UndefinedWeatherProviderApiKey)
        {
        }

        public WeatherApiKeyException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}