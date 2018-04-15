using System;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Graphics.Drawables;
using Android.Locations;
using Android.OS;
using Android.Support.V4.Widget;
using Android.Support.V7.App;
using Android.Text;
using Android.Views;
using Android.Widget;
using Sb49.Common;
using Sb49.Common.Droid.Listeners;
using Sb49.Common.Droid.Ui;
using Sb49.Common.Logging;
using Sb49.Weather.Droid.Common;
using Sb49.Weather.Droid.Model;
using Sb49.Weather.Droid.Service;
using Sb49.Weather.Droid.Service.Binders;
using Sb49.Weather.Droid.Ui.Adapters;
using Sb49.Weather.Droid.Ui.Fragments;
using Sb49.Weather.Exceptions;
using AndroidUtil = Sb49.Common.Droid.Util;

namespace Sb49.Weather.Droid.Ui.Activities
{
    [Activity(HardwareAccelerated = true)]
    public class MainActivity : AppCompatActivity
    {
        private const string ExtraWidgetId = AppSettings.AppPackageName + ".MainActivity.ExtraWidgetId";
        private const int RequestCodeSettings = 1;

        private View _menu;
        private DrawerLayout _drawerLayout;
        private MenuFragment _menuFragment;
        private WeatherContentFragment _contentFragment;
        private TextView _txtLocation;
        private ProgressBar _progressBar;
        private bool _isWaiting;
        private bool _isBound;
        private ServiceBinder _binder;
        private ServiceConnection _serviceConnection;
        private ServiceReceiver _serviceReceiver;
        private ImageView _imgAlert;
        private TooltipWindow _tooltipWindow;
        private int? _selectedWidgetId;
        private int? _widgetId;
        private int? _weatherProviderNameId;
        private bool _needRefresh;
        private bool _needWeatherUpdate;
        private CancellationTokenSource _cancellationTokenSource;
        private static readonly ILog Log = LogManager.GetLogger<MainActivity>();

