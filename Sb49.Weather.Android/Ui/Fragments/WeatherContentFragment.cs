using System;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Locations;
using Android.OS;
using Android.Support.V4.Content.Res;
using Android.Support.V7.Widget;
using Android.Text;
using Android.Text.Style;
using Android.Views;
using Android.Widget;
using Sb49.Common;
using Sb49.Common.Droid.Listeners;
using Sb49.Common.Droid.Ui;
using Sb49.Weather.Core;
using Sb49.Weather.Droid.Common;
using Sb49.Weather.Droid.Ui.Adapters;
using Sb49.Weather.Model;
using Sb49.Weather.Model.Core;

namespace Sb49.Weather.Droid.Ui.Fragments
{
    public class WeatherContentFragment : Fragment
    {
        private TextView _txtLogoWeather;
        private ImageView _imgLogoWeather;
        private string _weatherLink;
        private WeatherTools _weatherTools;
        private WeatherDailyItemAdapterBase _weatherDailyItemAdapter;
        private TooltipWindow _tooltipWindow;
        private RecyclerView _gridWeatherByDay;
        private RecyclerView.LayoutManager _layoutManager;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            _weatherLink = null;
            var view = inflater.Inflate(Resource.Layout.fragment_weather, container, false);

            if (savedInstanceState == null && view != null)
            {
                _txtLogoWeather = view.FindViewById<TextView>(Resource.Id.txtLogoWeather);
                if (_txtLogoWeather != null)
                    _txtLogoWeather.PaintFlags |= PaintFlags.UnderlineText;

                _imgLogoWeather = view.FindViewById<ImageView>(Resource.Id.imgLogoWeather);
                if (_imgLogoWeather != null)
                {
                    var today = DateTime.Today;
                    var month = today.Month;
                    var day = today.Day;

                    if (month == 12 && day >= 25 || month == 1 && day <= 13)
                        _imgLogoWeather.SetImageResource(Resource.Drawable.santa);

                    _imgLogoWeather.SetOnClickListener(new ClickListener(v =>
                    {
                        if (_tooltipWindow != null && !_tooltipWindow.IsDisposed && !_tooltipWindow.IsTooltipShown)
                            _tooltipWindow.ShowToolTip(v);
                    }));
                }

                _gridWeatherByDay = view.FindViewById<RecyclerView>(Resource.Id.gridWeatherByDay);
            }

            return view;
        }

