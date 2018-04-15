using System;
using System.Threading;
using Android.App;
using Android.Content;
using Sb49.Weather.Droid.Common;
using Sb49.Weather.Droid.Service.Core;
using Sb49.Weather.Model;
using Sb49.Weather.Model.Core;

namespace Sb49.Weather.Droid.Service
{
    [Service]
    [IntentFilter(
         new[]
         {
             ActionGetWeatherData
         })]
    public class WeatherBoundService : BoundServiceBase
    {
        private const string ServiceName = ".WeatherBoundService.";
        public const string ActionGetWeatherData = AppSettings.AppPackageName + ServiceName + "ActionGetWeatherData";
        public const string ExtraIsOnlyDaily = AppSettings.AppPackageName + ".extra_isonlydaily";

        public bool IsOnlyDaily { get; private set; }
        public IWeatherDataPoint Currently { get; private set; }
        public WeatherDataPointDaily[] Daily { get; private set; }

        protected override void OnHandleIntent(Intent serviceIntent)
        {
            if (string.IsNullOrEmpty(serviceIntent?.Action))
                return;
           
            var action = serviceIntent.Action;

            try
            {
                Clear();
                var token = CancellationTokenSource.Token;

                if (IfCancellationRequested(token))
                    return;

                IsOnlyDaily = serviceIntent.GetBooleanExtra(ExtraIsOnlyDaily, false);

                switch (action)
                {
                    case ActionGetWeatherData:
                        OnGetWeatherData(token);
                        break;
                }
            }
            catch (OperationCanceledException ex)
            {
                Log.Debug(ex);
                ServiceExceptions[action] = ex;
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Action: '{0}'. {1}", action, ex);
                ServiceExceptions[action] = ex;
            }
            finally
            {
                try
                {
                    if (IsBound)
                    {
                        var intent = new Intent(action);
                        SendOrderedBroadcast(intent, null);
                    }
                }
                catch (Exception ex)
                {
                    Log.Debug(ex);
                }
            }
        }

        private void OnGetWeatherData(CancellationToken token)
        {
            if (IfCancellationRequested(token))
                return;

            WeatherForecast weather;
            if (IsOnlyDaily)
            {
                weather = AppSettings.Default.Weather;
            }
            else
            {
                var api = new ServiceApi();
                weather = api.GetWeatherData(this, AppSettings.Default, token);
                AppSettings.Default.Weather = weather;
            }
            OnGetWeatherDaily(weather, token);
        }

        private void OnGetWeatherDaily(WeatherForecast weather, CancellationToken token)
        {
            if (IfCancellationRequested(token))
                return;

            if (weather == null)
                return;

            var utcNow = DateTime.UtcNow;
            Currently = weather.FindActualCurrently(utcNow);
            if (Currently != null)
                Daily = weather.GetLocalDaily(utcNow.ToLocalTime());
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                Clear();
            }
            catch (Exception ex)
            {
                Log?.Debug(ex);
            }

            base.Dispose(disposing);
        }

        private void Clear()
        {
            ServiceExceptions?.Clear();

            IsOnlyDaily = false;

            Currently?.Dispose();
            Currently = null;

            if (Daily != null)
            {
                foreach (var daily in Daily)
                {
                    daily?.Dispose();
                }
                Daily = null;
            }
        }
    }
}