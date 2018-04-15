using System;
using System.Linq;
using System.Reflection;
using Sb49.Weather.Code;

namespace Sb49.Weather.Model.Core
{
    public abstract class WeatherDataPointBase : IDisposable
    {
        public double? ApparentTemperature { get; set; }
        public double? Temperature { get; set; }
        public virtual double? MinTemperature { get; set; }
        public virtual double? MaxTemperature { get; set; }
        public double? Pressure { get; set; }
        public double? Humidity { get; set; }
        public double? Visibility { get; set; }
        public double? WindDirection { get; set; }
        public double? WindSpeed { get; set; }
        public double? DewPoint { get; set; }
        public WeatherCodes WeatherCode { get; set; }
        public string WeatherUnrecognizedCode { get; set; }
        public string Condition { get; set; }
        public string Icon { get; set; }

        ~WeatherDataPointBase()
        {
            OnDispose(false);
        }

        public object Clone()
        {
            var clone = CreateClone();
            SetProperties(source: this, destination: clone);
            return clone;
        }

        protected virtual object CreateClone()
        {
            return Activator.CreateInstance(GetType());
        }

        protected void SetProperties(object source, object destination)
        {
            if (source == null || destination == null)
                return;

            var sourceProperties = source.GetType().GetRuntimeProperties();
            var destinationType = destination.GetType();

            foreach (var sourcePropertyInfo in sourceProperties.Where(p => p.CanRead))
            {
                var destinationPropertyInfo = destinationType.GetRuntimeProperty(sourcePropertyInfo.Name);
                if(destinationPropertyInfo == null || !destinationPropertyInfo.CanWrite)
                    continue;

                var value = sourcePropertyInfo.GetValue(source);
                OnCloneSetValue(destination, destinationPropertyInfo, value);
            }
        }

        protected virtual void OnCloneSetValue(object destination, PropertyInfo destinationPropertyInfo, object value)
        {
            destinationPropertyInfo.SetValue(destination, value);
        }

        // ReSharper disable once UnusedParameter.Local
        internal static void ValidateDate(DateTime? date, DateTimeKind kind, string argumentName)
        {
            if (date.HasValue && date.Value.Kind != kind)
            {
                throw new ArgumentException(string.Format("Kind of {0} should be {1}.", argumentName,
                    kind));
            }
        }

        #region . IDisposable .

        protected virtual void OnDispose(bool disposing)
        {
        }

        public void Dispose()
        {
            OnDispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion . IDisposable .
    }
}