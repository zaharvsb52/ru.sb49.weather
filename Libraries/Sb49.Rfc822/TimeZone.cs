using System;
using System.Collections.Generic;

namespace Sb49.Rfc822
{
    /// <summary>
    /// Simple read-only structure to hold a time zone identifier and its UTC offset.
    /// </summary>
    public struct TimeZone
    {
        // Used to hold the date internally
        private readonly KeyValuePair<string, TimeSpan> _zone;

        public TimeZone(string identifier, TimeSpan offset)
        {
            _zone = new KeyValuePair<string, TimeSpan>(identifier, offset);
        }

        /// <summary>
        /// The time zone identifier, e.g. "EDT" (abbreviated format), "-0400" (numeric 
        /// format) or "D" (military format).
        /// </summary>
        public string Identifier => _zone.Key;

        /// <summary>
        /// The UTC offset of the time zone.
        /// </summary>
        public TimeSpan Offset => _zone.Value;
    }
}
