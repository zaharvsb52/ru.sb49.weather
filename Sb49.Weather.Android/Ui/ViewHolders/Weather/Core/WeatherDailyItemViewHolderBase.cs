using System;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;

namespace Sb49.Weather.Droid.Ui.ViewHolders
{
    public abstract class WeatherDailyItemViewHolderBase : RecyclerView.ViewHolder
    {
        private readonly Action<int> _contentListener;
        private readonly Action<int> _detailsContentListener;

        protected WeatherDailyItemViewHolderBase(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference,
            transfer)
        {
        }

        protected WeatherDailyItemViewHolderBase(View itemView, Action<int> contentListener, Action<int> detailsContentListener) : base(itemView)
        {
            ViewContent = itemView.FindViewById<View>(Resource.Id.viewContent);
            Date = itemView.FindViewById<TextView>(Resource.Id.txtDate);
            ConditionImage = itemView.FindViewById<ImageView>(Resource.Id.imgCondition);
            ViewLine = itemView.FindViewById<View>(Resource.Id.viewLine);

            _contentListener = contentListener;
            _detailsContentListener = detailsContentListener;
        }

        public View ViewContent { get; protected set; }
        public TextView Date { get; protected set; }
        public ImageView ConditionImage { get; protected set; }
        public View ViewLine { get; protected set; }
        public abstract View ViewDetailsContent { get; }
        public TextView Sunrise { get; protected set; }
        public TextView Sunset { get; protected set; }
        public RecyclerView WeatherByHourly { get; protected set; }

        protected void Init()
        {
            OnInit();
        }

        protected virtual void OnInit()
        {
            SubscribeEvents();
        }

        protected virtual void OnItemClick(object sender, EventArgs e)
        {
            var position = AdapterPosition;
            if (position < 0 || _contentListener == null)
                return;

            _contentListener(position);
        }

        protected virtual void OnDetailstemClick(object sender, EventArgs e)
        {
            var position = AdapterPosition;
            if (position < 0 || _detailsContentListener == null)
                return;

            _detailsContentListener(position);
        }

        protected override void Dispose(bool disposing)
        {
            UnsubscribeEvents();

            base.Dispose(disposing);
        }

        protected virtual void SubscribeEvents()
        {
            UnsubscribeEvents();
            if (_contentListener != null && ViewContent != null)
                ViewContent.Click += OnItemClick;
            if (_detailsContentListener != null && ViewDetailsContent != null)
                ViewDetailsContent.Click += OnDetailstemClick;
        }

        protected virtual void UnsubscribeEvents()
        {
            if (ViewContent != null && ViewContent.Handle != IntPtr.Zero)
                ViewContent.Click -= OnItemClick;
            if(ViewDetailsContent != null && ViewDetailsContent.Handle != IntPtr.Zero)
                ViewDetailsContent.Click -= OnDetailstemClick;
        }
    }
}