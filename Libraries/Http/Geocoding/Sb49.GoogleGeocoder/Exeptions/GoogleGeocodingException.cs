using System;
using Sb49.GoogleGeocoder.Model.Core;
using Sb49.Http.Provider.Exeptions;

namespace Sb49.GoogleGeocoder.Exeptions
{
    public class GoogleGeocodingException : ProviderServiceExceptionBase
    {
        public GoogleStatus Status { get; }

        public GoogleGeocodingException(GoogleStatus status) : base(GetMessage(null, status))
        {
            Status = status;
        }

        public GoogleGeocodingException(GoogleStatus status, string errorMessage)
            : base(GetMessage(errorMessage, status))
        {
            Status = status;
        }

        public GoogleGeocodingException(GoogleStatus status, Exception innerException)
            : base(GetMessage(null, status), innerException)
        {
            Status = status;
        }

        public GoogleGeocodingException(GoogleStatus status, string errorMessage, Exception innerException)
            : base(GetMessage(errorMessage, status), innerException)
        {
            Status = status;
        }

        private static string GetMessage(string errorMessage, GoogleStatus status)
        {
            return string.Format("Status = '{0}'. {1}", 
                status,
                string.IsNullOrEmpty(errorMessage)
                    ? "There was an error processing the geocoding request."
                    : errorMessage);
        }
    }
}