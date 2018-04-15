using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Support.V7.App;
using Sb49.Weather.Droid.Common;
using Sb49.Weather.Droid.Service;

namespace Sb49.Weather.Droid.Ui.Activities
{
    //[Activity(Theme = "@style/Theme.Custom.Splash", MainLauncher = true, NoHistory = true, LaunchMode = LaunchMode.SingleTask)]
    [Activity(Theme = "@style/Theme.Custom.Splash", MainLauncher = true, NoHistory = true)]
    public class SplashActivity : AppCompatActivity
    {
        protected override void OnResume()
        {
            base.OnResume();

            var startupWork = new Task(() =>
            {
                //Task.Delay(5000); // 5sec
                var api = new ServiceApi();
                api.ArrangeAppWidgetSettings(Application.Context);

                if (AppSettings.Default.CurrentLocation == null &&
                  AppSettings.Default.CheckIsGooglePlayServicesInstalled())
                {
                    var count = 0;
                    while (AppSettings.Default.CurrentLocation == null && count < 5)
                    {
                        Task.Delay(990).Wait();
                        count++;
                    }
                }
            });

            startupWork.ContinueWith(t =>
            {
                var activityIntent = new Intent(Application.Context, typeof(MainActivity));

                //var flags = ActivityFlags.NewTask;
                //if (Intent?.Action == AppWidgetManager.ActionAppwidgetConfigure)
                //{
                //    activityIntent.SetAction(AppWidgetManager.ActionAppwidgetConfigure);
                //    var widgetId = Intent.GetIntExtra(AppWidgetManager.ExtraAppwidgetId,
                //        AppWidgetManager.InvalidAppwidgetId);
                //    activityIntent.PutExtra(AppWidgetManager.ExtraAppwidgetId, widgetId);
                //    flags |= ActivityFlags.ClearTask;
                //}
                //else
                //{
                //    //activityIntent.SetFlags(ActivityFlags.ClearTop);
                //    //activityIntent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTop);
                //    //activityIntent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTask | ActivityFlags.ClearTop); //не работает SplashActivity
                //    //activityIntent.SetFlags(ActivityFlags.SingleTop);
                //    //activityIntent.AddCategory(Intent.CategoryLauncher);
                //    //activityIntent.SetFlags(ActivityFlags.ClearWhenTaskReset); //не работает SplashActivity

                //    flags |= ActivityFlags.ClearTop;
                //}
                //activityIntent.SetFlags(flags);

                activityIntent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTop);
                StartActivity(activityIntent);
                Finish();
            }, TaskScheduler.FromCurrentSynchronizationContext());

            startupWork.Start();
        }
    }
}