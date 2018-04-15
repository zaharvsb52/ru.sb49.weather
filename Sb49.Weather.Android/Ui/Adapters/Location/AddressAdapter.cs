using System;
using Android.Support.V7.Widget;
using Android.Views;
using Sb49.Weather.Droid.Model;
using Sb49.Weather.Droid.Ui.ViewHolders;

namespace Sb49.Weather.Droid.Ui.Adapters
{
    public class AddressAdapter : RecyclerView.Adapter
    {
        private readonly LocationAddress[] _addresses;
        public event EventHandler<LocationAddress> ItemClick;

        public AddressAdapter(LocationAddress[] addresses)
        {
            _addresses = addresses;
        }

        public override int ItemCount => _addresses?.Length ?? 0;

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.view_address_item, parent, false);
            return new LocationViewHolder(itemView, OnItemClick);
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var viewHolder = (LocationViewHolder)holder;
            var item = _addresses[position];
            viewHolder.Item.Text = item.GetAddress(LocationAddress.CommaDelimeter, false) ?? string.Empty;
        }

        private void OnItemClick(int position)
        {
            if (_addresses == null)
                return;

            ItemClick?.Invoke(this, _addresses[position]);
        }
    }
}