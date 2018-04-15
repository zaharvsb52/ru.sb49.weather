using System;
using System.Threading.Tasks;
using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.OS;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Sb49.Common.Logging;
using Sb49.Weather.Droid.Common;
using Sb49.Weather.Droid.Service;
using Sb49.Weather.Droid.Ui.AppWidget.Providers.Core;
using Sb49.Weather.Droid.Ui.Fragments;
using Sb49.Weather.Exceptions;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Sb49.Weather.Droid.Ui.Activities
{
    [Activity(Name = AppSettings.AppPackageName + ".Ui.Activities.SettingsActivity")]
    [IntentFilter(new[] { AppWidgetManager.ActionAppwidgetConfigure })]
    public class SettingsActivity : AppCompatActivity
    {
        public const string ExtraToolbarTitle = AppSettings.AppPackageName + ".extra_toolbartitle";
        public const string ExtraToolbarTitleId = AppSettings.AppPackageName + ".extra_toolbartitleid";
        public const string ExtraWidget = AppSettings.AppPackageName + ".extra_widget";

        private const int RequestCodeAppWidgetWeatherDataUpdateTask = 1;

        private bool _isActionAppwidGetConfigure;
        private AppWidgetSettings _settings;
        private int? _widgetId;
        private bool _isWaiting;
        private static readonly ILog Log = LogManager.GetLogger<SettingsActivity>();

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            try
            {
                SetContentView(Resource.Layout.activity_settings);

                var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
                SetSupportActionBar(toolbar);

                var extraTitleId = Intent.GetIntExtra(ExtraToolbarTitleId, -1);
                var extraTitle = Intent.GetStringExtra(ExtraToolbarTitle);

                if (extraTitleId > 0)
                {
                    SupportActionBar.SetTitle(extraTitleId);
                }
                else if (!string.IsNullOrEmpty(extraTitle))
                {
                    SupportActionBar.Title = extraTitle;
                }
                else
                {
                    SupportActionBar.SetTitle(Resource.String.PreferencesTitle);
                }
                
                var widgetId = Intent.GetIntExtra(AppWidgetManager.ExtraAppwidgetId, AppWidgetManager.InvalidAppwidgetId);
                var validWidgetId = widgetId != AppWidgetManager.InvalidAppwidgetId;
                _isActionAppwidGetConfigure = Intent.Action == AppWidgetManager.ActionAppwidgetConfigure;
                if (_isActionAppwidGetConfigure)
                {
                    if (!validWidgetId)
                    {
                        Finish();
                        return;
                    }

                    Intent.PutExtra(SettingsFragment.ExtraPreferenceXmlId, Resource.Xml.preferences_appwidget);

                    AppSettings.Default.DeleteAppWidgetSettings(new[] {widgetId});

                    _settings = AppSettings.Default.CreateAppWidgetSettings(widgetId);
                    AppSettings.Default.SaveAppWidgetSettings(_settings);

                    var intent = new Intent();
                    intent.PutExtra(AppWidgetManager.ExtraAppwidgetId, _settings.WidgetId);
                    SetResult(Result.Canceled, intent);
                }
                else
                {
                    if (Intent.GetBooleanExtra(ExtraWidget, false) && validWidgetId)
                        _widgetId = widgetId;
                }

                if (savedInstanceState == null)
                {
                    var fragment = new SettingsFragment();
                    using (var transaction = SupportFragmentManager.BeginTransaction())
                    {
                        transaction.Replace(Resource.Id.settingsContent, fragment);
                        transaction.Commit();
                    }
                }

                var usehomemenu = !_isActionAppwidGetConfigure;
                SupportActionBar.SetDisplayHomeAsUpEnabled(usehomemenu);
                SupportActionBar.SetHomeButtonEnabled(usehomemenu);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            switch (requestCode)
            {
                case RequestCodeAppWidgetWeatherDataUpdateTask:
                    if (resultCode == Result.Ok)
                    {
                        AppSettings.Default.ChangeStatus |= SettingsChangeStatus.NeedRequestCounterUpdate;
                        if (AppSettings.Default.ChangeStatus.HasFlag(SettingsChangeStatus.NeedAppWidgetUpdate))
                            AppSettings.Default.ChangeStatus ^= SettingsChangeStatus.NeedAppWidgetUpdate;
                        if (AppSettings.Default.ChangeStatus.HasFlag(
                            SettingsChangeStatus.NeedAppWidgetWeatherDataUpdate))
                        {
                            AppSettings.Default.ChangeStatus ^= SettingsChangeStatus.NeedAppWidgetWeatherDataUpdate;
                        }
                    }
                    ShowProgressbar(false);
                    return;
            }
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            if (_settings != null || _widgetId.HasValue)
                MenuInflater.Inflate(Resource.Menu.menu_settings_activity, menu);

            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnPrepareOptionsMenu(IMenu menu)
        {
            var item = menu?.FindItem(Resource.Id.menuItemCreateAppWidget);
            item?.SetVisible(_settings != null && _isActionAppwidGetConfigure);

            item = menu?.FindItem(Resource.Id.menuItemAppWidgetRefresh);
            item?.SetVisible(_widgetId.HasValue && !_isActionAppwidGetConfigure);
            item?.SetEnabled(!_isWaiting);

            return base.OnPrepareOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            try
            {
                switch (item.ItemId)
                {
                    case Android.Resource.Id.Home:
                        Finish();
                        return true;
                    case Resource.Id.menuItemCreateAppWidget:
                        AppSettings.Default.ChangeStatus = SettingsChangeStatus.None;

                        if (_settings == null)
                            throw new ArgumentNullException(nameof(_settings));

                        if(!_settings.ValidateLocationSettings(this, 1))
                            return false;

                        _settings.Weather = null;
                        AppSettings.Default.SaveAppWidgetSettings(_settings);

                        //var policy = new StrictMode.ThreadPolicy.Builder().PermitAll().Build();
                        //StrictMode.SetThreadPolicy(policy);

                        using (var appWidgetManager = AppWidgetManager.GetInstance(this))
                        {
                            using (var providerInfo = appWidgetManager.GetAppWidgetInfo(_settings.WidgetId))
                            {
                                if (providerInfo != null)
                                {
                                    var widgetIntent = new Intent(AppWidgetProviderBase.ActionWidgetCreate);
                                    widgetIntent.SetComponent(providerInfo.Provider);
                                    widgetIntent.PutExtra(AppWidgetManager.ExtraAppwidgetId, _settings.WidgetId);
                                    SendBroadcast(widgetIntent);
                                }
                            }
                        }

                        var intent = new Intent();
                        intent.PutExtra(AppWidgetManager.ExtraAppwidgetId, _settings.WidgetId);
                        SetResult(Result.Ok, intent);

                        Finish();
                        return true;
                    case Resource.Id.menuItemAppWidgetRefresh:
                        if (_widgetId.HasValue && !_isWaiting)
                        {
                            var widgetId = _widgetId.Value;
                            ShowProgressbar(true);

                            var pendingIntent = CreatePendingResult(RequestCodeAppWidgetWeatherDataUpdateTask, new Intent(), 0);
                            var serviceApi = new ServiceApi();
                            serviceApi.AppWidgetWeatherDataUpdateService(this, new[] {widgetId}, pendingIntent);
                        }
                        return true;
                    default:
                        return base.OnOptionsItemSelected(item);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);

                ShowProgressbar(false);

                var message = ex is WeatherExceptionBase
                    ? ex.Message
                    : GetString(Resource.String.InternalError);
                Toast.MakeText(this, message, AppSettings.ToastLength).Show();
            }

            return false;
        }

        protected override void OnPause()
        {
            base.OnPause();

            _isWaiting = false;
        }

        protected override void OnDestroy()
        {
           _settings = null;
            _widgetId = null;
            //AppSettings.GcCollect(log: _log); //HARDCODE:

            base.OnDestroy();
        }

        private void ShowProgressbar(bool visible)
        {
            if (_isWaiting == visible)
                return;

            _isWaiting = visible;
            Task.Run(() =>
            {
                RunOnUiThread(InvalidateOptionsMenu);
            });
        }
    }
}