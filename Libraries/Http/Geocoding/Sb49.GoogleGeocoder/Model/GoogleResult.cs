using System.Linq;
using Newtonsoft.Json;
using Sb49.GoogleGeocoder.Model.Core;

namespace Sb49.GoogleGeocoder.Model
{
    public class GoogleResult
    {
        [JsonProperty(PropertyName = "address_components")]
        public AddressComponent[] AddressComponents { get; set; }

        [JsonProperty(PropertyName = "formatted_address")]
        public string FormattedAddress { get; set; }

        [JsonProperty(PropertyName = "geometry")]
        public Geometry Geometry { get; set; }

        [JsonProperty(PropertyName = "types")]
        public string[] ResponseTypes { get; set; }

        [JsonIgnore]
        public GoogleAddressType[] Type
        {
            get
            {
                var googleEnumExtensions = new GoogleEnumExtensions();
                var result = ResponseTypes?.Select(googleEnumExtensions.EvaluateAddressType).ToArray();
                return result;
            }
        }
    }
}