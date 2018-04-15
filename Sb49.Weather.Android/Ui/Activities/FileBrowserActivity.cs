using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Sb49.Common.Logging;
using Sb49.Weather.Droid.Common;
using Sb49.Weather.Droid.Model;
using Sb49.Weather.Droid.Ui.Fragments;
using System.Threading.Tasks;
using Android;
using Android.Content.PM;
using Android.Support.V4.Provider;
using Newtonsoft.Json;
using Sb49.Common;
using Sb49.Common.Droid.Ui;
using Sb49.Weather.Droid.Documents;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Sb49.Weather.Droid.Ui.Activities
{
    [Activity]
    public class FileBrowserActivity : AppCompatActivity
    {
        public const string Authority = AppSettings.AppPackageName + ".file.documents"; //HARDCODE:
        public const string ExtraSearchQuery = AppSettings.AppPackageName + ".extra_searchquery";
        private const string CurrentParentKey = AppSettings.AppPackageName + ".FileBrowserActivity.CurrentParentKey";
        private const string PreviousParentsKey = AppSettings.AppPackageName + ".FileBrowserActivity.PreviousParentsKey";
        private const string DoNotCancelKey = AppSettings.AppPackageName + ".FileBrowserActivity.DoNotCancelKey";
        private const string OrderByKey = AppSettings.AppPackageName + ".FileBrowserActivity.OrderByKey";
        private const string SelectedItemKey = AppSettings.AppPackageName + ".FileBrowserActivity.SelectedItemKey";
        private const int MenuSettingsRequestCode = 1;

        private ImageView _imgNewFileName;
        private EditText _txtNewFileName;
        private Button _btnAction;
        private View _viewProgressbar;
        private bool _isWaiting;
        private bool _showCreateFolderMenu;
        private FileDocumentsProvider _documentsProvider;
        private FileDocumentItem _root;
        private FileDocumentItem _currentParent;
        private Stack<string> _previousParents;
        private bool _isCategoryOpenable;
        private string _mimeType;
        private FileDocumentItem _selectedItem;
        private FileDocumentItemSortOrders _orderBy;
        private bool _isActionOpenDocumentTree;
        private bool _doNotCancel;
        private static readonly ILog Log = LogManager.GetLogger<FileBrowserActivity>();

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.activity_file_browser);

            //Toolbar will now take on default actionbar characteristics
            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);
            SupportActionBar.Title = string.Empty;

            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetHomeButtonEnabled(true);

            try
            {
                _showCreateFolderMenu = false;
                _orderBy = FileDocumentItemSortOrders.Default;

                // documents provider
                _documentsProvider = new FileDocumentsProvider();
                _documentsProvider.AttachInfo(this,
                    CreateProviderInfo(string.Format("{0}.{1}", AppSettings.AppPackageName,
                        _documentsProvider.GetType().Name.ToLower()))); //HARDCODE:

                // root
                GetRoot();
                if (string.IsNullOrEmpty(_root?.DocumentId))
                    throw new ArgumentNullException(nameof(_root));

                // stack
                _previousParents = new Stack<string>();

                // start parameters
                var displayName = Intent.GetStringExtra(Intent.ExtraTitle);
                var mimeType = Intent.Type;
                var isCategoryOpenable = Intent.Categories?.Contains(Intent.CategoryOpenable) == true;
                var action = Intent?.Action;
                // end parameters

                var statusBar = FindViewById(Resource.Id.statusBar);
                _imgNewFileName = FindViewById<ImageView>(Resource.Id.imgNewFileName);
                _txtNewFileName = FindViewById<EditText>(Resource.Id.txtNewFileName);
                _btnAction = FindViewById<Button>(Resource.Id.btnAction);
                _viewProgressbar = FindViewById(Resource.Id.toolbarProgress);

                switch (action)
                {
                    case Intent.ActionCreateDocument:
                        _showCreateFolderMenu = true;
                        if (_imgNewFileName != null)
                        {
                            _imgNewFileName.SetImageResource(Resource.Drawable.ic_file);
                            _imgNewFileName.Visibility = ViewStates.Visible;
                        }

                        if (_txtNewFileName != null)
                        {
                            _txtNewFileName.Text = displayName ?? string.Empty;
                            _txtNewFileName.Visibility = ViewStates.Visible;
                        }

                        if (_btnAction != null)
                        {
                            _btnAction.SetText(Resource.String.Save);
                            _btnAction.LayoutParameters.Width = ViewGroup.LayoutParams.WrapContent;
                        }

                        _mimeType = mimeType;
                        _isCategoryOpenable = isCategoryOpenable;
                        break;
                    case Intent.ActionOpenDocumentTree:
                        _isActionOpenDocumentTree = true;
                        _showCreateFolderMenu = true;

                        if(_imgNewFileName != null)
                            _imgNewFileName.Visibility = ViewStates.Gone;

                        if (_txtNewFileName != null)
                            _txtNewFileName.Visibility = ViewStates.Gone;

                        if (_btnAction != null)
                            _btnAction.LayoutParameters.Width = ViewGroup.LayoutParams.MatchParent;
                        break;
                    default:
                        if (statusBar != null)
                            statusBar.Visibility = ViewStates.Gone;

                        _imgNewFileName?.Dispose();
                        _imgNewFileName = null;

                        _txtNewFileName?.Dispose();
                        _txtNewFileName = null;

                        _btnAction?.Dispose();
                        _btnAction = null;

                        _mimeType = mimeType;
                        _isCategoryOpenable = isCategoryOpenable;
                        break;
                }

                if (savedInstanceState == null)
                {
                    var searchQuery = Intent.GetStringExtra(ExtraSearchQuery);
                    var taskType = string.IsNullOrEmpty(searchQuery)
                        ? FileBrowserFragment.DocumentTaskTypes.QueryChildDocuments
                        : FileBrowserFragment.DocumentTaskTypes.QuerySearchDocuments;
                    ReplaceFragment(isAdd: true, useCustomAnimation: false, taskType: taskType, query: searchQuery);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                Toast.MakeText(this, GetString(Resource.String.InternalError),
                    AppSettings.ToastLength).Show();
            }
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            try
            {
                if (!_isWaiting)
                    MenuInflater.Inflate(Resource.Menu.menu_file_browser_activity, menu);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }

            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnPrepareOptionsMenu(IMenu menu)
        {
            var item = menu?.FindItem(Resource.Id.menuItemFileBrowserCreateFolder);
            item?.SetVisible(_showCreateFolderMenu && _currentParent != null &&
                             Util.HasFlags(_currentParent.Flags,
                                 (int) DocumentContractFlags.DirSupportsCreate));

            void SetCheckedHandler(int resId)
            {
                var menuItem = menu?.FindItem(resId);
                menuItem?.SetChecked(true);
            }

            switch (_orderBy)
            {
                case FileDocumentItemSortOrders.Default:
                case FileDocumentItemSortOrders.ByName:
                    SetCheckedHandler(Resource.Id.menuItemFileBrowserSortByName);
                    break;
                case FileDocumentItemSortOrders.ByLastModified:
                    SetCheckedHandler(Resource.Id.menuItemFileBrowserSortByDate);
                    break;
                case FileDocumentItemSortOrders.BySize:
                    SetCheckedHandler(Resource.Id.menuItemFileBrowserSortBySize);
                    break;
            }

            return base.OnPrepareOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            try
            {
                if (_isWaiting || item == null)
                    return false;

                switch (item.ItemId)
                {
                    case Android.Resource.Id.Home:
                        Finish();
                        return true;
                    case Resource.Id.menuItemFileBrowserUpward:
                        if (_currentParent != null && _currentParent.DocumentId != _root.DocumentId)
                        {
                            var previousParentId = _previousParents.Count == 0
                                ? GetParent(_currentParent.DocumentId) ?? _root.DocumentId
                                : _previousParents.Pop();
                            ReplaceFragment(parentDocumentId: previousParentId);
                        }
                        return true;
                    case Resource.Id.menuItemFileBrowserCreateFolder:
                        var dlg = new WidgetUtil();
                        dlg.InputDialog(context: this, titleId: Resource.String.CreateFolderTitle,
                            layoutId: Resource.Layout.text_input, editTextId: Resource.Id.txtInput,
                            okAction: (view, value) =>
                            {
                                if (string.IsNullOrEmpty(value))
                                    return false;

                                try
                                {
                                    var docId = _documentsProvider.CreateDocument(_currentParent.DocumentId,
                                        DocumentsContract.Document.MimeTypeDir, value);
                                    _previousParents.Push(_currentParent.DocumentId);
                                    ReplaceFragment(useCustomAnimation: false, parentDocumentId: docId);
                                }
                                catch (Exception ex)
                                {
                                    Log.Error(ex);
                                    Toast.MakeText(this, GetString(Resource.String.InternalError),
                                            AppSettings.ToastLength)
                                        .Show();
                                    return false;
                                }

                                return true;
                            });
                        return true;
                    case Resource.Id.menuItemFileBrowserSortByName:
                        item.SetChecked(!item.IsChecked);
                        if (item.IsChecked && _orderBy != FileDocumentItemSortOrders.Default && _orderBy != FileDocumentItemSortOrders.ByName)
                        {
                            _orderBy = FileDocumentItemSortOrders.ByName;
                            ReplaceFragment();
                        }
                        return true;
                    case Resource.Id.menuItemFileBrowserSortByDate:
                        item.SetChecked(!item.IsChecked);
                        if (item.IsChecked && _orderBy != FileDocumentItemSortOrders.ByLastModified)
                        {
                            _orderBy = FileDocumentItemSortOrders.ByLastModified;
                            ReplaceFragment();
                        }
                        return true;
                    case Resource.Id.menuItemFileBrowserSortBySize:
                        item.SetChecked(!item.IsChecked);
                        if (item.IsChecked && _orderBy != FileDocumentItemSortOrders.BySize)
                        {
                            _orderBy = FileDocumentItemSortOrders.BySize;
                            ReplaceFragment();
                        }
                        return true;
                    case Resource.Id.menuItemFileBrowserRefresh:
                        ReplaceFragment();
                        return true;
                    case Resource.Id.menuItemFileBrowserSettings:
                        var intent = new Intent(this, typeof(FileBrowserSettingsActivity));
                        _doNotCancel = true;
                        StartActivityForResult(intent, MenuSettingsRequestCode);
                        break;

                    default:
                        return base.OnOptionsItemSelected(item);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                Toast.MakeText(this, GetString(Resource.String.InternalError), AppSettings.ToastLength)
                    .Show();
            }

            return false;
        }

        protected override void OnResume()
        {
            base.OnResume();
            
            _txtNewFileName?.ClearFocus();
            SubscribeEvents();
        }

        protected override void OnPause()
        {
            base.OnPause();
            
            ShowProgressbar(false);
            UnsubscribeEvents();
        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);

            try
            {
                outState.PutBoolean(DoNotCancelKey, _doNotCancel);

                if (_currentParent != null)
                    outState.PutString(CurrentParentKey, JsonConvert.SerializeObject(_currentParent));

                if (_previousParents != null)
                {
                    var src = _previousParents.Reverse().ToArray();
                    outState.PutString(PreviousParentsKey, JsonConvert.SerializeObject(src));
                }

                if (_selectedItem != null)
                    outState.PutString(SelectedItemKey, JsonConvert.SerializeObject(_previousParents));

                outState.PutInt(OrderByKey, (int) _orderBy);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        protected override void OnRestoreInstanceState(Bundle savedInstanceState)
        {
            base.OnRestoreInstanceState(savedInstanceState);

            try
            {
                _doNotCancel = savedInstanceState.GetBoolean(DoNotCancelKey, false);

                if (_previousParents == null || _previousParents.Count == 0)
                {
                    var src = savedInstanceState.GetString(PreviousParentsKey);
                    if (!string.IsNullOrEmpty(src))
                        _previousParents = JsonConvert.DeserializeObject<Stack<string>>(src);
                }

                if (_selectedItem == null)
                {
                    var src = savedInstanceState.GetString(SelectedItemKey);
                    if (!string.IsNullOrEmpty(src))
                        _selectedItem = JsonConvert.DeserializeObject<FileDocumentItem>(src);
                }

                _orderBy = (FileDocumentItemSortOrders) savedInstanceState.GetInt(OrderByKey);

                if (_currentParent == null)
                {
                    var src = savedInstanceState.GetString(CurrentParentKey);
                    if (!string.IsNullOrEmpty(src))
                    {
                        _currentParent = JsonConvert.DeserializeObject<FileDocumentItem>(src);
                        ReplaceFragment();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if (requestCode == MenuSettingsRequestCode)
            {
                _doNotCancel = false;
                if(resultCode == Result.Ok)
                    ReplaceFragment();
            }
        }

        protected override void OnDestroy()
        {
            try
            {
                OnDispose();
            }
            catch (Exception ex)
            {
                Log.Debug(ex);
            }

            base.OnDestroy();
        }

        private void OnDocumentItemClick(FileDocumentItem item)
        {
            if(string.IsNullOrEmpty(item?.DocumentId))
                return;

            try
            {
                if (item.IsFolder)
                {
                    if (_currentParent != null)
                        _previousParents.Push(_currentParent.DocumentId);
                    ReplaceFragment(parentDocumentId: item.DocumentId);
                }
                else
                {
                    if (_txtNewFileName != null)
                        _txtNewFileName.Text = item.DisplayName ?? string.Empty;

                    _selectedItem = item;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                Toast.MakeText(this, GetString(Resource.String.InternalError), AppSettings.ToastLength)
                    .Show();
            }
        }

        private void OnButtonActionOnClick(object sender, EventArgs eventArgs)
        {
            try
            {
                var displayName = _txtNewFileName?.Text;
                if (!string.IsNullOrEmpty(displayName) && displayName == _selectedItem?.DisplayName)
                {
                    FinishWithResult(_selectedItem.DocumentId);
                    return;
                }

                var documentId = _documentsProvider?.CreateDocument(_currentParent?.DocumentId, _mimeType, displayName);
                FinishWithResult(documentId);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        private void FinishWithResult(string documentId)
        {
            //var uri = DocumentsContract.BuildDocumentUri(Authority, documentId);
            var file = _documentsProvider.GetFileForDocumentId(documentId);
            var uri = DocumentFile.FromFile(file).Uri;
            var intent = new Intent();
            intent.SetData(uri);
            SetResult(Result.Ok, intent);
            Finish();
        }

        private void OnPostExecute(FileDocumentItem parent)
        {
            try
            {
                _currentParent = parent ?? throw new ArgumentNullException(nameof(parent));
                var parentDisplayName = _currentParent.DisplayName;
                if (string.IsNullOrEmpty(parentDisplayName))
                    parentDisplayName = _root.DisplayName;
                SupportActionBar.Title = parentDisplayName ?? string.Empty;
                SupportActionBar.SetLogo(_currentParent.IconResourceId ?? Android.Resource.Color.Transparent);
                if(_isActionOpenDocumentTree && _btnAction != null)
                    _btnAction.Text = string.Format(GetString(Resource.String.SelectFolder), parentDisplayName);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                Toast.MakeText(this, GetString(Resource.String.InternalError), AppSettings.ToastLength)
                    .Show();
            }
            finally
            {
                ShowProgressbar(false);
            }
        }

        private void ReplaceFragment(bool isAdd = false, bool useCustomAnimation = true,
            FileBrowserFragment.DocumentTaskTypes taskType =
                FileBrowserFragment.DocumentTaskTypes.QueryChildDocuments, string parentDocumentId = null,
            string query = null)
        {
            try
            {
                ShowProgressbar(true);

                var isDesc = false;
                switch (_orderBy)
                {
                    case FileDocumentItemSortOrders.ByLastModified:
                    case FileDocumentItemSortOrders.BySize:
                        isDesc = true;
                        break;
                }

                var fragment = new FileBrowserFragment
                {
                    DocumentTaskType = taskType,
                    Provider = _documentsProvider,
                    ParentDocumentId = parentDocumentId ??
                                       (_currentParent == null ? _root.DocumentId : _currentParent.DocumentId),
                    SearchQuery = query,
                    SearchMimeType = _mimeType,
                    IsCategoryOpenable = _isCategoryOpenable,
                    OrderBy = _orderBy,
                    IsDesc = isDesc,
                    DoNotCancel = _doNotCancel,
                    ItemClickHandler = OnDocumentItemClick,
                    PostExecuteHandler = OnPostExecute
                };

                using (var ft = FragmentManager.BeginTransaction())
                {
                    if (useCustomAnimation)
                        ft.SetCustomAnimations(Resource.Animation.enter_anim, Resource.Animation.exit_anim);
                    else
                        ft.SetTransition(FragmentTransit.FragmentOpen);

                    if (isAdd)
                    {
                        ft.Add(Resource.Id.viewContent, fragment);
                    }
                    else
                    {
                        ft.Replace(Resource.Id.viewContent, fragment);
                        //.AddToBackStack(fragment.ParentDocumentId);
                    }
                    ft.Commit();
                }

                fragment.RunInBackground();
                _txtNewFileName?.ClearFocus();
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                ShowProgressbar(false);
            }
        }

        private void ShowProgressbar(bool visible, int sleepmsec = AppSettings.WaitActivitySleepMsec)
        {
            if (_isWaiting == visible)
                return;

            _isWaiting = visible;
            Task.Run(() =>
            {
                if (_isWaiting)
                    Task.Delay(sleepmsec).Wait();

                RunOnUiThread(() =>
                {
                    if (_viewProgressbar != null)
                        _viewProgressbar.Visibility = _isWaiting ? ViewStates.Visible : ViewStates.Gone;
                    InvalidateOptionsMenu();
                });
            });
        }

        private void GetRoot()
        {
            if(_documentsProvider == null)
                throw new ArgumentNullException(nameof(_documentsProvider));

            _root = null;
            using (var cursor = _documentsProvider.QueryRoots(null))
            {
                if (cursor != null)
                {
                    if (cursor.Count > 0 && cursor.MoveToNext())
                        _root = new FileDocumentItem(this, cursor, true);

                    cursor.Close();
                }
            }
        }

        private string GetParent(string documentId)
        {
            if (_documentsProvider == null)
                throw new ArgumentNullException(nameof(_documentsProvider));

            return _documentsProvider.GetParent(documentId);
        }

        private ProviderInfo CreateProviderInfo(string name)
        {
            var result = new ProviderInfo
            {
                Name = name,
                Authority = Authority,
                GrantUriPermissions = true,
                Exported = true,
                ReadPermission = Manifest.Permission.ManageDocuments,
                Enabled = false
            };

            result.WritePermission = result.ReadPermission;

            return result;
        }

        private void OnDispose()
        {
            UnsubscribeEvents();

            _root = null;
            _currentParent = null;
            _doNotCancel = false;
            _selectedItem = null;

            _documentsProvider?.Dispose();
            _documentsProvider = null;

            _previousParents?.Clear();
            _previousParents = null;
        }

        private void SubscribeEvents()
        {
            UnsubscribeEvents();
            
            if (_btnAction != null)
                _btnAction.Click += OnButtonActionOnClick;
        }

        private void UnsubscribeEvents()
        {
            if (_btnAction != null && _btnAction.Handle != IntPtr.Zero)
                _btnAction.Click -= OnButtonActionOnClick;
        }
    }
}