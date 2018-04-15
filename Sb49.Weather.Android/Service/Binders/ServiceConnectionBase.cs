using System;
using Android.App;
using Android.Content;
using Android.OS;

namespace Sb49.Weather.Droid.Service.Binders
{
    public abstract class ServiceConnectionBase<T> : Java.Lang.Object, IServiceConnection where T : Activity
    {
        private readonly WeakReference<T> _activity;

        protected ServiceConnectionBase(T activity)
        {
            _activity = new WeakReference<T>(activity);
        }

        public void OnServiceConnected(ComponentName name, IBinder service)
        {
            var activity = GetActivity();
            if (activity == null)
                return;

            var binder = service as ServiceBinder;
            if (binder != null)
                OnServiceConnected(activity, binder);
        }

        public void OnServiceDisconnected(ComponentName name)
        {
            var activity = GetActivity();
            if (activity == null)
                return;

            OnServiceDisconnected(activity);
        }

        protected abstract void OnServiceConnected(T activity, ServiceBinder binder);
        protected abstract void OnServiceDisconnected(T activity);

        protected T GetActivity()
        {
            if (_activity == null)
                return null;

            T activity;
            return _activity.TryGetTarget(out activity) ? activity : null;
        }
    }
}