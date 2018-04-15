using Newtonsoft.Json;
using Sb49.Geocoder.Core;

namespace Sb49.GoogleGeocoder.Model
{
    public class Location : ILocation
    {
        [JsonProperty("lat")]
        public double? Latitude { get; set; }

        [JsonProperty("lng")]
        public double? Longitude { get; set; }
    }
}