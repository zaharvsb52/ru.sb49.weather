using System;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;

namespace Sb49.Weather.Droid.Ui.ViewHolders
{
    public class LocationViewHolder : RecyclerView.ViewHolder
    {
        private readonly WeakReference<View> _view;
        private readonly Action<int> _onItemClickListener;

        public LocationViewHolder(View itemView, Action<int> onItemClickListener) : base(itemView)
        {
            Item = itemView.FindViewById<TextView>(Resource.Id.txtAddressItem);
            _view = new WeakReference<View>(itemView);

            if (onItemClickListener != null)
            {
                _onItemClickListener = onItemClickListener;
                SubscribeEvents();
            }
        }

        public TextView Item { get; }

        private void OnClick(object sender, EventArgs e)
        {
            if (_onItemClickListener == null || AdapterPosition < 0 || string.IsNullOrEmpty(Item?.Text))
                return;

            _onItemClickListener(AdapterPosition);
        }

        protected override void Dispose(bool disposing)
        {
            UnsubscribeEvents();

            base.Dispose(disposing);
        }

        private View GetView()
        {
            if (_view == null)
                return null;

            return _view.TryGetTarget(out View target) ? target : null;
        }

        private void SubscribeEvents()
        {
            var view = GetView();
            UnsubscribeEvents(view);
            if (_onItemClickListener != null && view != null)
                view.Click += OnClick;
        }

        private void UnsubscribeEvents(View view = null)
        {
            if (view == null)
                view = GetView();

            if (view != null)
                view.Click -= OnClick;
        }
    }
}