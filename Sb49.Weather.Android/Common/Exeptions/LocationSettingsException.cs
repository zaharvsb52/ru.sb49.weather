using System;
using Android.App;
using Sb49.Weather.Exceptions;

namespace Sb49.Weather.Droid.Common.Exeptions
{
    public class LocationSettingsException : WeatherExceptionBase
    {
        public LocationSettingsException() : base(Application.Context.GetString(Resource.String.ProhibitLocationSettings))
        {
        }

        public LocationSettingsException(string message) : base(message)
        {
        }

        public LocationSettingsException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}