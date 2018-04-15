using System;
using Android.Views;

namespace Sb49.Weather.Droid.Ui.ViewHolders
{
    public class MenuLocationViewHolder : MenuViewHolder
    {
        private readonly Action<int> _onEditClickListener;
        private readonly Action<int> _onDeleteClickListener;

        public MenuLocationViewHolder(View itemView, Action<int> onItemClickListener, Action<int> onEditClickListener,
            Action<int> onDeleteClickListener) : base(itemView, onItemClickListener)
        {
            _onEditClickListener = onEditClickListener;
            _onDeleteClickListener = onDeleteClickListener;
            Init();
        }

        private void Init()
        {
            SubscribeEvents();
        }

        protected override void OnInit()
        {
        }

        private void OnEditClick(object sender, EventArgs e)
        {
            if (AdapterPosition < 0)
                return;

            _onEditClickListener?.Invoke(AdapterPosition);
        }

        private void OnDeleteClick(object sender, EventArgs e)
        {
            if (AdapterPosition < 0)
                return;

            _onDeleteClickListener?.Invoke(AdapterPosition);
        }

        protected override void SubscribeEvents()
        {
            base.SubscribeEvents();
            if (_onEditClickListener != null && IconLeft != null)
                IconLeft.Click += OnEditClick;
            if (_onDeleteClickListener != null && IconRight != null)
                IconRight.Click += OnDeleteClick;
        }

        protected override void UnsubscribeEvents()
        {
            base.UnsubscribeEvents();
            if (_onEditClickListener != null && IconLeft != null && IconLeft.Handle != IntPtr.Zero)
                IconLeft.Click -= OnEditClick;
            if (_onDeleteClickListener != null && IconRight != null && IconRight.Handle != IntPtr.Zero)
                IconRight.Click -= OnDeleteClick;
        }
    }
}