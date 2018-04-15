using System;
using Android.Appwidget;
using Android.Content;
using Sb49.Common.Logging;
using Sb49.Weather.Droid.Common;
using Sb49.Weather.Droid.Service;

namespace Sb49.Weather.Droid.Ui.AppWidget.Providers.Core
{
    //http://startandroid.ru/ru/uroki/vse-uroki-spiskom.html
    //http://startandroid.ru/ru/uroki/vse-uroki-spiskom/195-urok-117-vidzhety-sozdanie-lifecycle.html
    //виджет занимает одну или несколько из этих ячеек по ширине и высоте. Чтобы конвертнуть ячейки в dp, используется формула 70 * n – 30, где n – это количество ячеек

    public abstract class AppWidgetProviderBase : AppWidgetProvider
    {
        public const string ActionWidgetCreate = AppSettings.AppPackageName + ".ActionWidgetCreate";
        protected readonly ILog Log;

        protected AppWidgetProviderBase()
        {
            Log = LogManager.GetLogger(GetType());
        }

        public override void OnUpdate(Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds)
        {
            base.OnUpdate(context, appWidgetManager, appWidgetIds);
            Log.Debug("OnUpdate.");

            if (appWidgetIds == null)
                return;

            try
            {
                var api = new ServiceApi();
                api.AppWidgetUpdateService(context, appWidgetIds);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        public override void OnReceive(Context context, Intent intent)
        {
            base.OnReceive(context, intent);

            var action = intent?.Action;
            Log.DebugFormat("OnReceive. Action: '{0}'.", action);

            try
            {
                switch (action)
                {
                    case ActionWidgetCreate:
                        var widgetId = intent.GetIntExtra(AppWidgetManager.ExtraAppwidgetId,
                            AppWidgetManager.InvalidAppwidgetId);
                        if (widgetId == AppWidgetManager.InvalidAppwidgetId)
                            return;

                        var api = new ServiceApi();
                        api.StartService(context, ControlService.ActionGeoTrackingServiceStart);
                        api.AppWidgetWeatherDataUpdateService(context, new[] { widgetId });
                        api.StartService(context, ControlService.ActionWeatherServiceStart);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Action: '{0}'. {1}", action, ex);
            }
        }

        public override void OnDeleted(Context context, int[] appWidgetIds)
        {
            try
            {
                base.OnDeleted(context, appWidgetIds);

                AppSettings.Default.DeleteAppWidgetSettings(appWidgetIds);
                var api = new ServiceApi();
                api.StartService(context, ControlService.ActionAppServiceStop);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }
    }
}