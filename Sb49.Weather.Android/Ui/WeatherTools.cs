using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Text;
using Android.Text.Style;
using Sb49.Common.Droid;
using Sb49.Common.Droid.Ui;
using Sb49.Common.Logging;
using Sb49.Common.Support.v7.Droid.Ui;
using Sb49.Weather.Code;
using Sb49.Weather.Core;
using Sb49.Weather.Droid.Common;
using Sb49.Weather.Model;
using Sb49.Weather.Model.Core;

namespace Sb49.Weather.Droid.Ui
{
    public sealed class WeatherTools : IDisposable
    {
        private IDictionary<int, Drawable> _drawableCache;
        private IDictionary<int, Bitmap> _windDirectionBitmapCache;
        private readonly Context _appContext;
        private AppCompatDrawableUtil _appCompatDrawableUtil;
        private WindTools _windTools;
        private DrawableUtil _drawableUtil;
        private static readonly ILog Log = LogManager.GetLogger<WeatherTools>();

        public WeatherTools()
        {
            _appContext = Application.Context;
            CultureInfo = AppSettings.Default.CurrentCultureInfo;
            _drawableCache = new ConcurrentDictionary<int, Drawable>();
            _windDirectionBitmapCache = new ConcurrentDictionary<int, Bitmap>();
        }

        ~WeatherTools()
        {
            OnDispose();
        }

        public Units ProviderUnits { get; set; }
        public CultureInfo CultureInfo { get; set; }
        public string DegreeString => _appContext.GetString(Resource.String.Degree);
        public string TemperatureUnitString => TemperatureUnitToString(AppSettings.Default.TemperatureUnit);
        public string WindSpeedUnitString => WindSpeedToString(AppSettings.Default.Units.WindSpeedUnit);
        public string PressureUnitString => PressureUnitToString(AppSettings.Default.Units.PressureUnit);
        public string VisibilityUnitString => DistanceUnitToString(AppSettings.Default.Units.VisibilityUnit);

        public int GetConditionIconId(WidgetTypes widgetType, IconStyles? iconStyles, IWeatherDataPoint dataPoint,
            bool useTwilight, bool isCurrently)
        {
            var weatherCode = dataPoint?.WeatherCode;

            // sunrise, sunset
            var dataPointDate = dataPoint?.Date;
            if (dataPointDate.HasValue)
            {
                if (isCurrently)
                {
                    var now = DateTime.UtcNow;
                    if (dataPointDate < now)
                        dataPointDate = now;
                }
            }

            var conditionIconResource = GetConditionIconResource(weatherCode, dataPoint?.WeatherUnrecognizedCode,
                useTwilight, dataPointDate, dataPoint?.Astronomy);
            var resourceName = GetConditionIconResource(widgetType, iconStyles, conditionIconResource,
                AppSettings.WeatherConditionIconTheme);

            var resourceId = GetIdentifier(resourceName);
            if (!resourceId.HasValue)
            {
                var name = resourceName;
                if (name.Contains("_min"))
                {
                    name = name.Replace("_min", string.Empty);
                    resourceId = GetIdentifier(name);
                }

                if (!resourceId.HasValue)
                {
                    if (name.Contains("_max"))
                    {
                        name = name.Replace("_max", string.Empty);
                        resourceId = GetIdentifier(name);
                    }
                }
            }

            if (!resourceId.HasValue && weatherCode != WeatherCodes.Undefined && weatherCode != WeatherCodes.Error)
            {
                Log.ErrorFormat("Can't find condition icon by code '{0}', resource name '{1}'.", weatherCode,
                    resourceName);
            }

            return resourceId ?? GetUnderfoundIcon(widgetType, iconStyles);
        }

        private int? GetIdentifier(string name)
        {
            var resId = _appContext.Resources.GetIdentifier(name, ResourceType.Drawable, _appContext.PackageName);
            return resId > 0 ? resId : (int?)null;
        }

