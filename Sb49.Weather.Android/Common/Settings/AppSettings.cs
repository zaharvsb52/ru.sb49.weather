using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading;
using System.Xml;
using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Content.PM;
using Android.Gms.Common;
using Android.Gms.Location;
using Android.Locations;
using Android.Support.V4.Provider;
using Android.Widget;
using Java.Util;
using Microsoft.Practices.Unity;
using Newtonsoft.Json;
using Sb49.Common.Logging;
using Sb49.Weather.Droid.Common.Crypto;
using Sb49.Weather.Droid.Model;
using Sb49.Geocoder.Core;
using Sb49.Security.Core;
using Sb49.Weather.Core;
using Sb49.Weather.Droid.Common.Json;
using Sb49.Weather.Droid.Ui.AppWidget.Updaters.Core;
using Sb49.Weather.Model;

namespace Sb49.Weather.Droid.Common
{
    internal sealed class AppSettings : IAppSettings
    {
        //HARDCODE:
        private const string CompanyNameShort = "sb49";
        private const string ProductNameShort = "weather";

        public const string SharedPreferencesFileName =
            CompanyNameShort + "_" + ProductNameShort + "_settings_shared_preferences";
        public const string TwilightCacheKey = CompanyNameShort + "_" + ProductNameShort + "_twilight_cache";

        //HARDCODE:
        public const string Tag = CompanyNameShort + "." + ProductNameShort;
        public const string AppPackageName = "ru." + Tag;

        public const int SchedulerDelayMsec = 20000;
        public const int SchedulerPeriodMsec = ConvertMinutesToMSec;
        public const int SchedulerGcDelayMsec = 15 * ConvertMinutesToMSec;

        public const int WaitMaxMsec = 5 * ConvertMinutesToMSec;
        public const int WaitGeoMaxMsec = 2 * ConvertMinutesToMSec;

        public const int LocationDeltaMeters = 5000;

        public const int WaitActivitySleepMsec = 100;

        public const int ConvertMinutesToMSec = 60000;

        public const int AddressCurrentLocationId = 0;
        public const int GoogleMapsGeocodingApiProviderId = 1001;

        public const ToastLength ToastLength = Android.Widget.ToastLength.Long;

        public const string TempFolder = "temp";
        public const string LogFilesFolder = "logs";
        public const string LogFileNameDefault = "log.txt";

        public const string WeatherConditionIconTheme = "_white";

        private readonly IDictionary<int, Model.Provider> _providers;
        private Lazy<IGeocoder> _geocoder;

        private Lazy<IDictionary<int, ISb49SecureString>> _cacheApiKeys;
        private Lazy<CurrentLocation> _cacheCurrentLocation;
        private Lazy<IDictionary<int, LocationAddress>> _cacheLocations;
        private Lazy<WeatherForecast> _cacheWeatherForecast;
        private Lazy<IDictionary<int, AppWidgetSettings>> _cacheAppWidgetSettings;
        
        private readonly IUnityContainer _container;
        private ILog _log = null;

        private AppSettings(IUnityContainer container, IDictionary<int, Model.Provider> providers,
            int defaultWeatherProviderId, string[] appWidgetProviderClassNames)
        {
            IsDisposed = false;
            _container = container;
            //SharedPreferences = PreferenceManager.GetDefaultSharedPreferences(Application.Context);
            SharedPreferences = AppContext.GetSharedPreferences(SharedPreferencesFileName, FileCreationMode.Private);
            _providers = providers;
            _geocoder = new Lazy<IGeocoder>(() => _container.Resolve<IGeocoder>(), LazyThreadSafetyMode.PublicationOnly);
            WeatherProviderIdDefault = defaultWeatherProviderId;
            ValidateWeatherProviderIdDefault();
            AppWidgetProviderClassNames = appWidgetProviderClassNames;
        }

        ~AppSettings()
        {
            OnDispose();
        }

        public static AppSettings Default { get; private set; } = null;
        public Context AppContext => Application.Context;

        public ISharedPreferences SharedPreferences { get; }
        public IGeocoder Geocoder => _geocoder.Value;

        public string Language => Locale.Default.Language;
        public string VersionName => GetPackageInfo()?.VersionName;
        public int VersionCode => GetPackageInfo().VersionCode;

        public string[] AppWidgetProviderClassNames { get; }

        public CultureInfo CurrentCultureInfo
        {
            get
            {
                var androidLocale = Locale.Default;
                CultureInfo cultureInfo;
                void SetDefaultCultureInfoHandler() => cultureInfo = CultureInfo.CurrentUICulture;

                try
                {
                    //var netLocale = androidLocale.ToString().Replace("_", "-");
                    cultureInfo =
                        CultureInfo.GetCultureInfo(string.Format("{0}-{1}", androidLocale.Language,
                            androidLocale.Country));
                }
                catch (Exception ex)
                {
                    _log.Error(ex);
                    SetDefaultCultureInfoHandler();
                }

                if (cultureInfo == null)
                    SetDefaultCultureInfoHandler();

                return cultureInfo;
            }
        }

