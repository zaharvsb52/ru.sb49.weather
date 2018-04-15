using System;
using System.Threading;
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
             ActionUpdateWeatherData,
             ActionWidgetsUpdate
         })]
    public class WidgetService : IntentService
    {
        private const string ServiceName = ".WidgetService.";
        public const string ActionUpdateWeatherData = AppSettings.AppPackageName + ServiceName + "ActionUpdateWeatherData";
        public const string ActionWidgetsUpdate = AppSettings.AppPackageName + ServiceName + "ActionWidgetsUpdate";
        public const string ExtraAppWidgetIds = AppSettings.AppPackageName + ".extra_appwidgetids";
        public const string ExtraPendingIntent = AppSettings.AppPackageName + ".extra_pendingintent";
        private readonly ILog _log = LogManager.GetLogger<WidgetService>();

        protected override void OnHandleIntent(Intent intent)
        {
            var action = intent?.Action;
            if (string.IsNullOrEmpty(action))
                return;

            var widgetsIds = GetAppWidgetIds(intent);

            try
            {
                switch (action)
                {
                    case ActionUpdateWeatherData:
                        OnWeatherDataUpdate(widgetsIds, intent.GetParcelableExtra(ExtraPendingIntent) as PendingIntent);
                        break;
                    case ActionWidgetsUpdate:
                        OnAppWidgetUpdate(widgetsIds);
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

        private void OnWeatherDataUpdate(int[] widgetsIds, PendingIntent pendingIntent)
        {
            if (widgetsIds == null || widgetsIds.Length == 0)
                return;

            try
            {
                var api = new ServiceApi();
                api.UpdateWeatherData(this, widgetsIds, CancellationToken.None);
                api.AppWidgetUpdate(this, widgetsIds);
            }
            finally
            {
                pendingIntent?.Send(this, Result.Ok, null);
            }
        }

        private void OnAppWidgetUpdate(int[] widgetsIds)
        {
            var api = new ServiceApi();
            api.AppWidgetUpdate(this, widgetsIds);
        }

        private int[] GetAppWidgetIds(Intent intent)
        {
            var widgetsIds = intent.GetIntArrayExtra(ExtraAppWidgetIds);
            return widgetsIds;
        }
    }
}