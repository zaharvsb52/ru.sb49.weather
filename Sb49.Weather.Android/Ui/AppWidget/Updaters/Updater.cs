using System;
using System.Collections.Generic;
using Android.Appwidget;
using Android.Content;
using Android.Widget;
using Sb49.Weather.Droid.Common;
using Sb49.Weather.Droid.Ui.AppWidget.Updaters.Core;

namespace Sb49.Weather.Droid.Ui.AppWidget.Updaters
{
    public class Updater
    {
        public int[] GetAppWidgetsIds(Context context, string[] appWidgetProviderClassNames)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (appWidgetProviderClassNames == null)
                throw new ArgumentNullException(nameof(appWidgetProviderClassNames));

            var result = new List<int>();
            using (var appWidgetManager = AppWidgetManager.GetInstance(context))
            {
                foreach (var className in appWidgetProviderClassNames)
                {
                    using (var provider = new ComponentName(context, className))
                    {
                        var appWidgetIds = appWidgetManager.GetAppWidgetIds(provider);
                        if (appWidgetIds != null && appWidgetIds.Length > 0)
                            result.AddRange(appWidgetIds);
                    }
                }
            }

            return result.Count == 0 ? null : result.ToArray();
        }

        public void UpdateAppWidget(Context context, string[] appWidgetProviderClassNames)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var appWidgetIds = GetAppWidgetsIds(context, appWidgetProviderClassNames);
            UpdateAppWidget(context, appWidgetIds);
        }

        public void UpdateAppWidget(Context context, int[] appWidgetIds)
        {
            if (appWidgetIds == null || appWidgetIds.Length == 0)
                return;

            if (context == null)
                throw new ArgumentNullException(nameof(context));

            using (var appWidgetManager = AppWidgetManager.GetInstance(context))
            {
                foreach (var appWidgetId in appWidgetIds)
                {
                    using (var providerInfo = appWidgetManager.GetAppWidgetInfo(appWidgetId))
                    {
                        if (providerInfo == null)
                            continue;

                        var widgetView = BuildRemoteViewsFactory(context, providerInfo.Provider.ClassName, appWidgetId);
                        if (widgetView != null)
                            appWidgetManager.UpdateAppWidget(appWidgetId, widgetView);
                    }
                }
            }
        }

        public IAppWidgetUpdater BuldUpdater(Context context, int appWidgetId)
        {
            using(var appWidgetManager = AppWidgetManager.GetInstance(context))
            {
                using (var providerInfo = appWidgetManager.GetAppWidgetInfo(appWidgetId))
                {
                    return AppSettings.Default.AppWidgetUpdaterFactory(providerInfo.Provider.ClassName);
                }
            }
        }

        public void Delete(Context context, int[] appWidgetIds)
        {
            if (appWidgetIds == null || appWidgetIds.Length == 0)
                return;

            using (var host = new AppWidgetHost(context, 1))
            {
                foreach (var widgetId in appWidgetIds)
                {
                    host.DeleteAppWidgetId(widgetId);
                }
            }
        }

        private RemoteViews BuildRemoteViewsFactory(Context context, string appWidgetProviderClassName, int appWidgetId)
        {
            var updater = AppSettings.Default.AppWidgetUpdaterFactory(appWidgetProviderClassName);
            return updater?.BuildRemoteViews(context, appWidgetId);
        }
    }
}