        public bool LandOrientation => GetBooleanById(Resource.Boolean.Land);
        public bool IsDisposed { get; private set; }

        #region Locations

        public bool UseTrackCurrentLocation
        {
            get => GetValue(Resource.String.UseTrackCurrentLocationKey,
                GetBooleanById(Resource.Boolean.UseTrackCurrentLocationDefault));
            set
            {
                using (var editor = SharedPreferences.Edit())
                {
                    editor.PutBoolean(GetStringById(Resource.String.UseTrackCurrentLocationKey), value);
                    editor.Commit();
                }
            }
        }

        public int SelectedLocationAddressId
        {
            get
            {
                if (UseTrackCurrentLocation)
                    return AddressCurrentLocationId;

                var result = GetValue(Resource.String.SelectedLocationAddressIdKey, AddressCurrentLocationId);
                var locations = GetLocations();
                if (locations == null || locations.Count == 0 || !locations.ContainsKey(result))
                {
                    UseTrackCurrentLocation = true;
                    return AddressCurrentLocationId;
                }

                return result;
            }
            set
            {
                var result = UseTrackCurrentLocation ? AddressCurrentLocationId : value;
                using (var editor = SharedPreferences.Edit())
                {
                    editor.PutInt(GetStringById(Resource.String.SelectedLocationAddressIdKey), result);
                    editor.Commit();
                }
            }
        }

        public LocationAddress LocationAddress
        {
            get
            {
                var locations = GetLocations();
                if (locations == null)
                    return null;

                var result = locations.ContainsKey(SelectedLocationAddressId)
                    ? locations[SelectedLocationAddressId]
                    : null;
                return result;
            }
            set
            {
                var index = SaveLocationAddress(value);
                SelectedLocationAddressId = index;
            }
        }

        public int SaveLocationAddress(LocationAddress value, int? key = null)
        {
            var locations = GetLocations();
            if (locations == null)
                throw new ArgumentNullException(nameof(locations));

            int index;
            if (key.HasValue && locations.ContainsKey(key.Value))
            {
                index = key.Value;
                locations[index] = value;
            }
            else
            {
                if (locations.Count > 1)
                {
                    var distinctLocations = new ConcurrentDictionary<int, LocationAddress>();
                    var addresses = locations.Where(p =>
                            p.Key != AddressCurrentLocationId && p.Value != null &&
                            !string.IsNullOrEmpty(p.Value.Locality) && p.Value.IsValid())
                        .Select(p => p.Value).Distinct(new LocationAddressEqualityComparer()).ToArray();
                    if (locations.ContainsKey(AddressCurrentLocationId))
                        distinctLocations[AddressCurrentLocationId] = locations[AddressCurrentLocationId];
                    var id = AddressCurrentLocationId + 1;
                    foreach (var address in addresses)
                    {
                        distinctLocations[id++] = address;
                    }

                    locations.Clear();
                    foreach (var pair in distinctLocations)
                    {
                        pair.Value.Id = pair.Key;
                        locations[pair.Key] = pair.Value;
                    }
                }

                index = GetLocationAddressId(UseTrackCurrentLocation, value);
                locations[index] = value;
            }

            SaveObject(key: GetStringById(Resource.String.LocationAddressKey), value: locations,
                providerType: ProviderType.SimpleZip);
            return index;
        }

        public void DeleteLocationAddress(int key)
        {
            var locations = GetLocations();
            if (locations == null)
                throw new ArgumentNullException(nameof(locations));

            if (!locations.ContainsKey(key))
                return;

            var value = locations[key];
            value?.Dispose();
            locations.Remove(key);

            SaveObject(key: GetStringById(Resource.String.LocationAddressKey), value: locations,
                providerType: ProviderType.SimpleZip);
        }

        public IDictionary<int, LocationAddress> GetLocations()
        {
            if (_cacheLocations?.Value != null)
                return _cacheLocations.Value;

            IDictionary<int, LocationAddress> locations =
                GetObject<Dictionary<int, LocationAddress>>(key: GetStringById(Resource.String.LocationAddressKey),
                    providerType: ProviderType.SimpleZip);
            locations = locations == null
                ? new ConcurrentDictionary<int, LocationAddress>()
                : new ConcurrentDictionary<int, LocationAddress>(locations);

            InitCacheLocations(locations);
            return locations;
        }

        public bool ValidateLocationSettings()
        {
            using (var locationManager = (LocationManager) AppContext.GetSystemService(Context.LocationService))
            {
                var criteriaForLocationService = new Criteria
                {
                    Accuracy = Accuracy.Fine
                };
                var acceptableLocationProviders = locationManager.GetProviders(criteriaForLocationService, true);
                return acceptableLocationProviders.Any();
            }
        }

        public bool CheckIsGooglePlayServicesInstalled()
        {
            return CheckIsGooglePlayServicesInstalled(out int _);
        }

        public bool CheckIsGooglePlayServicesInstalled(out int errorCode)
        {
            errorCode = GoogleApiAvailability.Instance.IsGooglePlayServicesAvailable(AppContext);
            if (errorCode == ConnectionResult.Success)
                return true;

            var errorString = GoogleApiAvailability.Instance.GetErrorString(errorCode);
            _log.ErrorFormat("There is a problem with Google Play Services on this device: '{0}' (Code = '{1}').",
                errorString, errorCode);

            return false;
        }

