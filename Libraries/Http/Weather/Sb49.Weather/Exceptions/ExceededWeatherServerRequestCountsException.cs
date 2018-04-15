using System;

namespace Sb49.Weather.Exceptions
{
    public class ExceededWeatherServerRequestCountsException : WeatherExceptionBase
    {
        public ExceededWeatherServerRequestCountsException() : base (Properties.Resources.ExceededWeatherServerRequestCounts)
        {
        }

        public ExceededWeatherServerRequestCountsException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}