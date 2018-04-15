using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security;
using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.Provider;
using Android.Support.V7.Preferences;
using Android.Util;
using Android.Views;
using Android.Widget;
using Sb49.Common.Droid.Ui;
using Sb49.Common.Logging;
using Sb49.Weather.Core;
using Sb49.Weather.Droid.Common;
using Sb49.Weather.Droid.Model;
using Sb49.Weather.Droid.Service;
using Sb49.Weather.Droid.Ui.Activities;
using Sb49.Weather.Droid.Ui.AppWidget.Updaters;
using Sb49.Weather.Droid.Ui.AppWidget.Updaters.Core;
using Sb49.Weather.Droid.Ui.Preferences;
using Sb49.Weather.Exceptions;
using ResourceType = Sb49.Common.Droid.ResourceType;

namespace Sb49.Weather.Droid.Ui.Fragments
{
    public class SettingsFragment : CustomPreferenceFragmentCompat, ISharedPreferencesOnSharedPreferenceChangeListener
    {
        public const string ExtraPreferenceXmlId = AppSettings.AppPackageName + ".extra_preferencexmlid";

        private const int RequestCodeLocation = 1;
        private const int RequestCodeCreateLogFile = 2;
        private const int RequestCodeApiKey = 3;
        private const string ApPmFormat = "tt ";

        private SwitchPreferenceCompat _hourFormatSwitch;
        private SwitchPreferenceCompat _extendedHourFormatSwitch;
        private ListPreference _dateFormatList;
        private SeekBarPreferenceCompat _clockTextSizeSeekBar;
        private ListPreference _weatherProviderServerList;
        private SwitchPreferenceCompat _useGoogleMapsGeocodingApiSwitch;
        private ListPreference _refreshIntervalList;
        private Preference _locationAddressScreen;
        private ListPreference _temperatureUnitList;
        private ListPreference _windSpeedUnitList;
        private ListPreference _pressureUnitList;
        private ListPreference _visibilityUnitList;
        private ImagePreference _appWidgetView;
        private ListPreference _appWidgetIconStyleList;
        private ListPreference _appWidgetBackgroundStyleList;
        private CustomSeekBarPreference _appWidgetOpacity;
        private SwitchPreferenceCompat _useTrackCurrentLocationSwitch;
        private EditTextPreference _coldAlertedTemperatureEditText;
        private EditTextPreference _hotAlertedTemperatureEditText;
        private ListPreference _geoTrackingServicePriorityList;
        private SwitchPreferenceCompat _loggingUseFile;
        private ListPreference _loggingSelectLogFileList;
        private CustomMultiSelectListPreference _loggingLevelsMultiSelectList;
        private IDictionary<string, bool> _excludeKeys;
        private IDictionary<string, bool> _logKeys;
        private AppWidgetSettings _appWidgetSettings;
        private bool _isActionAppwidGetConfigure;
        private IAppWidgetUpdater _appWidgetUpdater;
        private Dialogs _dialogs;
        private static readonly ILog Log = LogManager.GetLogger<SettingsFragment>();

