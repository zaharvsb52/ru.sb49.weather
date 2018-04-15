using Android.Content;
using Android.Widget;

namespace Sb49.Weather.Droid.Ui.AppWidget.Updaters.Core
{
    public interface IAppWidgetUpdater
    {
        bool HasClock { get; }

        int GetHeightDp(bool landOrientation);
        int GetWidthDp(bool landOrientation);
        RemoteViews BuildRemoteViews(Context context, int widgetId, bool demomode = false);
    }
}