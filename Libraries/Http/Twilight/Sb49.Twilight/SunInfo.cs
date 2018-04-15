using System;

namespace Sb49.Twilight
{
    public class SunInfo
    {
        /// <summary>
        /// The Utc sunrise date.
        /// </summary>
        public DateTime? Sunrise { get; set; }

        /// <summary>
        /// The Utc sunset date.
        /// </summary>
        public DateTime? Sunset { get; set; }

        public bool Validate()
        {
            return Sunrise.HasValue && Sunset.HasValue;
        }
    }
}