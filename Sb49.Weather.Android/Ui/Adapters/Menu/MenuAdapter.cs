using System;
using System.Collections.Generic;
using System.Linq;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Support.V4.Content;
using Android.Support.V7.Widget;
using Android.Views;
using Sb49.Common.Support.v7.Droid.Ui;
using Sb49.Weather.Droid.Common;
using Sb49.Weather.Droid.Model;
using Sb49.Weather.Droid.Ui.ViewHolders;

namespace Sb49.Weather.Droid.Ui.Adapters
{
    public class MenuAdapter : RecyclerView.Adapter
    {
        private int? _selectedProviderPosition;
        private bool _isOpenedWeatherProviderTitle;
        private int? _selectedProviderId;
        private int? _selectedLocationPosition;
        private int? _selectedLocationId;
        private bool _isWaiting;
        private bool _isEditing;
        private readonly int _wetherProviderIconId;
        private readonly AppCompatDrawableUtil _compatDrawableUtil;

        public event EventHandler<MenuItemEventArgs> ItemClick;

        public MenuAdapter(IEnumerable<MenuItem> menu, MenuItem[] weatherProviders, int currentWeatherProviderId,
            int currentLocationId, int wetherProviderIconId)
        {
            MenuItems = new List<MenuItem>(menu);
            WeatherProviders = weatherProviders;
            if (currentWeatherProviderId != -1)
                _selectedProviderId = currentWeatherProviderId;
            if (currentLocationId != -1)
            {
                _selectedLocationId = currentLocationId;
                ChangeSelectedPosition(_selectedLocationId.Value, ref _selectedLocationPosition,
                    new[] {MenuItemTypes.CurrentLocation, MenuItemTypes.Location}, false);
            }
            _wetherProviderIconId = wetherProviderIconId;
            _compatDrawableUtil = new AppCompatDrawableUtil();
        }

        public List<MenuItem> MenuItems { get; set; }
        public MenuItem[] WeatherProviders { get; set; }

        public bool IsWaiting
        {
            get => _isWaiting;
            set
            {
                if(_isWaiting == value)
                    return;

                _isWaiting = value;
                NotifyItem(item => item != null && item.MenuItemType != MenuItemTypes.EmptyLine &&
                                   item.MenuItemType != MenuItemTypes.Separator,
                    item => NotifyItemChanged(GetItemPosition(item)));
            }
        }

        protected bool IsEditing
        {
            get => _isEditing;
            set
            {
                if(_isEditing == value)
                    return;

                _isEditing = value;
                NotifyItem(item => item != null && item.MenuItemType != MenuItemTypes.EditLocation
                                   && item.MenuItemType != MenuItemTypes.EmptyLine &&
                                   item.MenuItemType != MenuItemTypes.Separator,
                    item => NotifyItemChanged(GetItemPosition(item)));
            }
        }

        public override int ItemCount => MenuItems?.Count ?? 0;
        public int? OldSelectedProviderId { get; private set; }

        public int? SelectedProviderId
        {
            get => _selectedProviderId;
            set
            {
                _selectedProviderId = value;
                if (_selectedProviderId.HasValue)
                {
                    ChangeSelectedPosition(_selectedProviderId.Value, ref _selectedProviderPosition,
                        new[] {MenuItemTypes.WeatherProvider}, true);
                }
                else
                {
                    _selectedProviderPosition = null;
                }
            }
        }