        public int GetLocationAddressId(bool useTrackCurrentLocation, LocationAddress locationAddress)
        {
            if (useTrackCurrentLocation)
                return AddressCurrentLocationId;

            if (locationAddress == null)
                throw new ArgumentNullException(nameof(locationAddress));

            var locations = GetLocations();
            var exists =
                locations.Where(p => p.Value != null && p.Key != AddressCurrentLocationId)
                    .Select(p => new {p.Key, p.Value})
                    .FirstOrDefault(p => p.Value.Equals(locationAddress));
            if (exists != null)
                return exists.Key;

            var maxIndex = (locations.Count == 0 ? AddressCurrentLocationId : locations.Max(p => p.Key)) + 1;
            return maxIndex;
        }

        public CurrentLocation CurrentLocation
        {
            get
            {
                if (_cacheCurrentLocation?.Value != null)
                    return _cacheCurrentLocation.Value;

                var result = GetObject<CurrentLocation>(key: GetStringById(Resource.String.CurrentLocationKey), providerType: ProviderType.Simple);
                InitCacheCurrentLocations(result);
                return result;
            }
            set
            {
                SaveObject(key: GetStringById(Resource.String.CurrentLocationKey), value: value,
                    providerType: ProviderType.Simple);
                ClearCacheCurrentLocations();
            }
        }

        #endregion Locations

        #region Units

        public TemperatureUnit TemperatureUnit
        {
            get
            {
                var result = GetEnum(Resource.String.TemperatureUnitKey,
                    TemperatureUnit.Celsius);
                return result;
            }
        }

        public SpeedUnit WindSpeedUnit
        {
            get
            {
                var result = GetEnum(Resource.String.WindSpeedUnitKey, SpeedUnit.MeterPerSec);
                return result;
            }
        }

        public PressureUnit PressureUnit
        {
            get
            {
                var result = GetEnum(Resource.String.PressureUnitKey, PressureUnit.MmHg);
                return result;
            }
        }

        public DistanceUnit VisibilityUnit
        {
            get
            {
                var result = GetEnum(Resource.String.VisibilityUnitKey, DistanceUnit.Kilometer);
                return result;
            }
        }

        public Units Units => new Units
        {
            TemperatureUnit = TemperatureUnit,
            WindSpeedUnit = WindSpeedUnit,
            PressureUnit = PressureUnit,
            VisibilityUnit = VisibilityUnit
        };

        #endregion Units

        #region Weather provider

        public int WeatherProviderIdDefault { get; }

        public int WeatherProviderId
        {
            get
            {
                var value = GetArrayValue(Resource.String.WeatherProviderServerKey, null, WeatherProviderIdDefault);
                return value;
            }
            set
            {
                ValidateWeatherProviderId(value);
                SaveObject(GetStringById(Resource.String.WeatherProviderServerKey), value);
            }
        }

        public int WeatherProviderNameId => GetWeatherProviderNameById(WeatherProviderId);

        public ISb49SecureString WeatherApiKey => GetApiKey(WeatherProviderId);

        public IWeatherProviderService WeatherProviderService => GetWeatherProviderService(WeatherProviderId);

        public int WeatherServiceTimerPeriodMinimumMsec
        {
            get
            {
                if (!int.TryParse(GetStringById(Resource.String.WeatherRefreshIntervalValueMinimum), out int result))
                    return Timeout.Infinite;
                return result <= 0 ? Timeout.Infinite : result * ConvertMinutesToMSec;
            }
        }

        public WeatherForecast Weather
        {
            get
            {
                if (_cacheWeatherForecast?.Value != null)
                    return _cacheWeatherForecast.Value;

                var result = GetObject<WeatherForecast>(key: GetStringById(Resource.String.WeatherDataKey), providerType: ProviderType.SimpleZip);
                InitWeatherForecast(result);
                return result;
            }
            set
            {
                SaveObject(key: GetStringById(Resource.String.WeatherDataKey), value: value, providerType: ProviderType.SimpleZip);
                ClearWeatherForecast();
            }
        }

        public void ValidateWeatherProviderId(int providerId)
        {
            var ids = GetWeatherProviderIds();
            if (!ids.Contains(providerId))
                throw new ArgumentOutOfRangeException();
        }

        public IWeatherProviderService GetWeatherProviderService(int providerId)
        {
            var factory = GetWeatherProviderServiceFactory();
            var result = factory.GetProvider(providerId);
            return result;
        }

        public int GetWeatherProviderNameById(int providerId)
        {
            ValidateWeatherProviderList();
            if (_providers.ContainsKey(providerId))
            {
                var provider = _providers[providerId];
                if (provider.ProviderType == ProviderTypes.WeatherProvider)
                    return provider.TitleId;
            }

            throw new ArgumentOutOfRangeException();
        }

