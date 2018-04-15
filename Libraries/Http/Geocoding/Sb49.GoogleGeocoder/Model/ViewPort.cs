using Newtonsoft.Json;

namespace Sb49.GoogleGeocoder.Model
{
    public class ViewPort
    {
        [JsonProperty(PropertyName = "northeast")]
        public Location NorthEast { get; set; }

        [JsonProperty(PropertyName = "southwest")]
        public Location SouthWest { get; set; }
    }
}