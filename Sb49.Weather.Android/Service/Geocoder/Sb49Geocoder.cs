using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using Android.Locations;
using Sb49.Common.Logging;
using Sb49.Geocoder.Core;
using Sb49.Http.Provider.Core;
using Sb49.Weather.Droid.Common;

namespace Sb49.Weather.Droid.Service
{
    public class Sb49Geocoder
    {
        private readonly ILog _log = LogManager.GetLogger<Sb49Geocoder>();

        public Task<IList<Address>> GetFromLocationAsync(Context context, double latitude, double longitude, int maxResults,
            CancellationToken? token)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (!token.HasValue)
                token = CancellationToken.None;

            return Task.Run(() =>
            {
                if (Android.Locations.Geocoder.IsPresent)
                {
                    IList<Address> result;
                    using (var geo = new Android.Locations.Geocoder(context))
                    {
                        try
                        {
                            result = geo.GetFromLocation(latitude, longitude, maxResults);
                        }
                        catch (Java.IO.IOException ex)
                        {
                            _log.DebugFormat("Geocoder bug. {0}", ex);
                            return GetFromLocation(latitude, longitude, maxResults, token);
                        }
                    }

                    if (result != null && result.Count > 0)
                        return result;
                }

                return GetFromLocation(latitude, longitude, maxResults, token);
            }, token.Value);
        }

        public Task<IList<Address>> GetFromLocationNameAsync(Context context, string locationName, int maxResults,
            CancellationToken? token)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (!token.HasValue)
                token = CancellationToken.None;

            return Task.Run(() =>
            {
                if (Android.Locations.Geocoder.IsPresent)
                {
                    IList<Address> result;
                    using (var geo = new Android.Locations.Geocoder(context))
                    {
                        try
                        {
                            result = geo.GetFromLocationName(locationName, maxResults);
                        }
                        catch (Java.IO.IOException ex)
                        {
                            _log.DebugFormat("Geocoder bug. {0}", ex);
                            return GetFromLocationName(locationName, maxResults, token);
                        }
                    }

                    if (result != null && result.Count > 0)
                        return result;
                }

                return GetFromLocationName(locationName, maxResults, token);
            }, token.Value);
        }

        private IList<Address> GetFromLocationName(string locationName, int maxResults, CancellationToken? token)
        {
            if (!ValidateUseGoogleMapsGeocodingApi())
                return null;

            var sb49Geo = CreateGeocoder();
            return ConvertToAddres(sb49Geo.GetFromLocationName(locationName, maxResults, token));
        }

        private IList<Address> GetFromLocation(double latitude, double longitude, int maxResults,
            CancellationToken? token)
        {
            if (!ValidateUseGoogleMapsGeocodingApi())
                return null;

            var sb49Geo = CreateGeocoder();
            return ConvertToAddres(sb49Geo.GetFromLocation(latitude, longitude, maxResults, token));
        }

        private IGeocoder CreateGeocoder()
        {
            var result = AppSettings.Default.Geocoder;
            result.ApiKey = AppSettings.Default.GetApiKey(AppSettings.GoogleMapsGeocodingApiProviderId);
            result.LanguageCode = AppSettings.Default.Language;
            return result;
        }

        private bool ValidateUseGoogleMapsGeocodingApi()
        {
            return AppSettings.Default.UseGoogleMapsGeocodingApi;
        }

        private IList<Address> ConvertToAddres(IList<ILocationAddress> addresses)
        {
            if (addresses == null)
                return null;

            var locale = Java.Util.Locale.Default;
            var result = new List<Address>();

            foreach (var address in addresses)
            {
                var javaAddress = new Address(locale)
                {
                    PostalCode = address.PostalCode,
                    CountryName = address.CountryName,
                    CountryCode = address.CountryCode,
                    AdminArea = address.AdminArea,
                    SubAdminArea = address.SubAdminArea,
                    Locality = address.Locality,
                    SubLocality = address.SubLocality,
                    Thoroughfare = address.Thoroughfare,
                    SubThoroughfare = address.SubThoroughfare,
                    Premises = address.Premises,
                    Phone = address.Phone,
                    Url = address.Url,
                    FeatureName = address.FeatureName
                };

                if (address.Latitude.HasValue)
                    javaAddress.Latitude = address.Latitude.Value;
                if (address.Longitude.HasValue)
                    javaAddress.Longitude = address.Longitude.Value;

                result.Add(javaAddress);
            }

            return result;
        }
    }
}