using System;
using Android.App;
using Sb49.Weather.Exceptions;

namespace Sb49.Weather.Droid.Common.Exeptions
{
    public class Sb49HttpRequestException : WeatherExceptionBase
    {
        public Sb49HttpRequestException() : base(Application.Context.GetString(Resource.String.ConnectFailure))
        {
        }

        public Sb49HttpRequestException(string message) : base(message)
        {
        }

        public Sb49HttpRequestException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}