        public Drawable GetConditionIcon(WidgetTypes widgetType, IconStyles? iconStyles, IWeatherDataPoint dataPoint,
            bool useTwilight, bool isCurrently)
        {
            var cachekey = GetConditionIconId(widgetType, iconStyles, dataPoint, useTwilight, isCurrently);
            if (!_drawableCache.ContainsKey(cachekey))
            {
                if (_appCompatDrawableUtil == null)
                    _appCompatDrawableUtil = new AppCompatDrawableUtil();

                _drawableCache[cachekey] = _appCompatDrawableUtil.GetDrawable(_appContext.Resources, cachekey,
                    _appContext.Theme);
            }

            return _drawableCache[cachekey];
        }

        public bool IsConditionExtreme(WeatherCodes? code)
        {
            return (code.HasValue && code.Value.HasFlag(WeatherCodes.Extreme));
        }

        public double ConvertTemperature(double temperature)
        {
            return Units.ConvertTemperature(temperature,
                ProviderUnits.TemperatureUnit, AppSettings.Default.TemperatureUnit);
        }

        public string ConvertTemperatureToString(double temperature, string format, params object[] args)
        {
            var result = ConvertTemperature(temperature);
            return Format(format, result, args);
        }

        public Java.Lang.ICharSequence ConvertTemperatureToAlertedStyle(Context context, double temperature,
            string format, params object[] args)
        {
            var temp = ConvertTemperature(temperature);
            var tempToString = Format(format, temp, args);
            var spannable = new SpannableString(tempToString);

            int styleId;
            if (IsColdTemperatureAlerted(temp))
                styleId = Resource.Style.coldAlertedTemperatureTextStyle;
            else if (IsHotTemperatureAlerted(temp))
                styleId = Resource.Style.hotAlertedTemperatureTextStyle;
            else
                return spannable;

            spannable.SetSpan(new TextAppearanceSpan(context, styleId), 0, tempToString.Length,
                SpanTypes.ExclusiveExclusive);
            return spannable;
        }

        public string ConvertTemperatureToString(double temperature1, double temperature2, string format,
            params object[] args)
        {
            var temp1 = ConvertTemperature(temperature1);
            var temp2 = ConvertTemperature(temperature2);
            return Format(format, temp1, temp2, args);
        }

        public string TemperatureUnitToString(TemperatureUnit unit)
        {
            return unit == TemperatureUnit.Fahrenheit ? "F" : "C";
        }

        public bool IsColdTemperatureAlerted(double? temperature, bool needConvertion = true)
        {
            if (!temperature.HasValue)
                return false;

            var temp = needConvertion ? ConvertTemperature(temperature.Value) : temperature.Value;
            var result = temp < AppSettings.Default.ColdAlertedTemperature;
            return result;
        }

        public bool IsHotTemperatureAlerted(double? temperature, bool needConvertion = true)
        {
            if (!temperature.HasValue)
                return false;

            var temp = needConvertion ? ConvertTemperature(temperature.Value) : temperature.Value;
            var result = temp > AppSettings.Default.HotAlertedTemperature;
            return result;
        }

        public bool IsTemperatureAlerted(double? temperature)
        {
            if (!temperature.HasValue)
                return false;

            var temp = ConvertTemperature(temperature.Value);
            var isCold = IsColdTemperatureAlerted(temp, false);
            var isHot = IsHotTemperatureAlerted(temp, false);
            return isCold || isHot;
        }

        public double ConvertWindSpeed(double windSpeed)
        {
            return Units.ConvertSpeed(windSpeed, ProviderUnits.WindSpeedUnit, AppSettings.Default.WindSpeedUnit);
        }

        public string WindSpeedToString(SpeedUnit unit)
        {
            return
                _appContext.GetString(_appContext.Resources.GetIdentifier(unit.ToString(), ResourceType.String,
                    _appContext.PackageName));
        }

        public string ConvertWindSpeedToString(double windSpeed, string format, params object[] args)
        {
            var result = ConvertWindSpeed(windSpeed);
            return Format(format, result, args);
        }

        public string WindDirectionToCardinal(double degrees)
        {
            var cardinal = GetWindTools().DegreesToCardinal(degrees);
            var windDirectionText = _windTools.GetString(cardinal);
            return windDirectionText;
        }

