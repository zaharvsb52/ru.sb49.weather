using System;
using Android.App;
using Sb49.Weather.Exceptions;

namespace Sb49.Weather.Droid.Common.Exeptions
{
    public class NothingForecastException : WeatherExceptionBase
    {
        public NothingForecastException() : base (Application.Context.GetString(Resource.String.NoForecastMessage))
        {
        }

        public NothingForecastException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}