using System;
using Android.Views;

namespace Sb49.Weather.Droid.Ui.ViewHolders
{
    public class MenuWeatherProviderTitleViewHolder : MenuViewHolder
    {
        public MenuWeatherProviderTitleViewHolder(View itemView, Action<int> onItemClickListener) : base(itemView, onItemClickListener)
        {
            ViewLine = itemView.FindViewById<View>(Resource.Id.viewLine);
        }

        public View ViewLine { get; }
    }
}