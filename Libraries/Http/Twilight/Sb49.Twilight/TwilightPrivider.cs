using System;
using System.Net.Http.Headers;
using System.Threading;
using Newtonsoft.Json;
using Sb49.Http.Provider.Core;
using Sb49.Twilight.Model;

namespace Sb49.Twilight
{
    public class TwilightPrivider : ProviderServiceBase
    {
        private const string DateTimeFormat = "yyyy-MM-dd";

        public override Uri BaseUri => new Uri("http://api.sunrise-sunset.org/");

        protected override bool OnValidateRequest(CancellationToken token)
        {
            if (IfCancellationRequested(token))
                return false;

            return true;
        }

        protected override void OnIncrementRequestCount()
        {
        }

        protected override object OnProcessResponse(HttpResponseHeaders responseHeaders, string json, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(json) || json == string.Empty)
                return null;
            return json;
        }

        public SunInfo Get(double latitude, double longitude, DateTime? utcDate = null, CancellationToken? token = null)
        {
            if(!token.HasValue)
                token = CancellationToken.None;

            if (IfCancellationRequested(token.Value))
                return null;

                var requestString = BuildRequestUri(latitude, longitude, utcDate);
                var responseResult = GetStringAsync(requestString, token.Value);
                if (responseResult == null)
                    return null;

                if (IfCancellationRequested(token.Value))
                    return null;

                var json = responseResult.ToString();
                if (string.IsNullOrWhiteSpace(json) || string.IsNullOrEmpty(json))
                    return null;

                var response = JsonConvert.DeserializeObject<TwilightResponse>(json);
                if (response?.Results == null || response.Status != TwilightResponse.StatusOk)
                    return null;

                if (IfCancellationRequested(token.Value))
                    return null;

                var result = new SunInfo();
                var dateTimeStr = (utcDate ?? DateTime.UtcNow).ToString(DateFormat);
                if (!string.IsNullOrEmpty(response.Results.Sunrise))
                {
                    var sunrise =
                        ConvertStringToDateTime(string.Format("{0} {1}", dateTimeStr, response.Results.Sunrise),
                            DateTimeUniversalStyles);
                    if (sunrise.HasValue)
                        result.Sunrise = sunrise.Value;
                }
                if (!string.IsNullOrEmpty(response.Results.Sunset))
                {
                    var sunset = ConvertStringToDateTime(
                        string.Format("{0} {1}", dateTimeStr, response.Results.Sunset), DateTimeUniversalStyles);
                    if (sunset.HasValue)
                    {
                        result.Sunset = sunset.Value;
                        if (result.Sunrise >= result.Sunset)
                            result.Sunset = result.Sunset.Value.AddDays(1);
                    }
                }

                if (IfCancellationRequested(token.Value))
                    return null;

                if (!result.Validate())
                    return null;

            return result;
        }

        private string BuildRequestUri(double latitude, double longitude, DateTime? date)
        {
            var datestr = date.HasValue ? string.Format("&date={0}", date.Value.ToString(DateTimeFormat)) : null;
            return string.Format(FormatProvider, "json?lat={0}&lng={1}{2}", latitude, longitude, datestr);
        }

        protected override bool IfCancellationRequested(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return false;
        }
    }
}