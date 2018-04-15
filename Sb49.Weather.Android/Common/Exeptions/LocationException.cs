using System;
using Android.App;
using Sb49.Weather.Exceptions;

namespace Sb49.Weather.Droid.Common.Exeptions
{
    public class LocationException : WeatherExceptionBase
    {
        public LocationException() : base(Application.Context.GetString(Resource.String.UndefinedLocation))
        {
        }

        public LocationException(string message) : base(message)
        {
        }

        public LocationException(int resourceMessageId) : base(Application.Context.GetString(resourceMessageId))
        {
        }

        public LocationException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}