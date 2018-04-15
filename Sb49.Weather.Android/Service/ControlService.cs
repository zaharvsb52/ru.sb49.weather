using System;
using Android.App;
using Android.Content;
using Sb49.Common.Logging;
using Sb49.Weather.Droid.Common;

namespace Sb49.Weather.Droid.Service
{
    [Service]
    [IntentFilter(
        new[]
        {
            ActionAppStart,
            ActionGeoTrackingServiceStart,
            ActionWeatherServiceStart,
            ActionAppServiceStart,
            ActionAppServiceStop
        })]
    public class ControlService : IntentService
    {
        private const string ServiceName = ".ControlService.";
        public const string ActionAppStart = AppSettings.AppPackageName + ServiceName + "ActionAppStart";
        public const string ActionGeoTrackingServiceStart = AppSettings.AppPackageName + ServiceName + "ActionGeoTrackingServiceStart";
        public const string ActionWeatherServiceStart = AppSettings.AppPackageName + ServiceName + "ActionWeatherServiceStart";
        public const string ActionAppServiceStart = AppSettings.AppPackageName + ServiceName + "ActionAppServiceStart";
        public const string ActionAppServiceStop = AppSettings.AppPackageName + ServiceName + "ActionAppServiceStop";
        private readonly ILog _log = LogManager.GetLogger<ControlService>();

        protected override void OnHandleIntent(Intent intent)
        {
            var action = intent?.Action;
            if (string.IsNullOrEmpty(action))
                return;

            try
            {
                var api = new ServiceApi();

                switch (action)
                {
                    case ActionAppStart:
                        OnAppStart(api);
                        break;
                    case ActionGeoTrackingServiceStart:
                        OnGeoTrackingServiceStart(api);
                        break;
                    case ActionWeatherServiceStart:
                        WeatherServiceStart(api);
                        break;
                    case ActionAppServiceStart:
                        OnAppServiceStart(api);
                        break;
                    case ActionAppServiceStop:
                        OnAppServiceStop(api);
                        break;
                }
            }
            catch (OperationCanceledException ex)
            {
                _log.Debug(ex);
            }
            catch (Exception ex)
            {
                _log.ErrorFormat("Action: '{0}'. {1}", action, ex);
            }
        }

        private void OnAppStart(ServiceApi api)
        {
            // стартуем GeoTrackingService
            GeoTrackingServiceStart(api);

            // стартуем GeoTrackingService
            WeatherServiceStart(api);

            OnArrangeAppWidgetSettings(api);

            // пытаемся остановить GeoTrackingService
            GeoTrackingServiceStop(api);

            // пытаемся остановить WeatherService
            WeatherServiceStop(api);
        }

        private void OnGeoTrackingServiceStart(ServiceApi api)
        {
            OnArrangeAppWidgetSettings(api);
            GeoTrackingServiceStartSafe(api);
        }

        private void OnAppServiceStart(ServiceApi api)
        {
            OnGeoTrackingServiceStart(api);
            WeatherServiceStart(api);
        }

        private void OnAppServiceStop(ServiceApi api)
        {
            GeoTrackingServiceStop(api);
            WeatherServiceStop(api);
            OnArrangeAppWidgetSettings(api);
        }

        private void OnArrangeAppWidgetSettings(ServiceApi api)
        {
            api.ArrangeAppWidgetSettings(this);
        }

        private void GeoTrackingServiceStart(ServiceApi api)
        {
            var serviceType = typeof(GeoTrackingService);
            if (!GeoTrackingService.IsServiceRunning)
                api.StartService(this, new Intent(this, serviceType));
        }

        private void GeoTrackingServiceStartSafe(ServiceApi api)
        {
            if (api.UseTrackCurrentLocation(this))
                GeoTrackingServiceStart(api);
            else
                GeoTrackingServiceStop(api);
        }

        private void GeoTrackingServiceStop(ServiceApi api)
        {
            if (!api.UseTrackCurrentLocation(this))
                api.StopService(this, new Intent(this, typeof(GeoTrackingService)));
        }

        private void WeatherServiceStart(ServiceApi api)
        {
            var serviceType = typeof(WeatherService);
            if (api.ExistsAppWidget(this) && !WeatherService.IsServiceRunning)
                api.StartService(this, new Intent(this, serviceType));
        }

        private void WeatherServiceStop(ServiceApi api)
        {
            if (!api.ExistsAppWidget(this))
                api.StopService(this, new Intent(this, typeof(WeatherService)));
        }
    }
}