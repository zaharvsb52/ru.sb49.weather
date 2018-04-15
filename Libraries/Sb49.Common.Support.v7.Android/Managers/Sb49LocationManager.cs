using System;
using System.Threading;
using Android;
using Android.Content;
using Android.Locations;
using Android.Support.V4.Content;

namespace Sb49.Common.Support.v7.Droid.Managers
{
    //https://android.googlesource.com/platform/frameworks/support/+/f2149b1/v7/appcompat/src/android/support/v7/app/TwilightManager.java
    public class Sb49LocationManager : IDisposable
    {
        private readonly WeakReference<Context> _context;
        private readonly Lazy<LocationManager> _locationManager;
        public Sb49LocationManager(Context context)
        {
            _context = new WeakReference<Context>(context);
            _locationManager= new Lazy<LocationManager>(() => (LocationManager)GetContext().GetSystemService(Context.LocationService), LazyThreadSafetyMode.PublicationOnly);
        }

        ~Sb49LocationManager()
        {
            OnDispose();
        }

        public Location GetLastKnownLocation()
        {
            Location coarseLocation = null;
            Location fineLocation = null;
            var context = GetContext();

            var permission = PermissionChecker.CheckSelfPermission(context, Manifest.Permission.AccessFineLocation);
            if (permission == PermissionChecker.PermissionGranted)
                coarseLocation = GetLastKnownLocationForProvider(LocationManager.NetworkProvider);

            permission = PermissionChecker.CheckSelfPermission(context, Manifest.Permission.AccessCoarseLocation);
            if (permission == PermissionChecker.PermissionGranted)
                fineLocation = GetLastKnownLocationForProvider(LocationManager.GpsProvider);

            if (coarseLocation != null && fineLocation != null)
            {
                // If we have both a fine and coarse location, use the latest
                if (fineLocation.Time > coarseLocation.Time)
                    return fineLocation;
                return coarseLocation;
            }

            // Else, return the non-null one (if there is one)
            return fineLocation ?? coarseLocation;
        }

        private Location GetLastKnownLocationForProvider(string provider)
        {
            if (_locationManager == null)
                return null;

            if (_locationManager.Value.IsProviderEnabled(provider))
                return _locationManager.Value.GetLastKnownLocation(provider);
            return null;
        }

        private Context GetContext()
        {
            Context context;
            return _context.TryGetTarget(out context) ? context : null;
        }

        #region . IDisposable .
        private void OnDispose()
        {
            _locationManager?.Value?.Dispose();
        }

        public void Dispose()
        {
            OnDispose();
            GC.SuppressFinalize(this);
        }
        #endregion . IDisposable .
    }
}