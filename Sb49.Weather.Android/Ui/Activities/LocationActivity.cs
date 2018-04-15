using System;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.OS;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Sb49.Common.Logging;
using Sb49.Http.Provider.Core;
using Sb49.Weather.Droid.Common;
using Sb49.Weather.Droid.Model;
using Sb49.Weather.Droid.Service;
using Sb49.Weather.Droid.Service.Binders;
using Sb49.Weather.Droid.Ui.Adapters;
using Toolbar = Android.Support.V7.Widget.Toolbar;
using AndroidUtil = Sb49.Common.Droid.Util;

namespace Sb49.Weather.Droid.Ui.Activities
{
    //http://stackoverflow.com/questions/19059894/google-geocoder-service-is-unavaliable-coordinates-to-address/19061688#19061688
    //http://developer.alexanderklimov.ru/android/views/autocompletetextview.php

    [Activity]
    public class LocationActivity : AppCompatActivity
    {
        public const string ExtraLocationChanged = AppSettings.AppPackageName + ".extra_locationchanged";
        public const string ExtraEmptyAddress = AppSettings.AppPackageName + ".extra_emptyaddress";

        private bool _isBound;
        private ServiceBinder _binder;
        private ServiceConnection _serviceConnection;
        private ServiceReceiver _serviceReceiver;
        private EditText _txtLocationAddress;
        private RecyclerView _viewAddress;
        RecyclerView.LayoutManager _layoutManager;
        private AddressAdapter _addressAdapter;
        private View _viewProgressbar;
        private bool _isWaiting;
        private IAppSettings _settings;
        private static readonly ILog Log = LogManager.GetLogger<LocationActivity>();

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            try
            {
                int? widgetId = Intent.GetIntExtra(AppWidgetManager.ExtraAppwidgetId,
                    AppWidgetManager.InvalidAppwidgetId);
                if (widgetId == AppWidgetManager.InvalidAppwidgetId)
                    widgetId = null;

                if (widgetId.HasValue)
                {
                    _settings = AppSettings.Default.FindAppWidgetSettings(widgetId.Value);
                    if (_settings == null)
                        Finish();
                }
                else
                {
                    _settings = AppSettings.Default;
                }

                SetContentView(Resource.Layout.activity_location);

                var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
                SetSupportActionBar(toolbar);
                SupportActionBar.SetTitle(Resource.String.LocationAddressTitle);

                SupportActionBar.SetDisplayHomeAsUpEnabled(true);
                SupportActionBar.SetHomeButtonEnabled(true);

                _txtLocationAddress = FindViewById<EditText>(Resource.Id.txtLocationAddress);
                if (!Intent.GetBooleanExtra(ExtraEmptyAddress, false))
                    _txtLocationAddress.Text = _settings.LocationAddress?.Address ?? string.Empty;

                var autocompleteTextView = _txtLocationAddress as AutoCompleteTextView;
                if(autocompleteTextView != null)
                {
                    var autoCompleteAdapter = GetAutoCompleteAdapter();
                    if (autoCompleteAdapter != null)
                        autocompleteTextView.Adapter = autoCompleteAdapter;
                }

                _viewAddress = FindViewById<RecyclerView>(Resource.Id.viewAddress);
                _layoutManager = new LinearLayoutManager(this);
                _viewAddress.SetLayoutManager(_layoutManager);

                _viewProgressbar = FindViewById(Resource.Id.toolbarProgress);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            try
            {
                if (!_isWaiting)
                    MenuInflater.Inflate(Resource.Menu.menu_location_activity, menu);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }

            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Android.Resource.Id.Home:
                    Finish();
                    return true;
                case Resource.Id.menuItemParseAddress:
                    var text = _txtLocationAddress?.Text;
                    if (!string.IsNullOrEmpty(text))
                        OnAddressSearch(text, false, null);
                    return true;
                case Resource.Id.menuItemMyLocation:
                    OnAddressSearch(null, true, 5);
                    return true;
                default:
                    return base.OnOptionsItemSelected(item);
            }
        }

        protected override void OnResume()
        {
            base.OnResume();

            SubscribeEvents();

            ValidateServiceReceiver();
            var intentFilter = new IntentFilter(LocationBoundService.ActionGetAddress)
            {
                Priority = (int) IntentFilterPriority.HighPriority
            };
            RegisterReceiver(_serviceReceiver, intentFilter);

            _serviceConnection?.Dispose();
            _serviceConnection = new ServiceConnection(this);
            var serviceIntent =
                AndroidUtil.CreateExplicitFromImplicitIntent(this, new Intent(LocationBoundService.ActionGetAddress));
            BindService(serviceIntent, _serviceConnection, Bind.AutoCreate);
        }

        protected override void OnPause()
        {
            base.OnPause();

            UnsubscribeEvents();
            ShowProgressbar(false);

            if (_isBound)
            {
                UnbindService(_serviceConnection);
                _isBound = false;
            }
            if (_serviceReceiver != null && _serviceReceiver.Handle != IntPtr.Zero)
                UnregisterReceiver(_serviceReceiver);
        }

