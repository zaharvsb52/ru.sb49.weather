using System;

namespace Sb49.Rfc822
{
    /// <summary>
    /// Maps a numeric time zone to its UTC offset.
    /// </summary>
    public class NumericTimeZoneMapper : ITimeZoneMapper
    {
        public TimeZone Map(string identifier)
        {
            int hours, minutes;

            var sign = identifier.Substring(0, 1);
            int.TryParse(sign + identifier.Substring(1, 2), out hours);
            int.TryParse(sign + identifier.Substring(3, 2), out minutes);

            var offset = new TimeSpan(hours, minutes, 0);

            return new TimeZone(identifier, offset);
        }
    }
}