        protected override void OnCreate(Bundle bundle)
        {
            //SetTheme(Resource.Style.Theme_Custom);
            base.OnCreate(bundle);

//#if DEBUG
//            var vmPolicy = new StrictMode.VmPolicy.Builder();
//            StrictMode.SetVmPolicy(vmPolicy.DetectActivityLeaks().PenaltyLog().Build());
//#endif

            try
            {
                _needWeatherUpdate = true;

                if (bundle == null)
                {
                    Log.Debug("OnCreate");
                }

                //SetTitle(Resource.String.WeatherTitle);
                SetContentView(Resource.Layout.activity_main);

                _menu = FindViewById(Resource.Id.tlbMenu);
                _drawerLayout = FindViewById<DrawerLayout>(Resource.Id.viewDrawerLayout);
                _progressBar = FindViewById<ProgressBar>(Resource.Id.spLoading);

                _txtLocation = FindViewById<TextView>(Resource.Id.txtLocation);
                _txtLocation?.SetOnClickListener(new ClickListener(view =>
                {
                    if (_tooltipWindow != null && !_tooltipWindow.IsDisposed && !_tooltipWindow.IsTooltipShown &&
                        (_tooltipWindow.TooltipText?.Length > 0 || _tooltipWindow.TooltipTextFormatted?.Length() > 0))
                    {
                        _tooltipWindow.ShowToolTip(view);
                    }
                }));
                CheckActionAppwidGetConfigure(Intent);

                _imgAlert = FindViewById<ImageView>(Resource.Id.imgAlert);
                SetAlertInvisible();

                _menuFragment = CreateMenuFragment();
                _contentFragment = CreateContentFragment();
                using (var ft = FragmentManager.BeginTransaction())
                {
                    ft.Replace(Resource.Id.viewMenu, _menuFragment);
                    ft.Replace(Resource.Id.viewContent, _contentFragment);
                    ft.Commit();
                }

                _cancellationTokenSource = new CancellationTokenSource();
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        private MenuFragment CreateMenuFragment()
        {
            var fragment = new MenuFragment
            {
                OptionMenuClickHandler = OnOptionMenuClick,
            };

            //var args = new Bundle();
            //args.PutBoolean(MenuFragment.ExistsAppWidgetArgName, _widgetId.HasValue);
            //fragment.Arguments = args;
            return fragment;
        }

        private WeatherContentFragment CreateContentFragment()
        {
            var fragment = new WeatherContentFragment();
            return fragment;
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            switch (requestCode)
            {
                case RequestCodeSettings:
                    if (AppSettings.Default.ChangeStatus.HasFlag(SettingsChangeStatus.Changed))
                    {
                        AppSettings.Default.ChangeStatus ^= SettingsChangeStatus.Changed;
                        _needRefresh = true;
                        return;
                    }
                    if (AppSettings.Default.ChangeStatus.HasFlag(SettingsChangeStatus.NeedRequestCounterUpdate))
                    {
                        AppSettings.Default.ChangeStatus ^= SettingsChangeStatus.NeedRequestCounterUpdate;
                        _needWeatherUpdate = true;
                    }
                    return;
                case MenuFragment.MenuAddPlaceId:
                    if (resultCode == Result.Ok &&
                        data?.GetBooleanExtra(LocationActivity.ExtraLocationChanged, false) == true)
                    {
                        _needRefresh = true;
                        _widgetId = null;
                    }
                    return;
            }
        }

        private AppWidgetSettings GetAppWidgetSettings(int widgetId)
        {
            if (widgetId == AppWidgetManager.InvalidAppwidgetId)
                return null;

            var settings = AppSettings.Default.FindAppWidgetSettings(widgetId);
            return settings;
        }

        private void OnUpdateSettings()
        {
            if (_widgetId.HasValue)
            {
                var settings = GetAppWidgetSettings(_widgetId.Value);
                if (settings != null)
                {
                    if (AppSettings.Default.WeatherProviderId != settings.WeatherProviderId)
                        AppSettings.Default.WeatherProviderId = settings.WeatherProviderId;

                    if (AppSettings.Default.UseTrackCurrentLocation != settings.UseTrackCurrentLocation)
                        AppSettings.Default.UseTrackCurrentLocation = settings.UseTrackCurrentLocation;

                    AppSettings.Default.LocationAddress = settings.LocationAddress;

                    AppSettings.Default.Weather = settings.Weather;
                }
            }

            _weatherProviderNameId = AppSettings.Default.GetWeatherProviderNameById(AppSettings.Default.WeatherProviderId);
        }

        private void CheckActionAppwidGetConfigure(Intent intent)
        {
            _selectedWidgetId = null;
            _widgetId = null;

            var action = intent?.Action;
            if (action == AppWidgetManager.ActionAppwidgetConfigure)
            {
                _selectedWidgetId = intent.GetIntExtra(AppWidgetManager.ExtraAppwidgetId,
                    AppWidgetManager.InvalidAppwidgetId);
                if (_selectedWidgetId == AppWidgetManager.InvalidAppwidgetId)
                    _selectedWidgetId = null;
                if (_selectedWidgetId.HasValue)
                    _widgetId = _selectedWidgetId;
            }
        }

        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);
            
            try
            {
                _needWeatherUpdate = true;
                CheckActionAppwidGetConfigure(intent);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);

            outState.PutInt(ExtraWidgetId, _widgetId ?? AppWidgetManager.InvalidAppwidgetId);
        }

