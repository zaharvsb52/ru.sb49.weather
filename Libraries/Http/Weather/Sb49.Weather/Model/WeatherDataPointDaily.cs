using System;
using System.Linq;
using System.Threading.Tasks;
using Sb49.Weather.Model.Core;

namespace Sb49.Weather.Model
{
    public class WeatherDataPointDaily : WeatherDataPoint
    {
        public WeatherDataPointDaily()
        {
        }

        public WeatherDataPointDaily(IWeatherDataPoint src)
        {
            SetProperties(source: src, destination: this);
        }

        private WeatherDataPointHourly[] _hourly;
        public WeatherDataPointHourly[] Hourly
        {
            get => _hourly;
            set
            {
                if (_hourly == value)
                    return;
                _hourly = value;

                Task.Run(async () =>
                {
                    await CalculateMinMaxTemperatureAsync().ConfigureAwait(false);
                });
            }
        }

        public void CalculateMinMaxTemperature()
        {
            if(Hourly == null)
                return;
            
            var temperatures = Hourly.Where(p => p.Temperature.HasValue).Select(p => p.Temperature).ToArray();
            var minTemperature = Hourly.Where(p => p.MinTemperature.HasValue).Min(p => p.MinTemperature);
            var maxTemperature = Hourly.Max(p => p.MaxTemperature);

            MinTemperature = new[] {MinTemperature, temperatures.Min(), minTemperature}.Where(p => p.HasValue).Min();
            MaxTemperature = new[] {MaxTemperature, temperatures.Max(), maxTemperature}.Where(p => p.HasValue).Max();
        }

        public Task CalculateMinMaxTemperatureAsync()
        {
            return Task.Run(() => CalculateMinMaxTemperature());
        }

        protected override void OnDispose(bool disposing)
        {
            Hourly = null;
            base.OnDispose(disposing);
        }
    }
}