        public Bitmap WindDirectionDrawable(double degrees)
        {
            var cachekey = (int) GetWindTools().Rotate(degrees, 180);
            if (!_windDirectionBitmapCache.ContainsKey(cachekey))
            {
                if (_drawableUtil == null)
                    _drawableUtil = new DrawableUtil();
                var source = BitmapFactory.DecodeResource(_appContext.Resources, Resource.Drawable.wind_direction);
                var bitmap = _drawableUtil.RotateBitmap(source, cachekey);
                _windDirectionBitmapCache[cachekey] = bitmap;
            }
            return _windDirectionBitmapCache[cachekey];
        }

        private WindTools GetWindTools()
        {
            return _windTools ?? (_windTools = new WindTools());
        }

        public double ConvertPressure(double pressure)
        {
            return Units.ConvertPressure(pressure, ProviderUnits.PressureUnit, AppSettings.Default.PressureUnit);
        }

        public string PressureUnitToString(PressureUnit unit)
        {
            return
                _appContext.GetString(_appContext.Resources.GetIdentifier(
                    unit.ToString(), ResourceType.String,
                    _appContext.PackageName));
        }

        public string ConvertPressureToString(double pressure, string format, params object[] args)
        {
            var result = ConvertPressure(pressure);
            return Format(format, result, args);
        }

        public string ConvertPressureToString(double pressure1, double pressure2, string format, params object[] args)
        {
            var press1 = ConvertPressure(pressure1);
            var press2 = ConvertPressure(pressure2);
            return Format(format, press1, press2, args);
        }

        public double ConvertVisibility(double distance)
        {
            return Units.ConvertDistance(distance, ProviderUnits.VisibilityUnit, AppSettings.Default.VisibilityUnit);
        }

        public string DistanceUnitToString(DistanceUnit unit)
        {
            return
                _appContext.GetString(
                    _appContext.Resources.GetIdentifier(unit.ToString(), ResourceType.String,
                        _appContext.PackageName));
        }

        public string ConvertVisibilityToString(double distance, string format, params object[] args)
        {
            var result = ConvertVisibility(distance);
            return Format(format, result, args);
        }

        public string Format(string format, params object[] args)
        {
            if (string.IsNullOrEmpty(format))
                throw new ArgumentNullException(nameof(format));
            if (args == null || args.Length == 0)
                throw new ArgumentException(nameof(args));

            var arglist = new List<object>();
            foreach (var arg in args)
            {
                var objects = arg as IEnumerable<object>;
                if (objects == null)
                    arglist.Add(arg);
                else
                    arglist.AddRange(objects);
            }

            return string.Format(CultureInfo, format, arglist.ToArray());
        }

        public double? CalculateMinTemperature(DateTime? utcDateTime, WeatherForecast weather, params double?[] temperatures)
        {
            if (weather == null || !utcDateTime.HasValue)
                return null;

            var resultList = new List<double?>();
            if (temperatures != null && temperatures.Length > 0)
                resultList.AddRange(temperatures);

            var date = utcDateTime.Value.Date;

            var daily = weather.Daily?.Where(p => p.Date?.Date == date).ToArray();
            if (daily?.Length > 0)
            {
                var dailyMinTemperature = daily.Min(p => p.MinTemperature);
                if (dailyMinTemperature.HasValue)
                    resultList.Add(dailyMinTemperature.Value);

                var dailyTemperature = daily.Min(p => p.Temperature);
                if (dailyTemperature.HasValue)
                    resultList.Add(dailyTemperature.Value);
            }

            var hourly = weather.Hourly?.Where(p => p.Date?.Date == date).ToArray();
            if (hourly?.Length > 0)
            {
                var hourlyMinTemperature = hourly.Min(p => p.MinTemperature);
                if (hourlyMinTemperature.HasValue)
                    resultList.Add(hourlyMinTemperature.Value);

                var hourlyTemperature = hourly.Min(p => p.Temperature);
                if (hourlyTemperature.HasValue)
                    resultList.Add(hourlyTemperature.Value);
            }

            return resultList.Count > 0 ? resultList.Where(p => p.HasValue).Min() : null;
        }

