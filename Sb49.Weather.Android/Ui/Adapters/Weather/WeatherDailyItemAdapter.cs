using System;
using System.Linq;
using Android.Animation;
using Android.Graphics;
using Android.Runtime;
using Android.Support.V4.Content;
using Android.Support.V7.Widget;
using Android.Text;
using Android.Views;
using Sb49.Weather.Droid.Ui.ViewHolders;
using Sb49.Weather.Model;

namespace Sb49.Weather.Droid.Ui.Adapters
{
    public class WeatherDailyItemAdapter : WeatherDailyItemAdapterBase
    {
        protected WeatherDailyItemAdapter(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference,
            transfer)
        {
        }

        public WeatherDailyItemAdapter(WeatherDataPointDaily[] daily, WeatherTools weatherTools) : base(daily, weatherTools)
        {
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var itemView = LayoutInflater.From(parent.Context).
               Inflate(Resource.Layout.view_weather_daily_item, parent, false);
            return new WeatherDailyItemViewHolder(itemView, OnItemClick, OnDetailstemClick);
        }

        protected override View GetContentView(RecyclerView.ViewHolder holder)
        {
            return ((WeatherDailyItemViewHolder)holder)?.ViewContent;
        }

        protected override View GetDetailsContentView(RecyclerView.ViewHolder holder)
        {
            return ((WeatherDailyItemViewHolder)holder)?.ViewDetailsContent;
        }

        protected override void OnBindContentViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            if (Daily == null)
                return;

            var holder = (WeatherDailyItemViewHolder) viewHolder;
            var context = holder.ItemView.Context;
            var item = Daily[position];
            var isTodayPosition = false;

            var dateText = string.Empty;
            var lineBackgroundColorId = Resource.Color.lineBackgroundColor;
            var dateTextColorId = Resource.Color.dateTextColor;
            var existsDate = holder.Date != null;
            if (item.Date.HasValue)
            {
                var itemDate = item.Date.Value.ToLocalTime();
                isTodayPosition = itemDate.Date == DateTime.Today;
                if (existsDate)
                {
                    dateText = WeatherTools.Format("{0:ddd}, {1}", itemDate,
                        isTodayPosition
                            ? context.GetString(Resource.String.Today)
                            : WeatherTools.Format("{0:d MMMM}", itemDate));
                }

                var dayOfWeek = itemDate.DayOfWeek;
                if (dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday)
                    dateTextColorId = Resource.Color.dateTextColorHighlight;

                var dayOfWeekPrev = itemDate.AddDays(1).DayOfWeek;
                if (dayOfWeekPrev == WeatherTools.CultureInfo.DateTimeFormat.FirstDayOfWeek)
                    lineBackgroundColorId = Resource.Color.lineBackgroundColorHighlight;
            }

            if (existsDate)
            {
                holder.Date.Text = dateText;
                holder.Date.SetTextColor(new Color(ContextCompat.GetColor(context, dateTextColorId)));
            }

            holder.ViewLine?.SetBackgroundColor(new Color(ContextCompat.GetColor(context, lineBackgroundColorId)));

            Java.Lang.ICharSequence minTemperatureText = new SpannableString(string.Empty);
            //var mintemps = new List<double?> {item.MinTemperature};
            //if (isTodayPosition && Data.Currently?.MinTemperature != null)
            //    mintemps.Add(Data.Currently.MinTemperature);
            //var minTemp = WeatherTools.CalculateMinTemperature(item.Date, Data, mintemps.ToArray());
            var minTemp = item.MinTemperature;

            Java.Lang.ICharSequence maxTemperatureText = new SpannableString(string.Empty);
            //var maxtemps = new List<double?> {item.MaxTemperature};
            //if (isTodayPosition && Data.Currently?.MaxTemperature != null)
            //    mintemps.Add(Data.Currently.MaxTemperature);
            //var maxTemp = WeatherTools.CalculateMaxTemperature(item.Date, Data, maxtemps.ToArray());
            var maxTemp = item.MaxTemperature;

            if (minTemp.HasValue && maxTemp.HasValue)
            {
                var degree = WeatherTools.DegreeString;
                minTemperatureText = WeatherTools.ConvertTemperatureToAlertedStyle(context, minTemp.Value, "{0:f0}{1}",
                    degree);
                maxTemperatureText = WeatherTools.ConvertTemperatureToAlertedStyle(context, maxTemp.Value, "{0:f0}{1}",
                    degree);
            }
            if (holder.MinTemperature != null && holder.MaxTemperature != null)
            {
                holder.MinTemperature.TextFormatted = minTemperatureText;
                holder.MaxTemperature.TextFormatted = maxTemperatureText;
            }