        public int? SelectedLocationId
        {
            get => _selectedLocationId;
            set
            {
                _selectedLocationId = value;
                if (_selectedLocationId.HasValue)
                {
                    ChangeSelectedPosition(_selectedLocationId.Value, ref _selectedLocationPosition,
                        new[] {MenuItemTypes.CurrentLocation, MenuItemTypes.Location}, true);
                }
                else
                {
                    _selectedLocationPosition = null;
                }
            }
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            switch (viewType)
            {
                case (int)MenuItemTypes.WeatherProviderTitle:
                    var itemView = LayoutInflater.From(parent.Context)
                        .Inflate(Resource.Layout.view_menu_item_weather_provider_title, parent, false);
                    return new MenuWeatherProviderTitleViewHolder(itemView, OnItemClick);
                case (int)MenuItemTypes.Location:
                    itemView = LayoutInflater.From(parent.Context)
                        .Inflate(Resource.Layout.view_menu_item, parent, false);
                    return new MenuLocationViewHolder(itemView, OnItemClick, OnEditClick, OnDeleteClick);
                case (int)MenuItemTypes.Separator:
                    itemView = LayoutInflater.From(parent.Context)
                        .Inflate(Resource.Layout.view_menu_item_separator, parent, false);
                    return new MenuSeparatortViewHolder(itemView);
                case (int)MenuItemTypes.EmptyLine:
                    itemView = LayoutInflater.From(parent.Context)
                        .Inflate(Resource.Layout.view_menu_item_empty_line, parent, false);
                    return new MenuEmptyLineViewHolder(itemView);
                default:
                    itemView = LayoutInflater.From(parent.Context)
                        .Inflate(Resource.Layout.view_menu_item, parent, false);
                    return new MenuViewHolder(itemView, OnItemClick);
            }
        }

        public override int GetItemViewType(int position)
        {
            if (ItemCount == 0)
                return (int)MenuItemTypes.Default;

            return (int)MenuItems[position].MenuItemType;
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var item = MenuItems[position];
            if (item.MenuItemType == MenuItemTypes.Separator || item.MenuItemType == MenuItemTypes.EmptyLine)
                return;

            OnBindViewHolder(holder as MenuViewHolder, item, position);
        }

        public void Clear()
        {
            _isOpenedWeatherProviderTitle = false;
            IsWaiting = false;
            IsEditing = false;
        }

        private void OnBindViewHolder(MenuViewHolder holder, MenuItem item, int position)
        {
            if (holder == null)
                return;

            if (item.TitleId.HasValue)
            {
                holder.Title.SetText(item.TitleId.Value);
            }
            else
            {
                holder.Title.Text = item.Title ?? string.Empty;
            }

            var resources = holder.ItemView.Resources;
            var context = holder.ItemView.Context;
            var iconId = item.IconId;
            var enabled = !IsWaiting && !IsEditing;
            var selected = (item.MenuItemType == MenuItemTypes.CurrentLocation ||
                            item.MenuItemType == MenuItemTypes.Location) &&
                           item.KeyId.HasValue && _selectedLocationPosition == position;
            Drawable drawable;

            if (item.MenuItemType == MenuItemTypes.WeatherProvider && item.KeyId.HasValue &&
                _selectedProviderPosition == position ||
                item.MenuItemType == MenuItemTypes.WeatherProviderTitle && !_isOpenedWeatherProviderTitle)
            {
                drawable = _compatDrawableUtil.GetDrawable(resources, _wetherProviderIconId, context.Theme);
                iconId = _wetherProviderIconId;
            }
            else if (IsEditing && item.MenuItemType == MenuItemTypes.Location && !item.IsReadOnly)
            {
                drawable = _compatDrawableUtil.GetDrawable(resources, Resource.Drawable.ic_menu_edit, context.Theme);
            }
            else if (item.IconId.HasValue)
            {
                drawable = _compatDrawableUtil.GetDrawable(resources, item.IconId.Value, context.Theme);
            }
            else
            {
                drawable = new ColorDrawable(
                    new Color(ContextCompat.GetColor(context, Android.Resource.Color.Transparent)));
            }

            ApplyTint(context, drawable, item.AttrTintColorId);
            holder.IconLeft.SetImageDrawable(drawable);

            OnBindViewHolder(holder as MenuWeatherProviderTitleViewHolder, item);
            OnBindViewHolder(holder as MenuLocationViewHolder, item, ref enabled);

            SetTintMode(holder: holder, enabled: enabled, selected: selected,
                isRestoredDefaultTintMode: item.IsRestoredDefaultTintMode, iconId: iconId);
        }

