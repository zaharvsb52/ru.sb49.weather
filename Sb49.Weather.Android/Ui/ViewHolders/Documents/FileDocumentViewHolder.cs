using System;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;

namespace Sb49.Weather.Droid.Ui.ViewHolders
{
    public class FileDocumentViewHolder : RecyclerView.ViewHolder
    {
        private readonly WeakReference<View> _view;
        private readonly Action<int> _onItemClickListener;

        public FileDocumentViewHolder(View itemView, Action<int> onItemClickListener) : base(itemView)
        {
            ViewContent = itemView.FindViewById<View>(Resource.Id.viewContent);
            Image = itemView.FindViewById<ImageView>(Resource.Id.imgItem);
            Item = itemView.FindViewById<TextView>(Resource.Id.txtItem);
            Subitem1 = itemView.FindViewById<TextView>(Resource.Id.txtSubitem1);
            Subitem2 = itemView.FindViewById<TextView>(Resource.Id.txtSubitem2);
            _view = new WeakReference<View>(itemView);

            _onItemClickListener = onItemClickListener;
            if (_onItemClickListener != null)
                SubscribeEvents();
        }

        public View ViewContent { get; }
        public ImageView Image { get; }
        public TextView Item { get; }
        public TextView Subitem1 { get; }
        public TextView Subitem2 { get; }

        private void OnClick(object sender, EventArgs e)
        {
            if (_onItemClickListener == null || AdapterPosition < 0)
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

            if (view != null && view.Handle != IntPtr.Zero)
                view.Click -= OnClick;
        }
    }
}