using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Android.App;
using Android.Content;
using Sb49.Common.Droid;
using Sb49.Weather.Droid.Common;
using Sb49.Weather.Droid.Service.Core;

namespace Sb49.Weather.Droid.Service
{
    [Service]
    [IntentFilter(
         new[]
         {
             ActionFeedback,
             ActionZipLogFiles,
             ActionExportLog
         })]
    public class FileBoundService : BoundServiceBase
    {
        private const string ServiceName = ".FileBoundService.";
        public const string ActionFeedback = AppSettings.AppPackageName + ServiceName + "ActionFeedback";
        public const string ActionZipLogFiles = AppSettings.AppPackageName + ServiceName + "ActionZipLogFiles";
        public const string ActionExportLog = AppSettings.AppPackageName + ServiceName + "ActionExportLog";
        public const string ExtraZipFileName = AppSettings.AppPackageName + ".extra_zipfilename";
        public const string ExtraLogFileName = AppSettings.AppPackageName + ".extra_logfilename";
        public const string ExtraComment = AppSettings.AppPackageName + ".extra_comment";

        public Result Result { get; private set; }

        protected override void OnHandleIntent(Intent serviceIntent)
        {
            if (string.IsNullOrEmpty(serviceIntent?.Action))
                return;
           
            var action = serviceIntent.Action;
            ServiceExceptions[action] = null;
            Result = Result.Canceled;

            try
            {
                var token = CancellationTokenSource.Token;
                if (IfCancellationRequested(token))
                    return;

                switch (action)
                {
                    case ActionFeedback:
                    case ActionZipLogFiles:
                        OnZipLogFiles(serviceIntent, token);
                        break;
                    case ActionExportLog:
                        OnExportLog(serviceIntent, token);
                        break;
                }
            }
            catch (OperationCanceledException ex)
            {
                Log.Debug(ex);
                ServiceExceptions[action] = ex;
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Action: '{0}'. {1}", action, ex);
                ServiceExceptions[action] = ex;
            }
            finally
            {
                try
                {
                    if (IsBound)
                    {
                        var intent = new Intent(action);
                        SendOrderedBroadcast(intent, null);
                    }
                }
                catch (Exception ex)
                {
                    Log.Debug(ex);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            Result = Result.Canceled;

            base.Dispose(disposing);
        }

        private void OnZipLogFiles(Intent serviceIntent, CancellationToken token)
        {
            var logFileName = serviceIntent.GetStringExtra(ExtraLogFileName);
            if (string.IsNullOrEmpty(logFileName) || !File.Exists(logFileName))
            {
                Result = Result.Ok;
                return;
            }

            //http://stackoverflow.com/questions/21724706/how-to-get-my-android-device-internal-download-folder-path
            //http://stackoverflow.com/questions/27247332/sent-email-with-attachment-in-the-local-files-directory

            var compressFileName = serviceIntent.GetStringExtra(ExtraZipFileName);
            if (string.IsNullOrEmpty(compressFileName))
                throw new ArgumentNullException(nameof(compressFileName));

            var tmpFolder = Path.GetDirectoryName(compressFileName);
            if (string.IsNullOrEmpty(tmpFolder))
                throw new ArgumentNullException(nameof(tmpFolder));

            if (Directory.Exists(tmpFolder))
            {
                DeleteFiles(tmpFolder, token);
            }
            else
            {
                Directory.CreateDirectory(tmpFolder);
            }

            var filesToCompress = new List<string>();
            foreach (var file in Directory.GetFiles(Path.GetDirectoryName(logFileName)))
            {
                if (IfCancellationRequested(token))
                    return;

                if (string.IsNullOrEmpty(file))
                    continue;

                filesToCompress.Add(file);
            }

            if (filesToCompress.Count > 0)
            {
                if (IfCancellationRequested(token))
                    return;

                var comment = serviceIntent.GetStringExtra(ExtraComment);
                var zipUtil = new ZipUtil();
                zipUtil.Zip(files: filesToCompress.ToArray(), zipFilePath: compressFileName, comment: comment,
                    token: token);
                if (IfCancellationRequested(token))
                    return;

                Result = Result.Ok;
            }
        }

        private void OnExportLog(Intent serviceIntent, CancellationToken token)
        {
            var fromFileName = serviceIntent.GetStringExtra(ExtraZipFileName);
            if (string.IsNullOrEmpty(fromFileName))
                throw new ArgumentNullException(nameof(fromFileName));

            Android.Net.Uri toUri = null;
            var extras = serviceIntent.Extras;
            if (extras != null && extras.ContainsKey(Intent.ExtraStream))
                toUri = extras.Get(Intent.ExtraStream) as Android.Net.Uri;

            if (toUri == null)
                throw new ArgumentNullException(nameof(toUri));

            //Удержание прав
            ContentResolver.TakePersistableUriPermission(toUri,
                ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission);

            var fileUtil = new FileUtil();
            fileUtil.Copy(context: this, fromFileName: fromFileName, toUri: toUri, token: token);

            Result = Result.Ok;
        }

        private void DeleteFiles(string tmpFolder, CancellationToken token)
        {
            if (string.IsNullOrEmpty(tmpFolder))
                return;

            if (Directory.Exists(tmpFolder))
            {
                foreach (var file in Directory.GetFiles(tmpFolder))
                {
                    try
                    {
                        if (IfCancellationRequested(token))
                            return;

                        if (File.Exists(file))
                            File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        Log.Debug(ex);
                    }
                }
            }
        }
    }
}