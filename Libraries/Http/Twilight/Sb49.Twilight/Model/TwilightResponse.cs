using Newtonsoft.Json;

namespace Sb49.Twilight.Model
{
    public class TwilightResponse
    {
        public const string StatusOk = "OK";

        [JsonProperty(PropertyName = "results")]
        public Results Results { get; set; }

        [JsonProperty(PropertyName = "status")]
        public string Status { get; set; }
    }
}