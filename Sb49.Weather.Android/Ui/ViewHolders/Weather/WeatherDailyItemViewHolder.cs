using System;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;

namespace Sb49.Weather.Droid.Ui.ViewHolders
{
    public class WeatherDailyItemViewHolder : WeatherDailyItemViewHolderBase
    {
        protected WeatherDailyItemViewHolder(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference,
            transfer)
        {
        }

        public WeatherDailyItemViewHolder(View itemView, Action<int> contentListener,
            Action<int> detailsContentListener) : base(itemView, contentListener, detailsContentListener)
        {
            ViewDetailsContent = itemView.FindViewById<View>(Resource.Id.viewDetailsContent);
            Sunrise = itemView.FindViewById<TextView>(Resource.Id.txtSunrise);
            Sunset = itemView.FindViewById<TextView>(Resource.Id.txtSunset);
            WeatherByHourly = itemView.FindViewById<RecyclerView>(Resource.Id.gridWeatherByHourly);

            MinTemperature = itemView.FindViewById<TextView>(Resource.Id.txtMinTemp);
            MaxTemperature = itemView.FindViewById<TextView>(Resource.Id.txtMaxTemp);
            WindDirectionImage = itemView.FindViewById<ImageView>(Resource.Id.imgWindDirection);
            WindDirection = itemView.FindViewById<TextView>(Resource.Id.txtWindDirection);
            WindSpeed = itemView.FindViewById<TextView>(Resource.Id.txtWindSpeed);
            Pressure = itemView.FindViewById<TextView>(Resource.Id.txtPressure);
            Humidity = itemView.FindViewById<TextView>(Resource.Id.txtHumidity);

            Init();
        }

        public override View ViewDetailsContent { get; }

        public TextView MinTemperature { get; }
        public TextView MaxTemperature { get; }
        public ImageView WindDirectionImage { get; }
        public TextView WindDirection { get; }
        public TextView WindSpeed { get; }
        public TextView Pressure { get; }
        public TextView Humidity { get; }
    }
}