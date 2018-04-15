using System;
using Sb49.Weather.Model.Core;

namespace Sb49.Weather.Model
{
    public class WeatherDataPointHourly : WeatherDataPointBase, IWeatherDataPoint
    {
        private readonly WeakReference<WeatherDataPointDaily> _parent;

        public WeatherDataPointHourly(WeatherDataPointDaily daily)
        {
            if (daily == null)
                throw new ArgumentNullException(nameof(daily));

            _parent = new WeakReference<WeatherDataPointDaily>(daily);
        }

        public WeatherDataPointHourly(WeatherDataPointDaily daily, IWeatherDataPoint src) : this(daily)
        {
            SetProperties(source: src, destination: this);
        }

        public WeatherDataPointDaily Daily
        {
            get
            {
                if(_parent == null)
                    return null;

                WeatherDataPointDaily daily;
                return _parent.TryGetTarget(out daily) ? daily : null;
            }
        }

        public int TimeOffsetSec { get; set; }

        public DateTime? Date
        {
            get
            {
                var dailyDate = Daily?.Date;
                if (!dailyDate.HasValue)
                    return null;

                var date = dailyDate.Value.Date;
                return date.AddSeconds(TimeOffsetSec);
            }
        }
        
        public IAstronomy Astronomy => Daily?.Astronomy;

        protected override object CreateClone()
        {
            return new WeatherDataPointHourly(Daily);
        }
    }
}