        public override void OnCreatePreferences(Bundle savedInstanceState, string rootKey)
        {
            try
            {
                PreferenceManager.SharedPreferencesName = AppSettings.SharedPreferencesFileName;
                PreferenceManager.SharedPreferencesMode = (int) FileCreationMode.Private;

                //AddPreferencesFromResource(Activity.Intent.GetIntExtra(ExtraPreferenceXmlId, Resource.Xml.preferences));
                SetPreferencesFromResource(
                    Activity.Intent.GetIntExtra(ExtraPreferenceXmlId, Resource.Xml.preferences),
                    rootKey);

                _isActionAppwidGetConfigure = Activity.Intent.Action == AppWidgetManager.ActionAppwidgetConfigure;
                int? widgetId = Activity.Intent.GetIntExtra(AppWidgetManager.ExtraAppwidgetId,
                    AppWidgetManager.InvalidAppwidgetId);
                if (widgetId == AppWidgetManager.InvalidAppwidgetId)
                    widgetId = null;
                if (widgetId.HasValue)
                {
                    _appWidgetSettings = AppSettings.Default.FindAppWidgetSettings(widgetId.Value);
                    if (!_isActionAppwidGetConfigure && _appWidgetSettings == null)
                    {
                        _appWidgetSettings = AppSettings.Default.CreateAppWidgetSettings(widgetId.Value);
                        AppSettings.Default.SaveAppWidgetSettings(_appWidgetSettings);
                        AppSettings.Default.ChangeStatus |= SettingsChangeStatus.NeedAppWidgetWeatherDataUpdate;
                    }

                    if (_appWidgetUpdater == null)
                    {
                        var updater = new Updater();
                        _appWidgetUpdater = updater.BuldUpdater(Context, _appWidgetSettings.WidgetId);
                    }
                }

                var appWidgetSettingsPreference = FindPreference(GetString(Resource.String.AppWidgetSettingsKey));
                if (appWidgetSettingsPreference != null)
                    appWidgetSettingsPreference.Enabled = widgetId.HasValue;

                if (_dialogs == null)
                    _dialogs = new Dialogs();

                #region widgetPreferences

                _appWidgetView = (ImagePreference) FindPreference(GetString(Resource.String.AppWidgetViewKey));

                _appWidgetIconStyleList =
                    (ListPreference) FindPreference(GetString(Resource.String.AppWidgetIconStyleKey));
                if (_appWidgetIconStyleList != null)
                {
                    SetListPreferenceValues(_appWidgetIconStyleList, typeof(IconStyles),
                        _appWidgetSettings.WidgetIconStyle.ToString());
                }

                _appWidgetBackgroundStyleList =
                    (ListPreference) FindPreference(GetString(Resource.String.AppWidgetBackgroundStyleKey));
                if (_appWidgetBackgroundStyleList != null)
                    _appWidgetBackgroundStyleList.Value = _appWidgetSettings.WidgetBackgroundStyle;

                _appWidgetOpacity =
                    (CustomSeekBarPreference) FindPreference(GetString(Resource.String.AppWidgetOpacityKey));
                if (_appWidgetOpacity != null)
                    _appWidgetOpacity.Progress = _appWidgetSettings.WidgetBackgroundOpacity;

                #endregion widgetPreferences

                #region clockPreferences

                var hasClock = _appWidgetUpdater?.HasClock == true;

                var appWidgetSettingsClockCategory = FindPreference(GetString(Resource.String.AppWidgetSettingsClockKey));
                if (appWidgetSettingsClockCategory != null)
                    appWidgetSettingsClockCategory.Visible = hasClock;

                _hourFormatSwitch =
                    (SwitchPreferenceCompat) FindPreference(GetString(Resource.String.HourFormatKey));
                if (_hourFormatSwitch != null)
                {
                    _hourFormatSwitch.Visible = hasClock;
                    if (hasClock)
                        _hourFormatSwitch.Checked = _appWidgetSettings.Use12HourFormat;
                    else
                        _hourFormatSwitch = null;
                }

                _extendedHourFormatSwitch =
                    (SwitchPreferenceCompat) FindPreference(GetString(Resource.String.ExtendedHourFormatKey));
                if (_extendedHourFormatSwitch != null)
                {
                    _extendedHourFormatSwitch.Visible = hasClock;
                    if (hasClock)
                        _extendedHourFormatSwitch.Checked = _appWidgetSettings.UseExtendedHourFormat;
                    else
                        _extendedHourFormatSwitch = null;
                }

                _dateFormatList = (ListPreference) FindPreference(GetString(Resource.String.DateFormatKey));
                if (_dateFormatList != null)
                {
                    _dateFormatList.Visible = hasClock;
                    if (hasClock)
                        _dateFormatList.Value = _appWidgetSettings.DateFormatValue;
                    else
                        _dateFormatList = null;
                }

                _clockTextSizeSeekBar = (SeekBarPreferenceCompat)FindPreference(GetString(Resource.String.ClockTextSizeKey));
                if (_clockTextSizeSeekBar != null)
                {
                    _clockTextSizeSeekBar.Visible = hasClock;
                    if (hasClock)
                        _clockTextSizeSeekBar.Progress = _appWidgetSettings.ClockTextSizeSp;
                    else
                        _clockTextSizeSeekBar = null;
                }

                if (hasClock)
                    OnUpdateClockFormatSummary();

                #endregion clockPreferences

                #region weatherPreferences

                _weatherProviderServerList =
                    (ListPreference) FindPreference(GetString(Resource.String.WeatherProviderServerKey));
                if (_weatherProviderServerList != null)
                {
                    var values = AppSettings.Default.GetWeatherProviderIds();
                    if (values != null)
                    {
                        _weatherProviderServerList.SetEntryValues(values.Select(p => p.ToString()).ToArray());
                        _weatherProviderServerList.SetEntries(
                            values.Select(p => GetString(AppSettings.Default.GetWeatherProviderNameById(p)))
                                .ToArray());
                    }

                    _weatherProviderServerList.Value = _appWidgetSettings.WeatherProviderId.ToString();
                }

                _refreshIntervalList =
                    (ListPreference) FindPreference(GetString(Resource.String.WeatherRefreshIntervalKey));
                if (_refreshIntervalList != null)
                    _refreshIntervalList.Value = _appWidgetSettings.WeatherServiceRefreshIntervalValue.ToString();

                _temperatureUnitList =
                    (ListPreference) FindPreference(GetString(Resource.String.TemperatureUnitKey));
                SetListPreferenceValues(_temperatureUnitList, typeof(TemperatureUnit),
                    AppSettings.Default.TemperatureUnit.ToString());

                _windSpeedUnitList = (ListPreference) FindPreference(GetString(Resource.String.WindSpeedUnitKey));
                SetListPreferenceValues(_windSpeedUnitList, typeof(SpeedUnit),
                    AppSettings.Default.WindSpeedUnit.ToString());

                _pressureUnitList = (ListPreference) FindPreference(GetString(Resource.String.PressureUnitKey));
                SetListPreferenceValues(_pressureUnitList, typeof(PressureUnit),
                    AppSettings.Default.PressureUnit.ToString());

                _visibilityUnitList =
                    (ListPreference) FindPreference(GetString(Resource.String.VisibilityUnitKey));
                SetListPreferenceValues(_visibilityUnitList, typeof(DistanceUnit),
                    AppSettings.Default.VisibilityUnit.ToString());

                #endregion weatherPreferences

                #region locationPreferences

                _useTrackCurrentLocationSwitch = (SwitchPreferenceCompat)
                    FindPreference(GetString(Resource.String.UseTrackCurrentLocationKey));
                SetUseTrackCurrentLocationSwitch();

                _locationAddressScreen = FindPreference(GetString(Resource.String.LocationAddressKey));
                if (_locationAddressScreen != null)
                    OnUpdateLocationSummary();

                #endregion locationPreferences

                #region alertPreferences

                _coldAlertedTemperatureEditText =
                    (EditTextPreference) FindPreference(GetString(Resource.String.ColdAlertedTemperatureKey));
                _hotAlertedTemperatureEditText =
                    (EditTextPreference) FindPreference(GetString(Resource.String.HotAlertedTemperatureKey));

                string ToStringHandler(double? temp) => temp?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

                if (_coldAlertedTemperatureEditText != null)
                {
                    _coldAlertedTemperatureEditText.Text = ToStringHandler(AppSettings.Default.ColdAlertedTemperature);
                    OnUpdateColdAlertedTemperatureSummary();
                }

                if (_hotAlertedTemperatureEditText != null)
                {
                    _hotAlertedTemperatureEditText.Text = ToStringHandler(AppSettings.Default.HotAlertedTemperature);
                    OnUpdateHotAlertedTemperatureSummary();
                }

                #endregion alertPreferences

                #region geoTracking

                _geoTrackingServicePriorityList =
                    (ListPreference) FindPreference(GetString(Resource.String.GeoTrackingServicePriorityKey));
                if (_geoTrackingServicePriorityList != null)
                {
                    var values = new[]
                    {
                        Android.Gms.Location.LocationRequest.PriorityHighAccuracy.ToString(),
                        Android.Gms.Location.LocationRequest.PriorityBalancedPowerAccuracy.ToString(),
                        Android.Gms.Location.LocationRequest.PriorityLowPower.ToString()
                    };
                    var entries = new[]
                    {
                        GetString(Resource.String.PriorityHighAccuracy),
                        GetString(Resource.String.PriorityBalancedPowerAccuracy),
                        GetString(Resource.String.PriorityLowPower)
                    };
                    _geoTrackingServicePriorityList.SetEntryValues(values);
                    _geoTrackingServicePriorityList.SetEntries(entries);
                    if (_geoTrackingServicePriorityList.Value == null)
                    {
                        _geoTrackingServicePriorityList.Value =
                            AppSettings.Default.GeoTrackingServicePriorityDefault.ToString();
                    }
                }

                #endregion geoTracking

                #region googleApiPreferences

                _useGoogleMapsGeocodingApiSwitch =
                    (SwitchPreferenceCompat)
                    FindPreference(GetString(Resource.String.UseGoogleMapsGeocodingApiKey));

                #endregion googleApiPreferences

                #region loggingPreference

                _loggingLevelsMultiSelectList = (CustomMultiSelectListPreference)
                    FindPreference(GetString(Resource.String.LoggingLevelsKey));

                _loggingUseFile =
                    (SwitchPreferenceCompat) FindPreference(GetString(Resource.String.LoggingUseFileKey));

                _loggingSelectLogFileList =
                    (ListPreference) FindPreference(GetString(Resource.String.LoggingSelectLogFileKey));
                OnUpdateLoggingSelectLogFileListSummary(AppSettings.Default.LoggingUseFile);

                #endregion loggingPreference

                #region excludeKeys

                _logKeys = new ConcurrentDictionary<string, bool>()
                {
                    [GetString(Resource.String.LoggingUseAndroidLogKey)] = true, //Contains
                    [GetString(Resource.String.LoggingUseFileKey)] = true, //Contains
                    [GetString(Resource.String.LogFileNameKey)] = true, //Contains
                    [GetString(Resource.String.LoggingLevelsKey)] = true, //Contains
                    [GetString(Resource.String.LoggingMaximumFileSizeKey)] = true, //Contains
                    [GetString(Resource.String.LoggingMaxSizeRollBackupsKey)] = true //Contains
                };

                _excludeKeys = new ConcurrentDictionary<string, bool>()
                {
                    [GetString(Resource.String.UseGoogleMapsGeocodingApiKey)] = true, //Contains
                    [GetString(Resource.String.LocationAddressKey)] = true, //Contains
                    [GetString(Resource.String.RequestCounterKey)] = false, //StartsWith
                    [GetString(Resource.String.WeatherApiKey)] = true, //Contains
                    [GetString(Resource.String.WeatherDataKey)] = true, //Contains
                    [GetString(Resource.String.UseTrackCurrentLocationKey)] = true, //Contains
                    [GetString(Resource.String.SelectedLocationAddressIdKey)] = true, //Contains
                    [GetString(Resource.String.AppWidgetSettingsCollectionKey)] = true, //Contains
                    [GetString(Resource.String.CurrentLocationKey)] = true, //Contains
                    [GetString(Resource.String.GeoTrackingServicePriorityKey)] = true, //Contains
                    [GetString(Resource.String.GeoTrackingServiceIntervalKey)] = true, //Contains
                    [GetString(Resource.String.GeoTrackingServiceSmallestDisplacementKey)] = true, //Contains
                    [GetString(Resource.String.IsBootCompletedReceiverKey)] = true, //Contains
                    [GetString(Resource.String.ShowFileSizeKey)] = true, //Contains
                    [GetString(Resource.String.ShowFolderSizeKey)] = true //Contains
                };

                #endregion excludeKeys
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                Toast.MakeText(Context, GetString(Resource.String.InternalError), AppSettings.ToastLength).Show();
                Activity?.Finish();
            }
        }