        public int[] GetWeatherProviderIds()
        {
            ValidateWeatherProviderList();
            return
                GetProviders().Where(p => p.ProviderType == ProviderTypes.WeatherProvider).Select(p => p.Id).ToArray();
        }

        public bool ValidateWeatherProvider(int providerId)
        {
            var factory = GetWeatherProviderServiceFactory();
            return factory.Exists(providerId);
        }

        public int ConvertToWeatherServiceTimerPeriod(int value)
        {
            if (value <= 0)
                return Timeout.Infinite;

            var result = value * ConvertMinutesToMSec;
            return result < WeatherServiceTimerPeriodMinimumMsec ? Timeout.Infinite : result;
        }

        private volatile bool _weatherDataUpdating;

        public bool WeatherDataUpdating => _weatherDataUpdating;

        void IAppSettings.BeginWeatherDataUpdate()
        {
            _weatherDataUpdating = true;
        }

        void IAppSettings.EndWeatherDataUpdate()
        {
            _weatherDataUpdating = false;
        }

        #endregion Weather provider

        #region Alert

        public double? ColdAlertedTemperature
        {
            get => GetAlertTemperature(Resource.String.ColdAlertedTemperatureKey);
            set => SaveAlertTemperature(Resource.String.ColdAlertedTemperatureKey, value);
        }

        public double? HotAlertedTemperature
        {
            get => GetAlertTemperature(Resource.String.HotAlertedTemperatureKey);
            set => SaveAlertTemperature(Resource.String.HotAlertedTemperatureKey, value);
        }

        #endregion Alert

        #region AppWidget

        int IAppSettings.WidgetId => AppWidgetManager.InvalidAppwidgetId;
        bool IAppSettings.IsNotAppWidget => true;

        public AppWidgetSettings CreateAppWidgetSettings(int widgetId)
        {
            var result = new AppWidgetSettings(widgetId);
            return result;
        }

        public AppWidgetSettings FindAppWidgetSettings(int widgetId)
        {
            var settings = GetAppWidgetSettings();
            var result = settings != null && settings.ContainsKey(widgetId) ? settings[widgetId] : null;
            return result;
        }

        public void SaveAppWidgetSettings(IAppSettings value)
        {
            if (value == null || value.WidgetId == AppWidgetManager.InvalidAppwidgetId || value.IsNotAppWidget)
                return;

            var appWidgetSettings = value as AppWidgetSettings;
            if (appWidgetSettings == null)
                return;

            var settings = GetAppWidgetSettings();
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            settings[appWidgetSettings.WidgetId] = appWidgetSettings;
            SaveAppWidgetSettings(settings);
        }

        public void SaveAppWidgetSettings(IDictionary<int, AppWidgetSettings> value)
        {
            var settings = value ?? new ConcurrentDictionary<int, AppWidgetSettings>();
            SaveObject(key: GetStringById(Resource.String.AppWidgetSettingsCollectionKey), value: settings, providerType: ProviderType.SimpleZip);
        }

        public void DeleteAppWidgetSettings(int[] widgetIds)
        {
            var settings = GetAppWidgetSettings();
            foreach (var widgetId in widgetIds.Where(p => settings.ContainsKey(p)))
            {
                var value = settings[widgetId];
                value?.Dispose();
                settings.Remove(widgetId);
            }

            SaveAppWidgetSettings(settings);
        }

        public IDictionary<int, AppWidgetSettings> GetAppWidgetSettings()
        {
            if (_cacheAppWidgetSettings?.Value != null)
                return _cacheAppWidgetSettings.Value;

            IDictionary<int, AppWidgetSettings> result =
                GetObject<Dictionary<int, AppWidgetSettings>>(
                    key: GetStringById(Resource.String.AppWidgetSettingsCollectionKey),
                    providerType: ProviderType.SimpleZip);

            result = result == null
                ? new ConcurrentDictionary<int, AppWidgetSettings>()
                : new ConcurrentDictionary<int, AppWidgetSettings>(result);

            InitCacheAppWidgetSettings(result);
            return result;
        }

        public IAppWidgetUpdater AppWidgetUpdaterFactory(string appWidgetProviderClassName)
        {
            if(string.IsNullOrEmpty(appWidgetProviderClassName))
                throw new ArgumentNullException(nameof(appWidgetProviderClassName));

            var updater = _container.IsRegistered<IAppWidgetUpdater>(appWidgetProviderClassName)
                ? _container.Resolve<IAppWidgetUpdater>(appWidgetProviderClassName)
                : null;

            if (updater == null)
            {
                throw new NotImplementedException(string.Format("Updater not defined for AppWidgetProvider class name '{0}'.",
                    appWidgetProviderClassName));
            }

            return updater;
        }

        #endregion AppWidget

        #region ApiKey

        public ISb49SecureString GetApiKey(int providerId)
        {
            var keys = GetApiKeys();
            return keys != null && keys.ContainsKey(providerId) ? keys[providerId] : null;
        }

