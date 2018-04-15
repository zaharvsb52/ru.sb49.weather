using System;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;

namespace Sb49.Weather.Droid.Ui.ViewHolders
{
    public class MenuViewHolder : RecyclerView.ViewHolder
    {
        private readonly WeakReference<View> _weakReferenceView;
        protected readonly Action<int> OnItemClickListener;

        public MenuViewHolder(View itemView, Action<int> onItemClickListener) : base(itemView)
        {
            IconLeft = itemView.FindViewById<ImageView>(Resource.Id.imgIconLeft);
            IconRight = itemView.FindViewById<ImageView>(Resource.Id.imgIconRight);
            Title = itemView.FindViewById<TextView>(Resource.Id.txtItem);
            _weakReferenceView = new WeakReference<View>(itemView);
            OnItemClickListener = onItemClickListener;
            Init();
        }

        public ImageView IconLeft { get; }
        public ImageView IconRight { get; }
        public TextView Title { get; }

        protected View View
        {
            get
            {
                if (_weakReferenceView == null)
                    return null;
                return _weakReferenceView.TryGetTarget(out View target) ? target : null;
            }
        }

        private void Init()
        {
            OnInit();
        }

        protected virtual void OnInit()
        {
            if (OnItemClickListener != null)
                SubscribeEvents();
        }

        protected virtual void OnItemClick(object sender, EventArgs e)
        {
            if (OnItemClickListener == null || AdapterPosition < 0 || string.IsNullOrEmpty(Title?.Text))
                return;

            OnItemClickListener(AdapterPosition);
        }

        protected override void Dispose(bool disposing)
        {
            UnsubscribeEvents();

            base.Dispose(disposing);
        }

        protected virtual void SubscribeEvents()
        {
            UnsubscribeEvents();
            if (OnItemClickListener != null && View != null)
                View.Click += OnItemClick;
        }

        protected virtual void UnsubscribeEvents()
        {
            if (View != null)
                View.Click -= OnItemClick;
        }
    }
}