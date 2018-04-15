using Android.App;
using Android.Content;
using Sb49.Common.Logging;
using Sb49.Weather.Droid.Common;

namespace Sb49.Weather.Droid.Service.Reciver
{
    [BroadcastReceiver(Name = AppSettings.AppPackageName + ".BootCompletedReceiver")]
    [IntentFilter(new[] {Intent.ActionBootCompleted, "android.intent.action.QUICKBOOT_POWERON"})]
    public class BootCompletedReceiver : BroadcastReceiver
    {
        private readonly ILog _log = LogManager.GetLogger<BootCompletedReceiver>();

        public override void OnReceive(Context context, Intent intent)
        {
            AppSettings.Default.IsBootCompletedReceiver = true;
            _log?.Debug("OnReceive.");
        }
    }
}