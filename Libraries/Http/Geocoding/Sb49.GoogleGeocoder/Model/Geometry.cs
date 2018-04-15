using Newtonsoft.Json;
using Sb49.GoogleGeocoder.Model.Core;

namespace Sb49.GoogleGeocoder.Model
{
    public class Geometry
    {
        [JsonProperty(PropertyName = "location")]
        public Location Location { get; set; }

        [JsonProperty(PropertyName = "location_type")]
        public string ResponseLocationType { get; set; }

        [JsonIgnore]
        public GoogleLocationType LocationType =>
            string.IsNullOrEmpty(ResponseLocationType)
                ? GoogleLocationType.Unknown
                : new GoogleEnumExtensions().EvaluateLocationType(ResponseLocationType);

        [JsonProperty(PropertyName = "viewport")]
        public ViewPort Viewport { get; set; }
    }
}