using System;
using System.Linq;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Views;
using Sb49.Weather.Droid.Ui.ViewHolders;
using Sb49.Weather.Model;

namespace Sb49.Weather.Droid.Ui.Adapters
{
    public abstract class WeatherDailyItemAdapterBase : RecyclerView.Adapter
    {
        private readonly WeakReference<WeatherTools> _weakReferenceWeatherTools;

        public event EventHandler<WeatherDailyItemEventArgs> ItemClick;

        protected WeatherDailyItemAdapterBase(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference,
            transfer)
        {
        }

        protected WeatherDailyItemAdapterBase(WeatherDataPointDaily[] daily, WeatherTools weatherTools)
        {
            Daily = daily;
            _weakReferenceWeatherTools = new WeakReference<WeatherTools>(weatherTools);
        }

        protected WeatherDataPointDaily[] Daily { get; }
        protected int? ExpandedPosition { get; set; }
        protected int? SelectedPosition { get; set; }
        protected int SelectedItemAnimationDuration { get; set; } = 800;
        protected LinearLayoutManager LayoutManager;
        protected RecyclerView RecyclerView { get; private set; }
        protected WeatherHourlyItemAdapter WeatherHourlyItemAdapter { get; set; }
        public override int ItemCount => Daily?.Length ?? 0;
        public abstract override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType);
        protected abstract View GetContentView(RecyclerView.ViewHolder holder);
        protected abstract View GetDetailsContentView(RecyclerView.ViewHolder holder);

        protected WeatherTools WeatherTools
        {
            get
            {
                if (_weakReferenceWeatherTools == null)
                    return null;

                return _weakReferenceWeatherTools.TryGetTarget(out WeatherTools weatherTools) ? weatherTools : null;
            }
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            OnBindContentViewHolder(holder, position);
            OnSelectedItem(GetContentView(holder), position);
            OnBindDetailsViewHolder(holder, GetDetailsContentView(holder), position);
        }

        protected abstract void OnBindContentViewHolder(RecyclerView.ViewHolder viewHolder, int position);
        protected abstract void OnSelectedItem(View view, int position);

        protected virtual void OnBindDetailsViewHolder(RecyclerView.ViewHolder holder, View detailsContent, int position)
        {
            var visibility = ExpandedPosition == position ? ViewStates.Visible : ViewStates.Gone;
            if (detailsContent != null)
                detailsContent.Visibility = visibility;

            if (visibility == ViewStates.Visible)
            {
                var e = new WeatherDailyItemEventArgs(position);
                OnItemClick(e);

                var daily = Daily?[position];
                var hourly = daily?.Hourly?.ToArray();
                OnBindDetailsViewHolder(holder as WeatherDailyItemViewHolderBase, position, hourly);

                //http://stackoverflow.com/questions/26875061/scroll-recyclerview-to-show-selected-item-on-top
                //RecyclerView?.ScrollToPosition(position);
                //RecyclerView?.SmoothScrollToPosition(position);
                RecyclerView?.PostDelayed(() => { RecyclerView?.SmoothScrollToPosition(position); }, 300);
            }
        }

        protected virtual void OnBindDetailsViewHolder(WeatherDailyItemViewHolderBase viewHolder, int position, WeatherDataPointHourly[] hourly)
        {
            if (Daily == null || viewHolder == null || WeatherTools == null)
                return;

            var item = Daily[position];
            var context = viewHolder.ItemView.Context;

            var sunriseText = string.Empty;
            if (item.Astronomy?.Sunrise != null)
            {
                sunriseText = WeatherTools.Format("{0} {1:t}", context.GetString(Resource.String.Sunrise),
                    item.Astronomy.Sunrise.Value.ToLocalTime());
            }
            if (viewHolder.Sunrise != null)
            {
                viewHolder.Sunrise.Text = sunriseText;
                viewHolder.Sunrise.Visibility = IsVisible(sunriseText);
            }

            var sunsetText = string.Empty;
            if (item.Astronomy?.Sunset != null)
            {
                sunsetText = WeatherTools.Format("{0} {1:t}", context.GetString(Resource.String.Sunset),
                    item.Astronomy.Sunset.Value.ToLocalTime());
            }
            if (viewHolder.Sunset != null)
            {
                viewHolder.Sunset.Text = sunsetText;
                viewHolder.Sunset.Visibility = IsVisible(sunsetText);
            }

            if (viewHolder.WeatherByHourly != null)
            {
                LayoutManager?.Dispose();
                LayoutManager = new LinearLayoutManager(context, LinearLayoutManager.Horizontal, false);
                WeatherHourlyItemAdapter?.Dispose();
                WeatherHourlyItemAdapter = new WeatherHourlyItemAdapter(hourly, _weakReferenceWeatherTools);
                viewHolder.WeatherByHourly.SetLayoutManager(LayoutManager);
                viewHolder.WeatherByHourly.SetAdapter(WeatherHourlyItemAdapter);
            }
        }

        protected ViewStates IsVisible(string text)
        {
            return string.IsNullOrEmpty(text) ? ViewStates.Gone : ViewStates.Visible;
        }

        public override void OnAttachedToRecyclerView(RecyclerView recyclerView)
        {
            base.OnAttachedToRecyclerView(recyclerView);
            RecyclerView = recyclerView;
        }

        public override void OnDetachedFromRecyclerView(RecyclerView recyclerView)
        {
            base.OnDetachedFromRecyclerView(recyclerView);
            RecyclerView = null;
        }
        
        protected override void Dispose(bool disposing)
        {
            LayoutManager?.Dispose();
            LayoutManager = null;
            WeatherHourlyItemAdapter?.Dispose();
            WeatherHourlyItemAdapter = null;
            RecyclerView = null;

            base.Dispose(disposing);
        }

        protected virtual void OnItemClick(WeatherDailyItemEventArgs e)
        {
            ItemClick?.Invoke(this, e);
        }

        public class WeatherDailyItemEventArgs : EventArgs
        {
            public WeatherDailyItemEventArgs(int position)
            {
                Position = position;
            }

            public int Position { get; }
        }
    }
}