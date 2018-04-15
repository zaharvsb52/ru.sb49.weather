using System;
using System.Globalization;
using Sb49.Http.Core;

namespace Sb49.Http.Provider.Core
{
    public abstract class ProviderServiceBase : HttpClientBase, IProviderService
    {
        protected const string DateFormat = "yyyyMMdd";

        ~ProviderServiceBase()
        {
            OnDispose(false);
        }

        public RequestCounterBase RequestCounter { get; protected set; }

        protected virtual IFormatProvider FormatProvider
        {
            get
            {
                var result = new NumberFormatInfo { NumberDecimalSeparator = "." };
                return result;
            }
        }

        protected virtual DateTimeStyles DateTimeLocalStyles => DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces;
        protected virtual DateTimeStyles DateTimeUniversalStyles => DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal | DateTimeStyles.AllowWhiteSpaces;
        protected virtual DateTimeStyles DateTimeUnspecifiedStyles => DateTimeStyles.AllowWhiteSpaces;

        protected virtual string[] DateTimeFormats => new[]
        {
            "d", "D",
            "f", "F",
            "yyyy-MM-dd H:m:s",
            "yyyy-M-d H:m:s",
            "d MMM yyyy",
            "dd MMM yyyy",
            DateFormat,
            DateFormat + " H:m:s",
            DateFormat + " H:m",
            DateFormat + " h:m:s tt",
            DateFormat + " h:m tt"
        };

        protected virtual DateTime? ConvertStringToDateTime(string source, DateTimeStyles styles)
        {
            if (string.IsNullOrEmpty(source))
                return null;

            var provider = CultureInfo.InvariantCulture;
            var date = DateTime.ParseExact(source, DateTimeFormats, provider, styles);
            return date;
        }

        protected virtual DateTime? ConvertUnixTimeToUtcDateTime(long? unixtime)
        {
            if (!unixtime.HasValue)
                return null;

            var unixepoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return unixepoch.AddSeconds(unixtime.Value);
        }

        #region . IDisposable .

        protected virtual void OnDispose(bool disposing)
        {
            RequestCounter = null;
        }

        public void Dispose()
        {
            OnDispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion . IDisposable .
    }
}