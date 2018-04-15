using System;
using Android.Content;
using Android.Graphics.Drawables;
using Android.Support.V7.Widget;
using Android.Views;
using Sb49.Common.Support.v7.Droid.Ui;
using Sb49.Weather.Droid.Common;
using Sb49.Weather.Droid.Model;
using Sb49.Weather.Droid.Ui.ViewHolders;

namespace Sb49.Weather.Droid.Ui.Adapters
{
    public class FileDocumentAdapter : RecyclerView.Adapter
    {
        private readonly FileDocumentItem[] _items;
        private int? _selectedPosition;
        private RecyclerView _recyclerView;

        public event EventHandler<ItemEventArgs> ItemClick;

        public FileDocumentAdapter(FileDocumentItem[] items)
        {
            _items = items;
        }

        public override int ItemCount => _items?.Length ?? 0;
        public bool ShowFileSize => AppSettings.Default.ShowFileSize;
        public bool ShowFolderSize => AppSettings.Default.ShowFolderSize;

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var itemView = LayoutInflater.From(parent.Context)
                .Inflate(Resource.Layout.view_list_file_item, parent, false);
            return new FileDocumentViewHolder(itemView, OnItemClick);
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            if(_items == null || ItemCount == 0)
                return;

            var holder = (FileDocumentViewHolder) viewHolder;
            var item = _items[position];
            if(item == null)
                return;

            var context = holder.ItemView.Context;
            var isFolder = item.IsFolder;

            if (holder.ViewContent != null)
            {
                int resId;
                if (_selectedPosition == position && !item.IsFolder)
                {
                    resId = Resource.Color.background_selected_list_file_item_color;
                    _recyclerView?.PostDelayed(() => { _recyclerView?.SmoothScrollToPosition(position); }, 300);
                }
                else
                {
                    resId = Android.Resource.Color.Transparent;
                }

                holder.ViewContent.SetBackgroundResource(resId);
            }

            if (holder.Image != null)
            {
                var resId = item.IconResourceId > 0 ? item.IconResourceId.Value : Android.Resource.Color.Transparent;
                if (item.TintColor.HasValue)
                {
                    var icon = ApplyTintColor(context, resId, item.TintColor.Value);
                    holder.Image.SetImageDrawable(icon);
                }
                else
                {
                    holder.Image.SetImageResource(resId);
                }
            }

            if (holder.Item != null)
                holder.Item.Text = item.DisplayName ?? string.Empty;

            if (holder.Subitem1 != null)
            {
                string subitem = null;
                if (item.LastModified.HasValue)
                    subitem = string.Format("{0:g}", item.LastModified.Value);
                holder.Subitem1.Text = subitem ?? string.Empty;
            }

            if (holder.Subitem2 != null)
            {
                string sizeText = null;

                if (ShowFileSize && item.Size.HasValue && !isFolder)
                {
                    sizeText = SizeToString(context, item.Size.Value);
                }

                if (ShowFolderSize && isFolder)
                {
                    if (item.Size > 0 && item.AvailableBytes >= 0)
                    {
                        sizeText = string.Format(context.GetString(Resource.String.FreeSpace),
                            SizeToString(context, item.AvailableBytes.Value),
                            SizeToString(context, item.Size.Value));
                    }
                }

                holder.Subitem2.Text = sizeText ?? string.Empty;
            }
        }

        public override void OnAttachedToRecyclerView(RecyclerView recyclerView)
        {
            base.OnAttachedToRecyclerView(recyclerView);
            _recyclerView = recyclerView;
        }

        public override void OnDetachedFromRecyclerView(RecyclerView recyclerView)
        {
            base.OnDetachedFromRecyclerView(recyclerView);
            _recyclerView = null;
        }

        protected override void Dispose(bool disposing)
        {
            _recyclerView = null;

            base.Dispose(disposing);
        }

        private void OnItemClick(int position)
        {
            if(_items == null || ItemCount == 0)
                return;

            if (_selectedPosition != position)
            {
                if (_selectedPosition.HasValue)
                    NotifyItemChanged(_selectedPosition.Value);

                _selectedPosition = position;
                NotifyItemChanged(position);
            }

            ItemClick?.Invoke(this, new ItemEventArgs(_items[position]));
        }

        private string SizeToString(Context context, long size)
        {
            const int kb = 1024;
            const int mb = 1048576;
            const int gb = 1073741824;

            string sizeText;
            if (size >= gb)
            {
                sizeText = string.Format("{0:N2} {1}", (double) size / gb,
                    context.GetString(Resource.String.Gb));
            }
            else if (size >= mb)
            {
                sizeText = string.Format("{0:N2} {1}", (double) size / mb,
                    context.GetString(Resource.String.Mb));
            }
            else if (size >= kb)
            {
                sizeText = string.Format("{0:N2} {1}", (double) size / kb,
                    context.GetString(Resource.String.Kb));
            }
            else
                sizeText = string.Format("{0} {1}", size, context.GetString(Resource.String.Byte));

            return sizeText;
        }

        private Drawable ApplyTintColor(Context context, int drawableId, int tintColor)
        {
            var compatDrawableUtil = new AppCompatDrawableUtil();
            var icon = compatDrawableUtil.GetDrawable(context.Resources, drawableId, context.Theme);
            return compatDrawableUtil.ChangeColorTint(icon, tintColor);
        }

        public class ItemEventArgs : EventArgs
        {
            public ItemEventArgs(FileDocumentItem item)
            {
                Item = item;
            }

            public FileDocumentItem Item { get; }
        }
    }
}