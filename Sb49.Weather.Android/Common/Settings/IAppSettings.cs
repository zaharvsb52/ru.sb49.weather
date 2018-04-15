using System;
using Sb49.Security.Core;
using Sb49.Weather.Core;
using Sb49.Weather.Droid.Model;
using Sb49.Weather.Model;

namespace Sb49.Weather.Droid.Common
{
    public interface IAppSettings : IDisposable
    {
        bool UseTrackCurrentLocation { get; set; }
        LocationAddress LocationAddress { get; set; }
        ISb49SecureString WeatherApiKey { get; }
        IWeatherProviderService WeatherProviderService { get; }
        WeatherForecast Weather { get; set; }
        int WeatherProviderId { get; set; }
        bool WeatherDataUpdating { get; }

        int WidgetId { get; }
        bool IsNotAppWidget { get; }
        void BeginWeatherDataUpdate();
        void EndWeatherDataUpdate();
    }
}