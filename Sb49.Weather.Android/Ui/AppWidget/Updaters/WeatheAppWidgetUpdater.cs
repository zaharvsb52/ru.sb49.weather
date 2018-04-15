using System;
using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Text;
using Android.Text.Style;
using Android.Views;
using Android.Widget;
using Sb49.Common;
using Sb49.Common.Droid;
using Sb49.Common.Logging;
using Sb49.Weather.Code;
using Sb49.Weather.Droid.Common;
using Sb49.Weather.Droid.Ui.Activities;
using Sb49.Weather.Droid.Ui.AppWidget.Updaters.Core;
using Sb49.Weather.Model;

namespace Sb49.Weather.Droid.Ui.AppWidget.Updaters
{
    public class WeatherAppWidgetUpdater : IAppWidgetUpdater
    {
        protected readonly ILog Log;

        public WeatherAppWidgetUpdater()
        {
            Log = LogManager.GetLogger(GetType());
        }

        public virtual bool HasClock => false;

        public virtual int GetHeightDp(bool landOrientation)
        {
            return landOrientation ? 68 : 94;
        }

        public virtual int GetWidthDp(bool landOrientation)
        {
            return landOrientation ? 420 : 338;
        }

        protected virtual RemoteViews CreateRemoteViews(Context context)
        {
            return new RemoteViews(context.PackageName, Resource.Layout.widget_weather);
        }

        public RemoteViews BuildRemoteViews(Context context, int widgetId, bool isDemoMode = false)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var settings = AppSettings.Default.FindAppWidgetSettings(widgetId);
            if (settings == null)
            {
                var message = context.GetString(Resource.String.UndefinedAppWidgetSettings);
                Log.Debug(message);

                if (isDemoMode)
                    throw new Exception(context.GetString(Resource.String.UndefinedAppWidgetSettings));
                return null;
            }

            if (settings.Weather == null && !isDemoMode)
                return null;

            var widgetView = CreateRemoteViews(context);

            var bgstyle = settings.WidgetBackgroundStyle;
            if (!string.IsNullOrEmpty(bgstyle) && !string.Equals(bgstyle, context.GetString(Resource.String.NoData),
                    StringComparison.OrdinalIgnoreCase))
            {
                var resid = context.Resources.GetIdentifier(bgstyle, ResourceType.Drawable,
                    context.PackageName);
                if (resid > 0)
                {
                    //https://oceanuz.wordpress.com/2013/07/11/widget-remoteviews-round-corners-background-and-colors/
                    //http://stackoverflow.com/questions/10307015/rounded-corners-on-dynamicly-set-imageview-in-widget

                    widgetView.SetInt(Resource.Id.imgWgBackground, "setImageResource", resid);
                    var max = context.Resources.GetInteger(Resource.Integer.AppWidgetOpacityMax);
                    widgetView.SetInt(Resource.Id.imgWgBackground, "setAlpha", max - settings.WidgetBackgroundOpacity);
                }
            }

            OnUpdateCustomPart(context, settings, widgetView);

            UpdateWeatherPart(context, widgetView, settings, isDemoMode);

            //Click pending
            if (!isDemoMode)
                SetOnClickPendingIntent(context, widgetView, widgetId);

            return widgetView;
        }

