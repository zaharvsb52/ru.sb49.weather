using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Sb49.Geocoder.Core;
using Sb49.GoogleGeocoder.Core;
using Sb49.GoogleGeocoder.Exeptions;
using Sb49.GoogleGeocoder.Model;
using Sb49.GoogleGeocoder.Model.Core;
using Sb49.Http.Core;
using Sb49.Http.Provider.Core;
using Sb49.Http.Provider.Exeptions;
using Sb49.Security.Core;

namespace Sb49.GoogleGeocoder.Impl
{
    public class GeocoderImpl : ProviderServiceBase, IGeocoder
    {
        protected const int MaxRequestCountPerDay = 1990; //2500
        private const string LanguageCodeDefault = "en";

        public GeocoderImpl(RequestCounterBase requestCounter)
        {
            if (requestCounter == null)
                throw new ArgumentNullException(nameof(requestCounter));

            RequestCounter = requestCounter;
        }

        public override Uri BaseUri => new Uri("https://maps.googleapis.com/maps/api/geocode/");
        public ISb49SecureString ApiKey { get; set; }
        public string LanguageCode { get; set; }
        public string RegionBias { get; set; }
        public Bounds BoundsBias { get; set; }
        public IList<GoogleComponentFilter> ComponentFilters { get; set; }

        public IList<ILocationAddress> GetFromLocation(double latitude, double longitude, int maxResults, CancellationToken? token)
        {
            var requestString = BuildRequestUri(latitude, longitude);
            var result = Get(requestString, maxResults, token);
            return result;
        }

        public IList<ILocationAddress> GetFromLocationName(string locationName, int maxResults, CancellationToken? token)
        {
            var requestString = BuildRequestUri(locationName);
            var result = Get(requestString, maxResults, token);
            return result;
        }

        private string BuildRequestUri(double latitude, double longitude)
        {
            return BuildRequestUri("latlng", string.Format(FormatProvider, "{0},{1}", latitude, longitude));
        }

        private string BuildRequestUri(string locationName)
        {
            return BuildRequestUri("address", locationName);
        }

        private string BuildRequestUri(string type, string value)
        {
            return string.Format(BuildRequestUri(), type, value);
        }

        private string BuildRequestUri()
        {
            var builder = new StringBuilder();
            builder.Append("json?{0}={1}");

            builder.AppendFormat("&key={0}", ApiKey?.Decrypt());

            builder.AppendFormat("&language={0}", GetValidLanguage(LanguageCode));

            if (!string.IsNullOrEmpty(RegionBias))
                builder.AppendFormat("&region={0}", RegionBias);

            if (BoundsBias != null)
            {
                builder.AppendFormat("&bounds={0},{1}|{2},{3}", BoundsBias.SouthWest?.Latitude,
                    BoundsBias.SouthWest?.Longitude, BoundsBias.NorthEast?.Latitude, BoundsBias.NorthEast?.Longitude);
            }

            if (ComponentFilters != null)
            {
                builder.AppendFormat("&components={0}",
                    string.Join("|", ComponentFilters.Select(x => x.ComponentFilter)));
            }

            return builder.ToString();
        }

        protected override bool OnValidateRequest(CancellationToken token)
        {
            ValidateApiKey();
            return ValidateRequestCount();
        }

        protected override void OnIncrementRequestCount()
        {
            RequestCounter++;
            RequestCounter.UpdatedDate = DateTime.UtcNow;
            RequestCounter?.Save();
        }

        private bool ValidateRequestCount()
        {
            if (RequestCounter == null)
                throw new ArgumentNullException(nameof(RequestCounter));

            var updatedDate = RequestCounter.UpdatedDate;
            if (!updatedDate.HasValue)
                return true;

            var date = updatedDate.Value.Date;
            var today = DateTime.UtcNow.Date;
            if ((today - date).TotalDays >= 1)
            {
                RequestCounter.Count = 0;
                return true;
            }

            var result = RequestCounter.Count < MaxRequestCountPerDay;
            if (!result)
                throw new ExceededServerRequestCountsException();

            return true;
        }

        protected override object OnProcessResponse(HttpResponseHeaders responseHeaders, string json, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(json) || json == string.Empty)
                return null;
            return json;
        }

        private IList<ILocationAddress> Get(string requestString, int maxResults, CancellationToken? token)
        {
            if (!token.HasValue)
                token = CancellationToken.None;
            return Get(requestString, maxResults, token.Value);
        }

