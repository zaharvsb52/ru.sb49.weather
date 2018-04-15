using System;
using Sb49.Http.Provider.Exeptions;

namespace Sb49.Weather.Exceptions
{
    public class WeatherExceptionBase : ProviderServiceExceptionBase 
    {
        public WeatherExceptionBase()
        {
        }

        public WeatherExceptionBase(string message) : base(message)
        {
        }

        public WeatherExceptionBase(string message, Exception inner) : base(message, inner)
        {
        }
    }
}