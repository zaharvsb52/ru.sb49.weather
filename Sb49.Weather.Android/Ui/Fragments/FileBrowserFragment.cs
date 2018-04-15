using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Android.App;
using Android.Database;
using Android.OS;
using Android.Provider;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Sb49.Common;
using Sb49.Common.Logging;
using Sb49.Weather.Droid.Common;
using Sb49.Weather.Droid.Documents;
using Sb49.Weather.Droid.Model;
using Sb49.Weather.Droid.Ui.Adapters;

namespace Sb49.Weather.Droid.Ui.Fragments
{
    public class FileBrowserFragment : Fragment
    {
        private FileDocumentAdapter _adapter;
        private CancellationTokenSource _cancellationTokenSource;
        private TextView _txtNoItems;
        private CustomAsyncTask _asyncTask;
        private static readonly ILog Log = LogManager.GetLogger<FileBrowserFragment>();

        public DocumentTaskTypes DocumentTaskType { get; set; } = DocumentTaskTypes.QueryChildDocuments;
        public DocumentsProvider Provider { get; set; }
        public string ParentDocumentId { get; set; }
        public string SearchQuery { get; set; }
        public string SearchMimeType { get; set; }
        public bool IsCategoryOpenable { get; set; }
        public FileDocumentItemSortOrders OrderBy { get; set; }
        public bool IsDesc { get; set; }
        public bool DoNotCancel { get; set; }
        public Action<FileDocumentItem> ItemClickHandler { get; set; }
        public Action<FileDocumentItem> PostExecuteHandler { get; set; }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.fragment_file_browser, container, false);

            if (savedInstanceState == null && view != null)
                _txtNoItems = (TextView) view.FindViewById(Resource.Id.txtNoItems);

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