        private IList<ILocationAddress> Get(string requestString, int maxResults, CancellationToken token)
        {
            var responseResult = GetStringAsync(requestString, token);
            var result = OnProcessResponse(responseResult, maxResults, token);
            return result;
        }

        private IList<ILocationAddress> OnProcessResponse(object responseResult, int maxResults, CancellationToken token)
        {
            if (IfCancellationRequested(token))
                return null;

            if (responseResult == null)
                return null;

            var json = responseResult as string;
            if (string.IsNullOrEmpty(json) || string.IsNullOrWhiteSpace(json))
                return null;

            var response = JsonConvert.DeserializeObject<GoogleResponse>(json);

            if (response?.Status == null)
            {
                throw new GoogleGeocodingException(GoogleStatus.Error, new HttpRequestException("Empty response."));
            }

            if (response.Status != GoogleStatus.Ok && response.Status != GoogleStatus.ZeroResults)
            {
                throw new GoogleGeocodingException(response.Status, response.ErrorMessage);
            }

            if (response.Results == null || response.Results.Length == 0)
            {
                throw new GoogleGeocodingException(GoogleStatus.Error, response.ErrorMessage,
                    new HttpRequestException("Empty response result."));
            }

            var result = new List<ILocationAddress>();
            if (response.Status == GoogleStatus.ZeroResults)
                return result;

            var count = 0;
            foreach (var respResult in response.Results.Where(p => p != null))
            {
                if (IfCancellationRequested(token))
                    return null;

                if (count++ >= maxResults)
                    break;

                if (respResult.AddressComponents == null)
                    continue;

                var address = new GoogleAddress("Google");
                foreach (var component in respResult.AddressComponents.Where(p => p != null))
                {
                    if (IfCancellationRequested(token))
                        return null;

                    foreach (var compType in component.Type)
                    {
                        if (IfCancellationRequested(token))
                            return null;

                        switch (compType)
                        {
                            case GoogleAddressType.PostalCode:
                                address.PostalCode = component.LongName;
                                continue;
                            case GoogleAddressType.Country:
                                address.CountryName = component.LongName;
                                address.CountryCode = component.ShortName;
                                continue;
                            case GoogleAddressType.AdministrativeAreaLevel1:
                                address.AdminArea = component.LongName;
                                continue;
                            case GoogleAddressType.AdministrativeAreaLevel2:
                                address.SubAdminArea = component.LongName;
                                continue;
                            case GoogleAddressType.Locality:
                                address.Locality = component.LongName;
                                continue;
                            case GoogleAddressType.SubLocalityLevel1:
                                address.SubLocality = component.LongName;
                                continue;
                            case GoogleAddressType.SubLocalityLevel2:
                                address.Thoroughfare = component.LongName;
                                continue;
                            case GoogleAddressType.SubLocalityLevel3:
                                address.SubThoroughfare = component.LongName;
                                continue;
                            case GoogleAddressType.Premise:
                                address.Premises = component.LongName;
                                continue;
                            default:
                                continue;
                        }
                    }
                }

                if (respResult.Geometry?.Location?.Latitude != null &&
                    respResult.Geometry.Location.Longitude.HasValue)
                {
                    address.Latitude = respResult.Geometry.Location.Latitude.Value;
                    address.Longitude = respResult.Geometry.Location.Longitude.Value;
                }

                result.Add(address);
            }

            return result;
        }

        private string GetValidLanguage(string languageCode)
        {
            if (string.IsNullOrEmpty(languageCode))
                return LanguageCodeDefault;

            var languages = new[]
            {
                LanguageCodeDefault, "ar", "kn", "bg", "ko", "bn", "lt", "ca", "lv", "cs", "ml", "da", "mr", "de", "nl",
                "el", "no", "en", "pl", "en-AU", "pt", "en-GB", "pt-BR", "es", "pt-PT", "eu", "ro", "eu", "ru", "fa", "sk",
                "fi", "sl", "fil", "sr", "fr", "sv", "gl", "ta", "gu", "te", "hi", "th", "hr", "tl", "hu", "tr",
                "id", "uk", "it", "vi", "iw", "zh-CN", "ja", "zh-TW"
            };
            var result =
                languages.FirstOrDefault(p => string.Equals(languageCode, p, StringComparison.OrdinalIgnoreCase));
            return result ?? LanguageCodeDefault;
        }

        protected override bool IfCancellationRequested(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return false;
        }

        private void ValidateApiKey()
        {
            if (ApiKey == null || !ApiKey.Validate())
                throw new ApiKeyException();
        }
    }
}