        protected virtual void UpdateWeatherPart(Context context, RemoteViews view, AppWidgetSettings settings, bool isDemoMode)
        {
            var weather = settings.Weather;
            var utcNow = DateTime.UtcNow;

            if (isDemoMode && weather == null)
            {
                var weatherDataPoint = new WeatherDataPoint
                {
                    Date = utcNow,
                    Temperature = 0,
                    MinTemperature = -1,
                    MaxTemperature = 1,
                    WeatherCode = WeatherCodes.ClearSky,
                    Condition = context.GetString(Resource.String.DemoCondition)
                };

                weather = new WeatherForecast(providerId: settings.WeatherProviderId, latitude: null, longitude: null,
                    units: AppSettings.Default.Units, languageCode: AppSettings.Default.Language, link: null,
                    hasIcons: false)
                {
                    Currently = weatherDataPoint,
                    Daily = new[] {weatherDataPoint}
                };
            }

            if(weather?.MaxPublishedDate == null)
                return;

            var actualUtcDate = utcNow > weather.MaxPublishedDate ? weather.MaxPublishedDate.Value : utcNow;
            var currently = weather.FindActualCurrentlyAsync(actualUtcDate).Result;
            if (currently == null)
                return;

            var temperature = string.Empty;
            using (var weatherTools = new WeatherTools
            {
                ProviderUnits = weather.Units,
            })
            {
                var degree = weatherTools.DegreeString;
                if (currently.Temperature.HasValue)
                {
                    temperature = weatherTools.ConvertTemperatureToString(currently.Temperature.Value, "{0:f0}{1}",
                        degree);
                }
                SetTextFormatted(view, Resource.Id.txtTemp, temperature,
                    weatherTools.IsTemperatureAlerted(currently.Temperature));

                var minTempText = string.Empty;
                var maxTempText = string.Empty;
                var visibility = ViewStates.Gone;
                var minTemp = weatherTools
                    .CalculateMinTemperatureAsync(actualUtcDate, weather, currently.MinTemperature).Result;
                var maxTemp = weatherTools
                    .CalculateMaxTemperatureAsync(actualUtcDate, weather, currently.MaxTemperature).Result;
                if (minTemp.HasValue && maxTemp.HasValue)
                {
                    minTempText = weatherTools.ConvertTemperatureToString(minTemp.Value, "{0:f0}{1}", degree);
                    maxTempText = weatherTools.ConvertTemperatureToString(maxTemp.Value, "{0:f0}{1}", degree);
                    visibility = ViewStates.Visible;
                }
                SetTextFormatted(view, Resource.Id.txtLow, minTempText, weatherTools.IsTemperatureAlerted(minTemp));
                view.SetViewVisibility(Resource.Id.txtLowIndicator, visibility);
                SetTextFormatted(view, Resource.Id.txtHigh, maxTempText, weatherTools.IsTemperatureAlerted(maxTemp));
                view.SetViewVisibility(Resource.Id.txtHighIndicator, visibility);

                var conditionIconId = weatherTools.GetConditionIconId(WidgetTypes.AppWidget, settings.WidgetIconStyle,
                    currently, true, true);
                view.SetImageViewResource(Resource.Id.imgCondition, conditionIconId);

                //"Небольшой снегопад "
                SetTextFormatted(view, Resource.Id.txtCondition,
                    (currently.Condition ?? string.Empty).Trim().ToCapital(),
                    weatherTools.IsConditionExtreme(currently.WeatherCode));
            }

            var locality = (settings.LocationAddress?.GetDisplayLocality() ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(locality) && isDemoMode)
            {
                locality = context.GetString(settings.UseTrackCurrentLocation
                    ? Resource.String.CurrentLocationEmpty
                    : Resource.String.DemoLocality);
            }
            view.SetTextViewText(Resource.Id.txtLocation, locality);
        }

        protected void SetOnClickPendingIntent(Context context, RemoteViews widgetView, int widgetId)
        {

            var activityIntent = new Intent(context, typeof(MainActivity));
            activityIntent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTask);

            //var activityIntent = new Intent(context, typeof(SplashActivity));

            activityIntent.SetAction(AppWidgetManager.ActionAppwidgetConfigure);
            activityIntent.PutExtra(AppWidgetManager.ExtraAppwidgetId, widgetId);

            var pIntent = PendingIntent.GetActivity(context, widgetId, activityIntent, 0);
            //var pIntent = PendingIntent.GetActivity(context, widgetId, configIntent, PendingIntentFlags.UpdateCurrent);
            //var pIntent = PendingIntent.GetActivity(context, widgetId, configIntent, PendingIntentFlags.CancelCurrent);
            widgetView.SetOnClickPendingIntent(Resource.Id.viewWidget, pIntent);
        }

        protected void SetTextFormatted(RemoteViews view, int resourceId, string text, bool isAlerted)
        {
            if (!string.IsNullOrEmpty(text) && isAlerted)
            {
                var spannable = new SpannableString(text);
                spannable.SetSpan(new UnderlineSpan(), 0, text.Length, SpanTypes.ExclusiveExclusive);
                view.SetCharSequence(resourceId, "setText", spannable);
                return;
            }

            view.SetTextViewText(resourceId, text ?? string.Empty);
        }

        protected virtual void OnUpdateCustomPart(Context context, AppWidgetSettings settings, RemoteViews view)
        {
        }
    }
}