        public override void OnActivityResult(int requestCode, int resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            try
            {
                // восстанавливаем параметры
                RestoreChangeStatusParams();

                if (resultCode != (int)Result.Ok || data == null)
                    return;

                switch (requestCode)
                {
                    case RequestCodeLocation:

                        if (_locationAddressScreen != null &&
                            data.GetBooleanExtra(LocationActivity.ExtraLocationChanged, false) &&
                            _appWidgetSettings != null)
                        {
                            if (!_isActionAppwidGetConfigure && _appWidgetSettings != null &&
                                _appWidgetSettings.ValidateLocationSettings(Activity, 0))
                            {
                                AppSettings.Default.ChangeStatus |= SettingsChangeStatus.NeedAppWidgetWeatherDataUpdate;
                            }

                            SetUseTrackCurrentLocationSwitch();
                            OnUpdateLocationSummary();
                        }
                        break;
                    case RequestCodeCreateLogFile:
                        if (_loggingUseFile == null)
                            throw new ArgumentNullException(nameof(_loggingUseFile));

                        var uri = data.Data;
                        var path = uri?.Path;
                        var title = string.IsNullOrEmpty(path)
                            ? AppSettings.LogFileNameDefault
                            : System.IO.Path.GetFileName(path);

                        using (var docfile = DocumentFile.FromSingleUri(Context, uri))
                        {
                            if (string.IsNullOrEmpty(path))
                            {
                                docfile?.Delete();
                                StartSelectLogFileActivity(logFileName: path, title: title);
                                throw new WeatherExceptionBase(GetString(Resource.String.IllegalLogFileUri),
                                    new ArgumentNullException(string.Format("Can't convert uri '{0}' to file path.", uri)));
                            }

                            using (var file = new Java.IO.File(path))
                            {
                                if (!(file.CanRead() && file.CanWrite()))
                                {
                                    docfile?.Delete();
                                    StartSelectLogFileActivity(logFileName: path, title: title);
                                    throw new WeatherExceptionBase(GetString(Resource.String.IllegalLogFileUri),
                                        new SecurityException(string.Format("Not permission to uri '{0}'", uri)));
                                }
                            }
                        }

                        AppSettings.Default.LogFileName = path;
                        if (!_loggingUseFile.Checked)
                            _loggingUseFile.Checked = true;
                        OnUpdateLoggingSelectLogFileListSummary(true);
                        AppSettings.Default.ChangeStatus |= SettingsChangeStatus.LogChanged;
                        break;
                    case RequestCodeApiKey:
                        if (data.GetBooleanExtra(ApiKeyManagementActivity.ExtraDelete, false) &&
                            _useGoogleMapsGeocodingApiSwitch != null &&
                            _useGoogleMapsGeocodingApiSwitch.Checked != AppSettings.Default.UseGoogleMapsGeocodingApi)
                        {
                            _useGoogleMapsGeocodingApiSwitch.Checked = AppSettings.Default.UseGoogleMapsGeocodingApi;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                if (_isActionAppwidGetConfigure)
                    return;

                var message = ex is WeatherExceptionBase
                    ? ex.Message
                    : GetString(Resource.String.InternalError);
                Toast.MakeText(Context, message, AppSettings.ToastLength).Show();
            }
        }

        public override void StartActivityForResult(Intent intent, int requestCode)
        {
            SaveChangeStatusParams();
            base.StartActivityForResult(intent, requestCode);
        }

        public override void OnResume()
        {
            base.OnResume();

            SubscribeEvents();
        }

        public override void OnPause()
        {
            base.OnPause();

            UnsubscribeEvents();
        }

        public override void OnStop()
        {
            base.OnStop();

            OnLoggingConfigureChanged();
            OnAppWidgetUpdate();
        }

        private void OnLoggingConfigureChanged()
        {
            if (AppSettings.Default.ChangeStatus.HasFlag(SettingsChangeStatus.Skip) ||
                !AppSettings.Default.ChangeStatus.HasFlag(SettingsChangeStatus.LogChanged))
            {
                return;
            }

            AppSettings.Default.ChangeStatus ^= SettingsChangeStatus.LogChanged;
            Toast.MakeText(Context, Resource.String.LoggingPropertiesChanged, AppSettings.ToastLength).Show();
        }

        private void OnAppWidgetUpdate()
        {
            if (_isActionAppwidGetConfigure || _appWidgetSettings == null ||
                AppSettings.Default.ChangeStatus.HasFlag(SettingsChangeStatus.Skip))
            {
                return;
            }

            var needAppWidgetUpdate =
                AppSettings.Default.ChangeStatus.HasFlag(SettingsChangeStatus.NeedAppWidgetUpdate);
            var needAppWidgetWeatherDataUpdate =
                AppSettings.Default.ChangeStatus.HasFlag(SettingsChangeStatus.NeedAppWidgetWeatherDataUpdate);

            if (!needAppWidgetUpdate && !needAppWidgetWeatherDataUpdate)
                return;

            try
            {
                var widgetIds = new[] { _appWidgetSettings.WidgetId };
                var api = new ServiceApi();
                if (needAppWidgetUpdate)
                {
                    api.AppWidgetUpdateService(Context, widgetIds);
                    AppSettings.Default.ChangeStatus ^= SettingsChangeStatus.NeedAppWidgetUpdate;
                }
                if (needAppWidgetWeatherDataUpdate)
                {
                    api.AppWidgetWeatherDataUpdateService(Context, widgetIds);
                    AppSettings.Default.ChangeStatus ^= SettingsChangeStatus.NeedAppWidgetWeatherDataUpdate;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                var weatherEx = ex as WeatherExceptionBase;
                if (weatherEx != null)
                    Toast.MakeText(Context, weatherEx.Message, AppSettings.ToastLength).Show();
            }
        }

        #region OnDestroy

        public override void OnDestroy()
        {
            try
            {
                _isActionAppwidGetConfigure = false;
                UnsubscribeEvents();

                _excludeKeys?.Clear();
                _excludeKeys = null;

                _logKeys?.Clear();
                _logKeys = null;

                _appWidgetUpdater = null;
                _dialogs = null;
            }
            catch (Exception ex)
            {
                Log.Debug(ex);
            }

            base.OnDestroy();
        }

        #endregion OnDestroy

        #region OnPreferenceTreeClick

        public override bool OnPreferenceTreeClick(Preference preference)
        {
            var result = base.OnPreferenceTreeClick(preference);

            var key = preference?.Key;
            if (_locationAddressScreen != null && _locationAddressScreen.Key == key)
            {
                StartLocationActivity();
                return result;
            }
            if (key == GetString(Resource.String.ApiKeysManagementKey))
            {
                var intent = new Intent(Context, typeof(ApiKeyManagementActivity));
                StartActivityForResult(intent, RequestCodeApiKey);
                return result;
            }

            var customPreference = preference as CustomPreference;
            if (customPreference == null || !customPreference.NeedStartNewActivity)
                return result;

            try
            {
                if (customPreference.ActivityFragmentLayoutResource.HasValue)
                {
                    var intent = new Intent(Context, typeof(SettingsActivity));
                    intent.PutExtra(ExtraPreferenceXmlId, customPreference.ActivityFragmentLayoutResource.Value);
                    if (customPreference.ActivityTitleResource.HasValue)
                    {
                        intent.PutExtra(SettingsActivity.ExtraToolbarTitleId,
                            customPreference.ActivityTitleResource.Value);
                    }
                    else
                    {
                        intent.PutExtra(SettingsActivity.ExtraToolbarTitle, preference.Title);
                    }

                    if (_appWidgetSettings != null)
                        intent.PutExtra(AppWidgetManager.ExtraAppwidgetId, _appWidgetSettings.WidgetId);

                    if (customPreference.Values == "widget")
                        intent.PutExtra(SettingsActivity.ExtraWidget, true);

                    StartActivity(intent);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }

            return result;
        }

        private void StartLocationActivity()
        {
            var intent = new Intent(Context, typeof(LocationActivity));
            intent.PutExtra(AppWidgetManager.ExtraAppwidgetId, _appWidgetSettings.WidgetId);
            StartActivityForResult(intent, RequestCodeLocation);
        }

        #endregion OnPreferenceTreeClick

        #region OnPreferenceChanging

        private void OnPreferenceChanging(object sender, Preference.PreferenceChangeEventArgs e)
        {
            if (string.IsNullOrEmpty(e?.Preference?.Key) || e.NewValue == null)
                return;

            var key = e.Preference?.Key;
            var newvalue = e.NewValue.ToString();

            try
            {
                #region alert

                if (key == _coldAlertedTemperatureEditText?.Key || key == _hotAlertedTemperatureEditText?.Key)
                {
                    var value = double.NaN;
                    var emptyValue = string.IsNullOrEmpty(newvalue);
                    if (emptyValue || double.TryParse(newvalue, out value))
                    {
                        var temp = emptyValue || double.IsNaN(value) ? (double?) null : value;
                        if (key == _coldAlertedTemperatureEditText?.Key)
                        {
                            AppSettings.Default.ColdAlertedTemperature = temp;
                            OnUpdateColdAlertedTemperatureSummary();
                        }
                        else if (key == _hotAlertedTemperatureEditText?.Key)
                        {
                            AppSettings.Default.HotAlertedTemperature = temp;
                            OnUpdateHotAlertedTemperatureSummary();
                        }
                        return;
                    }

                    e.Handled = false;
                    Toast.MakeText(Context, GetString(Resource.String.InputIntegerError),
                        AppSettings.ToastLength).Show();
                }

                #endregion alert

                #region appWidget

                else if (key == _appWidgetIconStyleList?.Key)
                {
                    if (_appWidgetSettings != null)
                    {
                        _appWidgetSettings.WidgetIconStyle = (IconStyles) Enum.Parse(typeof(IconStyles), newvalue);
                        SaveNotPersistentPreference(true);
                    }
                }
                else if (key == _appWidgetBackgroundStyleList?.Key)
                {
                    if (_appWidgetSettings != null)
                    {
                        _appWidgetSettings.WidgetBackgroundStyle = newvalue;
                        SaveNotPersistentPreference(true);
                    }
                }
                else if (key == _appWidgetOpacity?.Key)
                {
                    if (_appWidgetSettings != null)
                    {
                        _appWidgetSettings.WidgetBackgroundOpacity = int.Parse(newvalue);
                        SaveNotPersistentPreference(true);
                        OnUpdateAppWidget(null);
                    }
                }
                else if (key == _hourFormatSwitch?.Key)
                {
                    if (_appWidgetSettings != null)
                    {
                        _appWidgetSettings.Use12HourFormat = Convert.ToBoolean(newvalue);
                        SaveNotPersistentPreference(true);
                        OnUpdateClockFormatSummary();
                    }
                }
                else if (key == _extendedHourFormatSwitch?.Key)
                {
                    if (_appWidgetSettings != null)
                    {
                        _appWidgetSettings.UseExtendedHourFormat = Convert.ToBoolean(newvalue);
                        SaveNotPersistentPreference(true);
                        OnUpdateClockFormatSummary();
                    }
                }
                else if (key == _dateFormatList?.Key)
                {
                    if (_appWidgetSettings != null)
                    {
                        _appWidgetSettings.DateFormatValue = newvalue;
                        SaveNotPersistentPreference(true);
                    }
                }
                else if (key == _clockTextSizeSeekBar?.Key)
                {
                    if (_appWidgetSettings != null)
                    {
                        _appWidgetSettings.ClockTextSizeSp = int.Parse(newvalue);
                        SaveNotPersistentPreference(true);
                    }
                }

                #region useTrackCurrentLocationSwitch

                else if (key == _useTrackCurrentLocationSwitch?.Key)
                {
                    var value = Convert.ToBoolean(newvalue);

                    if (value)
                    {
                        if (!AppSettings.Default.ValidateLocationSettings())
                        {
                            e.Handled = false;
                            var format = GetString(Resource.String.LocationDisabledMessageFormat);
                            var message = string.Format(format, System.Environment.NewLine);
                            new WidgetUtil().ShowDialog(context: Context, titleId: null, message: message,
                                okAction: () =>
                                {
                                    var intent = new Intent(Android.Provider.Settings.ActionLocationSourceSettings);
                                    StartActivityForResult(intent, 0);
                                });
                            return;
                        }

                        if (!_dialogs.GoogleApiAvailabilityDialog(Activity, 0))
                        {
                            e.Handled = false;
                            return;
                        }

                        if(_appWidgetSettings != null)
                        {
                            _appWidgetSettings.UseTrackCurrentLocation = true;
                            SaveNotPersistentPreference(false, true);
                            OnUpdateLocationSummary();
                        }
                    }
                    else
                    {
                        e.Handled = false;
                        StartLocationActivity();
                    }
                }

                #endregion useTrackCurrentLocationSwitch
                
                #region weatherProviderServerList

                else if (key == _weatherProviderServerList?.Key)
                {
                    void SaveHandler(bool handled, int provId)
                    {
                        if (handled && _appWidgetSettings != null)
                        {
                            _appWidgetSettings.WeatherProviderId = provId;
                            SaveNotPersistentPreference(false, true);
                        }
                    }

                    var newProviderId = int.Parse(newvalue);
                    e.Handled = _dialogs.ApiKeyDialog(activity: Activity, providerType: ProviderTypes.WeatherProvider,
                        providerId: newProviderId,
                        alwaysShowDialog: false,
                        okAction: () =>
                        {
                            ((ListPreference) e.Preference).Value = newvalue;
                            SaveHandler(true, newProviderId);
                        }, cancelAction: null, toastLength: AppSettings.ToastLength, log: Log);

                    SaveHandler(e.Handled, newProviderId);
                }

                #endregion weatherProviderServerList

                else if (key == _refreshIntervalList?.Key)
                {
                    if (_appWidgetSettings != null)
                    {
                        _appWidgetSettings.WeatherServiceRefreshIntervalValue = int.Parse(newvalue);
                        SaveNotPersistentPreference(false);
                    }
                }

                #endregion appWidget

                #region useGoogleApi

                else if (key == _useGoogleMapsGeocodingApiSwitch?.Key)
                {
                    var isChecked = Convert.ToBoolean(newvalue);
                    if (isChecked)
                    {
                        e.Handled = _dialogs.ApiKeyDialog(activity: Activity, providerType: ProviderTypes.GoogleApiProvider, 
                            providerId: AppSettings.GoogleMapsGeocodingApiProviderId, alwaysShowDialog: false, okAction: () =>
                            {
                                ((SwitchPreferenceCompat) e.Preference).Checked = true;
                            }, cancelAction: null, toastLength: AppSettings.ToastLength, log: Log);
                    }
                }

                #endregion useGoogleApi

                #region logger

                else if (key == _loggingUseFile?.Key)
                {
                    var value = Convert.ToBoolean(newvalue);
                    if (value)
                    {
                        e.Handled = false;
                        if (_loggingSelectLogFileList == null)
                            throw new ArgumentNullException(nameof(_loggingSelectLogFileList));

                        OnDisplayPreferenceDialog(_loggingSelectLogFileList);
                    }
                    else
                    {
                        OnUpdateLoggingSelectLogFileListSummary(false);
                    }
                }
                else if (key == _loggingSelectLogFileList?.Key)
                {
                    if (_loggingUseFile == null)
                        throw new ArgumentNullException(nameof(_loggingUseFile));

                    if (newvalue == GetString(Resource.String.zero))
                    {
                        AppSettings.Default.LogFileName = AppSettings.Default.CreateInternalLogFilePath();
                        if (!_loggingUseFile.Checked)
                            _loggingUseFile.Checked = true;
                    }
                    else if (newvalue == GetString(Resource.String.one))
                    {
                        StartSelectLogFileActivity(logFileName: AppSettings.Default.LogFileName, title: AppSettings.LogFileNameDefault);
                    }
                    else
                    {
                        throw new ArgumentOutOfRangeException();
                    }

                    OnUpdateLoggingSelectLogFileListSummary(_loggingUseFile.Checked);
                }
                else if (key == _loggingLevelsMultiSelectList?.Key)
                {
                    var values = e.NewValue.JavaCast<Java.Util.HashSet>();
                    if (values == null || values.IsEmpty)
                    {
                        e.Handled = false;
                        Toast.MakeText(Context, GetString(Resource.String.EmptyLoggingLevelsError),
                            AppSettings.ToastLength).Show();
                    }
                }

                #endregion logger
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                Toast.MakeText(Context, GetString(Resource.String.InternalError), AppSettings.ToastLength).Show();
            }
        }

        #endregion OnPreferenceChanging

        #region ISharedPreferencesOnSharedPreferenceChangeListener.OnSharedPreferenceChanged

        void ISharedPreferencesOnSharedPreferenceChangeListener.OnSharedPreferenceChanged(ISharedPreferences sharedPreferences, string key)
        {
            if (_isActionAppwidGetConfigure || string.IsNullOrEmpty(key))
                return;

            bool ExistsHandler(IDictionary<string, bool> keys)
            {
                return keys != null && (keys.ContainsKey(key) || keys.Any(p => !p.Value && key.StartsWith(p.Key)));
            }

            if (ExistsHandler(_excludeKeys))
                return;

            if (AppSettings.Default.ChangeStatus.HasFlag(SettingsChangeStatus.LogChanged) || ExistsHandler(_logKeys))
            {
                if (!AppSettings.Default.ChangeStatus.HasFlag(SettingsChangeStatus.LogChanged))
                    AppSettings.Default.ChangeStatus |= SettingsChangeStatus.LogChanged;
                return;
            }

            if (!AppSettings.Default.ChangeStatus.HasFlag(SettingsChangeStatus.Changed))
                AppSettings.Default.ChangeStatus |= SettingsChangeStatus.Changed;
        }

        #endregion ISharedPreferencesOnSharedPreferenceChangeListener.OnSharedPreferenceChanged

        #region OnUpdate

        private void SetUseTrackCurrentLocationSwitch()
        {
            if (_useTrackCurrentLocationSwitch != null)
                _useTrackCurrentLocationSwitch.Checked = _appWidgetSettings.UseTrackCurrentLocation;
        }

        private void OnUpdateClockFormatSummary()
        {
            if (_appWidgetSettings == null)
                return;

            var format = _appWidgetSettings.GetHourFormat(ApPmFormat);
            var summary = DateTime.Now.ToString(format, AppSettings.Default.CurrentCultureInfo);
            if (_hourFormatSwitch != null)
                _hourFormatSwitch.Summary = summary;
            if (_extendedHourFormatSwitch != null)
                _extendedHourFormatSwitch.Summary = summary;
        }

        private void OnBindViewHolderAppWidget(object sender, EventArgs eventArgs)
        {
            try
            {
                var viewAppWidget = AppWidgetFindViewById();
                if (viewAppWidget == null)
                    return;

                using (var wallpaperManager = WallpaperManager.GetInstance(Context))
                {
                    var wallpaperDrawable = wallpaperManager?.Drawable;
                    if (wallpaperDrawable != null)
                        viewAppWidget.Background = wallpaperDrawable;
                }

                OnUpdateAppWidget(viewAppWidget);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        private void OnUpdateAppWidget(ViewGroup viewGroup)
        {
            if (_appWidgetSettings == null || _appWidgetUpdater == null)
                return;

            if (viewGroup == null)
            {
                viewGroup = AppWidgetFindViewById();
                if (viewGroup == null)
                    return;
            }

            var remoteView = _appWidgetUpdater.BuildRemoteViews(Context, _appWidgetSettings.WidgetId, true);
            
            //BUG: Application.Context use for unthrow: android.support.v7.widget.AppCompatImageView can't use method with RemoteViews: setBackgroundResource(int)
            var view = remoteView.Apply(Application.Context, viewGroup);

            viewGroup.RemoveAllViews();
            viewGroup.AddView(view);

            var landOrientation = AppSettings.Default.LandOrientation;
            var height = (int) TypedValue.ApplyDimension(ComplexUnitType.Dip,
                _appWidgetUpdater.GetHeightDp(landOrientation), Resources.DisplayMetrics);
            var width = (int) TypedValue.ApplyDimension(ComplexUnitType.Dip,
                _appWidgetUpdater.GetWidthDp(landOrientation), Resources.DisplayMetrics);
            var layoutParams = new LinearLayout.LayoutParams(width, height);
            view.LayoutParameters = layoutParams;
            view.RequestLayout();
        }

        private ViewGroup AppWidgetFindViewById()
        {
            return (ViewGroup) _appWidgetView?.ViewHolder.FindViewById(Resource.Id.viewAppWidget);
        }

        private void OnUpdateLocationSummary()
        {
            if (_locationAddressScreen == null)
                return;

            _locationAddressScreen.Enabled = !_appWidgetSettings?.UseTrackCurrentLocation ?? false;
            var summary = _appWidgetSettings?.LocationAddress?.Address;
            _locationAddressScreen.Summary = string.IsNullOrEmpty(summary)
                ? GetString(Resource.String.UndefinedIt)
                : summary;
        }

        private void OnUpdateColdAlertedTemperatureSummary()
        {
            if (_coldAlertedTemperatureEditText == null)
                return;

            _coldAlertedTemperatureEditText.Summary =
                ConvertAlertedTemperatureToString(AppSettings.Default.ColdAlertedTemperature);
        }

        private void OnUpdateHotAlertedTemperatureSummary()
        {
            if (_hotAlertedTemperatureEditText == null)
                return;

            _hotAlertedTemperatureEditText.Summary =
                ConvertAlertedTemperatureToString(AppSettings.Default.HotAlertedTemperature);
        }

        private string ConvertAlertedTemperatureToString(double? temperature)
        {
            if (!temperature.HasValue)
                return GetString(Resource.String.UndefinedFemale);

            var tempunit = AppSettings.Default.TemperatureUnit;
            var temp = Units.ConvertTemperature(temperature.Value, TemperatureUnit.Celsius,
                tempunit);
            return
                string.Format("{0:f0}{1}", temp,
                    GetString(Resources.GetIdentifier(tempunit.ToString(), ResourceType.String,
                        Context.PackageName)));
        }

        private void OnUpdateLoggingSelectLogFileListSummary(bool useLogFile)
        {
            if (_loggingSelectLogFileList == null)
                return;

            var summary = string.Empty;
            if (useLogFile)
            {
                summary = AppSettings.Default.IsInternalLogFilePath() == true
                    ? Resources.GetStringArray(Resource.Array.LoggingSelectLogFileEntries).FirstOrDefault()
                    : AppSettings.Default.LogFileName;
            }

            _loggingSelectLogFileList.Summary = summary ?? string.Empty;
        }

        private void SetListPreferenceValues(ListPreference listPreference, Type enumType, string defaultValue)
        {
            if (listPreference == null || enumType == null)
                return;

            var values = Enum.GetNames(enumType).ToArray();
            var entries = new List<string>();
            var packageName = Context.PackageName;
            var defType = ResourceType.String;
            foreach (var value in values)
            {
                entries.Add(GetString(Resources.GetIdentifier(value, defType, packageName)));
            }
            listPreference.SetEntryValues(values);
            listPreference.SetEntries(entries.ToArray());

            if (listPreference.Value == null && !string.IsNullOrEmpty(defaultValue))
                listPreference.Value = defaultValue;
        }

        #endregion OnUpdate

        private void SaveNotPersistentPreference(bool needAppWidgetUpdate, bool needAppWidgetWeatherDataUpdate = false)
        {
            AppSettings.Default.SaveAppWidgetSettings(_appWidgetSettings);

            if (needAppWidgetUpdate)
                AppSettings.Default.ChangeStatus |= SettingsChangeStatus.NeedAppWidgetUpdate;

            if (needAppWidgetWeatherDataUpdate)
                AppSettings.Default.ChangeStatus |= SettingsChangeStatus.NeedAppWidgetWeatherDataUpdate;
        }

        private void RestoreChangeStatusParams()
        {
            if (AppSettings.Default.ChangeStatus.HasFlag(SettingsChangeStatus.Skip))
                AppSettings.Default.ChangeStatus ^= SettingsChangeStatus.Skip;
        }

        private void SaveChangeStatusParams()
        {
            if (!AppSettings.Default.ChangeStatus.HasFlag(SettingsChangeStatus.Skip))
                AppSettings.Default.ChangeStatus |= SettingsChangeStatus.Skip;
        }

        private void StartSelectLogFileActivity(string logFileName, string title)
        {
            //var intent = new Intent(Intent.ActionCreateDocument);
            //intent.AddCategory(Intent.CategoryOpenable);
            //intent.SetType("text/plain");
            //intent.PutExtra(Intent.ExtraTitle, AppSettings.LogFileNameDefault);

            var intent = new Intent(Context, typeof(FileBrowserActivity));
            intent.SetAction(Intent.ActionCreateDocument);

            if (!string.IsNullOrEmpty(logFileName))
                intent.PutExtra(FileBrowserActivity.ExtraSearchQuery, logFileName);

            intent.AddCategory(Intent.CategoryOpenable);

            if (!string.IsNullOrEmpty(title))
                intent.PutExtra(Intent.ExtraTitle, title);

            StartActivityForResult(intent, RequestCodeCreateLogFile);
        }

        private void SubscribeEvents()
        {
            UnsubscribeEvents();
            if (!_isActionAppwidGetConfigure)
                PreferenceScreen?.SharedPreferences.RegisterOnSharedPreferenceChangeListener(this);
            if (_weatherProviderServerList != null)
                _weatherProviderServerList.PreferenceChange += OnPreferenceChanging;
            if (_useGoogleMapsGeocodingApiSwitch != null)
                _useGoogleMapsGeocodingApiSwitch.PreferenceChange += OnPreferenceChanging;
            if (_useTrackCurrentLocationSwitch != null)
                _useTrackCurrentLocationSwitch.PreferenceChange += OnPreferenceChanging;
            if (_coldAlertedTemperatureEditText != null)
                _coldAlertedTemperatureEditText.PreferenceChange += OnPreferenceChanging;
            if (_hotAlertedTemperatureEditText != null)
                _hotAlertedTemperatureEditText.PreferenceChange += OnPreferenceChanging;
            if (_loggingUseFile != null)
                _loggingUseFile.PreferenceChange += OnPreferenceChanging;
            if (_loggingSelectLogFileList != null)
                _loggingSelectLogFileList.PreferenceChange += OnPreferenceChanging;
            if (_loggingLevelsMultiSelectList != null)
                _loggingLevelsMultiSelectList.PreferenceChange += OnPreferenceChanging;
            if (_appWidgetIconStyleList != null)
                _appWidgetIconStyleList.PreferenceChange += OnPreferenceChanging;
            if (_appWidgetBackgroundStyleList != null)
                _appWidgetBackgroundStyleList.PreferenceChange += OnPreferenceChanging;
            if (_appWidgetOpacity != null)
                _appWidgetOpacity.PreferenceChange += OnPreferenceChanging;
            if (_hourFormatSwitch != null)
                _hourFormatSwitch.PreferenceChange += OnPreferenceChanging;
            if (_extendedHourFormatSwitch != null)
                _extendedHourFormatSwitch.PreferenceChange += OnPreferenceChanging;
            if (_dateFormatList != null)
                _dateFormatList.PreferenceChange += OnPreferenceChanging;
            if (_clockTextSizeSeekBar != null)
                _clockTextSizeSeekBar.PreferenceChange += OnPreferenceChanging;
            if (_refreshIntervalList != null)
                _refreshIntervalList.PreferenceChange += OnPreferenceChanging;
            if (_appWidgetView != null)
                _appWidgetView.BindViewHolder += OnBindViewHolderAppWidget;
        }

        private void UnsubscribeEvents()
        {
            PreferenceScreen?.SharedPreferences.UnregisterOnSharedPreferenceChangeListener(this);
            if (_weatherProviderServerList != null)
                _weatherProviderServerList.PreferenceChange -= OnPreferenceChanging;
            if (_useGoogleMapsGeocodingApiSwitch != null)
                _useGoogleMapsGeocodingApiSwitch.PreferenceChange -= OnPreferenceChanging;
            if (_useTrackCurrentLocationSwitch != null)
                _useTrackCurrentLocationSwitch.PreferenceChange -= OnPreferenceChanging;
            if (_coldAlertedTemperatureEditText != null)
                _coldAlertedTemperatureEditText.PreferenceChange -= OnPreferenceChanging;
            if (_hotAlertedTemperatureEditText != null)
                _hotAlertedTemperatureEditText.PreferenceChange -= OnPreferenceChanging;
            if (_loggingUseFile != null)
                _loggingUseFile.PreferenceChange -= OnPreferenceChanging;
            if (_loggingSelectLogFileList != null)
                _loggingSelectLogFileList.PreferenceChange -= OnPreferenceChanging;
            if (_loggingLevelsMultiSelectList != null)
                _loggingLevelsMultiSelectList.PreferenceChange -= OnPreferenceChanging;
            if (_appWidgetIconStyleList != null)
                _appWidgetIconStyleList.PreferenceChange -= OnPreferenceChanging;
            if (_appWidgetBackgroundStyleList != null)
                _appWidgetBackgroundStyleList.PreferenceChange -= OnPreferenceChanging;
            if (_appWidgetOpacity != null)
                _appWidgetOpacity.PreferenceChange -= OnPreferenceChanging;
            if (_hourFormatSwitch != null)
                _hourFormatSwitch.PreferenceChange -= OnPreferenceChanging;
            if (_extendedHourFormatSwitch != null)
                _extendedHourFormatSwitch.PreferenceChange -= OnPreferenceChanging;
            if (_dateFormatList != null)
                _dateFormatList.PreferenceChange -= OnPreferenceChanging;
            if(_clockTextSizeSeekBar != null)
                _clockTextSizeSeekBar.PreferenceChange -= OnPreferenceChanging;
            if (_refreshIntervalList != null)
                _refreshIntervalList.PreferenceChange -= OnPreferenceChanging;
            if (_appWidgetView != null)
                _appWidgetView.BindViewHolder -= OnBindViewHolderAppWidget;
        }
    }
}