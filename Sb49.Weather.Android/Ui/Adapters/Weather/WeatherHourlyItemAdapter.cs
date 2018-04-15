using System;
using Android.Support.V7.Widget;
using Android.Text;
using Android.Views;
using Sb49.Weather.Droid.Ui.ViewHolders;
using Sb49.Weather.Model;

namespace Sb49.Weather.Droid.Ui.Adapters
{
    public class WeatherHourlyItemAdapter : RecyclerView.Adapter
    {
        private readonly WeatherDataPointHourly[] _hourly;
        private readonly WeakReference<WeatherTools> _weakReferenceWeatherTools;

        public WeatherHourlyItemAdapter(WeatherDataPointHourly[] hourly, WeakReference<WeatherTools> weakReferenceWeatherTools)
        {
            _hourly = hourly;
            _weakReferenceWeatherTools = weakReferenceWeatherTools;
        }

        public override int ItemCount => _hourly?.Length ?? 0;

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var itemView = LayoutInflater.From(parent.Context).
              Inflate(Resource.Layout.view_weather_hourly_details_item, parent, false);
            return new WeatherHourlyItemViewHolder(itemView);
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            if (_hourly == null || _weakReferenceWeatherTools == null)
                return;

            if (!_weakReferenceWeatherTools.TryGetTarget(out WeatherTools weatherTools))
                return;

            var holder = (WeatherHourlyItemViewHolder)viewHolder;
            var item = _hourly[position];

            var hourText = string.Empty;
            if (item.Date.HasValue)
            {
                var itemDate = item.Date.Value.ToLocalTime();
                hourText = weatherTools.Format("{0:HH:mm}", itemDate);
            }
            if (holder.Hour != null)
                holder.Hour.Text = hourText ?? string.Empty;

            holder.ConditionImage?.SetImageDrawable(weatherTools.GetConditionIcon(WidgetTypes.Item, null, item, true, false));

            Java.Lang.ICharSequence tempText = new SpannableString(string.Empty);
            var degree = weatherTools.DegreeString;
            if (item.Temperature.HasValue)
            {
                tempText = weatherTools.ConvertTemperatureToAlertedStyle(holder.ItemView.Context,
                    item.Temperature.Value, "{0:f0}{1}", degree);
            }
            if (holder.Temperature != null)
                holder.Temperature.TextFormatted = tempText;

            var visibility = ViewStates.Gone;
            var winSpeedText = string.Empty;
            var windDirectionText = string.Empty;
            var existsWindDirectionImage = holder.WindDirectionImage != null;
            var existsWindDirection = holder.WindDirection != null;
            if (item.WindDirection.HasValue)
            {
                if (existsWindDirectionImage)
                {
                    visibility = ViewStates.Visible;
                    holder.WindDirectionImage.SetImageBitmap(
                        weatherTools.WindDirectionDrawable(item.WindDirection.Value));
                }

                if (existsWindDirection)
                {
                    windDirectionText = weatherTools.WindDirectionToCardinal(item.WindDirection.Value);
                }
            }

            if (item.WindSpeed.HasValue)
            {
                winSpeedText = weatherTools.ConvertWindSpeedToString(item.WindSpeed.Value, "{0:f0} {1}",
                    weatherTools.WindSpeedUnitString);
            }

            if (existsWindDirectionImage)
                holder.WindDirectionImage.Visibility = visibility;
            if (existsWindDirection)
                holder.WindDirection.Text = windDirectionText;
            if (holder.WindSpeed != null)
                holder.WindSpeed.Text = winSpeedText;
        }
    }
}