        private void OnBindViewHolder(MenuWeatherProviderTitleViewHolder holder, MenuItem item)
        {
            if (holder == null || item == null || item.MenuItemType != MenuItemTypes.WeatherProviderTitle)
                return;

            if (_isOpenedWeatherProviderTitle)
            {
                holder.Title.SetText(Resource.String.WeatherProviderTitle);
                holder.ViewLine.Visibility = ViewStates.Visible;
            }
            else
            {
                holder.ViewLine.Visibility = ViewStates.Gone;
                if (_selectedProviderId.HasValue)
                {
                    holder.Title.SetText(AppSettings.Default.GetWeatherProviderNameById(_selectedProviderId.Value));
                }
            }
        }

        private void OnBindViewHolder(MenuLocationViewHolder holder, MenuItem item, ref bool enabled)
        {
            if (holder == null || item == null || item.MenuItemType != MenuItemTypes.Location)
                return;

            enabled = !IsWaiting;
            var visibility = IsEditing && !item.IsReadOnly;
            holder.IconRight.Visibility = visibility ? ViewStates.Visible : ViewStates.Gone;
        }

        private void OnItemClick(int position)
        {
            if (ItemCount == 0)
                return;

            var item = MenuItems[position];

            if (!IsEditing && item.MenuItemType == MenuItemTypes.WeatherProviderTitle)
            {
                if (_isOpenedWeatherProviderTitle)
                {
                    NotifyItem(p => p != null && p.MenuItemType == MenuItemTypes.WeatherProvider, p =>
                    {
                        NotifyItemRemoved(GetItemPosition(p));
                        MenuItems.Remove(p);
                    });
                }
                else
                {
                    var length = WeatherProviders?.Length;
                    if (length > 0)
                    {
                        MenuItems.InsertRange(position + 1, WeatherProviders);
                        for (var i = 1; i <= length.Value; i++)
                        {
                            NotifyItemInserted(position + i);
                        }

                        if (_selectedProviderId.HasValue)
                        {
                            ChangeSelectedPosition(_selectedProviderId.Value, ref _selectedProviderPosition,
                                new[] {MenuItemTypes.WeatherProvider}, false);
                        }
                    }
                }

                if (_selectedLocationId.HasValue)
                {
                    ChangeSelectedPosition(_selectedLocationId.Value, ref _selectedLocationPosition,
                        new[] {MenuItemTypes.CurrentLocation, MenuItemTypes.Location}, false);
                }
                _isOpenedWeatherProviderTitle = !_isOpenedWeatherProviderTitle;
                NotifyItemChanged(position);
            }

            if (!IsWaiting)
            {
                if (item.MenuItemType == MenuItemTypes.WeatherProvider && item.KeyId.HasValue)
                {
                    if (_selectedProviderPosition != position)
                    {
                        if (!IsWaiting)
                        {
                            if (_selectedProviderPosition.HasValue)
                            {
                                NotifyItemChanged(_selectedProviderPosition.Value);
                            }
                            NotifyItemChanged(position);
                            _selectedProviderPosition = position;
                            OldSelectedProviderId = _selectedProviderId;
                            _selectedProviderId = item.KeyId;
                        }
                    }
                }
                else if (!IsEditing && (item.MenuItemType == MenuItemTypes.CurrentLocation ||
                                     item.MenuItemType == MenuItemTypes.Location) && item.KeyId.HasValue)
                {
                    if (_selectedLocationPosition != position)
                    {
                        if (!IsWaiting)
                        {
                            if (_selectedLocationPosition.HasValue)
                                NotifyItemChanged(_selectedLocationPosition.Value);
                            NotifyItemChanged(position);
                            _selectedLocationPosition = position;
                            _selectedLocationId = item.KeyId;
                        }
                    }
                }
                else if (item.MenuItemType == MenuItemTypes.EditLocation)
                {
                    if (!IsWaiting)
                    {
                        var editItems = MenuItems.Where(p => p != null && p.MenuItemType == MenuItemTypes.Location)
                            .ToArray();
                        if (editItems.Length > 0)
                            IsEditing = !_isEditing;
                    }
                }
            }

            OnItemClick(item, position, MenuItemEventActions.None);
        }

        private void OnEditClick(int position)
        {
            if (ItemCount == 0 || !IsEditing)
                return;

            var item = MenuItems[position];
            if (item.IsReadOnly)
                return;

            OnItemClick(item, position, MenuItemEventActions.EditLocation);
        }

