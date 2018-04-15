using Android.App;
using Android.Appwidget;
using Android.Content;
using Sb49.Weather.Droid.Ui.AppWidget.Providers.Core;

namespace Sb49.Weather.Droid.Ui.AppWidget.Providers
{
    [BroadcastReceiver(Label = "@string/WeatherClockAppWidgetName")]
    [IntentFilter(new[] {AppWidgetManager.ActionAppwidgetUpdate})]
    [MetaData(AppWidgetManager.MetaDataAppwidgetProvider, Resource = "@xml/widget_weather_clock_info42")]
    public class WeatherClockAppWidgetProvider : AppWidgetProviderBase
    {
    }
}