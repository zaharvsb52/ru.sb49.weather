using Android.App;
using Android.Appwidget;
using Android.Content;
using Sb49.Weather.Droid.Ui.AppWidget.Providers.Core;

namespace Sb49.Weather.Droid.Ui.AppWidget.Providers
{
    [BroadcastReceiver(Label = "@string/WeatherAppWidgetName")]
    [IntentFilter(new[] {AppWidgetManager.ActionAppwidgetUpdate})]
    [MetaData(AppWidgetManager.MetaDataAppwidgetProvider, Resource = "@xml/widget_weather_info41")]
    public class WeatherAppWidgetProvider : AppWidgetProviderBase
    {
    }
}