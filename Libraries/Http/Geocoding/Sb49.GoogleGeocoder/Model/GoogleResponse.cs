using Newtonsoft.Json;
using Sb49.GoogleGeocoder.Model.Core;

namespace Sb49.GoogleGeocoder.Model
{
    //http://stackoverflow.com/questions/3001132/parse-google-maps-geocode-json-response-to-object-using-json-net
    public class GoogleResponse
    {
        [JsonProperty(PropertyName = "results")]
        public GoogleResult[] Results { get; set; }

        [JsonProperty(PropertyName = "status")]
        public string ResponseStatus { get; set; }

        [JsonProperty(PropertyName = "error_message")]
        public string ErrorMessage { get; set; }

        [JsonIgnore]
        public GoogleStatus Status => string.IsNullOrEmpty(ResponseStatus)
            ? GoogleStatus.Error
            : new GoogleEnumExtensions().EvaluateStatus(ResponseStatus);
    }
}