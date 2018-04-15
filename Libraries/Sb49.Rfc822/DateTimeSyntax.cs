using System;

namespace Sb49.Rfc822
{
    /// <summary>
    /// Variations on the RFC 822 date and time syntax.
    /// </summary>
    [Flags]
    public enum DateTimeSyntax
    {
        /// <summary>
        /// Minimal syntax, e.g. 9 May 06 12:34.
        /// </summary>
        None = 0,

        /// <summary>
        /// Uses two digits for days 1 through 9, e.g. 09 May 06 12:34.
        /// </summary>
        /// <example></example>
        TwoDigitDay = 1,

        /// <summary>
        /// Uses 4 digits for the year instead of two, e.g. 9 May 2006 12:34.
        /// </summary>
        FourDigitYear = 2,

        /// <summary>
        /// Includes seconds in the time, e.g. 9 May 06 12:34:56.
        /// </summary>
        WithSeconds = 4,

        /// <summary>
        /// Includes the abbreviated day name, e.g. Tue, 9 May 06 12:34.
        /// </summary>
        WithDayName = 8,

        /// <summary>
        /// Includes am/pm.
        /// </summary>
        UseAmPm = 16,

        /// <summary>
        /// Uses 2 digits for the time, e.g. 9 May 2006 02:04.
        /// </summary>
        TwoDigitTime = 32,

        /// <summary>
        /// The time zone is numeric, e.g. 9 May 06 12:34 +0400.
        /// </summary>
        NumericTimeZone = 64
    }
}