        public void UpdateView(int? weatherProviderNameResourceId, double? locationLatitude, double? locationLongitude,
            WeatherForecast weather, IWeatherDataPoint currently, WeatherDataPointDaily[] daily, out bool isAlerted)
        {
            isAlerted = false;
            _weatherLink = null;

            if (View == null || Activity == null)
                return;

            if (_txtLogoWeather != null)
            {
                if (weatherProviderNameResourceId.HasValue)
                    _txtLogoWeather.SetText(weatherProviderNameResourceId.Value);
                else
                    _txtLogoWeather.Text = string.Empty;
            }

            if (_weatherTools == null)
                _weatherTools = new WeatherTools();

            if (weather != null)
                _weatherTools.ProviderUnits = weather.Units;

            if (_txtLogoWeather != null && !string.IsNullOrEmpty(weather?.Link))
            {
                _weatherLink = weather.Link;
                SubscribeEvents();
            }

            GetWeatherDate(currently?.Date, weather?.UpdatedDate, out string publishedDateText,
                out string updatedDateText);

            float? locationDistance = null;
            if (locationLatitude.HasValue && locationLongitude.HasValue && weather?.Latitude != null &&
                weather.Longitude != null)
            {
                var distance = new float[1];
                Location.DistanceBetween(locationLatitude.Value, locationLongitude.Value,
                    weather.Latitude.Value, weather.Longitude.Value, distance);
                locationDistance = Math.Abs(distance[0]);
            }

            UpdateTooltip(publishedDateText, updatedDateText, locationDistance,
                weather == null ? null : AppSettings.Default.GetRequestCounter(weather.ProviderId)?.Count,
                AppSettings.Default.UseGoogleMapsGeocodingApi
                    ? AppSettings.Default.GetRequestCounter(AppSettings.GoogleMapsGeocodingApiProviderId)?.Count
                    : null);

            Java.Lang.ICharSequence tempText = new SpannableString(string.Empty);
            var degree = _weatherTools.DegreeString;
            var txtTemp = View.FindViewById<TextView>(Resource.Id.txtTemp);
            txtTemp.SetCompoundDrawables(null, null, null, null);

            if (currently?.Temperature != null)
            {
                tempText = _weatherTools.ConvertTemperatureToAlertedStyle(Activity, currently.Temperature.Value,
                    "{0:f1}{1}{2}",
                    degree,
                    _weatherTools.TemperatureUnitString);

                if (_weatherTools.IsTemperatureAlerted(currently.Temperature.Value))
                {
                    isAlerted = true;
                    var drawable = ResourcesCompat.GetDrawable(Resources, Resource.Drawable.alert, Activity.Theme);
                    var px = (int) Resources.GetDimension(Resource.Dimension.alertImageDimen);
                    drawable.SetBounds(0, 0, px, px);
                    //blinkingAnimation.SetBounds(0, 0, blinkingAnimation.IntrinsicWidth, blinkingAnimation.IntrinsicHeight);
                    txtTemp.SetCompoundDrawables(null, null, drawable, null);
                }
            }
            txtTemp.TextFormatted = tempText;

            var imgCondition = View.FindViewById<ImageView>(Resource.Id.imgCondition);
            if (imgCondition != null)
            {
                if (currently == null)
                {
                    imgCondition.SetImageResource(Android.Resource.Color.Transparent);
                }
                else
                {
                    imgCondition.SetImageDrawable(
                        _weatherTools.GetConditionIcon(WidgetTypes.Widget, null, currently, true, true));
                }
            }

            var txtCondition = View.FindViewById<TextView>(Resource.Id.txtCondition);
            txtCondition.SetCompoundDrawables(null, null, null, null);

            var conditionText = (currently?.Condition ?? string.Empty).Trim().ToCapital();
            var conditionTextFormatted = new SpannableString(conditionText);
            if (_weatherTools.IsConditionExtreme(currently?.WeatherCode))
            {
                isAlerted = true;
                var drawable = ResourcesCompat.GetDrawable(Resources, Resource.Drawable.alert, Activity.Theme);
                var px = (int) Resources.GetDimension(Resource.Dimension.alertImageDimen);
                drawable.SetBounds(0, 0, px, px);
                //blinkingAnimation.SetBounds(0, 0, blinkingAnimation.IntrinsicWidth, blinkingAnimation.IntrinsicHeight);
                txtCondition.SetCompoundDrawables(null, null, drawable, null);
                conditionTextFormatted.SetSpan(
                    new TextAppearanceSpan(Activity, Resource.Style.conditionAlertedTextStyle), 0, conditionText.Length,
                    SpanTypes.ExclusiveExclusive);
            }
            txtCondition.TextFormatted = conditionTextFormatted;

            var txtWind = View.FindViewById<TextView>(Resource.Id.txtWind);
            txtWind.Visibility = currently?.WindDirection != null || currently?.WindSpeed != null
                ? ViewStates.Visible
                : ViewStates.Gone;
            var imgWindDirection = View.FindViewById<ImageView>(Resource.Id.imgWindDirection);
            var txtWindDirection = View.FindViewById<TextView>(Resource.Id.txtWindDirection);
            var windDirectionText = string.Empty;

            if (currently?.WindDirection != null)
            {
                txtWind.Visibility = ViewStates.Visible;
                imgWindDirection.Visibility = ViewStates.Visible;
                imgWindDirection.SetImageBitmap(_weatherTools.WindDirectionDrawable(currently.WindDirection.Value));
                windDirectionText = _weatherTools.WindDirectionToCardinal(currently.WindDirection.Value);
            }
            else
            {
                imgWindDirection.Visibility = ViewStates.Gone;
            }
            if (txtWindDirection != null)
                txtWindDirection.Text = windDirectionText;

            var txtWindSpeed = View.FindViewById<TextView>(Resource.Id.txtWindSpeed);
            var windSpeedText = string.Empty;
            if (currently?.WindSpeed != null)
            {
                windSpeedText = _weatherTools.ConvertWindSpeedToString(currently.WindSpeed.Value, "{0:f1} {1}",
                    _weatherTools.WindSpeedUnitString);
            }
            txtWindSpeed.Text = windSpeedText;

            var txtPressure = View.FindViewById<TextView>(Resource.Id.txtPressure);
            var pressureText = string.Empty;
            if (currently?.Pressure != null)
            {
                pressureText = _weatherTools.ConvertPressureToString(currently.Pressure.Value, "{1} {0:f0} {2}",
                    GetString(Resource.String.Pressure), _weatherTools.PressureUnitString);
            }
            txtPressure.Text = pressureText;

            var txtHumidity = View.FindViewById<TextView>(Resource.Id.txtHumidity);
            var humidity = string.Empty;
            if (currently?.Humidity != null)
                humidity = _weatherTools.Format("{0} {1:p0}", GetString(Resource.String.Humidity),
                    currently.Humidity);
            txtHumidity.Text = humidity;

            var txtApparentTemp = View.FindViewById<TextView>(Resource.Id.txtApparentTemp);
            var apparentTemp = string.Empty;
            if (currently?.ApparentTemperature != null)
            {
                apparentTemp = _weatherTools.ConvertTemperatureToString(currently.ApparentTemperature.Value,
                    "{1} {0:f0}{2}", GetString(Resource.String.ApparentTemperature), degree);
            }
            txtApparentTemp.Text = apparentTemp;

            var txtVisibility = View.FindViewById<TextView>(Resource.Id.txtVisibility);
            var valueText = string.Empty;
            if (currently?.Visibility != null)
            {
                valueText = _weatherTools.ConvertVisibilityToString(currently.Visibility.Value, "{1} {0:f0} {2}",
                    GetString(Resource.String.Visibility), _weatherTools.VisibilityUnitString);
            }
            else if (currently?.DewPoint != null)
            {
                valueText = _weatherTools.ConvertTemperatureToString(currently.DewPoint.Value, "{1} {0:f0}{2}",
                    GetString(Resource.String.DewPoint), degree);
            }
            txtVisibility.Text = valueText;

            var txtSunInfo = View.FindViewById<TextView>(Resource.Id.txtSunInfo);
            var sunInfo = string.Empty;
            if (currently?.Astronomy?.Sunrise != null && currently.Astronomy.Sunset.HasValue)
            {
                sunInfo = _weatherTools.Format("{0} {1:t} {2} {3:t}", GetString(Resource.String.Sunrise),
                    currently.Astronomy.Sunrise.Value.ToLocalTime(), GetString(Resource.String.Sunset),
                    currently.Astronomy.Sunset.Value.ToLocalTime());
            }

            var landOrientation = AppSettings.Default.LandOrientation;
            if (!landOrientation && string.IsNullOrEmpty(valueText))
            {
                txtVisibility.Text = sunInfo;
                txtSunInfo.Visibility = ViewStates.Gone;
            }
            else
            {
                txtSunInfo.Text = sunInfo;
                txtSunInfo.Visibility = ViewStates.Visible;
            }

            if (_gridWeatherByDay == null)
                return;

            LayoutManagerDispose();
            WeatherDailyItemAdapterDispose();

            if (landOrientation)
            {
                _layoutManager = new LinearLayoutManager(Activity, LinearLayoutManager.Horizontal, false);
                var scrollView = View.FindViewById<ScrollView>(Resource.Id.viewScroll);
                var viewDetailsContent = View.FindViewById<View>(Resource.Id.viewDetailsContent);
                if (viewDetailsContent != null && viewDetailsContent.Handle != IntPtr.Zero)
                    viewDetailsContent.Visibility = ViewStates.Gone;
                _weatherDailyItemAdapter =
                    new WeatherDailyItemAdapterLand(daily, _weatherTools, viewDetailsContent, scrollView);
            }
            else
            {
                _layoutManager = new LinearLayoutManager(Activity, LinearLayoutManager.Vertical, false);
                _weatherDailyItemAdapter = new WeatherDailyItemAdapter(daily, _weatherTools);
            }

            _gridWeatherByDay.SetLayoutManager(_layoutManager);
            _gridWeatherByDay.SetAdapter(_weatherDailyItemAdapter);
        }

