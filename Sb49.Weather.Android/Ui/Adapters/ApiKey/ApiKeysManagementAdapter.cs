using System;
using System.Collections.Generic;
using Android.Graphics.Drawables;
using Android.Support.V4.Content;
using Android.Support.V7.Widget;
using Android.Text;
using Android.Text.Method;
using Android.Text.Style;
using Android.Views;
using Sb49.Common.Support.v7.Droid.Ui;
using Sb49.Security.Core;
using Sb49.Weather.Droid.Ui.ViewHolders;

namespace Sb49.Weather.Droid.Ui.Adapters
{
    public class ApiKeysManagementAdapter : RecyclerView.Adapter
    {
        private readonly Model.Provider[] _providers;
        private AppCompatDrawableUtil _compatDrawableUtil;

        public event EventHandler<ItemEventArgs> ItemClick;

        public ApiKeysManagementAdapter(Model.Provider[] providers, IDictionary<int, ISb49SecureString> keys)
        {
            _providers = providers;
            ApiKeys = keys;
        }

        public IDictionary<int, ISb49SecureString> ApiKeys { get; set; }

        public override int ItemCount => _providers?.Length ?? 0;

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var itemView = LayoutInflater.From(parent.Context)
                .Inflate(Resource.Layout.view_apikey_item, parent, false);
            return new ApiKeysManagementViewHolder(itemView, OnItemClick);
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            var holder = (ApiKeysManagementViewHolder) viewHolder;
            var item = _providers[position];
            if (_compatDrawableUtil == null)
                _compatDrawableUtil = new AppCompatDrawableUtil();
            var resources = holder.ItemView.Resources;
            var context = holder.ItemView.Context;

            var transparent = ContextCompat.GetColor(context, Android.Resource.Color.Transparent);
            Drawable editDrawable = null;
            Drawable deleteDrawable = null;
            Drawable existsDrawable = null;

            if (!item.IsReadOnly)
            {
                editDrawable =
                    _compatDrawableUtil.GetDrawable(resources, Resource.Drawable.ic_menu_edit, context.Theme);
            }

            if (ApiKeys != null && ApiKeys.ContainsKey(item.Id))
            {
                if (!item.IsReadOnly)
                {
                    deleteDrawable =
                        _compatDrawableUtil.GetDrawable(resources, Resource.Drawable.ic_delete, context.Theme);
                }
                existsDrawable = _compatDrawableUtil.GetDrawable(resources, Resource.Drawable.ic_check, context.Theme);
            }

            if (editDrawable == null)
                holder.EditButton.SetImageResource(transparent);
            else
                holder.EditButton.SetImageDrawable(editDrawable);

            if (deleteDrawable == null)
                holder.DeleteButton.SetImageResource(transparent);
            else
                holder.DeleteButton.SetImageDrawable(deleteDrawable);

            if (existsDrawable == null)
                holder.ExistsApiKey.SetImageResource(transparent);
            else
                holder.ExistsApiKey.SetImageDrawable(existsDrawable);

            holder.ProviderName.MovementMethod = null;
            var title = context.GetString(item.TitleId);
            var spannable = new SpannableString(title);
            if (item.UrlApiId.HasValue)
            {
                spannable.SetSpan(new URLSpan(context.GetString(item.UrlApiId.Value)), 0,
                    title.Length, SpanTypes.ExclusiveExclusive);
                holder.ProviderName.MovementMethod = LinkMovementMethod.Instance;
            }
            holder.ProviderName.TextFormatted = spannable;
        }

        protected override void Dispose(bool disposing)
        {
            _compatDrawableUtil = null;

            base.Dispose(disposing);
        }

        private void OnItemClick(int position, ApiKeysManagementViewHolder.EditActions action)
        {
            if (_providers == null || action == ApiKeysManagementViewHolder.EditActions.None)
                return;

            ItemClick?.Invoke(this, new ItemEventArgs(_providers[position], position, action));
        }

        public class ItemEventArgs : EventArgs
        {
            public ItemEventArgs(Model.Provider item, int position, ApiKeysManagementViewHolder.EditActions action)
            {
                Item = item;
                Position = position;
                Action = action;
            }

            public Model.Provider Item { get; }
            public int Position { get; }
            public ApiKeysManagementViewHolder.EditActions Action { get; }
        }
    }
}