        public IDictionary<int, ISb49SecureString> GetApiKeys(Android.Net.Uri uri = null)
        {
            if (uri != null)
                ClearCacheApiKeys();

            if (_cacheApiKeys?.Value != null)
                return _cacheApiKeys.Value;

            var keys = GetObject<Dictionary<int, ISb49SecureString>>(key: GetStringById(Resource.String.WeatherApiKey),
                uri: uri, providerType: ProviderType.Default, converters: GetSecureStringConverter());

            IDictionary<int, ISb49SecureString> result;
            if (keys == null)
            {
                result = new ConcurrentDictionary<int, ISb49SecureString>
                {
                    [WeatherProviderIdDefault] =
                    _container.Resolve<ISb49SecureString>(new DependencyOverride(typeof(string),
                        "c09d5ddewq1456asdf6498d910e2a63cf8e3560=="))
                };
            }
            else
            {
                result = new ConcurrentDictionary<int, ISb49SecureString>(keys);
            }

            InitCacheApiKeys(result);
            return result;
        }

        public void SaveApiKey(int providerId, string src)
        {
            var keys = GetApiKeys();
            if (keys != null)
            {
                keys[providerId] = _container.Resolve<ISb49SecureString>(new DependencyOverride(typeof(string), src));
            }
            SaveApiKeys(keys);
        }

        public bool ValidateApiKey(int providerId)
        {
            var keys = GetApiKeys();
            return keys != null && keys.ContainsKey(providerId) && keys[providerId].Validate();
        }

        public void SaveApiKeys(IDictionary<int, ISb49SecureString> apiKeys, Android.Net.Uri uri = null)
        {
            var day = DateTime.Today.Day;
            var providerType = day % 2 == 0 ? ProviderType.Aes001 : ProviderType.Des001;

            SaveObject(key: GetStringById(Resource.String.WeatherApiKey), value: apiKeys, uri: uri,
                providerType: providerType, converters: GetSecureStringConverter());

            ClearCacheApiKeys();
        }

        #endregion ApiKey

        #region RequestCounter

        public RequestCounter GetRequestCounter(int providerId)
        {
            var result = GetObject(GetRequestCounterKey(providerId), new RequestCounter(providerId));
            return result;
        }

        public void SaveRequestCounter(int providerId, RequestCounter value)
        {
            SaveObject(GetRequestCounterKey(providerId), value);
        }

        #endregion RequestCounter

        #region provider

        public Model.Provider[] GetProviders()
        {
            ValidateProviderList();
            return _providers.Values.Where(p => p != null).ToArray();
        }

        public int GetProviderNameById(int providerId)
        {
            ValidateProviderList();
            if (_providers.ContainsKey(providerId))
            {
                var provider = _providers[providerId];
                return provider.TitleId;
            }

            throw new ArgumentOutOfRangeException();
        }

        #endregion provider

        #region GeoTracking

        public int GeoTrackingServicePriorityDefault
            => LocationRequest.PriorityBalancedPowerAccuracy;

        public int GeoTrackingServicePriority
        {
            get
            {
                var result = GetArrayValue(Resource.String.GeoTrackingServicePriorityKey, null,
                    GeoTrackingServicePriorityDefault);
                return result;
            }
        }

        public int GeoTrackingServiceFastestIntervalMsec => GeoTrackingServiceIntervalMsec / 6;

        public int GeoTrackingServiceIntervalMsec
        {
            get
            {
                var result = GetValue(Resource.String.GeoTrackingServiceIntervalKey,
                    GetIntegerById(Resource.Integer.GeoTrackingServiceIntervalDefault));
                return result * ConvertMinutesToMSec;
            }
        }

        public int GeoTrackingServiceSmallestDisplacementMeters
        {
            get
            {
                var result = GetValue(Resource.String.GeoTrackingServiceSmallestDisplacementKey,
                    GetIntegerById(Resource.Integer.GeoTrackingServiceSmallestDisplacementDefault));
                return result;
            }
        }

        #endregion GeoTracking

        #region Google Api

        public bool UseGoogleMapsGeocodingApi
        {
            get
            {
                var result = GetValue(Resource.String.UseGoogleMapsGeocodingApiKey,
                    GetBooleanById(Resource.Boolean.UseGoogleMapsGeocodingApiDefault));
                return result;
            }
            set
            {
                using (var editor = SharedPreferences.Edit())
                {
                    editor.PutBoolean(GetStringById(Resource.String.UseGoogleMapsGeocodingApiKey), value);
                    editor.Commit();
                }
            }
        }

        #endregion Google Api

        #region logger

        public bool UseAndroidLog
        {
            get
            {
                var result = GetValue(Resource.String.LoggingUseAndroidLogKey,
                    GetBooleanById(Resource.Boolean.LoggingUseAndroidLogDefault));
                return result;
            }
        }

        public bool LoggingUseFile
        {
            get
            {
                var result = GetValue(Resource.String.LoggingUseFileKey,
                    GetBooleanById(Resource.Boolean.LoggingUseFileDefault));
                return result;
            }
        }

        public ICollection<string> LoggingLevels =>
            SharedPreferences.GetStringSet(GetStringById(Resource.String.LoggingLevelsKey),
                AppContext.Resources.GetStringArray(Resource.Array.LoggingLevelsValuesDefault));

