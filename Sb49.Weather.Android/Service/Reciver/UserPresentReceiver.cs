using Android.App;
using Android.Content;
using Sb49.Common.Logging;
using Sb49.Weather.Droid.Common;

namespace Sb49.Weather.Droid.Service.Reciver
{
    [BroadcastReceiver(Name = AppSettings.AppPackageName + ".UserPresentReceiver")]
    [IntentFilter(new[] {Intent.ActionUserPresent})]
    public class UserPresentReceiver : BroadcastReceiver
    {
        private readonly ILog _log = LogManager.GetLogger<UserPresentReceiver>();

        public override void OnReceive(Context context, Intent intent)
        {
            try
            {
                _log?.Debug("OnReceive.");

                var api = new ServiceApi();
                api.ArrangeAppWidgetSettings(context);
                api.AppWidgetUpdateService(context, null);
            }
            catch (System.Exception ex)
            {
                _log?.Error(ex);
            }
        }
    }
}