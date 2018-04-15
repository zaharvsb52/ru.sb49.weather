using System;

namespace Sb49.Weather.Model.Core
{
    public class AstronomyInfo : IAstronomyInfo
    {
        private DateTime? _sunrise;

        /// <summary>
        /// The Utc sunrise date.
        /// </summary>
        public virtual DateTime? Sunrise
        {
            get => _sunrise;
            set
            {
                WeatherDataPointBase.ValidateDate(value, DateTimeKind.Utc, nameof(Sunrise));
                _sunrise = value;
            }
        }

        private DateTime? _sunset;

        /// <summary>
        /// The Utc sunset date.
        /// </summary>
        public virtual DateTime? Sunset
        {
            get => _sunset;
            set
            {
                WeatherDataPointBase.ValidateDate(value, DateTimeKind.Utc, nameof(Sunset));
                _sunset = value;
            }
        }
    }
}