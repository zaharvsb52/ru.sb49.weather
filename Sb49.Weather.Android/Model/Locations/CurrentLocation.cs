using System;
using Newtonsoft.Json;

namespace Sb49.Weather.Droid.Model
{
    public sealed class CurrentLocation
    {
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        /// <summary>
        /// The Utc last update date.
        /// </summary>
        public DateTime? UpdatedDate { get; set; }

        [JsonIgnore]
        public bool HasCoordinates => Latitude.HasValue && Longitude.HasValue;
    }
}