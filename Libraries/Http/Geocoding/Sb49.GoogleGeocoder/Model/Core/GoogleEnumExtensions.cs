namespace Sb49.GoogleGeocoder.Model.Core
{
    public sealed class GoogleEnumExtensions
    {
        /// <remarks>
        /// http://code.google.com/apis/maps/documentation/geocoding/#StatusCodes
        /// </remarks>
        public GoogleStatus EvaluateStatus(string status)
        {
            switch (status)
            {
                case "OK": return GoogleStatus.Ok;
                case "ZERO_RESULTS": return GoogleStatus.ZeroResults;
                case "OVER_QUERY_LIMIT": return GoogleStatus.OverQueryLimit;
                case "REQUEST_DENIED": return GoogleStatus.RequestDenied;
                case "INVALID_REQUEST": return GoogleStatus.InvalidRequest;
                default: return GoogleStatus.Error;
            }
        }

        /// <remarks>
		/// https://developers.google.com/maps/documentation/geocoding/?csw=1#Results
		/// </remarks>
		public GoogleLocationType EvaluateLocationType(string type)
        {
            switch (type)
            {
                case "ROOFTOP":
                    return GoogleLocationType.Rooftop;
                case "RANGE_INTERPOLATED":
                    return GoogleLocationType.RangeInterpolated;
                case "GEOMETRIC_CENTER":
                    return GoogleLocationType.GeometricCenter;
                case "APPROXIMATE":
                    return GoogleLocationType.Approximate;
                default:
                    return GoogleLocationType.Unknown;
            }
        }

        /// <remarks>
		/// http://code.google.com/apis/maps/documentation/geocoding/#Types
		/// </remarks>
		public GoogleAddressType EvaluateAddressType(string type)
        {
            switch (type)
            {
                case "street_address":
                    return GoogleAddressType.StreetAddress;
                case "route":
                    return GoogleAddressType.Route;
                case "intersection":
                    return GoogleAddressType.Intersection;
                case "political":
                    return GoogleAddressType.Political;
                case "country":
                    return GoogleAddressType.Country;
                case "administrative_area_level_1":
                    return GoogleAddressType.AdministrativeAreaLevel1;
                case "administrative_area_level_2":
                    return GoogleAddressType.AdministrativeAreaLevel2;
                case "administrative_area_level_3":
                    return GoogleAddressType.AdministrativeAreaLevel3;
                case "colloquial_area":
                    return GoogleAddressType.ColloquialArea;
                case "locality":
                    return GoogleAddressType.Locality;
                case "sublocality":
                    return GoogleAddressType.SubLocality;
                case "neighborhood":
                    return GoogleAddressType.Neighborhood;
                case "premise":
                    return GoogleAddressType.Premise;
                case "subpremise":
                    return GoogleAddressType.Subpremise;
                case "postal_code":
                    return GoogleAddressType.PostalCode;
                case "natural_feature":
                    return GoogleAddressType.NaturalFeature;
                case "airport":
                    return GoogleAddressType.Airport;
                case "park":
                    return GoogleAddressType.Park;
                case "point_of_interest":
                    return GoogleAddressType.PointOfInterest;
                case "post_box":
                    return GoogleAddressType.PostBox;
                case "street_number":
                    return GoogleAddressType.StreetNumber;
                case "floor":
                    return GoogleAddressType.Floor;
                case "room":
                    return GoogleAddressType.Room;
                case "postal_town":
                    return GoogleAddressType.PostalTown;
                case "establishment":
                    return GoogleAddressType.Establishment;
                case "sublocality_level_1":
                    return GoogleAddressType.SubLocalityLevel1;
                case "sublocality_level_2":
                    return GoogleAddressType.SubLocalityLevel2;
                case "sublocality_level_3":
                    return GoogleAddressType.SubLocalityLevel3;
                case "sublocality_level_4":
                    return GoogleAddressType.SubLocalityLevel4;
                case "sublocality_level_5":
                    return GoogleAddressType.SubLocalityLevel5;
                case "postal_code_suffix":
                    return GoogleAddressType.PostalCodeSuffix;
                default:
                    return GoogleAddressType.Unknown;
            }
        }
    }
}