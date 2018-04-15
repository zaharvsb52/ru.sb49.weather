using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Android.Locations;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Sb49.Weather.Droid.Common;
using AndroidUtil = Sb49.Common.Droid.Util;

namespace Sb49.Weather.Droid.Model
{
    public sealed class LocationAddress : IWeatherLocationAddress
    {
        public const string CommaDelimeter = ", ";
        public const string SpaceDelimeter = " ";

        private JsonSerializerSettings _jsonSerializerSettings;
        private IContractResolver _contractResolver;

        public LocationAddress() 
        {
        }

        public LocationAddress(Address address)
        {
            if(address == null)
                throw new ArgumentNullException(nameof(address));

            ConvertAddress(address);
        }

        ~LocationAddress()
        {
            OnDispose();
        }

        [JsonIgnore]
        public int? Id { get; set; }
        public string PostalCode { get; set; }
        public string CountryName { get; set; }
        public string CountryCode { get; set; }
        public string AdminArea { get; set; }
        public string SubAdminArea { get; set; }
        public string Locality { get; set; }
        public string DisplayLocality { get; set; }
        public string SubLocality { get; set; }
        public string Thoroughfare { get; set; }
        public string SubThoroughfare { get; set; }
        public string Premises { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string Phone { get; set; }
        public string Url { get; set; }
        public string FeatureName { get; set; }

        [JsonIgnore]
        public bool HasCoordinates => Latitude.HasValue && Longitude.HasValue;

        [JsonIgnore]
        public bool HasCoordinatesOnly => string.IsNullOrEmpty(Locality) && HasCoordinates;

        [JsonIgnore]
        public string Address => GetAddress(CommaDelimeter);

        public string GetLocality()
        {
            return string.IsNullOrEmpty(Locality) && HasCoordinates
                ? AppSettings.Default.GetStringById(Resource.String.CurrentLocationEmpty)
                : Locality;
        }

        public string GetDisplayLocality()
        {
            if (!string.IsNullOrEmpty(DisplayLocality))
                return DisplayLocality;

            return GetLocality();
        }

        public string GetAddress(string delimeter, bool useGetLocality = true)
        {
            var list = new List<string>();
            var locality = useGetLocality ? GetLocality() : Locality;
            if (!string.IsNullOrEmpty(locality))
                list.Add(locality);
            if (!string.IsNullOrEmpty(AdminArea))
                list.Add(AdminArea);
            if (!string.IsNullOrEmpty(CountryCode))
                list.Add(CountryCode);
            if (HasCoordinates)
                list.Add(CoordinatesToString());

            if (list.Count == 0)
                return null;

            var result = string.Join(delimeter, list.Distinct());
            if (!string.IsNullOrEmpty(result) && !string.IsNullOrEmpty(PostalCode))
                result = string.Format("{0} {1}", PostalCode, result);
            return result;
        }
        public string CoordinatesToString()
        {
            return HasCoordinates ? string.Format(GetFormatProvider(), "({0:n4}, {1:n4})", Latitude, Longitude) : null;
        }

        public string[] GetAddressLine(string delimeter)
        {
            var lineList = new List<string>();

            var emptyCountryName = string.IsNullOrEmpty(CountryName);
            var line =
                string.Format("{0}{1}", emptyCountryName ? null : CountryName,
                    emptyCountryName ? null : (string.IsNullOrEmpty(CountryCode) ? null :
                        string.Format(" ({0})", CountryCode)));
            if (!string.IsNullOrEmpty(line))
            {
                if(!string.IsNullOrEmpty(PostalCode))
                    line = string.Format("{0} {1}", PostalCode, line);
                lineList.Add(line);
            }

            var list = new List<string>();

            void AddHandler(string src)
            {
                if (string.IsNullOrEmpty(src))
                    return;
                list.Add(src);
            }

            AddHandler(AdminArea);
            AddHandler(SubAdminArea);
            AddHandler(GetLocality());
            AddHandler(SubLocality);
            AddHandler(Thoroughfare);
            AddHandler(SubThoroughfare);
            AddHandler(Premises);
            AddHandler(CoordinatesToString());

            var address = list.Count == 0 ? null : string.Join(delimeter, list.Distinct());
            if (!string.IsNullOrEmpty(address))
                lineList.Add(address);

            return lineList.Count == 0 ? null : lineList.ToArray();
        }

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(Locality) || HasCoordinates;
        }

