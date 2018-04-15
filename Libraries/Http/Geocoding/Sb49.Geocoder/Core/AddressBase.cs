using Sb49.Http.Provider.Core;

namespace Sb49.Geocoder.Core
{
    /// <summary>
    /// Most basic and generic form of address.
    /// Just the full address string and a lat/long
    /// </summary>
    public abstract class AddressBase : ILocationAddress
    {
		protected AddressBase(string provider)
		{
			Provider = provider;
		}

        public string Provider { get; }

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
        public abstract string Address { get; }

        public bool HasCoordinates => Latitude.HasValue && Longitude.HasValue;
        public virtual bool HasCoordinatesOnly => string.IsNullOrEmpty(Locality) && HasCoordinates;

        public abstract string GetAddress(string delimeter, bool useGetLocality = true);
        public abstract bool IsValid();
    }
}