        protected override void OnRestoreInstanceState(Bundle savedInstanceState)
        {
            base.OnRestoreInstanceState(savedInstanceState);

            try
            {
                _widgetId = savedInstanceState.GetInt(ExtraWidgetId, AppWidgetManager.InvalidAppwidgetId);
                if (_widgetId == AppWidgetManager.InvalidAppwidgetId)
                    _widgetId = null;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        protected override void OnResume()
        {
            base.OnResume();

            try
            {
                // если изменился settings
                GeoTrackingServiceStartSafeAsync();

                OnUpdateSettings();
                UpdateLocationAsync();
                _menuFragment?.UpdateMenu();
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }

            SubscribeEvents();

            ValidateServiceReceiver();
            var intentFilter = new IntentFilter(WeatherBoundService.ActionGetWeatherData)
            {
                Priority = (int) IntentFilterPriority.HighPriority
            };
            RegisterReceiver(_serviceReceiver, intentFilter);

            _serviceConnection?.Dispose();
            _serviceConnection = new ServiceConnection(this);
            var serviceIntent = AndroidUtil.CreateExplicitFromImplicitIntent(this,
                new Intent(WeatherBoundService.ActionGetWeatherData));
            serviceIntent.SetAction(WeatherBoundService.ActionGetWeatherData);
            BindService(serviceIntent, _serviceConnection, Bind.AutoCreate);

            if (_needRefresh)
            {
                _needRefresh = false;
                OnRefresh(showProgressbar: true, isOnlyDaily: false, infoReason: "_needRefresh = true");
                return;
            }

            if (_needWeatherUpdate)
            {
                _needWeatherUpdate = false;
                UpdateWeatherContentFragmentAsync(_cancellationTokenSource?.Token);
            }
        }

        protected override void OnPause()
        {
            base.OnPause();

            try
            {
                _needWeatherUpdate = false;
                _cancellationTokenSource?.Cancel();

                UnsubscribeEvents();
                CloseDrawerLayout();
                
                if (_isWaiting)
                    ShowProgressbar(false);

                if (_isBound)
                {
                    //ApplicationContext.UnbindService(_serviceConnection);
                    UnbindService(_serviceConnection);
                    _isBound = false;
                }
                if (_serviceReceiver != null && _serviceReceiver.Handle != IntPtr.Zero)
                    UnregisterReceiver(_serviceReceiver);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        protected override void OnDestroy()
        {
            try
            {
                UnsubscribeEvents();

                if (_cancellationTokenSource != null)
                {
                    _cancellationTokenSource.Cancel();
                    _cancellationTokenSource.Dispose();
                    _cancellationTokenSource = null;
                }

                _selectedWidgetId = null;
                _widgetId = null;

                _serviceConnection?.Dispose();
                _serviceConnection = null;

                _serviceReceiver?.Dispose();
                _serviceReceiver = null;

                if (_menuFragment != null)
                    _menuFragment.OptionMenuClickHandler = null;
                _menuFragment?.Dispose();
                _menuFragment = null;

                _contentFragment?.Dispose();
                _contentFragment = null;

                TooltipWindowDispose();
            }
            catch (Exception ex)
            {
                Log.Debug(ex);
            }
            AppSettings.GcCollect(); //HARDCODE:

            base.OnDestroy();

            AppSettings.GcCollect(true, Log); //HARDCODE:
        }

        private void CreateServiceReceiver()
        {
            _serviceReceiver = new ServiceReceiver();
        }

        private void ValidateServiceReceiver()
        {
            if (_serviceReceiver == null)
            {
                CreateServiceReceiver();
                return;
            }
            if (_serviceReceiver.Handle == IntPtr.Zero)
            {
                _serviceReceiver.Dispose();
                CreateServiceReceiver();
            }
        }

        private void TooltipWindowDispose()
        {
            _tooltipWindow?.Dispose();
            _tooltipWindow = null;
        }

        private void OnHomeMenuClick(object sender, EventArgs e)
        {
            OnHomeMenuClick();
        }

        private void OnHomeMenuClick()
        {
            if (_menuFragment == null)
                return;
            if (!CloseDrawerLayout())
                _drawerLayout.OpenDrawer((int)GravityFlags.Left);
        }

        private bool CloseDrawerLayout()
        {
            if (_menuFragment == null)
                return false;

            var result = _drawerLayout.IsDrawerOpen((int)GravityFlags.Left);
            if (result)
                _drawerLayout.CloseDrawer((int) GravityFlags.Left);
            return result;
        }

        private void UpdateWeatherContentFragmentAsync(CancellationToken? cancellationToken)
        {
            if(_isWaiting)
                return;

            ShowProgressbar(true);
            var token = cancellationToken ?? CancellationToken.None;

            Task.Run(() =>
            {
                void IfCancellationRequested(CancellationToken cancelToken)
                {
                    cancelToken.ThrowIfCancellationRequested();
                }

                try
                {
                    var weather = AppSettings.Default.Weather;

                    if (weather == null)
                    {
                        IfCancellationRequested(token);
                        OnRefresh(showProgressbar: false, isOnlyDaily: false, infoReason: "weather is null");
                        return;
                    }
                    if (weather.UpdatedDate == null)
                    {
                        IfCancellationRequested(token);
                        OnRefresh(showProgressbar: false, isOnlyDaily: false,
                            infoReason: "weather.UpdatedDate is null");
                        return;
                    }
                    if (weather.MaxPublishedDate < DateTime.UtcNow)
                    {
                        IfCancellationRequested(token);
                        OnRefresh(showProgressbar: false, isOnlyDaily: false,
                            infoReason: "weather.UpdatedDate is outdated");
                        return;
                    }
                    if (weather.ProviderId != AppSettings.Default.WeatherProviderId)
                    {
                        IfCancellationRequested(token);
                        OnRefresh(showProgressbar: false, isOnlyDaily: false, infoReason: "weather.ProviderId changed");
                        return;
                    }
                    if (!string.IsNullOrEmpty(weather.LanguageCode) &&
                        weather.LanguageCode != AppSettings.Default.Language)
                    {
                        IfCancellationRequested(token);
                        OnRefresh(showProgressbar: false, isOnlyDaily: false,
                            infoReason: "weather.LanguageCode changed");
                        return;
                    }
                    if (string.IsNullOrEmpty(weather.AddressHashCode))
                    {
                        IfCancellationRequested(token);
                        OnRefresh(showProgressbar: false, isOnlyDaily: false,
                            infoReason: "weather.AddressHashCode is null");
                        return;
                    }

                    IfCancellationRequested(token);
                    var address = AppSettings.Default.LocationAddress;
                    if (!string.Equals(weather.AddressHashCode, address?.GetMd5Code()))
                    {
                        IfCancellationRequested(token);
                        OnRefresh(showProgressbar: false, isOnlyDaily: false,
                            infoReason: "weather.AddressHashCode changed");
                        return;
                    }
                    if (AppSettings.Default.UseTrackCurrentLocation)
                    {
                        IfCancellationRequested(token);
                        var currentLocation = AppSettings.Default.CurrentLocation;
                        if (address?.HasCoordinates == true && currentLocation?.HasCoordinates == true)
                        {
                            var distance = new float[1];
                            // ReSharper disable PossibleInvalidOperationException
                            Location.DistanceBetween(currentLocation.Latitude.Value, currentLocation.Longitude.Value,
                                address.Latitude.Value, address.Longitude.Value, distance);
                            // ReSharper restore PossibleInvalidOperationException
                            if (Math.Abs(distance[0]) >= AppSettings.LocationDeltaMeters)
                            {
                                OnRefresh(showProgressbar: false, isOnlyDaily: false,
                                    infoReason: "current location changed");
                                return;
                            }
                        }
                    }

                    IfCancellationRequested(token);
                    OnRefresh(showProgressbar: false, isOnlyDaily: true, infoReason: null);
                }
                catch (System.OperationCanceledException)
                {
                    ShowProgressbar(false);
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                    ShowProgressbar(false);
                    Toast.MakeText(this, GetString(Resource.String.InternalError),
                        AppSettings.ToastLength).Show();
                }
            }, token);
        }

        private async void OnOptionMenuClick(MenuItem e, MenuAdapter.MenuItemEventActions action, bool isNotCloseDrawerLayout)
        {
            try
            {
                if (e == null)
                    return;

                switch (e.MenuId)
                {
                    case MenuFragment.MenuRefreshId:
                        OnRefresh(showProgressbar: true, isOnlyDaily: false, infoReason: "menu refresh click");
                        return;
                    case MenuFragment.MenuSettingsId:
                        AppSettings.Default.ChangeStatus = SettingsChangeStatus.None;
                        var intent = new Intent(this, typeof(SettingsActivity));
                        intent.SetFlags(ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission);
                        if (_selectedWidgetId.HasValue)
                            intent.PutExtra(AppWidgetManager.ExtraAppwidgetId, _selectedWidgetId.Value);
                        StartActivityForResult(intent, RequestCodeSettings);
                        return;
                    case MenuFragment.MenuAboutId:
                        intent = new Intent(this, typeof(AboutActivity));
                        StartActivity(intent);
                        return;
                    default:
                        if (action == MenuAdapter.MenuItemEventActions.EditLocation)
                        {
                            _widgetId = null;
                            UpdateDisplayLocation(AppSettings.Default.LocationAddress);
                            return;
                        }

                        if (action == MenuAdapter.MenuItemEventActions.DeleteLocation)
                        {
                            _widgetId = null;
                            OnRefresh(showProgressbar: true, isOnlyDaily: false, infoReason: "delete location");
                            return;
                        }

                        if (e.MenuItemType == MenuItemTypes.WeatherProvider && e.KeyId.HasValue)
                        {
                            _widgetId = null;
                            if (!_isWaiting && AppSettings.Default.WeatherProviderId != e.KeyId)
                            {
                                var providerId = e.KeyId.Value;
                                if (AppSettings.Default.WeatherProviderId != providerId)
                                {
                                    AppSettings.Default.WeatherProviderId = providerId;
                                    _weatherProviderNameId = AppSettings.Default.GetWeatherProviderNameById(providerId);
                                    OnRefresh(showProgressbar: true, isOnlyDaily: false,
                                        infoReason: "selected weather provider changed");
                                }
                            }
                            return;
                        }

                        if (e.MenuItemType == MenuItemTypes.CurrentLocation && e.KeyId.HasValue)
                        {
                            _widgetId = null;
                            if (!_isWaiting && AppSettings.Default.UseTrackCurrentLocation != true)
                            {
                                AppSettings.Default.UseTrackCurrentLocation = true;
                                OnRefresh(showProgressbar: true, isOnlyDaily: false, infoReason: "selected location changed");
                            }
                            return;
                        }

                        if (e.MenuItemType == MenuItemTypes.Location && e.KeyId.HasValue)
                        {
                            _widgetId = null;
                            if (!_isWaiting)
                            {
                                var notNeedRefresh = await Task.Run(() =>
                                {
                                    bool? result = null;

                                    try
                                    {
                                        var locations = AppSettings.Default.GetLocations();
                                        if (locations.ContainsKey(e.KeyId.Value))
                                        {
                                            var location = locations[e.KeyId.Value];
                                            if (location == null)
                                                return null;

                                            var settingsLocation = AppSettings.Default.LocationAddress;
                                            if (!location.Equals(settingsLocation))
                                            {
                                                if (AppSettings.Default.UseTrackCurrentLocation)
                                                    AppSettings.Default.UseTrackCurrentLocation = false;
                                                AppSettings.Default.LocationAddress = location;
                                                result = location.EqualsMd5Code(settingsLocation);
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Error(ex);
                                        RunOnUiThread(() => Toast.MakeText(this,
                                            GetString(Resource.String.InternalError), AppSettings.ToastLength).Show());
                                    }

                                    return result;
                                });

                                if (notNeedRefresh.HasValue)
                                {
                                    if (notNeedRefresh.Value)
                                    {
                                        UpdateLocationAsync();
                                    }
                                    else
                                    {
                                        OnRefresh(showProgressbar: true, isOnlyDaily: false,
                                            infoReason: "selected location changed");
                                    }
                                }
                            }
                        }
                        return;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
            finally
            {
                try
                {
                    if (!isNotCloseDrawerLayout)
                        CloseDrawerLayout();

                    // если изменился settings
                    GeoTrackingServiceStartSafeAsync();
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }
            }
        }

        private void OnRefresh(bool showProgressbar, bool isOnlyDaily, string infoReason)
        {
            try
            {
                _needWeatherUpdate = false;

                if (showProgressbar)
                {
                    if (_isWaiting)
                        return;

                    ShowProgressbar(true);
                }

                var intent =
                    AndroidUtil.CreateExplicitFromImplicitIntent(this,
                        new Intent(WeatherBoundService.ActionGetWeatherData));
                if (isOnlyDaily)
                {
                    intent.PutExtra(WeatherBoundService.ExtraIsOnlyDaily, true);
                }
                else
                {
                    Log.InfoFormat("OnRefresh ('{0}').", infoReason);
                    UpdateDisplayLocation(null);
                    _contentFragment?.UpdateView(null, null, null, null, null, null, out bool _);
                }
                StartService(intent);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                ShowProgressbar(false);
            }
        }

        private void OnUpdateWeatherData(string action)
        {
            if (string.IsNullOrEmpty(action) || !_isBound)
                return;

            try
            {
                if (_contentFragment == null)
                    return;

                var service = _binder.Service as WeatherBoundService;
                if (service == null)
                    return;

                string errorMessage = null;
                if (service.ServiceExceptions != null && service.ServiceExceptions.ContainsKey(action))
                {
                    var ex = service.ServiceExceptions[action];
                    if (ex != null)
                    {
                        var exception = Util.FindException(ex, typeof(WeatherExceptionBase),
                            typeof(System.OperationCanceledException), typeof(TimeoutException));
                        if (exception is WeatherExceptionBase)
                        {
                            errorMessage = exception.Message;
                        }
                        else if (exception is System.OperationCanceledException)
                        {
                            errorMessage = GetString(Resource.String.TaskCanceled);
                        }
                        else if (exception is TimeoutException)
                        {
                            errorMessage = GetString(Resource.String.TaskTimeout);
                        }
                        else
                        {
                            errorMessage = GetString(Resource.String.InternalError);
                        }
                    }
                }

                try
                {
                    var weather = AppSettings.Default.Weather;

                    UpdateLocationAsync();
                    
                    var location = AppSettings.Default.LocationAddress;
                    _contentFragment.UpdateView(_weatherProviderNameId, location?.Latitude, location?.Longitude, weather,
                        service.Currently, service.Daily, out bool isAlerted);

                    SetAlertInvisible();
                    if (isAlerted && _imgAlert != null)
                    {
                        _imgAlert.SetBackgroundResource(Resource.Drawable.anim_blink_alert);
                        var blinkingAnimation = (AnimationDrawable)_imgAlert.Background;
                        _imgAlert.Visibility = ViewStates.Visible;
                        blinkingAnimation.Start();
                    }

                    if (string.IsNullOrEmpty(errorMessage))
                    {
                        // refresh widget
                        if (!service.IsOnlyDaily && weather != null && _widgetId.HasValue &&
                            _widgetId == _selectedWidgetId)
                        {
                            var widgetSetting = AppSettings.Default.FindAppWidgetSettings(_widgetId.Value);
                            if (widgetSetting != null && location != null)
                            {
                                widgetSetting.Weather = weather;
                                widgetSetting.LocationAddress = location;
                                AppSettings.Default.SaveAppWidgetSettings(widgetSetting);
                            }
                        }
                    }
                    else
                    {
                        Toast.MakeText(this, errorMessage, AppSettings.ToastLength).Show();
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                    Toast.MakeText(this, GetString(Resource.String.InternalError), AppSettings.ToastLength)
                        .Show();
                }
            }
            finally
            {
                ShowProgressbar(false);
            }
        }

        private void SetAlertInvisible()
        {
            if (_imgAlert != null)
                _imgAlert.Visibility = ViewStates.Gone;
        }

        private void OnCreateTooltip()
        {
            TooltipWindowDispose();
            _tooltipWindow = new TooltipWindow(context: this, tooltipLayoutId: Resource.Layout.tooltip_layout,
                tooltipId: Resource.Id.tooltipText, delayedMsec: 5000);
        }

        private void UpdateDisplayLocation(LocationAddress locationAddress)
        {
            if (_txtLocation != null)
                _txtLocation.Text = locationAddress?.GetDisplayLocality() ?? string.Empty;
        }

        private void UpdateLocationAsync()
        {
            var locationAddress = AppSettings.Default.LocationAddress;
            UpdateDisplayLocation(locationAddress);

            Task.Run(() =>
            {
                try
                {
                    if (_tooltipWindow == null)
                        OnCreateTooltip();

                    try
                    {
                        _tooltipWindow.TooltipText = string.Empty;
                    }
                    catch (ObjectDisposedException)
                    {
                        OnCreateTooltip();
                    }

                    if (locationAddress == null)
                    {
                        TooltipWindowDispose();
                        return;
                    }

                    var lines = locationAddress.GetAddressLine(", ");
                    if (lines != null && lines.Length == 2)
                    {
                        _tooltipWindow.TooltipTextFormatted =
                            new SpannableString(string.Join(System.Environment.NewLine, lines));
                    }
                    else
                    {
                        _tooltipWindow.TooltipText = locationAddress.GetAddress(LocationAddress.SpaceDelimeter) ??
                                                     string.Empty;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                    RunOnUiThread(() => Toast.MakeText(this,
                        GetString(Resource.String.InternalError), AppSettings.ToastLength).Show());
                }
            });
        }

        private static void GeoTrackingServiceStartSafeAsync()
        {
            Task.Run(() =>
            {
                try
                {
                    var api = new ServiceApi();
                    api.StartService(Application.Context, ControlService.ActionAppServiceStart);
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }
            });
        }

        private void ShowProgressbar(bool visible, int sleepmsec = AppSettings.WaitActivitySleepMsec)
        {
            if(_isWaiting == visible)
                return;

            _isWaiting = visible;

            Task.Run(() =>
            {
                if (_isWaiting)
                    Task.Delay(sleepmsec).Wait();

                RunOnUiThread(() =>
                {
                    if (_menuFragment != null)
                        _menuFragment.IsWaiting = _isWaiting;

                    if (_isWaiting)
                        SetAlertInvisible();

                    if (_progressBar != null)
                        _progressBar.Visibility = _isWaiting ? ViewStates.Visible : ViewStates.Gone;
                });
            });
        }

        private void SubscribeEvents()
        {
            UnsubscribeEvents();
            if (_menu != null)
                _menu.Click += OnHomeMenuClick;
        }

        private void UnsubscribeEvents()
        {
            if (_menu != null)
                _menu.Click -= OnHomeMenuClick;
        }

        private class ServiceReceiver : BroadcastReceiver
        {
            public override void OnReceive(Context context, Intent intent)
            {
                if (intent == null)
                    return;

                if (intent.Action == WeatherBoundService.ActionGetWeatherData)
                {
                    ((MainActivity) context).OnUpdateWeatherData(intent.Action);
                    InvokeAbortBroadcast();
                }
            }
        }

        private class ServiceConnection : ServiceConnectionBase<MainActivity>
        {
            public ServiceConnection(MainActivity activity) : base(activity)
            {
            }

            protected override void OnServiceConnected(MainActivity activity, ServiceBinder binder)
            {
                activity._binder = binder;
                activity._isBound = true;
            }

            protected override void OnServiceDisconnected(MainActivity activity)
            {
                activity._isBound = false;
            }
        }
    }
}