        public string GetMd5Code()
        {
            if (_contractResolver == null)
                _contractResolver = new LocationAddressDynamicContractResolver(new[] {nameof(DisplayLocality)});

            var result = JsonConvert.SerializeObject(this, GetJsonSerializerSettings(_contractResolver));
            return AndroidUtil.GetMd5Code(result, false);
        }

        public override string ToString()
        {
            var result = JsonConvert.SerializeObject(this, GetJsonSerializerSettings(null)); 
            return result;
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var locationAddress = obj as LocationAddress;
            if (locationAddress == null)
                return false;

            return int.Equals(GetHashCode(), locationAddress.GetHashCode());
        }

        public bool EqualsCoordinates(LocationAddress locationAddress)
        {
            if (locationAddress == null)
                return false;

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            return HasCoordinates && Locality == locationAddress.Locality && Longitude == locationAddress.Longitude;
        }

        public bool EqualsMd5Code(LocationAddress locationAddress)
        {
            if (locationAddress == null)
                return false;

            return string.Equals(GetMd5Code(), locationAddress.GetMd5Code());
        }

        public static LocationAddress Parse(string address)
        {
            if (string.IsNullOrEmpty(address))
                return null;

            var result = new LocationAddress();
            var formatProvider = GetFormatProvider();
            var parse = address.Split( ",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in parse)
            {
                var value = p.Trim();
                if (string.IsNullOrEmpty(value))
                    continue;

                if (!result.Latitude.HasValue && value.Length > 1 && value.StartsWith("("))
                {
                    if (double.TryParse(value.Substring(1), NumberStyles.Number, formatProvider, out double latitude))
                    {
                        result.Latitude = latitude;
                        continue;
                    }
                }
                if (!result.Longitude.HasValue && value.Length > 1 && value.EndsWith(")"))
                {
                    if (double.TryParse(value.Substring(0, value.Length - 1), NumberStyles.Number, formatProvider,
                        out double longitude))
                    {
                        result.Longitude = longitude;
                        continue;
                    }
                }
                if (string.IsNullOrEmpty(result.Locality))
                {
                    result.Locality = value;
                    continue;
                }
                if (string.IsNullOrEmpty(result.CountryCode))
                    result.CountryCode = value.ToUpper();
            }

            return result;
        }

        private static IFormatProvider GetFormatProvider()
        {
            var result = new NumberFormatInfo {NumberDecimalSeparator = "."};
            return result;
        }

        private void ConvertAddress(Address address)
        {
            if (address == null)
                return;

            PostalCode = address.PostalCode;
            CountryName = address.CountryName;
            CountryCode = address.CountryCode;
            AdminArea = address.AdminArea;
            SubAdminArea = address.SubAdminArea;
            Locality = address.Locality;
            SubLocality = address.SubLocality;
            Thoroughfare = address.Thoroughfare;
            SubThoroughfare = address.SubThoroughfare;
            Premises = address.Premises;
            Latitude = address.HasLatitude ? address.Latitude : (double?) null;
            Longitude = address.HasLongitude ? address.Longitude : (double?) null;
            Phone = address.Phone;
            Url = address.Url;
            FeatureName = address.FeatureName;
        }

        private JsonSerializerSettings GetJsonSerializerSettings(IContractResolver contractResolver)
        {
            if (_jsonSerializerSettings == null)
            {
                _jsonSerializerSettings = new JsonSerializerSettings
                {
                    Formatting = Formatting.None
                };
            }
            _jsonSerializerSettings.ContractResolver = contractResolver;
            return _jsonSerializerSettings;
        }

        #region . IDisposable .

        private void OnDispose()
        {
            _contractResolver = null;
            _jsonSerializerSettings = null;
        }

        public void Dispose()
        {
            OnDispose();
            GC.SuppressFinalize(this);
        }

        #endregion . IDisposable .
    }
}