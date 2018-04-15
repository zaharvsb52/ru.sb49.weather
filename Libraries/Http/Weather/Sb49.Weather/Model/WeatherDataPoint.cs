using System;
using System.Reflection;
using Sb49.Weather.Model.Core;

namespace Sb49.Weather.Model
{
    public class WeatherDataPoint : WeatherDataPointBase, IWeatherDataPoint
    {
        public WeatherDataPoint()
        {
        }

        public WeatherDataPoint(IWeatherDataPoint src)
        {
            SetProperties(src, this);
        }

        private DateTime? _date;

        /// <summary>
        /// The Utc date of calculation.
        /// </summary>
        public virtual DateTime? Date
        {
            get => _date;
            set
            {
                ValidateDate(value, DateTimeKind.Utc, nameof(Date));
                _date = value;
            }
        }

        public IAstronomy Astronomy { get; set;}

        protected override void OnCloneSetValue(object destination, PropertyInfo destinationPropertyInfo, object value)
        {
            if (value is IAstronomy astronomy)
            {
                var valueAstronomy = new Astronomy
                {
                    Sunrise = astronomy.Sunrise,
                    Sunset = astronomy.Sunset
                };

                if (astronomy.PreviousInfo != null)
                {
                    valueAstronomy.PreviousInfo = new AstronomyInfo
                    {
                        Sunrise = astronomy.PreviousInfo.Sunrise,
                        Sunset = astronomy.PreviousInfo.Sunset
                    };
                }

                if (astronomy.NextInfo != null)
                {
                    valueAstronomy.NextInfo = new AstronomyInfo
                    {
                        Sunrise = astronomy.NextInfo.Sunrise,
                        Sunset = astronomy.NextInfo.Sunset
                    };
                }

                value = valueAstronomy;
            }

            base.OnCloneSetValue(destination, destinationPropertyInfo, value);
        }

        protected override void OnDispose(bool disposing)
        {
            Astronomy = null;
            base.OnDispose(disposing);
        }
    }
}