        public Task<double?> CalculateMinTemperatureAsync(DateTime? utcDateTime, WeatherForecast weather,
            params double?[] temperatures)
        {
            return Task.Run(() => CalculateMinTemperature(utcDateTime, weather, temperatures));
        }

        public double? CalculateMaxTemperature(DateTime? utcDateTime, WeatherForecast weather, params double?[] temperatures)
        {
            if (weather == null || !utcDateTime.HasValue)
                return null;

            var resultList = new List<double?>();
            if (temperatures != null && temperatures.Length > 0)
                resultList.AddRange(temperatures);

            var date = utcDateTime.Value.Date;

            var daily = weather.Daily?.Where(p => p.Date?.Date == date).ToArray();
            if (daily?.Length > 0)
            {
                var dailyMaxTemperature = daily.Max(p => p.MaxTemperature);
                if (dailyMaxTemperature.HasValue)
                    resultList.Add(dailyMaxTemperature.Value);

                var dailyTemperature = daily.Max(p => p.Temperature);
                if (dailyTemperature.HasValue)
                    resultList.Add(dailyTemperature.Value);
            }

            var hourly = weather.Hourly?.Where(p => p.Date?.Date == date).ToArray();
            if (hourly?.Length > 0)
            {
                var hourlyMaxTemperature = hourly.Max(p => p.MaxTemperature);
                if (hourlyMaxTemperature.HasValue)
                    resultList.Add(hourlyMaxTemperature.Value);

                var hourlyTemperature = hourly.Max(p => p.Temperature);
                if (hourlyTemperature.HasValue)
                    resultList.Add(hourlyTemperature.Value);
            }

            return resultList.Count > 0 ? resultList.Where(p => p.HasValue).Max() : null;
        }

        public Task<double?> CalculateMaxTemperatureAsync(DateTime? utcDateTime, WeatherForecast weather,
            params double?[] temperatures)
        {
            return Task.Run(() => CalculateMaxTemperature(utcDateTime, weather, temperatures));
        }

        #region Condition

        private string GetConditionIconResource(WidgetTypes widgetType, IconStyles? iconStyles, string codes,
            string theme)
        {
            var result =
                string.Format("{0}{1}{2}{3}{4}", GetIconStylesPrefix(widgetType, iconStyles),
                    WidgetTypeToValue(widgetType), codes, GetIconStylesToValue(widgetType, iconStyles), theme);
            return result;
        }

