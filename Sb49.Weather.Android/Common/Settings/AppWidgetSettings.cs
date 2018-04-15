using System;
using Android.App;
using Newtonsoft.Json;
using Sb49.Security.Core;
using Sb49.Weather.Core;
using Sb49.Weather.Droid.Common.Exeptions;
using Sb49.Weather.Droid.Model;
using Sb49.Weather.Droid.Ui;
using Sb49.Weather.Model;

namespace Sb49.Weather.Droid.Common
{
    public sealed class AppWidgetSettings : IAppSettings
    {
        public AppWidgetSettings(int widgetId)
        {
            WidgetId = widgetId;
            WeatherProviderId = AppSettings.Default.WeatherProviderIdDefault;
        }

        ~AppWidgetSettings()
        {
            OnDispose();
        }

        public int WidgetId { get; }

        [JsonIgnore]
        public bool IsNotAppWidget => false;

        #region AppWidget style

        public IconStyles WidgetIconStyle { get; set; } = IconStyles.FancyWidgets;

        public string WidgetBackgroundStyle { get; set; } =
            AppSettings.Default.GetStringById(Resource.String.AppWidgetBackgroundStyleValueDefault);

        public int WidgetBackgroundOpacity { get; set; } =
            AppSettings.Default.GetIntegerById(Resource.Integer.AppWidgetOpacityDefault);

        #endregion AppWidget style

        #region AppWidget clock

        public bool Use12HourFormat { get; set; }

        public bool UseExtendedHourFormat { get; set; } =
            AppSettings.Default.GetBooleanById(Resource.Boolean.ExtendedHourFormatDefault);

        public string DateFormatValue { get; set; } =
            AppSettings.Default.GetStringById(Resource.String.DateFormatValueDefault);

        public string GetDateFormat()
        {
            string day;
            var monthDay = AppSettings.Default.CurrentCultureInfo?.DateTimeFormat.MonthDayPattern;
            switch (DateFormatValue)
            {
                case "0":
                    day = "EEE";
                    monthDay = monthDay?.Replace("MMMM", "MMM");
                    break;
                case "1":
                    day = "EEE";
                    break;
                case "2":
                    day = "EEEE";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var result = string.Format("{0}, {1}", day, monthDay);
            return result;
        }

        public string GetHourFormat(bool use12HourFormat, bool useExtendedHourFormat, string ampm)
        {
            string result;
            if (use12HourFormat)
            {
                result = useExtendedHourFormat ? "hh:mm" : "h:mm";
                if (!string.IsNullOrEmpty(ampm))
                    result = ampm + result;
            }
            else
            {
                result = useExtendedHourFormat ? "HH:mm" : "H:mm";
            }
            return result;
        }

        public string GetHourFormat(string ampm)
        {
            return GetHourFormat(Use12HourFormat, UseExtendedHourFormat, ampm);
        }

        public int ClockTextSizeSp { get; set; } =
            AppSettings.Default.GetIntegerById(Resource.Integer.ClockTextSizeDefault);

        #endregion AppWidget clock

        #region AppWidget locations

        public bool UseTrackCurrentLocation { get; set; } =
            AppSettings.Default.GetBooleanById(Resource.Boolean.UseTrackCurrentLocationDefault);

        public LocationAddress LocationAddress { get; set; }

        #endregion AppWidget locations

        #region AppWidget weather provider

        private int _weatherProviderId;

        public int WeatherProviderId
        {
            get { return _weatherProviderId; }
            set
            {
                AppSettings.Default.ValidateWeatherProviderId(value);
                _weatherProviderId = value;
            }
        }

        public int WeatherServiceRefreshIntervalValue { get; set; } =
            Convert.ToInt32(AppSettings.Default.GetStringById(Resource.String.WeatherRefreshIntervalValueDefault));

        [JsonIgnore]
        public int WeatherServiceRefreshIntervalMsec
            => AppSettings.Default.ConvertToWeatherServiceTimerPeriod(WeatherServiceRefreshIntervalValue);

        [JsonIgnore]
        public ISb49SecureString WeatherApiKey => AppSettings.Default.GetApiKey(WeatherProviderId);

        [JsonIgnore]
        public IWeatherProviderService WeatherProviderService
            => AppSettings.Default.GetWeatherProviderService(WeatherProviderId);

        public WeatherForecast Weather { get; set; }

        private volatile bool _weatherDataUpdating;

        [JsonIgnore]
        public bool WeatherDataUpdating => _weatherDataUpdating;

        void IAppSettings.BeginWeatherDataUpdate()
        {
            _weatherDataUpdating = true;
        }

        void IAppSettings.EndWeatherDataUpdate()
        {
            _weatherDataUpdating = false;
        }

        #endregion AppWidget weather provider

        public bool ValidateLocationSettings(Activity activity, int requestCode)
        {
            if (UseTrackCurrentLocation)
            {
                if (!AppSettings.Default.ValidateLocationSettings())
                    throw new LocationSettingsException();

                if (activity != null && !new Dialogs().GoogleApiAvailabilityDialog(activity, requestCode))
                    return false;

                var currentLocation = AppSettings.Default.CurrentLocation;
                if (currentLocation == null || !currentLocation.HasCoordinates)
                    throw new LocationException(Resource.String.UndefinedCurrentLocation);
            }
            else
            {
                var location = LocationAddress;
                if (location == null || !location.IsValid())
                    throw new LocationException();
            }
            return true;
        }

        #region . IDisposable .

        private void OnDispose()
        {
            Weather = null;
            LocationAddress = null;
        }

        public void Dispose()
        {
            OnDispose();
            GC.SuppressFinalize(this);
        }

        #endregion . IDisposable .
    }
}