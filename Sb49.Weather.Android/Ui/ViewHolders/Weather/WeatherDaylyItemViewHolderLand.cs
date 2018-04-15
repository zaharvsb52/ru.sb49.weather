using System;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;

namespace Sb49.Weather.Droid.Ui.ViewHolders
{
    public class WeatherDaylyItemViewHolderLand : WeatherDailyItemViewHolderBase
    {
        private readonly WeakReference<View> _viewDetailsContentWeakReference;

        protected WeatherDaylyItemViewHolderLand(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference,
            transfer)
        {
        }

        public WeatherDaylyItemViewHolderLand(View itemView, WeakReference<View> viewDetailsContentWeakReference, Action<int> contentListener,
            Action<int> detailsContentListener) : base(itemView, contentListener, detailsContentListener)
        {
            _viewDetailsContentWeakReference = viewDetailsContentWeakReference;
            Day = itemView.FindViewById<TextView>(Resource.Id.txtDay);
            MinMaxTemperature = itemView.FindViewById<TextView>(Resource.Id.txtMinMaxTemp);

            Init();
        }

        public override View ViewDetailsContent
        {
            get
            {
                if (_viewDetailsContentWeakReference == null)
                    return null;

                return _viewDetailsContentWeakReference.TryGetTarget(out View viewDetailsContent)
                    ? viewDetailsContent
                    : null;
            }
        }

        public TextView Day { get; }
        public TextView MinMaxTemperature { get; }

        protected override void OnInit()
        {
            if (ViewDetailsContent != null)
            {
                Sunrise = ViewDetailsContent.FindViewById<TextView>(Resource.Id.txtSunrise);
                Sunset = ViewDetailsContent.FindViewById<TextView>(Resource.Id.txtSunset);
                WeatherByHourly = ViewDetailsContent.FindViewById<RecyclerView>(Resource.Id.gridWeatherByHourly);
            }

            base.OnInit();
        }
    }
}