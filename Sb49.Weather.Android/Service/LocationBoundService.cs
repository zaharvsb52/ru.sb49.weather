using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Android.App;
using Android.Content;
using Android.Locations;
using Sb49.Weather.Droid.Common;
using Sb49.Weather.Droid.Model;
using Sb49.Weather.Droid.Service.Core;

namespace Sb49.Weather.Droid.Service
{
    [Service]
    [IntentFilter(
         new[]
         {
             ActionGetAddress
         })]
    public class LocationBoundService : BoundServiceBase
    {
        private const string ServiceName = ".LocationBoundService.";
        public const string ActionGetAddress = AppSettings.AppPackageName + ServiceName + "ActionGetAddress";
        public const string ExtraAddressValue = AppSettings.AppPackageName + ".extra_addressvalue";
        public const string ExtraAddressCount = AppSettings.AppPackageName + ".extra_addresscount";
        public const string ExtraUseGeolocation = AppSettings.AppPackageName + ".extra_usegeolocation";

        public LocationAddress[] GeoAddresses { get; private set; }

        protected override void OnHandleIntent(Intent serviceIntent)
        {
            if (string.IsNullOrEmpty(serviceIntent?.Action))
                return;
           
            var action = serviceIntent.Action;
            ServiceExceptions[action] = null;

            try
            {
                var token = CancellationTokenSource.Token;
                if (IfCancellationRequested(token))
                    return;

                switch (action)
                {
                    case ActionGetAddress:
                        OnGetAddress(serviceIntent, token);
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
            GeoAddresses = null;

            base.Dispose(disposing);
        }

        private void OnGetAddress(Intent serviceIntent, CancellationToken token)
        {
            GeoAddresses = null;
            if (IfCancellationRequested(token))
                return;
            var addresses = new List<Address>();

            var useGeolocation = serviceIntent.GetBooleanExtra(ExtraUseGeolocation, false);
            var address = serviceIntent.GetStringExtra(ExtraAddressValue);

            if (!useGeolocation)
            {
                var result = LocationAddress.Parse(address);
                GeoAddresses = new[] {result};
                return;
            }

            var api = new ServiceApi();
            var count = serviceIntent.GetIntExtra(ExtraAddressCount, 10);
            if (string.IsNullOrEmpty(address))
            {
                if (IfCancellationRequested(token))
                    return;

                var addrs = api.GetCurrentLocations(this, token: token, geoMaxResult: count);
                if (addrs != null && addrs.Length > 0)
                    addresses.AddRange(addrs);
            }
            else
            {
                if (IfCancellationRequested(token))
                    return;

                var locationAddress = LocationAddress.Parse(address);

                var geo = new Sb49Geocoder();
                // ReSharper disable PossibleInvalidOperationException
                var task = locationAddress.HasCoordinatesOnly
                    ? geo.GetFromLocationAsync(this, locationAddress.Latitude.Value, locationAddress.Longitude.Value,
                        count, token)
                    : geo.GetFromLocationNameAsync(this, address, count, token);
                // ReSharper restore PossibleInvalidOperationException
                api.TaskWait(task, api.WaitGeoMsec, token);

                var addrs = task.Result?.ToArray();
                if (addrs != null && addrs.Length > 0)
                    addresses.AddRange(addrs);
            }

            if (addresses.Count > 0)
            {
                if (IfCancellationRequested(token))
                    return;

                var locationAddress = addresses
                    .Where(p => !string.IsNullOrEmpty(p.Locality) || p.HasLatitude && p.HasLongitude)
                    .Select(p => new LocationAddress(p))
                    .Distinct(new LocationAddressEqualityComparer())
                    .ToArray();
                if (IfCancellationRequested(token))
                    return;

                if (locationAddress.Length > 0)
                {
                    if (IfCancellationRequested(token))
                        return;

                    var notEmptyLocalityAddress =
                        locationAddress.Where(p => !string.IsNullOrEmpty(p.Locality)).ToArray();
                    if (notEmptyLocalityAddress.Length > 0)
                        locationAddress = notEmptyLocalityAddress;

                    if (IfCancellationRequested(token))
                        return;

                    locationAddress = locationAddress
                        .OrderBy(p => p.CountryCode)
                        .ThenBy(p => p.PostalCode)
                        .ThenBy(p => p.AdminArea)
                        .ThenBy(p => p.Locality)
                        .ToArray();
                    GeoAddresses = locationAddress;
                }
            }
        }
    }
}