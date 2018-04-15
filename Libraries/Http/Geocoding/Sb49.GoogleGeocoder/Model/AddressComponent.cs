using System.Linq;
using Newtonsoft.Json;
using Sb49.GoogleGeocoder.Model.Core;

namespace Sb49.GoogleGeocoder.Model
{
    public class AddressComponent
    {
        [JsonProperty(PropertyName = "long_name")]
        public string LongName { get; set; }

        [JsonProperty(PropertyName = "short_name")]
        public string ShortName { get; set; }

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