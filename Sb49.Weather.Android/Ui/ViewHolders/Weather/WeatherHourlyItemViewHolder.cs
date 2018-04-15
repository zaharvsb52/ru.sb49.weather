using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;

namespace Sb49.Weather.Droid.Ui.ViewHolders
{
    public class WeatherHourlyItemViewHolder : RecyclerView.ViewHolder
    {
        public WeatherHourlyItemViewHolder(View itemView) : base(itemView)
        {
            Hour = itemView.FindViewById<TextView>(Resource.Id.txtHour);
            ConditionImage = itemView.FindViewById<ImageView>(Resource.Id.imgCondition);
            Temperature = itemView.FindViewById<TextView>(Resource.Id.txtTemp);
            WindDirectionImage = itemView.FindViewById<ImageView>(Resource.Id.imgWindDirection);
            WindDirection = itemView.FindViewById<TextView>(Resource.Id.txtWindDirection);
            WindSpeed = itemView.FindViewById<TextView>(Resource.Id.txtWindSpeed);
        }

        public TextView Hour { get; }
        public ImageView ConditionImage { get; }
        public TextView Temperature { get; }
        public ImageView WindDirectionImage { get; }
        public TextView WindDirection { get; }
        public TextView WindSpeed { get; }
    }
}