        public string LogFileName
        {
            get
            {
                var result = GetValue<string>(Resource.String.LogFileNameKey);
                return result;
            }
            set
            {
                using (var editor = SharedPreferences.Edit())
                {
                    editor.PutString(GetStringById(Resource.String.LogFileNameKey), value);
                    editor.Commit();
                }
            }
        }

        public int LoggingMaximumFileSize
        {
            get
            {
                var result = GetValue(Resource.String.LoggingMaximumFileSizeKey,
                    GetIntegerById(Resource.Integer.LoggingMaximumFileSizeDefault));
                return result;
            }
        }

        public int LoggingMaxSizeRollBackups
        {
            get
            {
                var result = GetValue(Resource.String.LoggingMaxSizeRollBackupsKey,
                    GetIntegerById(Resource.Integer.LoggingMaxSizeRollBackupsDefault));
                return result;
            }
        }

        public bool IsLogFileExisted => LoggingUseFile && !string.IsNullOrEmpty(LogFileName);

        public bool? IsInternalLogFilePath()
        {
            if (string.IsNullOrEmpty(LogFileName))
                return null;

            return LogFileName.ToLower().StartsWith(AppContext.FilesDir.Path.ToLower());
        }

        public string CreateInternalLogFilePath()
        {
            var result = Path.Combine(AppContext.FilesDir.Path, LogFilesFolder, LogFileNameDefault);
            return result;
        }

        public void LoggingConfigure()
        {
            var logfilename = LogFileName.Length > 1 &&
                              LogFileName.StartsWith(Path.DirectorySeparatorChar.ToString())
                ? LogFileName.Substring(1)
                : LogFileName;

            string xmlTemplate;
            using (var reader = AppContext.Resources.GetXml(Resource.Xml.log4net))
            {
                var xmlDocument = new XmlDocument();
                xmlDocument.Load(reader);
                xmlTemplate = xmlDocument.InnerXml;
            }

            var loggingLevels = string.Join(Environment.NewLine,
                LoggingLevels.Select(p => string.Format(
                    @"<filter type=""log4net.Filter.LevelMatchFilter"">{0}<acceptOnMatch value=""true"" />{0}<levelToMatch value=""{1}"" />{0}</filter>",
                    Environment.NewLine,
                    p.ToUpper())));
            if (!string.IsNullOrEmpty(loggingLevels))
            {
                loggingLevels = string.Format("{0}{1}{0}{2}", Environment.NewLine,
                    loggingLevels,
                    @"<filter type=""log4net.Filter.DenyAllFilter"" />");
            }

            var xml = string.Format(xmlTemplate,
                loggingLevels,
                logfilename,
                LoggingMaxSizeRollBackups,
                LoggingMaximumFileSize,
                UseAndroidLog ? @"<appender-ref ref=""AndroidLogAppender"" />" : null,
                IsLogFileExisted ? @"<appender-ref ref=""RollingLogFileAppender"" />" : null);

            LogManager.Adapter.Configure(Tag, xml);
        }

        #endregion logger

        #region documents settings

        public bool ShowFileSize => GetValue(Resource.String.ShowFileSizeKey,
            GetBooleanById(Resource.Boolean.ShowFileSizeDefault));

        public bool ShowFolderSize => GetValue(Resource.String.ShowFolderSizeKey,
            GetBooleanById(Resource.Boolean.ShowFolderSizeDefault));

        #endregion documents settings

        #region BootCompletedReceiver

        public bool IsBootCompletedReceiver
        {
            get => GetValue(Resource.String.IsBootCompletedReceiverKey, false);
            set
            {
                using (var editor = SharedPreferences.Edit())
                {
                    editor.PutBoolean(GetStringById(Resource.String.IsBootCompletedReceiverKey), value);
                    editor.Commit();
                }
            }
        }

        #endregion BootCompletedReceiver

        #region SettingsChangeStatus

        public SettingsChangeStatus ChangeStatus { get; set; } = SettingsChangeStatus.None;

        #endregion SettingsChangeStatus

        #region public methods

        public void ClearCache()
        {
            ClearCacheApiKeys();
            ClearCacheCurrentLocations();
            ClearCacheLocations();
            ClearWeatherForecast();
            ClearCacheAppWidgetSettings();
        }

        public static void GcCollect(bool needCompactOnce = false, ILog log = null)
        {
            if (needCompactOnce)
            {
                try
                {
                    GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                }
                catch (Exception ex)
                {
                    log?.Debug(ex);
                }
            }

            try
            {
                GC.Collect();
                log?.DebugFormat("GC.Collect (LargeObjectHeapCompactionMode: '{0}').",
                    GCSettings.LargeObjectHeapCompactionMode);
            }
            catch (Exception ex)
            {
                log?.Debug(ex);
            }
        }

        public T GetValue<T>(int id, T defaultValue = default(T))
        {
            var key = GetStringById(id);
            return GetValue(key, defaultValue);
        }