            if (!DoNotCancel)
                Cancel();
            UnsubscribeEvents();
        }

        public override void OnDestroy()
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

        public void RunInBackground()
        {
            if (_cancellationTokenSource == null)
                _cancellationTokenSource = new CancellationTokenSource();

            AsyncTaskDispose();
            _asyncTask = new CustomAsyncTask(this, _cancellationTokenSource.Token);
            _asyncTask.Execute(DocumentTaskType);
        }

        public void PostExecute(FileDocumentItem parent, FileDocumentItem[] result, Exception error)
        {
            try
            {
                UnsubscribeEvents();
                AdapterDispose();

                var recyclerView = (RecyclerView) View.FindViewById(Resource.Id.gridDocuments);
                if (recyclerView != null)
                {
                    var layoutManager = new LinearLayoutManager(Activity, LinearLayoutManager.Vertical, false);
                    recyclerView.SetLayoutManager(layoutManager);
                    _adapter = new FileDocumentAdapter(result);
                    recyclerView.SetAdapter(_adapter);
                    SubscribeEvents();
                }

                if (_txtNoItems != null)
                    _txtNoItems.Visibility = _adapter?.ItemCount > 0 ? ViewStates.Gone : ViewStates.Visible;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                error = ex;
            }
            finally
            {
                PostExecuteHandler?.Invoke(parent);
                if (error != null)
                {
                    Toast.MakeText(Activity, GetString(Resource.String.InternalError), AppSettings.ToastLength)
                        .Show();
                }
            }
        }

        private void OnItemClick(object sender, FileDocumentAdapter.ItemEventArgs e)
        {
            ItemClickHandler?.Invoke(e?.Item);
        }

        private void Cancel()
        {
            _cancellationTokenSource?.Cancel();
        }

        private void OnDispose()
        {
            Cancel();

            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            AdapterDispose();
            AsyncTaskDispose();
        }

        private void AdapterDispose()
        {
            _adapter?.Dispose();
            _adapter = null;
        }

        private void AsyncTaskDispose()
        {
            _asyncTask?.Dispose();
            _asyncTask = null;
        }

        private void SubscribeEvents()
        {
            UnsubscribeEvents();

            if (_adapter != null)
                _adapter.ItemClick += OnItemClick;
        }

        private void UnsubscribeEvents()
        {
            if (_adapter != null && _adapter.Handle != IntPtr.Zero)
                _adapter.ItemClick -= OnItemClick;
        }

        private class CustomAsyncTask : AsyncTask<object, int, FileDocumentItem[]>
        {
            private readonly WeakReference<FileBrowserFragment> _fragmentWeakReference;
            private readonly CancellationToken _token;
            private FileDocumentItem _parent;
            private Exception _ex;

            public CustomAsyncTask(FileBrowserFragment fragment, CancellationToken? token)
            {
                _fragmentWeakReference = new WeakReference<FileBrowserFragment>(fragment);
                _token = token ?? CancellationToken.None;
            }

            protected override FileDocumentItem[] RunInBackground(params object[] parameters)
            {
                if (IfCancellationRequested())
                    return null;

                try
                {
                    var fragment = GetFragment();
                    if (fragment == null)
                        return null;

                    if (string.IsNullOrEmpty(fragment.ParentDocumentId))
                        throw new ArgumentNullException(nameof(fragment.ParentDocumentId));

                    if (fragment.DocumentTaskType == DocumentTaskTypes.QuerySearchDocuments &&
                        string.IsNullOrEmpty(fragment.SearchQuery))
                    {
                        throw new ArgumentNullException(nameof(fragment.SearchQuery));
                    }

                    _parent = null;
                    var result = RunInBackground(fragment.DocumentTaskType, fragment.Provider,
                        fragment.ParentDocumentId,
                        fragment.SearchQuery, fragment.SearchMimeType, fragment.IsCategoryOpenable,
                        out string parentDocumentId);

                    if (IfCancellationRequested())
                        return null;

                    if ((result == null || result.Length == 0) &&
                        fragment.DocumentTaskType == DocumentTaskTypes.QuerySearchDocuments)
                    {
                        result = RunInBackground(DocumentTaskTypes.QueryChildDocuments, fragment.Provider,
                            fragment.ParentDocumentId, null, fragment.SearchMimeType, fragment.IsCategoryOpenable,
                            out parentDocumentId);
                    }

                    if (IfCancellationRequested())
                        return null;

                    var parentId = !string.IsNullOrEmpty(parentDocumentId) &&
                                   parentDocumentId != fragment.ParentDocumentId
                        ? parentDocumentId
                        : fragment.ParentDocumentId;

                    var parents = RunInBackground(DocumentTaskTypes.QueryDocument, fragment.Provider,
                        parentId, null, null, null, out parentDocumentId);
                    _parent = parents?.First();

                    return OrderBy(result, fragment.OrderBy, fragment.IsDesc);
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                    _ex = ex;
                }

                return null;
            }

            protected override void OnPostExecute(Java.Lang.Object result)
            {
                if (IfCancellationRequested())
                    return;

                base.OnPostExecute(result);
            }

            protected override void OnPostExecute(FileDocumentItem[] result)
            {
                base.OnPostExecute(result);

                if (IfCancellationRequested())
                    return;

                GetFragment()?.PostExecute(_parent, result, _ex);
            }

            protected override void Dispose(bool disposing)
            {
                _parent = null;

                base.Dispose(disposing);
            }

            private ICursor GetQuery(DocumentTaskTypes taskType, DocumentsProvider provider, string parentDocumentId, string query)
            {
                switch (taskType)
                {
                    case DocumentTaskTypes.QueryChildDocuments:
                        return provider.QueryChildDocuments(parentDocumentId, FileDocumentItem.DocumentProjection,
                            (string) null);
                    case DocumentTaskTypes.QuerySearchDocuments:
                        return provider.QuerySearchDocuments(parentDocumentId, query,
                            FileDocumentItem.DocumentProjection);
                    case DocumentTaskTypes.QueryDocument:
                        return provider.QueryDocument(parentDocumentId, FileDocumentItem.DocumentProjection);
                    default:
                        throw new NotImplementedException(taskType.ToString());
                }
            }

            private FileDocumentItem[] RunInBackground(DocumentTaskTypes documentTaskType, DocumentsProvider provider,
                string parentDocumentId, string query, string searchMimeType, bool? isCategoryOpenable,
                out string parentId)
            {
                parentId = null;
                var result = new List<FileDocumentItem>();
                var isFirstRecord = documentTaskType == DocumentTaskTypes.QuerySearchDocuments;

                using (var cursor = GetQuery(documentTaskType, provider, parentDocumentId, query))
                {
                    if (cursor == null || cursor.Count == 0)
                        return null;

                    while (cursor.MoveToNext())
                    {
                        if (IfCancellationRequested())
                            return null;

                        if (isFirstRecord)
                        {
                            isFirstRecord = false;

                            var columnIndex =
                                cursor.GetColumnIndex(Sb49DocumentsContract.Document.ColumnParentDocumentId);
                            if (columnIndex >= 0)
                                parentId = cursor.GetString(columnIndex);
                        }

                        var item = new FileDocumentItem(provider.Context, cursor);

                        if (!item.IsFolder)
                        {
                            if (isCategoryOpenable == true &&
                                !Util.HasFlags(item.Flags, (int)DocumentContractFlags.SupportsWrite))
                            {
                                continue;
                            }

                            if (!string.IsNullOrEmpty(searchMimeType) && !string.Equals(item.MimeType, searchMimeType,
                                    StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }
                        }

                        result.Add(item);
                    }

                    cursor.Close();
                }

                if (IfCancellationRequested())
                    return null;

                return result.ToArray();
            }

            private FileDocumentItem[] OrderBy(FileDocumentItem[] items, FileDocumentItemSortOrders orderBy, bool isDesc)
            {
                if (items == null || items.Length <= 1)
                    return items;

                switch (orderBy)
                {
                    case FileDocumentItemSortOrders.Default:
                    case FileDocumentItemSortOrders.ByName:
                        return isDesc
                            ? items.OrderByDescending(p => p.IsFolder).ThenByDescending(p => p.DisplayName).ToArray()
                            : items.OrderByDescending(p => p.IsFolder).ThenBy(p => p.DisplayName).ToArray();
                    case FileDocumentItemSortOrders.ByLastModified:
                        return isDesc
                            ? items.OrderByDescending(p => p.LastModified).ThenBy(p => p.DisplayName).ToArray()
                            : items.OrderBy(p => p.LastModified).ThenBy(p => p.DisplayName).ToArray();
                    case FileDocumentItemSortOrders.BySize:
                        return isDesc
                            ? items.OrderByDescending(p => p.Size).ThenBy(p => p.DisplayName).ToArray()
                            : items.OrderBy(p => p.Size).ThenBy(p => p.DisplayName).ToArray();
                    default:
                        throw new NotImplementedException(orderBy.ToString());
                }
            }

            private bool IfCancellationRequested()
            {
                return _token.IsCancellationRequested;
            }

            private FileBrowserFragment GetFragment()
            {
                return _fragmentWeakReference != null &&
                       _fragmentWeakReference.TryGetTarget(out FileBrowserFragment fragment)
                    ? fragment
                    : null;
            }
        }

        public enum DocumentTaskTypes
        {
            QueryChildDocuments,
            QuerySearchDocuments,
            QueryDocument
        }
    }
}