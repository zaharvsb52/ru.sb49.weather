using System;
using Android.OS;

namespace Sb49.Weather.Droid.Service.Binders
{
    public class ServiceBinder : Binder
    {
        private readonly WeakReference<Android.App.Service> _service;

        public ServiceBinder(Android.App.Service service)
        {
            _service = new WeakReference<Android.App.Service>(service);
        }
        protected ServiceBinder(IntPtr javaReference, Android.Runtime.JniHandleOwnership transfer) : base (javaReference, transfer)
        {
        }

        public Android.App.Service Service
        {
            get
            {
                if (_service == null)
                    return null;

                Android.App.Service service;
                return _service.TryGetTarget(out service) ? service : null;
            }
        }
    }
}