        public T GetValue<T>(string key, T defaultValue = default(T))
        {
            var type = typeof(T);
            if (type == typeof(bool))
                return (T) (object) SharedPreferences.GetBoolean(key, Convert.ToBoolean(defaultValue));
            if (type == typeof(int))
                return (T) (object) SharedPreferences.GetInt(key, Convert.ToInt32(defaultValue));
            if (type == typeof(string))
                return (T) (object) SharedPreferences.GetString(key, Convert.ToString(defaultValue));
            if (type == typeof(string[]))
                return (T) SharedPreferences.GetStringSet(key, defaultValue as ICollection<string>);

            throw new NotImplementedException();
        }

        public string GetStringById(int id)
        {
            return AppContext.GetString(id);
        }

        public bool GetBooleanById(int id)
        {
            return AppContext.Resources.GetBoolean(id);
        }

        public int GetIntegerById(int id)
        {
            return AppContext.Resources.GetInteger(id);
        }

        public int GetArrayValue(int id, int? defaultid, int defaultvalue)
        {
            var indexStr = GetValue<string>(id);
            if (defaultid.HasValue && (string.IsNullOrEmpty(indexStr) || !int.TryParse(indexStr, out int intvalue)))
                indexStr = GetStringById(defaultid.Value);

            if (int.TryParse(indexStr, out intvalue))
                return intvalue;

            return defaultvalue;
        }

        #endregion public methods

        #region privet methods

        private IWeatherProviderServiceFactory GetWeatherProviderServiceFactory()
        {
            return _container.Resolve<IWeatherProviderServiceFactory>();
        }

        private ISb49Crypto GetCrypto(ProviderType providerType)
        {
            var crypto = _container.Resolve<ISb49Crypto>();
            crypto.ProviderType = providerType;
            return crypto;
        }

        private T GetObject<T>(string key, T defaultValue = null, Android.Net.Uri uri = null, ProviderType providerType = ProviderType.None,
            params JsonConverter[] converters)
            where T : class
        {
            string buffer;
            if (uri == null)
            {
                buffer = GetValue<string>(key);
            }
            else
            {
                using (var documentFile = DocumentFile.FromSingleUri(AppContext, uri))
                {
                    if (documentFile.Exists())
                    {
                        using (var stream = AppContext.ContentResolver.OpenInputStream(uri))
                        {
                            using (var reader = new StreamReader(stream, Encoding.UTF8))
                            {
                                buffer = reader.ReadToEnd();
                                reader.Close();
                            }

                            stream.Close();
                        }
                    }
                    else
                    {
                        return defaultValue;
                    }
                }
            }

            if (string.IsNullOrEmpty(buffer))
                return defaultValue;

            var result = DeserializeObject<T>(buffer, providerType, converters);
            if (result == null)
                return defaultValue;
            return result;
        }

        private void SaveObject(string key, object value, Android.Net.Uri uri = null,
            ProviderType providerType = ProviderType.None, params JsonConverter[] converters)
        {
            string buffer;
            if (value == null)
            {
                buffer = string.Empty;
            }
            else
            {
                var type = value.GetType();
                if (type.IsPrimitive || type == typeof(string))
                {
                    buffer = value.ToString();
                }
                else
                {
                    buffer = SerializeObject(value, providerType, converters);
                }
            }

            if (uri == null)
            {
                using (var editor = SharedPreferences.Edit())
                {
                    editor.PutString(key, buffer);
                    editor.Commit();
                }
            }
            else
            {
                var byteArray = Encoding.UTF8.GetBytes(buffer ?? string.Empty);
                using (var openFileDescriptor = AppContext.ContentResolver.OpenFileDescriptor(uri, "w"))
                {
                    using (var fileOutputStream = new Java.IO.FileOutputStream(openFileDescriptor.FileDescriptor))
                    {
                        fileOutputStream.Write(byteArray);
                        fileOutputStream.Close();
                        openFileDescriptor.Close();
                    }
                }

                //using (var stream = AppContext.ContentResolver.OpenOutputStream(uri, "w"))
                //{
                //    using (var writer = new StreamWriter(stream, Encoding.UTF8))
                //    {
                //        writer.Write(buffer ?? string.Empty);
                //        writer.Close();
                //    }

                //    stream.Close();
                //}
            }
        }

        private string SerializeObject(object value, ProviderType providerType = ProviderType.None, params JsonConverter[] converters)
        {
            var json = value == null ? string.Empty : JsonConvert.SerializeObject(value, converters);
            if (providerType != ProviderType.None)
            {
                var crypto = GetCrypto(providerType);
                return crypto.Encrypt(json);
            }

            return json;
        }

        private T DeserializeObject<T>(string value, ProviderType providerType, params JsonConverter[] converters) where T : class
        {
            if (string.IsNullOrEmpty(value))
                return null;

            string json;
            if (providerType == ProviderType.None)
            {
                json = value;
            }
            else
            {
                var crypto = GetCrypto(providerType);
                json = crypto.Decrypt(value);
            }

            if (string.IsNullOrEmpty(json) || string.IsNullOrWhiteSpace(json))
                return null;

            var result = JsonConvert.DeserializeObject<T>(json, converters);
            return result;
        }

