using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.V7.Widget;
using Android.Text;
using Android.Views;
using Android.Widget;
using Sb49.Common.Droid.Ui;
using Sb49.Common.Logging;
using Sb49.Weather.Droid.Common;
using Sb49.Weather.Droid.Model;
using Sb49.Weather.Droid.Ui.Activities;
using Sb49.Weather.Droid.Ui.Adapters;

namespace Sb49.Weather.Droid.Ui.Fragments
{
    public class MenuFragment : Fragment
    {
        public const int MenuAddPlaceId = 1021;
        private const int MenuEditPlaceId = 1031;
        public const int MenuRefreshId = 1041;
        public const int MenuSettingsId = 1051;
        public const int MenuAboutId = 2001;

        private RecyclerView _menuContainer;
        private MenuAdapter _menuAdapter;
        private static readonly ILog Log = LogManager.GetLogger<MenuFragment>();

        public Action<MenuItem, MenuAdapter.MenuItemEventActions, bool> OptionMenuClickHandler { get; set; }

        public bool IsWaiting
        {
            get => _menuAdapter?.IsWaiting ?? false;
            set
            {
                if (_menuAdapter != null)
                    _menuAdapter.IsWaiting = value;
            }
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.fragment_menu, container, false);

            try
            {
                if (savedInstanceState == null)
                {
                    if (view != null)
                    {
                        _menuContainer = view.FindViewById<RecyclerView>(Resource.Id.menuLeftDrawer);
                        _menuContainer.HasFixedSize = true;
                        _menuContainer.SetLayoutManager(
                            new LinearLayoutManager(Activity, LinearLayoutManager.Vertical, false));

                        var menu = CreateOptionsMenu();

                        var id = 0;
                        var providerIds = AppSettings.Default.GetWeatherProviderIds();
                        var providers = providerIds.Where(p => AppSettings.Default.ValidateWeatherProvider(p))
                            .Select(providerId => new MenuItem(id++, menuItemType: MenuItemTypes.WeatherProvider)
                            {
                                KeyId = providerId,
                                TitleId = AppSettings.Default.GetWeatherProviderNameById(providerId),
                                AttrTintColorId = Resource.Attribute.menuDrawerLayoutItemFixIconBackground,
                                IsRestoredDefaultTintMode = true
                            }).ToArray();

                        var currentLocationId = AppSettings.Default.SelectedLocationAddressId;
                        var currentProviderId = AppSettings.Default.WeatherProviderId;
                        _menuAdapter = new MenuAdapter(menu: menu, weatherProviders: providers,
                            currentWeatherProviderId: currentProviderId, currentLocationId: currentLocationId,
                            wetherProviderIconId: Resource.Drawable.umbrela);
                        _menuContainer?.SetAdapter(_menuAdapter);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }

            return view;
        }

        public override void OnResume()
        {
            base.OnResume();
            SubscribeEvents();
        }

        public override void OnPause()
        {
            base.OnPause();
            UnsubscribeEvents();
        }

        protected override void Dispose(bool disposing)
        {
            AdapterDispose();

            base.Dispose(disposing);
        }

        public void UpdateMenu()
        {
            _menuAdapter.Clear();
            _menuAdapter.MenuItems = CreateOptionsMenu().ToList();
            _menuAdapter.SelectedProviderId = AppSettings.Default.WeatherProviderId;
            _menuAdapter.SelectedLocationId = AppSettings.Default.SelectedLocationAddressId;
            _menuAdapter.NotifyDataSetChanged();
        }

        private LocationAddress[] GetNotCurrentLocations()
        {
            var locations = AppSettings.Default.GetLocations();
            foreach (var pair in locations.Where(p => p.Value != null))
            {
                pair.Value.Id = pair.Key;
            }

            var result =
                locations.Where(p => p.Key != AppSettings.AddressCurrentLocationId && 
                    p.Value != null && p.Value.IsValid() &&
                        !string.IsNullOrEmpty(p.Value.Locality) && p.Value.Id.HasValue)
                    .Select(p => p.Value)
                    .Distinct(new LocationAddressEqualityComparer())
                    .ToArray();
            return result;
        }

        private MenuItem[] CreateOptionsMenu()
        {
            var id = 0;

            var menu = new List<MenuItem>
            {
                new MenuItem(id++, menuItemType: MenuItemTypes.WeatherProviderTitle)
                {
                    AttrTintColorId = Resource.Attribute.menuDrawerLayoutItemFixIconBackground,
                    IsRestoredDefaultTintMode = true
                },

                new MenuItem(id++, menuItemType: MenuItemTypes.Separator),
                new MenuItem(id++, menuItemType: MenuItemTypes.CurrentLocation)
                {
                    IconId = Resource.Drawable.ic_menu_current_location,
                    TitleId = Resource.String.CurrentLocationTitle,
                    KeyId = AppSettings.AddressCurrentLocationId,
                    AttrTintColorId = Resource.Attribute.menuDrawerLayoutItemIconBackground
                }
            };

            var locations = GetNotCurrentLocations();
            if (locations != null)
            {
                foreach (var location in locations.Where(p => p?.Id != null))
                {
                    menu.Add(new MenuItem(id++,  menuItemType: MenuItemTypes.Location)
                    {
                        KeyId = location.Id,
                        IconId = Resource.Drawable.ic_menu_location,
                        Title = string.IsNullOrEmpty(location.DisplayLocality) ? location.Locality : location.DisplayLocality,
                        IsReadOnly = false,
                        AttrTintColorId = Resource.Attribute.menuDrawerLayoutItemIconBackground
                    });
                }
            }

            menu.Add(new MenuItem(id++, menuId: MenuAddPlaceId, menuItemType: MenuItemTypes.AddLocation)
            {
                IconId = Resource.Drawable.ic_menu_add_location_material,
                TitleId = Resource.String.AddLocationTitle,
                AttrTintColorId = Resource.Attribute.menuDrawerLayoutItemIconBackground
            });

            menu.Add(new MenuItem(id++, menuId: MenuEditPlaceId, menuItemType: MenuItemTypes.EditLocation)
            {
                IconId = Resource.Drawable.ic_menu_edit,
                TitleId = Resource.String.MenuItemEditLocationTitle,
                AttrTintColorId = Resource.Attribute.menuDrawerLayoutItemIconBackground
            });
            menu.Add(new MenuItem(id++, menuItemType: MenuItemTypes.Separator));

            menu.Add(new MenuItem(id++, menuId: MenuRefreshId)
            {
                IconId = Resource.Drawable.ic_menu_refresh,
                TitleId = Resource.String.RefreshTitle,
                AttrTintColorId = Resource.Attribute.menuDrawerLayoutItemIconBackground
            });

            menu.Add(new MenuItem(id++, menuItemType: MenuItemTypes.EmptyLine));
            menu.Add(new MenuItem(id++, menuId: MenuSettingsId)
            {
                IconId = Resource.Drawable.ic_menu_settings,
                TitleId = Resource.String.PreferencesTitle,
                AttrTintColorId = Resource.Attribute.menuDrawerLayoutItemIconBackground
            });

            menu.Add(new MenuItem(id, menuId: MenuAboutId)
            {
                IconId = Resource.Drawable.yandex_bkn_d_white,
                TitleId = Resource.String.AboutTitle,
                AttrTintColorId = Resource.Attribute.menuDrawerLayoutItemFixIconBackground,
                IsRestoredDefaultTintMode = true
            });

            return menu.ToArray();
        }

        private void StartLocationActivity(int requestCode)
        {
            var intent = new Intent(Activity, typeof(LocationActivity));
            if(requestCode == MenuAddPlaceId)
                intent.PutExtra(LocationActivity.ExtraEmptyAddress, true);
            Activity.StartActivityForResult(intent, requestCode);
        }

        private void OnOptionMenuClick(object sender, MenuAdapter.MenuItemEventArgs e)
        {
            if (e?.MenuItem == null)
                return;

            try
            {
                var item = e.MenuItem;
                var dialogs = new Dialogs();
                var dlg = new WidgetUtil();

                switch (item.MenuId)
                {
                    case MenuAddPlaceId:
                        if (IsWaiting)
                            return;
                        StartLocationActivity(item.MenuId);
                        return;
                    default:
                        switch (e.Action)
                        {
                            case MenuAdapter.MenuItemEventActions.EditLocation:
                                if (IsWaiting)
                                    return;

                                dlg.InputDialog(context: Activity, titleId: Resource.String.EditLocationTitle,
                                    layoutId: Resource.Layout.text_input, editTextId: Resource.Id.txtInput,
                                    createAction: (view, input) =>
                                    {
                                        if (input == null)
                                            return;

                                        input.Text = item.Title ?? string.Empty;

                                        // android:inputType="textPostalAddress"
                                        input.InputType =
                                            InputTypes.ClassText | InputTypes.DatetimeVariationDate |
                                            InputTypes.DatetimeVariationTime | InputTypes.TextVariationShortMessage;
                                    },
                                    okAction: (view, value) =>
                                    {
                                        if (string.IsNullOrEmpty(value))
                                            return false;

                                        try
                                        {
                                            var locationAddress = AppSettings.Default.LocationAddress;
                                            locationAddress.DisplayLocality = value;
                                            AppSettings.Default.SaveLocationAddress(locationAddress,
                                                AppSettings.Default.SelectedLocationAddressId);
                                            item.Title = value;
                                            _menuAdapter.NotifyItemChanged(e.Position);
                                            if (_menuAdapter.SelectedLocationId.HasValue &&
                                                _menuAdapter.SelectedLocationId == item.KeyId)
                                            {
                                                OptionMenuClickHandler?.Invoke(e.MenuItem, e.Action, true);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Log.Error(ex);
                                            Toast.MakeText(Activity, GetString(Resource.String.InternalError),
                                                AppSettings.ToastLength).Show();
                                        }

                                        return true;
                                    });
                                return;
                            case MenuAdapter.MenuItemEventActions.DeleteLocation:
                                if (IsWaiting || !item.KeyId.HasValue)
                                    return;

                                dlg.ShowDialog(context: Activity, titleId: Resource.String.DeleteLocationTitle,
                                    message: string.Format(GetString(Resource.String.DeleteLocationConfirmation), item.Title),
                                    okAction: () =>
                                    {
                                        try
                                        {
                                            if(_menuAdapter == null)
                                                return;

                                            _menuAdapter.DeleteItem(e.Position);

                                            AppSettings.Default.DeleteLocationAddress(item.KeyId.Value);
                                            if (_menuAdapter.SelectedLocationId == item.KeyId)
                                            {
                                                AppSettings.Default.UseTrackCurrentLocation = true;
                                                var currentLocationItem =
                                                    _menuAdapter.MenuItems.SingleOrDefault(
                                                        p => p.MenuItemType == MenuItemTypes.CurrentLocation &&
                                                             p.KeyId.HasValue);
                                                if (currentLocationItem != null)
                                                    _menuAdapter.SelectedLocationId = currentLocationItem.KeyId;
                                                OptionMenuClickHandler?.Invoke(e.MenuItem, e.Action, true);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Log.Error(ex);
                                            Toast.MakeText(Activity, GetString(Resource.String.InternalError),
                                                AppSettings.ToastLength).Show();
                                        }
                                    });
                                return;
                            default:
                                if (item.MenuItemType == MenuItemTypes.WeatherProvider && item.KeyId.HasValue)
                                {
                                    var providerId = item.KeyId.Value;
                                    var handled = dialogs.ApiKeyDialog(activity: Activity, providerType: ProviderTypes.WeatherProvider, 
                                        providerId: providerId, alwaysShowDialog: false, 
                                        okAction: () =>
                                        {
                                            _menuAdapter.SelectedProviderId = providerId;
                                            OptionMenuClickHandler?.Invoke(e.MenuItem, e.Action, false);
                                        }, cancelAction: () =>
                                        {
                                            _menuAdapter.SelectedProviderId = _menuAdapter.OldSelectedProviderId;
                                        }, toastLength: AppSettings.ToastLength, log: Log);

                                    if (!handled)
                                        return;
                                }
                                break;
                        }
                        break;
                }

                OptionMenuClickHandler?.Invoke(e.MenuItem, e.Action, false);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                _menuAdapter?.Clear();
                Toast.MakeText(Activity, GetString(Resource.String.InternalError), AppSettings.ToastLength).Show();
            }
        }

        private void AdapterDispose()
        {
            _menuAdapter?.Dispose();
            _menuAdapter = null;
        }

        private void SubscribeEvents()
        {
            UnsubscribeEvents();
            if (_menuAdapter != null)
                _menuAdapter.ItemClick += OnOptionMenuClick;
        }

        private void UnsubscribeEvents()
        {
            if (_menuAdapter != null && _menuAdapter.Handle != IntPtr.Zero)
                _menuAdapter.ItemClick -= OnOptionMenuClick;
        }
    }
}