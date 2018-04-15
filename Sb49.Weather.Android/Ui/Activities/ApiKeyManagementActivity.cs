using System;
using System.Linq;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Sb49.Common.Droid.Ui;
using Sb49.Common.Logging;
using Sb49.Http.Provider.Exeptions;
using Sb49.Weather.Droid.Common;
using Sb49.Weather.Droid.Ui.Adapters;
using Sb49.Weather.Droid.Ui.ViewHolders;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Sb49.Weather.Droid.Ui.Activities
{
    [Activity]
    public class ApiKeyManagementActivity : AppCompatActivity
    {
        public const string ExtraDelete = AppSettings.AppPackageName + ".extra_delete";
        private const int RequestCodeExport = 1;
        private const int RequestCodeImport = 2;

        private ApiKeysManagementAdapter _adapter;
        private static readonly ILog Log = LogManager.GetLogger<ApiKeyManagementActivity>();

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.activity_apikey_management);

            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);
            SupportActionBar.SetTitle(Resource.String.ApiKeysManagementTitle);

            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetHomeButtonEnabled(true);

            var recyclerView = FindViewById<RecyclerView>(Resource.Id.gridApiKeys);
            recyclerView.SetLayoutManager(new LinearLayoutManager(this, LinearLayoutManager.Vertical, false));

            var providers = AppSettings.Default.GetProviders().OrderBy(p => p.ProviderType).ThenBy(p => p.Id).ToArray();
            _adapter = new ApiKeysManagementAdapter(providers, AppSettings.Default.GetApiKeys());

            recyclerView.SetAdapter(_adapter);
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_apikey_management_activity, menu);
            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            const string mimeType = "text/plain";

            switch (item.ItemId)
            {
                case Android.Resource.Id.Home:
                    Finish();
                    return true;
                case Resource.Id.menuItemExportApiKeys:
                    OnStartSafActivity(Intent.ActionCreateDocument, mimeType, "apikeys", RequestCodeExport);
                    return true;
                case Resource.Id.menuItemImportApiKeys:
                    OnStartSafActivity(Intent.ActionOpenDocument, mimeType, null, RequestCodeImport);
                    return true;
                default:
                    return base.OnOptionsItemSelected(item);
            }
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if (resultCode != Result.Ok || data == null)
                return;

            try
            {
                switch (requestCode)
                {
                    case RequestCodeExport:
                        AppSettings.Default.SaveApiKeys(AppSettings.Default.GetApiKeys(), data.Data);
                        break;
                    case RequestCodeImport:
                        var keys = AppSettings.Default.GetApiKeys(data.Data);
                        if (keys == null || keys.Count == 0)
                            throw new ApiKeyException();

                        AppSettings.Default.SaveApiKeys(keys);
                        if (_adapter != null)
                        {
                            _adapter.ApiKeys = AppSettings.Default.GetApiKeys();
                            _adapter.NotifyDataSetChanged();

                            //HARDCODE:
                            if (AppSettings.Default.UseGoogleMapsGeocodingApi)
                            {
                                var googleMapsGeocodingApi = _adapter.ApiKeys.FirstOrDefault(
                                    p => p.Key == AppSettings.GoogleMapsGeocodingApiProviderId);
                                if (googleMapsGeocodingApi.Value?.Validate() != true)
                                {
                                    AppSettings.Default.UseGoogleMapsGeocodingApi = false;
                                    SetChangeActivityResult();
                                }
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                Toast.MakeText(this, Resource.String.InternalError, AppSettings.ToastLength).Show();
            }
        }

        protected override void OnResume()
        {
            base.OnResume();
            SubscribeEvents();
        }

        protected override void OnPause()
        {
            base.OnPause();
            UnsubscribeEvents();
        }

        protected override void OnDestroy()
        {
            try
            {
                _adapter?.Dispose();
                _adapter = null;
            }
            catch (Exception ex)
            {
                Log.Debug(ex);
            }
            //AppSettings.GcCollect(log: _log); //HARDCODE:

            base.OnDestroy();
        }

        private void OnItemClick(object sender, ApiKeysManagementAdapter.ItemEventArgs e)
        {
            if (e?.Item == null || e.Item.IsReadOnly)
                return;

            var providerId = e.Item.Id;

            try
            {
                switch (e.Action)
                {
                    case ApiKeysManagementViewHolder.EditActions.Edit:
                        var dialogs = new Dialogs();
                        dialogs.ApiKeyDialog(activity: this, providerType: e.Item.ProviderType,
                            providerId: providerId, alwaysShowDialog: true, okAction: () =>
                            {
                                NotifyItemChanged(e.Position);
                            }, cancelAction: null, toastLength: AppSettings.ToastLength, log: Log);
                        break;
                    case ApiKeysManagementViewHolder.EditActions.Delete:
                        var keys = AppSettings.Default.GetApiKeys();
                        if (keys.ContainsKey(providerId))
                        {
                            var dlg = new WidgetUtil();
                            dlg.ShowDialog(context: this, titleId: Resource.String.ApiKeysManagementTitle,
                                message: GetString(Resource.String.IcDeleteApiKeyConfirmation),
                                okAction: () =>
                                {
                                    try
                                    {
                                        keys.Remove(providerId);
                                        AppSettings.Default.SaveApiKeys(keys);

                                        //HARDCODE:
                                        if (providerId == AppSettings.GoogleMapsGeocodingApiProviderId &&
                                            AppSettings.Default.UseGoogleMapsGeocodingApi)
                                        {
                                            AppSettings.Default.UseGoogleMapsGeocodingApi = false;
                                            SetChangeActivityResult();
                                        }

                                        NotifyItemChanged(e.Position);
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Error(ex);
                                        Toast.MakeText(this, GetString(Resource.String.InternalError),
                                            AppSettings.ToastLength).Show();
                                    }
                                });
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                Toast.MakeText(this, Resource.String.InternalError, AppSettings.ToastLength).Show();
            }
        }

        private void OnStartSafActivity(string action, string mimeType, string extraTitle, int requestCode)
        {
            try
            {
                var intent = new Intent(action);
                intent.AddCategory(Intent.CategoryOpenable);
                intent.SetType(mimeType);

                if (!string.IsNullOrEmpty(extraTitle))
                    intent.PutExtra(Intent.ExtraTitle, extraTitle);

                StartActivityForResult(intent, requestCode);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        private void SetChangeActivityResult()
        {
            var intent = new Intent();
            intent.PutExtra(ExtraDelete, true);
            SetResult(Result.Ok, intent);
        }

        private void NotifyItemChanged(int position)
        {
            if (_adapter == null)
                return;

            _adapter.ApiKeys = AppSettings.Default.GetApiKeys();
            _adapter.NotifyItemChanged(position);
        }

        private void SubscribeEvents()
        {
            UnsubscribeEvents();
            if (_adapter != null)
                _adapter.ItemClick += OnItemClick;
        }

        private void UnsubscribeEvents()
        {
            if (_adapter != null)
                _adapter.ItemClick -= OnItemClick;
        }
    }
}