using Android.App;
using Sb49.Weather.Exceptions;

namespace Sb49.Weather.Droid.Common.Exeptions
{
    public class WeatherProviderException : WeatherExceptionBase
    {
        public WeatherProviderException() : base(Application.Context.GetString(Resource.String.UndefinedWeatherServiceProvider))
        {
        }
    }
}