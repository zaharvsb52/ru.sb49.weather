using System;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;

namespace Sb49.Weather.Droid.Ui.ViewHolders
{
    public class ApiKeysManagementViewHolder : RecyclerView.ViewHolder
    {
        private readonly Action<int, EditActions> _onItemClickListener;

        public ApiKeysManagementViewHolder(View itemView, Action<int, EditActions> onItemClickListener) : base(itemView)
        {
            EditButton = itemView.FindViewById<ImageView>(Resource.Id.imgEdit);
            DeleteButton = itemView.FindViewById<ImageView>(Resource.Id.imgDelete);
            ExistsApiKey = itemView.FindViewById<ImageView>(Resource.Id.imgExists);
            ProviderName = itemView.FindViewById<TextView>(Resource.Id.txtProviderName);
            _onItemClickListener = onItemClickListener;
            if (_onItemClickListener != null)
                SubscribeEvents();
        }

        public ImageView EditButton { get; }
        public ImageView DeleteButton { get; }
        public ImageView ExistsApiKey { get; }
        public TextView ProviderName { get; }

        private void OnDeleteButtonClick(object sender, EventArgs e)
        {
            if (_onItemClickListener == null || AdapterPosition < 0)
                return;

            _onItemClickListener(AdapterPosition, EditActions.Delete);
        }

        private void OnEditButtonClick(object sender, EventArgs e)
        {
            if (_onItemClickListener == null || AdapterPosition < 0 || sender == null)
                return;

            _onItemClickListener(AdapterPosition, EditActions.Edit);
        }

        protected override void Dispose(bool disposing)
        {
            UnsubscribeEvents();

            base.Dispose(disposing);
        }

        private void SubscribeEvents()
        {
            UnsubscribeEvents();
            if (_onItemClickListener == null)
                return;

            if (DeleteButton != null)
                DeleteButton.Click += OnDeleteButtonClick;
            if (EditButton != null)
                EditButton.Click += OnEditButtonClick;
        }

        private void UnsubscribeEvents()
        {
            if (DeleteButton != null)
                DeleteButton.Click -= OnDeleteButtonClick;
            if (EditButton != null)
                EditButton.Click -= OnEditButtonClick;
        }

        public enum EditActions
        {
            None,
            Edit,
            Delete
        }
    }
}