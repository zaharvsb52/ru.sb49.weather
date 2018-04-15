using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Android.App;
using Android.Content;
using Android.OS;
using Sb49.Common.Logging;
using Sb49.Weather.Droid.Service.Binders;

namespace Sb49.Weather.Droid.Service.Core
{
    public abstract class BoundServiceBase : IntentService
    {
        protected CancellationTokenSource CancellationTokenSource;
        protected bool IsBound;
        protected readonly ILog Log;

        protected BoundServiceBase()
        {
            CancellationTokenSource = new CancellationTokenSource();
            ServiceExceptions = new ConcurrentDictionary<string, Exception>();
            Log = LogManager.GetLogger(GetType());
        }

        public IDictionary<string, Exception> ServiceExceptions { get; protected set; }

        public override IBinder OnBind(Intent intent)
        {
            var binder = new ServiceBinder(this);
            IsBound = true;
            return binder;
        }

        public override bool OnUnbind(Intent intent)
        {
            IsBound = false;
            CancelAllTask();
            return base.OnUnbind(intent);
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                OnDispose();
                ServiceExceptions?.Clear();
                ServiceExceptions = null;
            }
            catch(Exception ex)
            {
                Log.Debug(ex);
            }

            base.Dispose(disposing);
        }

        public override bool StopService(Intent name)
        {
            try
            {
                CancelAllTask();
            }
            catch (Exception ex)
            {
                Log.Debug(ex);
            }
            
            return base.StopService(name);
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

        protected virtual void CancelAllTask()
        {
            CancellationTokenSource?.Cancel();
        }

        protected virtual void OnDispose()
        {
            CancelAllTask();
            CancellationTokenSource?.Dispose();
            CancellationTokenSource = null;
        }

        protected virtual bool IfCancellationRequested(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return false;
        }
    }
}