        private T GetEnum<T>(int preferenceKey, T defaultValue) where T : struct, IConvertible
        {
            if (!typeof(T).IsEnum)
            {
                throw new ArgumentException(string.Format("Generic type '{0}' must be an enumerated type.",
                    typeof(T).Name));
            }

            var index = GetValue(preferenceKey, defaultValue.ToString(CultureInfo.InvariantCulture));
            if (string.IsNullOrEmpty(index) || !Enum.TryParse(index, true, out T unit))
                return defaultValue;
            return unit;
        }

        private PackageInfo GetPackageInfo(PackageInfoFlags flags = 0)
        {
            var result = AppContext.PackageManager.GetPackageInfo(AppContext.PackageName, flags);
            return result;
        }

        private void ValidateWeatherProviderIdDefault()
        {
            if (GetWeatherProviderIds().Where(p => p == WeatherProviderIdDefault).ToArray().Length == 0)
                throw new Exception("Illegal default weather provider id.");
        }

        private void ValidateProviderList()
        {
            if (_providers == null || _providers.Count == 0)
                throw new Exception("Undefined providers list.");
        }

        private void ValidateWeatherProviderList()
        {
            ValidateProviderList();
            if (_providers.All(p => p.Value?.ProviderType != ProviderTypes.WeatherProvider))
                throw new Exception("Undefined weather providers list.");
        }

        private string GetRequestCounterKey(int providerId)
        {
            return string.Format("{0}_{1}", GetStringById(Resource.String.RequestCounterKey), providerId);
        }

        private double? GetAlertTemperature(int keyId)
        {
            var tempStr = GetValue<string>(keyId);
            if (string.IsNullOrEmpty(tempStr))
                return null;

            if (!int.TryParse(tempStr, out int temp))
                return null;

            var result = Units.ConvertTemperature(temp, TemperatureUnit.Celsius, TemperatureUnit);
            return result;
        }

        private void SaveAlertTemperature(int keyId, double? value)
        {
            var tempStr = value.HasValue
                ? ((int) Units.ConvertTemperature(value.Value, TemperatureUnit, TemperatureUnit.Celsius)).ToString()
                : string.Empty;

            var key = GetStringById(keyId);
            using (var editor = SharedPreferences.Edit())
            {
                editor.PutString(key, tempStr);
                editor.Commit();
            }
        }

        private JsonConverter GetSecureStringConverter()
        {
            return _container.Resolve<SecureStringConverter>();
        }

        #region cache

        private void InitCacheApiKeys(IDictionary<int, ISb49SecureString> value)
        {
            ClearCacheApiKeys();
            _cacheApiKeys = new Lazy<IDictionary<int, ISb49SecureString>>(() => value);
        }

        private void ClearCacheApiKeys()
        {
            if (_cacheApiKeys?.Value?.Values != null)
            {
                foreach (var value in _cacheApiKeys.Value.Values)
                {
                    value?.Dispose();
                }
                _cacheApiKeys.Value.Clear();
            }
            _cacheApiKeys = null;
        }

        private void InitCacheCurrentLocations(CurrentLocation value)
        {
            ClearCacheCurrentLocations();
            _cacheCurrentLocation = new Lazy<CurrentLocation>(() => value);
        }

        private void ClearCacheCurrentLocations()
        {
            _cacheCurrentLocation = null;
        }

        private void InitCacheLocations(IDictionary<int, LocationAddress> value)
        {
            ClearCacheLocations();
            _cacheLocations = new Lazy<IDictionary<int, LocationAddress>>(() => value);
        }

        private void ClearCacheLocations()
        {
            if (_cacheLocations?.Value?.Values != null)
            {
                foreach (var value in _cacheLocations.Value.Values)
                {
                    value?.Dispose();
                }
                _cacheLocations.Value.Clear();
            }
            _cacheLocations = null;
        }

        private void InitWeatherForecast(WeatherForecast value)
        {
            ClearWeatherForecast();
            _cacheWeatherForecast = new Lazy<WeatherForecast>(() => value);
        }

        private void ClearWeatherForecast()
        {
            _cacheWeatherForecast?.Value?.Dispose();
            _cacheWeatherForecast = null;
        }

        private void InitCacheAppWidgetSettings(IDictionary<int, AppWidgetSettings> value)
        {
            ClearCacheAppWidgetSettings();
            _cacheAppWidgetSettings = new Lazy<IDictionary<int, AppWidgetSettings>>(() => value);
        }

        private void ClearCacheAppWidgetSettings()
        {
            if (_cacheAppWidgetSettings?.Value?.Values != null)
            {
                foreach (var value in _cacheAppWidgetSettings.Value.Values)
                {
                    value?.Dispose();
                }
                _cacheAppWidgetSettings.Value.Clear();
            }
            _cacheAppWidgetSettings = null;
        }

        #endregion cache

        #endregion privet methods

        #region . IDisposable .

        private void OnDispose()
        {
            try
            {
                ClearCache();
                _geocoder = null;
                ChangeStatus = SettingsChangeStatus.None;
            }
            finally
            {
                IsDisposed = true;
            }
        }

        public void Dispose()
        {
            OnDispose();
            GC.SuppressFinalize(this);
        }

        #endregion . IDisposable .
    }
}