        private void OnCreateTooltip()
        {
            TooltipWindowDispose();
            _tooltipWindow = new TooltipWindow(Activity, Resource.Layout.tooltip_layout, Resource.Id.tooltipText)
            {
                DelayedMsec = 20000
            };
        }

        private void OnLogoWeatherClick(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_weatherLink))
                return;

            var uri = Android.Net.Uri.Parse(_weatherLink);
            var intent = new Intent(Intent.ActionView, uri);
            StartActivity(intent);
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

        protected override void Dispose(bool disposing)
        {
            WeatherToolsDispose();
            LayoutManagerDispose();
            WeatherDailyItemAdapterDispose();
            TooltipWindowDispose();

            base.Dispose(disposing);
        }

        private void TooltipWindowDispose()
        {
            _tooltipWindow?.Dispose();
            _tooltipWindow = null;
        }

        private void WeatherToolsDispose()
        {
            _weatherTools?.Dispose();
            _weatherTools = null;
        }

        private void LayoutManagerDispose()
        {
            _layoutManager?.Dispose();
            _layoutManager = null;
        }

        private void WeatherDailyItemAdapterDispose()
        {
            _weatherDailyItemAdapter?.Dispose();
            _weatherDailyItemAdapter = null;
        }

        private void GetWeatherDate(DateTime? publishedDate, DateTime? updatedDate, out string publishedDateText, out string updatedDateText)
        {
            publishedDateText = null;
            updatedDateText = null;

            var txtPublishedDate = View.FindViewById<TextView>(Resource.Id.txtPublishedDate);
            if (publishedDate.HasValue)
            {
                publishedDateText =
                    string.Format("{0} {1}", GetString(Resource.String.PublishedDateText),
                        publishedDate.Value.ToLocalTime().ToString("G", _weatherTools.CultureInfo));
            }
            if (txtPublishedDate != null)
                txtPublishedDate.Text = publishedDateText ?? string.Empty;
            
            var txtLastUpdate = View.FindViewById<TextView>(Resource.Id.txtLastUpdate);
            var lastupdate = updatedDate?.ToLocalTime();
            if (lastupdate.HasValue)
            {
                updatedDateText = string.Format("{0} {1}",
                    GetString(Resource.String.LastUpdatedText),
                    lastupdate.Value.ToString("G", _weatherTools.CultureInfo));
            }
            if (txtLastUpdate != null)
                txtLastUpdate.Text = updatedDateText ?? string.Empty;

            var viewInfoScroll = View.FindViewById<HorizontalScrollView>(Resource.Id.viewInfoScroll);
            viewInfoScroll?.ComputeScroll();
        }

        private void UpdateTooltip(string weatherPublishedDateText, string weatherUpdatedDateText,
            float? locationDistance, int? weatherRequestCount, int? googleMapsGeocodingApiCount)
        {
            if (_tooltipWindow == null || _tooltipWindow.IsDisposed)
                OnCreateTooltip();

            try
            {
                _tooltipWindow.TooltipText = string.Empty;
            }
            catch (ObjectDisposedException)
            {
                OnCreateTooltip();
            }

            var weatherSection = GetString(Resource.String.WeatherTitle);
            var tooltip = string.Format("{0}{1}{2} {3}", weatherSection, System.Environment.NewLine,
                GetString(Resource.String.RequestCountMessage), weatherRequestCount ?? 0);

            if (locationDistance.HasValue)
            {
                tooltip = string.Format("{0}{1}{2} {3:N3} {4}", tooltip, System.Environment.NewLine,
                    GetString(Resource.String.LocationDistance), locationDistance / 1000.0,
                    _weatherTools?.DistanceUnitToString(DistanceUnit.Kilometer));
            }

            if (!string.IsNullOrEmpty(weatherPublishedDateText))
                tooltip = string.Format("{0}{1}{2}", tooltip, System.Environment.NewLine, weatherPublishedDateText);

            if (!string.IsNullOrEmpty(weatherUpdatedDateText))
                tooltip = string.Format("{0}{1}{2}", tooltip, System.Environment.NewLine, weatherUpdatedDateText);

            var googleMapsGeocodingApiSectionStart = 0;
            var googleMapsGeocodingApiSectionEnd = 0;
            if (googleMapsGeocodingApiCount.HasValue)
            {
                var googleMapsGeocodingApiSection = GetString(Resource.String.GoogleMapsGeocodingApiTitle);
                googleMapsGeocodingApiSectionStart = tooltip.Length + 1;
                googleMapsGeocodingApiSectionEnd = googleMapsGeocodingApiSectionStart + googleMapsGeocodingApiSection.Length + 1;
                tooltip = string.Format("{0}{1}{1}{2}{1}{3} {4}", tooltip, System.Environment.NewLine,
                    googleMapsGeocodingApiSection,
                    GetString(Resource.String.RequestCountMessage), googleMapsGeocodingApiCount);
            }

            var textFormatted = new SpannableString(tooltip);
            textFormatted.SetSpan(new UnderlineSpan(), 0, weatherSection.Length, SpanTypes.ExclusiveExclusive);
            if (googleMapsGeocodingApiSectionStart > 0)
            {
                textFormatted.SetSpan(new UnderlineSpan(), googleMapsGeocodingApiSectionStart,
                    googleMapsGeocodingApiSectionEnd, SpanTypes.ExclusiveExclusive);
            }
            _tooltipWindow.TooltipTextFormatted = textFormatted;
        }

        private void SubscribeEvents()
        {
            UnsubscribeEvents();
            if (_txtLogoWeather != null)
                _txtLogoWeather.Click += OnLogoWeatherClick;
        }

        private void UnsubscribeEvents()
        {
            if (_txtLogoWeather != null)
                _txtLogoWeather.Click -= OnLogoWeatherClick;
        }
    }
}