using System;

namespace Sb49.Weather.Model.Core
{
    public interface IAstronomyInfo
    {
        /// <summary>
        /// The Utc sunrise date.
        /// </summary>
        DateTime? Sunrise { get; }

        /// <summary>
        /// The Utc sunset date.
        /// </summary>
        DateTime? Sunset { get; }
    }
}