        private void OnDeleteClick(int position)
        {
            if (ItemCount == 0 || !IsEditing)
                return;

            var item = MenuItems[position];
            if (item.IsReadOnly)
                return;

            OnItemClick(item, position, MenuItemEventActions.DeleteLocation);
        }

        public void DeleteItem(int position)
        {
            if(ItemCount == 0)
                return;

            NotifyItemRemoved(position);
            MenuItems.Remove(MenuItems[position]);
            if (MenuItems.All(p => p.MenuItemType != MenuItemTypes.Location))
                IsEditing = false;
        }

        private void OnItemClick(MenuItem menuItem, int position, MenuItemEventActions action)
        {
            if (ItemClick == null || menuItem == null ||
                menuItem.MenuItemType == MenuItemTypes.EmptyLine || menuItem.MenuItemType == MenuItemTypes.Separator ||
                menuItem.MenuItemType == MenuItemTypes.WeatherProviderTitle ||
                menuItem.MenuItemType == MenuItemTypes.EditLocation ||
                IsEditing && action == MenuItemEventActions.None)
            {
                return;
            }

            var e = new MenuItemEventArgs(menuItem, position, action);
            ItemClick.Invoke(this, e);
        }

        private void ChangeSelectedPosition(int keyId, ref int? selectedPosition, MenuItemTypes[] filter, bool notifyNeed)
        {
            if (ItemCount == 0 || filter == null || filter.Length == 0)
                return;

            var items = MenuItems.Where(p => filter.Contains(p.MenuItemType) && p.KeyId.HasValue).ToArray();
            var item = items.SingleOrDefault(p => p.KeyId == keyId);
            if (item != null)
            {
                if (notifyNeed && selectedPosition.HasValue)
                    NotifyItemChanged(selectedPosition.Value);
                selectedPosition = GetItemPosition(item);
                if (notifyNeed)
                    NotifyItemChanged(selectedPosition.Value);
            }
        }

        private void SetTintMode(MenuViewHolder holder, bool enabled, bool selected, bool isRestoredDefaultTintMode, int? iconId)
        {
            if (holder?.Title == null || holder.IconLeft == null)
                return;

            holder.Title.Enabled = enabled;
            holder.IconLeft.Enabled = enabled;
            holder.IconLeft.Selected = selected;

            if (enabled && isRestoredDefaultTintMode && iconId.HasValue)
            {
                var drawable = _compatDrawableUtil.GetDrawable(holder.ItemView.Context.Resources, iconId.Value,
                    holder.ItemView.Context.Theme);
                holder.IconLeft.SetImageDrawable(drawable);
            }
        }

        private int GetItemPosition(MenuItem item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (ItemCount == 0)
                return -1;

            return MenuItems.IndexOf(item);
        }

        private void NotifyItem(Func<MenuItem, bool> filterHandler, Action<MenuItem> actionHandler)
        {
            var items = MenuItems?.Where(p => p != null && filterHandler(p)
            ).ToArray();

            if (items == null)
                return;

            foreach (var item in items)
            {
                actionHandler?.Invoke(item);
            }
        }

        private void ApplyTint(Context context, Drawable drawable, int? attrId)
        {
            if(!attrId.HasValue)
                return;

            var typedArray = context.ObtainStyledAttributes(new[] { attrId.Value });
            try
            {
                if (typedArray != null && typedArray.Length() > 0)
                {
                    var tint = typedArray.GetColorStateList(0);
                    _compatDrawableUtil.ChangeColorTint(drawable, tint);
                }
            }
            finally
            {
                typedArray?.Recycle();
            }
        }

        public class MenuItemEventArgs : EventArgs
        {
            public MenuItemEventArgs(MenuItem menuItem, int position, MenuItemEventActions action)
            {
                MenuItem = menuItem;
                Position = position;
                Action = action;
 }

            public MenuItem MenuItem { get; }
            public int Position { get; }
            public MenuItemEventActions Action { get; }
 }

        public enum MenuItemEventActions
        {
            None,
            EditLocation,
            DeleteLocation
        }
    }
}