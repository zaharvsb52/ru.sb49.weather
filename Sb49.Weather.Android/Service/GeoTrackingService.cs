using System;
using Android.App;
using Android.Content;
using Android.Gms.Common;
using Android.Gms.Common.Apis;
using Android.Gms.Location;
using Android.Locations;
using Android.OS;
using Sb49.Common.Logging;
using Sb49.Common.Support.v7.Droid.Permissions;
using Sb49.Weather.Droid.Common;
using Sb49.Weather.Droid.Model;

namespace Sb49.Weather.Droid.Service
{
    //https://developers.google.com/android/reference/com/google/android/gms/location/LocationRequest

    //[Service(Name = AppSettings.AppPackageName + ".GeoTrackingService", Process = ":" + AppSettings.CompanyNameShort + "plib")]
    [Service(Name = AppSettings.AppPackageName + ".GeoTrackingService")]
    public class GeoTrackingService : Android.App.Service, GoogleApiClient.IConnectionCallbacks,
        GoogleApiClient.IOnConnectionFailedListener, Android.Gms.Location.ILocationListener
    {
        private GoogleApiClient _apiClient;
        private LocationRequest _locRequest;
        internal static bool IsServiceRunning;

        private readonly ILog _log = LogManager.GetLogger<GeoTrackingService>();

        public override void OnCreate()
        {
            base.OnCreate();
            _log.Info("Service is started.");
        }

        public override IBinder OnBind(Intent intent)
        {
            return null;
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            var result = StartCommandResult.NotSticky;

            try
            {
                var permissionChecker = new Sb49PermissionChecker();
                var status = permissionChecker.CheckPermissionAsync(this, permissionChecker.PermissionLocation).Result;
                if (status != PermissionStatus.Granted)
                {
                    _log.Error("Location permission denied.");
                    StopSelf();
                    return result;
                }

                if (!AppSettings.Default.CheckIsGooglePlayServicesInstalled())
                {
                    StopSelf();
                    return result;
                }

                if (_apiClient == null)
                {
                    _apiClient = new GoogleApiClient.Builder(this, this, this)
                        .AddApi(LocationServices.API).Build();
                }

                if (_locRequest == null)
                    _locRequest = new LocationRequest();
                _apiClient.Connect();

                result = StartCommandResult.Sticky;
                IsServiceRunning = true;
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }

            return result;
        }

        public override void OnLowMemory()
        {
            _log.Info("OnLowMemory");
            AppSettings.GcCollect(true, _log); //HARDCODE:

            base.OnLowMemory();
        }

        public override async void OnDestroy()
        {
            _log.Info("Service is stopped.");

            try
            {
                IsServiceRunning = false;
                if (_apiClient?.IsConnected == true)
                {
                    await LocationServices.FusedLocationApi.RemoveLocationUpdates(_apiClient, this);
                    _apiClient?.Disconnect();
                }

                _locRequest?.Dispose();
                _locRequest = null;

                _apiClient?.Dispose();
                _apiClient = null;
            }
            catch (Exception ex)
            {
                _log.Debug(ex);
            }

            base.OnDestroy();
        }

        #region IConnectionCallbacks

        public void OnConnected(Bundle connectionHint)
        {
            try
            {
                _log.Debug("OnConnected.");
                Resume();
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
        }

        public void OnConnectionSuspended(int cause)
        {
            _log.Debug("Connection is suspended.");
        }

        #endregion #regiobn IConnectionCallbacks

        #region IOnConnectionFailedListener

        public void OnConnectionFailed(ConnectionResult result)
        {
            _log.ErrorFormat("Connection failed. {0} ({1}).", result?.ErrorMessage, result?.ErrorCode);
            StopSelf();
        }

        #endregion IOnConnectionFailedListener

        #region ILocationListener

        public void OnLocationChanged(Location location)
        {
            _log.Debug("Location changed.");

            try
            {
                if (location == null)
                    throw new ArgumentNullException(nameof(location));

                var currentLocation = new CurrentLocation
                {
                    Latitude = location.Latitude,
                    Longitude = location.Longitude,
                    UpdatedDate = DateTime.UtcNow
                };

                AppSettings.Default.CurrentLocation = currentLocation;
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
        }

        #endregion ILocationListener

        private void Resume()
        {
            if (_apiClient?.IsConnected != true)
                return;

            _locRequest.SetPriority(AppSettings.Default.GeoTrackingServicePriority);
            _locRequest.SetSmallestDisplacement(AppSettings.Default.GeoTrackingServiceSmallestDisplacementMeters);

            //_locRequest.SetFastestInterval(500);
            //_locRequest.SetInterval(1000);
            _locRequest.SetFastestInterval(AppSettings.Default.GeoTrackingServiceFastestIntervalMsec);
            _locRequest.SetInterval(AppSettings.Default.GeoTrackingServiceIntervalMsec);

            //_locRequest.SetNumUpdates(NumUpdates);

            LocationServices.FusedLocationApi.RequestLocationUpdates(_apiClient, _locRequest, this);
        }
    }
}