using System;

namespace Sb49.Http.Provider.Core
{
    public interface ILocationAddress
    {
        string PostalCode { get; set; }
        string CountryName { get; set; }
        string CountryCode { get; set; }
        string AdminArea { get; set; }
        string SubAdminArea { get; set; }
        string Locality { get; set; }
        string SubLocality { get; set; }
        string Thoroughfare { get; set; }
        string SubThoroughfare { get; set; }
        string Premises { get; set; }
        double? Latitude { get; set; }
        double? Longitude { get; set; }
        string Phone { get; set; }
        string Url { get; set; }
        string FeatureName { get; set; }
        string Address { get; }
        bool HasCoordinates { get; }
        bool HasCoordinatesOnly { get; }

        string GetAddress(string delimeter, bool useGetLocality = true);
        bool IsValid();
    }
}