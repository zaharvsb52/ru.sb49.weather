using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.V4.Content;
using Android.Support.V7.App;
using Android.Text;
using Android.Text.Method;
using Android.Text.Style;
using Android.Views;
using Android.Widget;
using Sb49.Common.Droid;
using Sb49.Common.Logging;
using Sb49.Weather.Droid.Common;
using Sb49.Weather.Droid.Service;
using Sb49.Weather.Droid.Service.Binders;
using Toolbar = Android.Support.V7.Widget.Toolbar;
using AndroidUtil = Sb49.Common.Droid.Util;

namespace Sb49.Weather.Droid.Ui.Activities
{
    [Activity]
    public class AboutActivity : AppCompatActivity
    {
        private const string MimeTypeZip = "application/zip";
        private const int RequestCodeExportLog = 1;

        private ServiceConnection _serviceConnection;
        private ServiceReceiver _serviceReceiver;
        private bool _isBound;
        private ServiceBinder _binder;
        private string _version;
        private string _versionFull;
        private string _compressFileName;
        private Queue<Intent> _serviceQueue;
        private string _tmpFolder;
        private string _productName;
        private View _viewProgressbar;
        private bool _isWaiting;
        private static readonly ILog Log = LogManager.GetLogger<AboutActivity>();

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.activity_about);

            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);
            SupportActionBar.SetTitle(Resource.String.AboutTitle);

            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetHomeButtonEnabled(true);

            _serviceQueue = new Queue<Intent>();
            _tmpFolder = Path.Combine(FilesDir.Path, AppSettings.TempFolder, AppSettings.LogFilesFolder);
            _productName = AssemblyAttributeAccessors.GetAssemblyProduct(GetType());
            if (!string.IsNullOrEmpty(_productName))
                _productName = _productName.Replace(".", "_");

            var assembliesList = new List<string>();
            const string mono = "mono.android"; //HARDCODE:
            const string googleLocation = "xamarin.googleplayservices.location"; //HARDCODE:
            const string assformat = "{0} v.{1}";
            var names = new[]
            {
                mono, googleLocation, "log4net", "newtonsoft.json", "nodatime", "sb49.googlegeocoder", "sb49.weather",
                "sb49.provider.darksky", "sb49.provider.openweathermap", "sb49.provider.yahooweather"
            };
            var assemblies =
                AssemblyAttributeAccessors.FindAssemblies(a => names.Contains(a.GetName().Name.ToLower())).ToList();
            string assvers = null;
            if (assemblies.Count > 0)
            {
                void AddVersionHandler(string assname, Func<Assembly, string> getTitleHandler,
                    Func<string, string> getHardcoreVersionHandler)
                {
                    var ass = assemblies.FirstOrDefault(p => p.GetName().Name.ToLower() == assname);
                    if (ass != null)
                    {
                        var ver = AssemblyAttributeAccessors.GetAssemblyInformationalVersion(ass);
                        assemblies.Remove(ass);
                        if (!string.IsNullOrEmpty(ver))
                        {
                            assembliesList.Add(string.Format(assformat, getTitleHandler(ass),
                                getHardcoreVersionHandler == null ? ver : getHardcoreVersionHandler(ver)));
                        }
                    }
                }

                AddVersionHandler(mono, a => a.GetName().Name, null);

                AddVersionHandler(googleLocation, AssemblyAttributeAccessors.GetAssemblyTitle,
                    v => string.IsNullOrEmpty(v) || v == "1.0.0.0" ? "29.0.0.2" : v); //HARDCODE:

                assembliesList.AddRange(assemblies.Select(p => string.Format(assformat,
                        AssemblyAttributeAccessors.GetAssemblyTitle(p),
                        AssemblyAttributeAccessors.GetAssemblyFileVersion(p)))
                    .Distinct());

                assvers = "," + System.Environment.NewLine + string.Join(", ", assembliesList);
            }

            _version = string.Format("{0} ({1})", AppSettings.Default.VersionName,
                AssemblyAttributeAccessors.GetAssemblyFileVersion(GetType()));
            _versionFull = string.Format("{0}{1}", _version, assvers);

            var version = FindViewById<TextView>(Resource.Id.txtVersion);
            version.Text = _versionFull;

            var copyright = FindViewById<TextView>(Resource.Id.txtCopyright);
            copyright.Text = AssemblyAttributeAccessors.GetAssemblyCopyright(GetType());

            var textFancyWidgets = GetString(Resource.String.IconsCopyRightFancyWidgets);
            var spannable = new SpannableString(textFancyWidgets);
            var txtFancyWidgets = FindViewById<TextView>(Resource.Id.txtFancyWidgets);
            txtFancyWidgets.MovementMethod = LinkMovementMethod.Instance;
            spannable.SetSpan(new URLSpan(GetString(Resource.String.IconsCopyRightFancyWidgetsUrl)), 0,
                textFancyWidgets.Length, SpanTypes.ExclusiveExclusive);
            txtFancyWidgets.TextFormatted = spannable;

            var textYandexWeather = GetString(Resource.String.IconsCopyRightYandexWeather);
            spannable = new SpannableString(textYandexWeather);
            var txtYandexWeather = FindViewById<TextView>(Resource.Id.txtYandexWeather);
            txtYandexWeather.MovementMethod = LinkMovementMethod.Instance;
            spannable.SetSpan(new URLSpan(GetString(Resource.String.IconsCopyRightYandexWeatherUrl)), 0,
                textYandexWeather.Length, SpanTypes.ExclusiveExclusive);
            txtYandexWeather.TextFormatted = spannable;

            _viewProgressbar = FindViewById(Resource.Id.toolbarProgress);
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            try
            {
                if (!_isWaiting)
                    MenuInflater.Inflate(Resource.Menu.menu_about_activity, menu);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }

            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnPrepareOptionsMenu(IMenu menu)
        {
            try
            {
                var item = menu?.FindItem(Resource.Id.menuItemExportLogFile);
                item?.SetVisible(AppSettings.Default.IsLogFileExisted);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }

            return base.OnPrepareOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            try
            {
                switch (item.ItemId)
                {
                    case Android.Resource.Id.Home:
                        Finish();
                        return true;
                    case Resource.Id.menuItemFeedback:
                        _serviceQueue.Clear();
                        var intent = GetFileServiceIntent(FileBoundService.ActionFeedback, null);
                        _serviceQueue.Enqueue(intent);
                        return StartFileService();
                    case Resource.Id.menuItemExportLogFile:
                        _serviceQueue.Clear();
                        intent = GetFileServiceIntent(FileBoundService.ActionZipLogFiles, null);
                        _serviceQueue.Enqueue(intent);
                        return StartFileService();
                    default:
                        return base.OnOptionsItemSelected(item);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                ShowProgressbar(false);
                ShowError(ex, Resource.String.InternalError);
            }

            return false;
        }

        private void OnFileBoundServiceResult(string action)
        {
            //http://stackoverflow.com/questions/2197741/how-can-i-send-emails-from-my-android-application

            try
            {
                if (!_isBound)
                    return;

                var service = _binder.Service as FileBoundService;
                if (service == null)
                    return;

                if (service.ServiceExceptions != null && service.ServiceExceptions.ContainsKey(action))
                {
                    var exception = service.ServiceExceptions[action];
                    if (exception != null)
                    {
                        ShowError(exception, Resource.String.InternalError);
                        return;
                    }
                }

                if (service.Result != Result.Ok)
                    return;

                Intent activityIntent;
                var requestCode = 0;

                switch (action)
                {
                    case FileBoundService.ActionFeedback:
                        var emailIntent = new Intent(Intent.ActionSend);
                        //emailIntent.SetData(Uri.Parse("mailto:" + GetString(Resource.String.FeedbackMail)));
                        emailIntent.PutExtra(Intent.ExtraEmail, new[] { GetString(Resource.String.FeedbackMail) });
                        var applicationName = GetString(Resource.String.ApplicationName);
                        emailIntent.PutExtra(Intent.ExtraSubject,
                            string.Format("{0} ver. {1}", applicationName, _version));
                        var bodytext = string.Format("Model: {0}{1}OS: {2}{1}Application: {3}", AndroidUtil.GetDeviceName(),
                            System.Environment.NewLine, AndroidUtil.GetAndroidVersion(),
                            string.Format("{0} ver. {1}", applicationName, _versionFull));

                        var intentType = "text/plain; charset=UTF-8;";

                        var zipFile = new Java.IO.File(_compressFileName);
                        if (zipFile.Exists())
                        {
                            //emailIntent.SetType("application/zip");
                            intentType += " " + MimeTypeZip;
                            var uri = FileProvider.GetUriForFile(this,
                                Application.Context.PackageName + ".FileProvider", zipFile);
                            //emailIntent.PutExtra(Intent.ExtraStream, Uri.Parse("file://" + _compressFileName));
                            emailIntent.PutExtra(Intent.ExtraStream, uri);
                        }
                        else
                        {
                            //throw new FileNotFoundException(string.Format("File '{0}' not found.", _compressFileName));
                            bodytext += string.Format("{0}No log-file (logToFile: {1})", System.Environment.NewLine,
                                AppSettings.Default.LoggingUseFile);
                        }

                        emailIntent.SetType(intentType);
                        emailIntent.PutExtra(Intent.ExtraText, bodytext);

                        activityIntent =
                            Intent.CreateChooser(emailIntent, GetString(Resource.String.EmailChooserTitle));
                        break;
                    case FileBoundService.ActionZipLogFiles:
                        activityIntent = new Intent(Intent.ActionCreateDocument);
                        activityIntent.AddCategory(Intent.CategoryOpenable);
                        activityIntent.SetType(MimeTypeZip);
                        var title = Path.GetFileName(_compressFileName)?.ToLower();
                        if (!string.IsNullOrEmpty(title))
                            activityIntent.PutExtra(Intent.ExtraTitle, title);
                        requestCode = RequestCodeExportLog;
                        break;
                    case FileBoundService.ActionExportLog:
                        return;
                    default:
                        throw new NotImplementedException(action);
                }

                if(activityIntent != null)
                    StartActivityForResult(activityIntent, requestCode);
            }
            catch (ActivityNotFoundException ex)
            {
                ShowError(ex, Resource.String.NoEmailClientsInstalled);
            }
            catch (Exception ex)
            {
                ShowError(ex, Resource.String.InternalError);
            }
            finally
            {
                ShowProgressbar(false);
            }
        }

        protected override void OnResume()
        {
            base.OnResume();

            ValidateServiceReceiver();
            var intentFilter = new IntentFilter(FileBoundService.ActionFeedback)
            {
                Priority = (int)IntentFilterPriority.HighPriority
            };
            intentFilter.AddAction(FileBoundService.ActionZipLogFiles);
            intentFilter.AddAction(FileBoundService.ActionExportLog);

            RegisterReceiver(_serviceReceiver, intentFilter);

            ServiceConnectionDispose();
            _serviceConnection = new ServiceConnection(this);

            var serviceIntent = AndroidUtil.CreateExplicitFromImplicitIntent(this,
                new Intent(FileBoundService.ActionZipLogFiles));
            BindService(serviceIntent, _serviceConnection, Bind.AutoCreate);

            try
            {
                StartFileService();
            }
            catch (Exception ex)
            {
                ShowProgressbar(false);
                ShowError(ex, Resource.String.InternalError);
            }
        }

        protected override void OnPause()
        {
            base.OnPause();

            ShowProgressbar(false);
            
            if (_isBound)
            {
                UnbindService(_serviceConnection);
                _isBound = false;
            }
            if (_serviceReceiver != null && _serviceReceiver.Handle != IntPtr.Zero)
                UnregisterReceiver(_serviceReceiver);
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            try
            {
                if (requestCode == RequestCodeExportLog && resultCode == Result.Ok && data != null)
                {
                    _serviceQueue.Clear();

                    var intent = GetFileServiceIntent(FileBoundService.ActionExportLog, data.Data);
                    _serviceQueue.Enqueue(intent);
                }
            }
            catch (Exception ex)
            {
                ShowError(ex, Resource.String.InternalError);
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
            //AppSettings.GcCollect(log: _log); //HARDCODE:

            base.OnDestroy();
        }

        private bool StartFileService()
        {
            if (_isWaiting || _serviceQueue == null || _serviceQueue.Count == 0)
                return false;

            ShowProgressbar(true);

            while (_serviceQueue.Count > 0)
            {
                var intent = _serviceQueue.Dequeue();
                if(intent == null)
                    continue;

                StartService(intent);
            }

            return true;
        }

        private Intent GetFileServiceIntent(string action, IParcelable uri)
        {
            var intent = AndroidUtil.CreateExplicitFromImplicitIntent(this, new Intent(action));

            switch (action)
            {
                case FileBoundService.ActionFeedback:
                case FileBoundService.ActionZipLogFiles:
                    _compressFileName = Path.Combine(_tmpFolder,
                        string.Format("log_{0}_{1:yyyyMMddHHmmss}.zip", _productName, DateTime.Now));
                    intent.PutExtra(FileBoundService.ExtraZipFileName, _compressFileName);
                    intent.PutExtra(FileBoundService.ExtraLogFileName, AppSettings.Default.LogFileName);
                    intent.PutExtra(FileBoundService.ExtraComment, _versionFull);
                    break;
                case FileBoundService.ActionExportLog:
                    intent.PutExtra(FileBoundService.ExtraZipFileName, _compressFileName);
                    if (uri != null)
                        intent.PutExtra(Intent.ExtraStream, uri);
                    break;
                default:
                    throw new NotImplementedException(action);
            }

            return intent;
        }

        private void CreateServiceReceiver()
        {
            _serviceReceiver = new ServiceReceiver();
        }

        private void ValidateServiceReceiver()
        {
            if (_serviceReceiver == null)
            {
                CreateServiceReceiver();
                return;
            }
            if (_serviceReceiver.Handle == IntPtr.Zero)
            {
                _serviceReceiver.Dispose();
                CreateServiceReceiver();
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

        private void ShowError(Exception ex, int messageId)
        {
            if (ex != null)
                Log.Error(ex);
            ShowProgressbar(false);
            RunOnUiThread(() =>
            {
                Toast.MakeText(this, GetString(messageId), AppSettings.ToastLength).Show();
            });
        }

        private void OnDispose()
        {
            ServiceConnectionDispose();

            _serviceReceiver?.Dispose();
            _serviceReceiver = null;

            if (_serviceQueue != null)
            {
                foreach (var item in _serviceQueue)
                {
                    item?.Dispose();
                }
                _serviceQueue.Clear();
                _serviceQueue = null;
            }
            
            try
            {
                if (!string.IsNullOrEmpty(_compressFileName))
                {
                    using (var zipFile = new Java.IO.File(_compressFileName))
                    {
                        using (var parent = zipFile.ParentFile)
                        {
                            if (parent?.Exists() == true)
                            {
                                var files = parent.ListFiles();
                                foreach (var file in files)
                                {
                                    file.Delete();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex);
            }
            finally
            {
                _compressFileName = null;
            }
        }

        private void ServiceConnectionDispose()
        {
            _serviceConnection?.Dispose();
            _serviceConnection = null;
        }

        private class ServiceReceiver : BroadcastReceiver
        {
            public override void OnReceive(Context context, Intent intent)
            {
                if (!string.IsNullOrEmpty(intent?.Action))
                {
                    ((AboutActivity)context).OnFileBoundServiceResult(intent.Action);
                    InvokeAbortBroadcast();
                }
            }
        }

        private class ServiceConnection : ServiceConnectionBase<AboutActivity>
        {
            public ServiceConnection(AboutActivity activity) : base(activity)
            {
            }

            protected override void OnServiceConnected(AboutActivity activity, ServiceBinder binder)
            {
                activity._binder = binder;
                activity._isBound = true;
            }

            protected override void OnServiceDisconnected(AboutActivity activity)
            {
                activity._isBound = false;
            }
        }
    }
}