        private string WidgetTypeToValue(WidgetTypes widgetType)
        {
            switch (widgetType)
            {
                case WidgetTypes.AppWidget:
                case WidgetTypes.Widget:
                    return "_widget";
                case WidgetTypes.Item:
                    return null;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private string GetIconStylesPrefix(WidgetTypes widgetType, IconStyles? iconStyles)
        {
            if (widgetType == WidgetTypes.Widget || widgetType == WidgetTypes.Item || !iconStyles.HasValue)
                return "yandex";

            switch (iconStyles)
            {
                case IconStyles.FancyWidgets:
                    return "fancy";
                case IconStyles.YandexColor:
                case IconStyles.YandexMono:
                    return "yandex";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private int GetUnderfoundIcon(WidgetTypes widgetType, IconStyles? iconStyles)
        {
            const int yandexWidgetUndefinedIcon = Resource.Drawable.yandex_widget_ovc_white;

            switch (widgetType)
            {
                case WidgetTypes.AppWidget:
                    switch (iconStyles)
                    {
                        case IconStyles.FancyWidgets:
                            return Resource.Drawable.fancy_widget_ovc_white;
                        case IconStyles.YandexColor:
                            return yandexWidgetUndefinedIcon;
                        case IconStyles.YandexMono:
                            return Resource.Drawable.yandex_widget_ovc_mono_white;
                        default:
                            if (iconStyles.HasValue)
                                throw new ArgumentOutOfRangeException();
                            throw new ArgumentNullException(nameof(iconStyles));
                    }
                case WidgetTypes.Widget:
                    return yandexWidgetUndefinedIcon;
                case WidgetTypes.Item:
                    return Resource.Drawable.yandex_ovc_white;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private string GetIconStylesToValue(WidgetTypes widgetType, IconStyles? iconStyles)
        {
            if (widgetType == WidgetTypes.AppWidget && !iconStyles.HasValue)
                throw new ArgumentNullException(nameof(iconStyles));

            return widgetType == WidgetTypes.AppWidget && iconStyles == IconStyles.YandexMono ? "_mono" : null;
        }

        private string GetConditionIconResource(WeatherCodes? codes, string weatherUnrecognizedCode, bool useTwilight,
            DateTime? dateTime, IAstronomy astronomy)
        {
            if (!codes.HasValue || codes == WeatherCodes.Undefined)
            {
                Log.DebugFormat("Undefined condition icon code '{0}'.", codes);
                return null;
            }

            if (codes == WeatherCodes.Error)
            {
                Log.ErrorFormat("Unrecognized condition icon code '{0}'.", weatherUnrecognizedCode);
                return null;
            }

            var needTwilight = false;

            var group = "_ovc";
            if (codes == WeatherCodes.ClearSky)
            {
                group = "_skc";
                needTwilight = true;
            }
            else if (codes.Value.HasFlag(WeatherCodes.ClearSky))
            {
                group = "_bkn";
                needTwilight = true;
            }
            else if (codes.Value.HasFlag(WeatherCodes.Fog))
            {
                group = "_fg";
                needTwilight = true;
            }

            var precipitation = string.Empty;
            if (codes.Value.HasFlag(WeatherCodes.Heavy) || codes.Value.HasFlag(WeatherCodes.Storm))
            {
                precipitation += "_max";
            }
            else if (codes.Value.HasFlag(WeatherCodes.Light))
            {
                precipitation += "_min";
            }

            if (codes.Value.HasFlag(WeatherCodes.Thunderstorm))
            {
                precipitation += "_ts";
            }
            if (codes.Value.HasFlag(WeatherCodes.Rain))
            {
                precipitation += "_ra";
            }
            if (codes.Value.HasFlag(WeatherCodes.Snow))
            {
                precipitation += "_sn";
            }
            if (codes.Value.HasFlag(WeatherCodes.Hail))
            {
                precipitation = "_gr";
            }

            var result = string.Format("{0}{1}", group, precipitation);

            if (needTwilight)
            {
                var twilightCalculatorState = "_d";

                if (useTwilight && dateTime.HasValue && astronomy?.Sunrise != null && astronomy.Sunset.HasValue)
                {
                    if ((dateTime <= astronomy.Sunrise || dateTime >= astronomy.Sunset) &&
                        (
                            dateTime <= astronomy.NextInfo?.Sunrise || dateTime >= astronomy.NextInfo?.Sunset ||
                            dateTime <= astronomy.PreviousInfo?.Sunrise || dateTime >= astronomy.PreviousInfo?.Sunset))
                    {
                        twilightCalculatorState = "_n";
                    }
                }

                result += twilightCalculatorState;
            }

            return string.IsNullOrEmpty(group) ? null : result;
        }

        #endregion Condition

        #region . IDisposable .

        private void OnDispose()
        {
            if (_drawableCache != null)
            {
                foreach (var p in _drawableCache)
                {
                    p.Value?.Dispose();
                }

                _drawableCache.Clear();
                _drawableCache = null;
            }
            if (_windDirectionBitmapCache != null)
            {
                foreach (var p in _windDirectionBitmapCache)
                {
                    p.Value?.Recycle();
                }

                _windDirectionBitmapCache.Clear();
                _windDirectionBitmapCache = null;
            }

            _appCompatDrawableUtil = null;
            _drawableUtil = null;
            _windTools = null;
        }

        public void Dispose()
        {
            OnDispose();
            GC.SuppressFinalize(this);
        }

        #endregion . IDisposable .
    }

    public enum WidgetTypes
    {
        AppWidget,
        Widget,
        Item
    }
}