            holder.ConditionImage?.SetImageDrawable(WeatherTools.GetConditionIcon(WidgetTypes.Item, null, item,
                isTodayPosition, isTodayPosition));

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
                        WeatherTools.WindDirectionDrawable(item.WindDirection.Value));
                }

                if (existsWindDirection)
                {
                    windDirectionText = WeatherTools.WindDirectionToCardinal(item.WindDirection.Value);
                }
            }

            if (item.WindSpeed.HasValue)
            {
                winSpeedText = WeatherTools.ConvertWindSpeedToString(item.WindSpeed.Value, "{0:f0} {1}",
                    WeatherTools.WindSpeedUnitString);
            }

            if (existsWindDirectionImage)
                holder.WindDirectionImage.Visibility = visibility;
            if (existsWindDirection)
                holder.WindDirection.Text = windDirectionText;
            if (holder.WindSpeed != null)
                holder.WindSpeed.Text = winSpeedText;
        }

        protected override void OnBindDetailsViewHolder(WeatherDailyItemViewHolderBase viewHolder, int position, WeatherDataPointHourly[] hourly)
        {
            if (viewHolder == null)
                return;

            base.OnBindDetailsViewHolder(viewHolder, position, hourly);

            var holder = (WeatherDailyItemViewHolder) viewHolder;
            var context = holder.ItemView.Context;

            var pressureText = string.Empty;
            var minPressure = hourly?.Min(p => p.Pressure);
            var maxPressure = hourly?.Max(p => p.Pressure);
            if (minPressure.HasValue && maxPressure.HasValue)
            {
                if (hourly.Length == 1 || (int)minPressure.Value == (int)maxPressure.Value)
                {
                    pressureText = WeatherTools.ConvertPressureToString(minPressure.Value, "{1} {0:f0} {2}",
                        context.GetString(Resource.String.Pressure), WeatherTools.PressureUnitString);
                }
                else
                {
                    pressureText = WeatherTools.ConvertPressureToString(minPressure.Value, maxPressure.Value,
                        "{2} {0:f0}-{1:f0} {3}", context.GetString(Resource.String.Pressure),
                        WeatherTools.PressureUnitString);
                }
            }
            if (holder.Pressure != null)
            {
                holder.Pressure.Text = pressureText;
                holder.Pressure.Visibility = IsVisible(pressureText);
            }

            var humidityText = string.Empty;
            var minHumidity = hourly?.Min(p => p.Humidity);
            var maxHumidity = hourly?.Max(p => p.Humidity);
            if (minHumidity.HasValue && maxHumidity.HasValue)
            {
                var txthumidity = context.GetString(Resource.String.Humidity);
                humidityText = hourly.Length == 1 || (int)(minHumidity * 100) == (int)(maxHumidity * 100)
                    ? WeatherTools.Format("{0} {1:p0}", txthumidity, minHumidity)
                    : WeatherTools.Format("{0} {1:f0}-{2:p0}", txthumidity, minHumidity * 100, maxHumidity);
            }
            if (holder.Humidity != null)
            {
                holder.Humidity.Text = humidityText;
                holder.Humidity.Visibility = IsVisible(humidityText);
            }
        }

        protected override void OnBindDetailsViewHolder(RecyclerView.ViewHolder holder, View detailsContent, int position)
        {
            var daily = Daily?[position];
            if (ExpandedPosition != position || daily == null)
            {
                if (detailsContent != null)
                    detailsContent.Visibility = ViewStates.Gone;
                return;
            }

            base.OnBindDetailsViewHolder(holder, detailsContent, position);
        }

        protected override void OnSelectedItem(View view, int position)
        {
            if (view == null)
                return;

            if (SelectedPosition == position)
            {
                var colorAnimation = ValueAnimator.OfObject(new ArgbEvaluator(), 0, 1);
                colorAnimation.Update += (s, e) =>
                {
                    var value = (int) e.Animation.AnimatedValue;
                    view.SetBackgroundResource(value == 0
                        ? Resource.Drawable.background_selected_weather_daily_item_color
                        : Resource.Color.background_weather_daily_item_color);
                };

                colorAnimation.SetDuration(SelectedItemAnimationDuration).Start();
            }
        }

        private void OnItemClick(int position)
        {
            SelectedPosition = position;
            if (ExpandedPosition == position)
            {
                ExpandedPosition = null;
                NotifyItemChanged(position);
            }
            else
            {
                if (ExpandedPosition.HasValue)
                    NotifyItemChanged(ExpandedPosition.Value);

                ExpandedPosition = position;
                NotifyItemChanged(position);
            }
        }

        private void OnDetailstemClick(int position)
        {
            SelectedPosition = null;
            ExpandedPosition = null;
            NotifyItemChanged(position);
        }
    }
}