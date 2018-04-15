using System;
using Android.Graphics;
using Android.Runtime;
using Android.Support.V4.Content;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Sb49.Weather.Droid.Ui.ViewHolders;
using Sb49.Weather.Model;

namespace Sb49.Weather.Droid.Ui.Adapters
{
    public class WeatherDailyItemAdapterLand : WeatherDailyItemAdapterBase
    {
        private readonly WeakReference<ScrollView> _scrollView;
        private readonly WeakReference<View> _viewDetailsContent;

        protected WeatherDailyItemAdapterLand(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference,
            transfer)
        {
        }

        public WeatherDailyItemAdapterLand(WeatherDataPointDaily[] daily, WeatherTools weatherTools,
            View viewDetailsContent, ScrollView scrollView) : base(daily, weatherTools)
        {
            _viewDetailsContent = new WeakReference<View>(viewDetailsContent);
            _scrollView = new WeakReference<ScrollView>(scrollView);
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var itemView = LayoutInflater.From(parent.Context)
                .Inflate(Resource.Layout.view_weather_daily_item, parent, false);
            return new WeatherDaylyItemViewHolderLand(itemView, _viewDetailsContent, OnItemClick, null);
        }

        protected override void OnBindContentViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            if (Daily == null)
                return;

            var holder = (WeatherDaylyItemViewHolderLand) viewHolder;
            var context = holder.ItemView.Context;
            var item = Daily[position];

            var dayText = string.Empty;
            var dateText = string.Empty;
            var lineBackgroundColorId = Resource.Color.lineBackgroundColor;
            var dateTextColorId = Resource.Color.dateTextColor;
            var existsDay = holder.Day != null;
            var existsDate = holder.Date != null;
            var isTodayPosition = false;
            if (item.Date.HasValue)
            {
                var itemDate = item.Date.Value.ToLocalTime();
                isTodayPosition = itemDate.Date == DateTime.Today;
                if (existsDay)
                    dayText = WeatherTools.Format("{0:ddd}", itemDate);

                if (existsDate)
                {
                    dateText = WeatherTools.Format("{0}",
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

            var datecolor = new Color(ContextCompat.GetColor(context, dateTextColorId));
            if (existsDay)
            {
                holder.Day.Text = dayText;
                holder.Day.SetTextColor(datecolor);
            }
            if (existsDate)
            {
                holder.Date.Text = dateText;
                holder.Date.SetTextColor(datecolor);
            }

            holder.ViewLine?.SetBackgroundColor(new Color(ContextCompat.GetColor(context, lineBackgroundColorId)));

            var minMaxTemperatureText = string.Empty;
            if (item.MinTemperature.HasValue && item.MaxTemperature.HasValue)
            {
                minMaxTemperatureText = WeatherTools.ConvertTemperatureToString(item.MinTemperature.Value,
                    item.MaxTemperature.Value, "{0:f0}{2}   {1:f0}{2}", WeatherTools.DegreeString);
            }
            if (holder.MinMaxTemperature != null)
                holder.MinMaxTemperature.Text = minMaxTemperatureText;

            holder.ConditionImage?.SetImageDrawable(WeatherTools.GetConditionIcon(WidgetTypes.Item, null, item,
                isTodayPosition, isTodayPosition));
        }

        protected override View GetContentView(RecyclerView.ViewHolder holder)
        {
            return ((WeatherDaylyItemViewHolderLand) holder)?.ViewContent;
        }

        protected override View GetDetailsContentView(RecyclerView.ViewHolder holder)
        {
            return ((WeatherDaylyItemViewHolderLand) holder)?.ViewDetailsContent;
        }

        protected override void OnBindDetailsViewHolder(RecyclerView.ViewHolder holder, View detailsContent, int position)
        {
            var daily = Daily?[position];
            if (ExpandedPosition != position || daily == null)
                return;

            base.OnBindDetailsViewHolder(holder, detailsContent, position);
        }

        protected override void OnSelectedItem(View view, int position)
        {
            if (view == null)
                return;

            if (ExpandedPosition == position)
            {
                view.SetBackgroundColor(Color.Transparent);

                if (_scrollView != null)
                {
                    if (_scrollView.TryGetTarget(out ScrollView scrollView))
                        scrollView.PostDelayed(() => { scrollView.FullScroll(FocusSearchDirection.Down); }, 400);
                }
            }
            else
            {
                view.SetBackgroundResource(Resource.Color.background_weather_daily_item_color);
            }
        }

        private void OnItemClick(int position)
        {
            SelectedPosition = position;
            if (ExpandedPosition != position)
            {
                if (ExpandedPosition.HasValue)
                    NotifyItemChanged(ExpandedPosition.Value);

                ExpandedPosition = position;
                NotifyItemChanged(position);
            }
        }
    }
}