        protected override void OnDestroy()
        {
            try
            {
                UnsubscribeEvents();

                _serviceConnection?.Dispose();
                _serviceConnection = null;

                _serviceReceiver?.Dispose();
                _serviceReceiver = null;

                _addressAdapter?.Dispose();
                _addressAdapter = null;
            }
            catch (Exception ex)
            {
                Log.Debug(ex);
            }
            //AppSettings.GcCollect(log: _log); //HARDCODE:

            base.OnDestroy();
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

        private void OnGetAddress()
        {
            try
            {
                if (!_isBound)
                    return;

                var service = _binder.Service as LocationBoundService;
                if (service == null)
                    return;

                var addresses = service.GeoAddresses?.ToArray();
                _addressAdapter = new AddressAdapter(addresses);
                _viewAddress?.SetAdapter(_addressAdapter);

                if (addresses == null)
                {
                    Toast.MakeText(this, GetString(Resource.String.UnableDetermineAddress), AppSettings.ToastLength)
                        .Show();
                    return;
                }

                SubscribeItemClickEvent();
            }
            finally
            {
                ShowProgressbar(false);
            }
        }

        private void OnAddressSearch(string src, bool useGeolocation, int? addressCount)
        {
            try
            {
                if (!_isWaiting)
                {
                    ShowProgressbar(true);
                    var intent =
                        AndroidUtil.CreateExplicitFromImplicitIntent(this,
                            new Intent(LocationBoundService.ActionGetAddress));
                    if (!string.IsNullOrEmpty(src))
                        intent.PutExtra(LocationBoundService.ExtraAddressValue, src);
                    if (addressCount.HasValue)
                        intent.PutExtra(LocationBoundService.ExtraAddressCount, addressCount.Value);
                    intent.PutExtra(LocationBoundService.ExtraUseGeolocation, useGeolocation);
                    StartService(intent);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                ShowProgressbar(false);
            }
        }

        private void OnEdittextKeyPress(object sender, View.KeyEventArgs e)
        {
            e.Handled = e.KeyCode == Keycode.Enter;
            if (e.Handled && e.Event.Action == KeyEventActions.Down)
            {
                var text = _txtLocationAddress?.Text;
                if (!string.IsNullOrEmpty(text))
                    OnAddressSearch(text, true, 30);
            }
        }

        private void OnAddressItemClick(object sender, LocationAddress e)
        {
            try
            {
                if (_settings == null)
                    throw new ArgumentNullException(nameof(_settings));

                if (!ValidateAddress(e))
                    return;

                _settings.UseTrackCurrentLocation = false;
                _settings.LocationAddress = e;
                AppSettings.Default.SaveAppWidgetSettings(_settings);

                var intent = new Intent();
                intent.PutExtra(ExtraLocationChanged, true);
                SetResult(Result.Ok, intent);
                Finish();
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                ShowError(Resource.String.InternalError);
            }
        }

        private bool ValidateAddress(ILocationAddress newAddress)
        {
            if (newAddress == null)
            {
                ShowError(Resource.String.EmptyLocationAddressError);
                return false;
            }

            if (string.IsNullOrEmpty(newAddress.Locality))
            {
                ShowError(Resource.String.LocalityIsNullLocationAddressError);
                return false;
            }

            if (_settings.IsNotAppWidget)
            {
                var locations = AppSettings.Default.GetLocations();
                if (locations != null && locations.Any(p => p.Value != null && p.Value.Equals(newAddress)))
                {
                    ShowError(Resource.String.ExstsLocationAddressError);
                    return false;
                }
            }

            return true;
        }

        private ArrayAdapter GetAutoCompleteAdapter()
        {
            var locations = AppSettings.Default.GetLocations();
            if (locations == null || locations.Count == 0)
                return null;

            var autoCompleteOptions = locations.Where(p => !string.IsNullOrEmpty(p.Value?.Locality))
                .Select(p => p.Value.Locality)
                .ToArray();

            return autoCompleteOptions.Length == 0
                ? null
                : new ArrayAdapter(this, Android.Resource.Layout.SimpleDropDownItem1Line,
                    autoCompleteOptions.Distinct().OrderBy(p => p).ToArray());
        }

        private void ShowError(int messageId)
        {
            RunOnUiThread(() =>
            {
                Toast.MakeText(this, messageId, AppSettings.ToastLength).Show();
            });
        }

        private void ShowProgressbar(bool visible, int sleepmsec = AppSettings.WaitActivitySleepMsec)
        {
            if (_isWaiting == visible)
                return;

            _isWaiting = visible;
            Task.Run(() =>
            {
                if (_isWaiting)
                    Task.Delay(sleepmsec).Wait();

                RunOnUiThread(() =>
                {
                    if (_viewProgressbar != null)
                        _viewProgressbar.Visibility = _isWaiting ? ViewStates.Visible : ViewStates.Gone;
                    InvalidateOptionsMenu();
                });
            });
        }

        private void SubscribeEvents()
        {
            UnsubscribeEvents();
            if (_txtLocationAddress != null)
                _txtLocationAddress.KeyPress += OnEdittextKeyPress;
        }

        private void SubscribeItemClickEvent()
        {
            UnsubscribeItemClickEvent();
            if (_addressAdapter != null)
                _addressAdapter.ItemClick += OnAddressItemClick;
        }

        private void UnsubscribeEvents()
        {
            if (_txtLocationAddress != null)
                _txtLocationAddress.KeyPress -= OnEdittextKeyPress;
            UnsubscribeItemClickEvent();
        }

        private void UnsubscribeItemClickEvent()
        {
            if (_addressAdapter != null)
                _addressAdapter.ItemClick -= OnAddressItemClick;
        }

        private class ServiceReceiver : BroadcastReceiver
        {
            public override void OnReceive(Context context, Intent intent)
            {
                if (intent?.Action == LocationBoundService.ActionGetAddress)
                {
                    ((LocationActivity) context).OnGetAddress();
                    InvokeAbortBroadcast();
                }
            }
        }

        private class ServiceConnection : ServiceConnectionBase<LocationActivity>
        {
            public ServiceConnection(LocationActivity activity) : base(activity)
            {
            }

            protected override void OnServiceConnected(LocationActivity activity, ServiceBinder binder)
            {
                activity._binder = binder;
                activity._isBound = true;
            }

            protected override void OnServiceDisconnected(LocationActivity activity)
            {
                activity._isBound = false;
            }
        }
    }
}