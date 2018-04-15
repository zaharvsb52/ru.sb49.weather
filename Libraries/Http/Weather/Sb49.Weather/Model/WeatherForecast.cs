using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sb49.Weather.Core;
using Sb49.Weather.Model.Core;

namespace Sb49.Weather.Model
{
    public class WeatherForecast : IDisposable
    {
        private Lazy<DateTime?> _maxPublishedDate;

        public WeatherForecast(int providerId, double? latitude, double? longitude, Units units, string languageCode, string link, bool hasIcons)
        {
            ProviderId = providerId;
            Latitude = latitude;
            Longitude = longitude;
            Units = units;
            LanguageCode = languageCode;
            Link = link;
            HasIcons = hasIcons;
        }

        ~WeatherForecast()
        {
            OnDispose();
        }

        public static string[] NonSerializedProperties => new[] {nameof(MaxPublishedDate)};

        public int ProviderId { get;}
        public double? Latitude { get; }
        public double? Longitude { get; }
        public Units Units { get; }
        public string Link { get; }
        public string LanguageCode { get; }
        public bool HasIcons { get; }
        public string AddressHashCode { get; set; }

        /// <summary>
        /// The Utc last update date.
        /// </summary>
        public DateTime? UpdatedDate { get; set; }

        public DateTime? MaxPublishedDate
        {
            get
            {
                if (_maxPublishedDate == null)
                {
                    _maxPublishedDate = new Lazy<DateTime?>(() =>
                    {
                        var task = Task.Run(async () =>
                        {
                            var maxDate = await GetMaxPublishedDateAsync().ConfigureAwait(false);
                            return maxDate;
                        });

                        return task.Result;
                    });
                }
                return _maxPublishedDate?.Value;
            }
        }

        private WeatherDataPoint _currently;

        public WeatherDataPoint Currently
        {
            get => _currently;
            set
            {
                if(_currently == value)
                    return;
                _currently = value;
                ClearMaxPublishedDate();
            }
        }

        private WeatherDataPoint[] _daily;

        public WeatherDataPoint[] Daily
        {
            get => _daily;
            set
            {
                if(_daily == value)
                    return;
                _daily = value;
                ClearMaxPublishedDate();
            }
        }

        private WeatherDataPoint[] _hourly;

        public WeatherDataPoint[] Hourly
        {
            get => _hourly;
            set
            {
                if (_hourly == value)
                    return;
                _hourly = value;
                ClearMaxPublishedDate();
            }
        }

        public WeatherDataPointDaily[] GetLocalDaily(DateTime localDateTime)
        {
            if (Daily == null || Daily.Length == 0)
                return null;

            WeatherDataPointBase.ValidateDate(localDateTime, DateTimeKind.Local, nameof(localDateTime));

            var date = localDateTime.Date;

            var dailyPoints = Daily.Where(d => d?.Date != null && d.Date.Value.ToLocalTime().Date >= date).ToArray();
            if (dailyPoints.Length == 0)
                return null;

            var result = dailyPoints.OrderBy(p => p.Date).Select(d => new WeatherDataPointDaily(d)).ToArray();

            if (result.Length == 0)
                return null;
            if (Hourly == null || Hourly.Length == 0)
                return result;

            foreach (var daily in result)
            {
                if(daily?.Date == null)
                    continue;

                var dailyLocalDate = daily.Date.Value.ToLocalTime().Date;
                var hourly = Hourly.Where(h => h?.Date != null && h.Date.Value.ToLocalTime().Date == dailyLocalDate)
                    .ToArray();

                if (hourly.Length == 0)
                    continue;

                var dailyDate = daily.Date.Value.Date;
                daily.Hourly = hourly.OrderBy(p => p.Date)
                    .Select(h => new WeatherDataPointHourly(daily, h)
                    {
                        TimeOffsetSec = h.Date.HasValue ? (int) (h.Date.Value - dailyDate).TotalSeconds : 0
                    }).ToArray();
            }

            return result;
        }

        public  Task<WeatherDataPointDaily[]> GetLocalDailyAsync(DateTime localDateTime)
        {
            return Task.Run(() => GetLocalDaily(localDateTime));
        }

        public IWeatherDataPoint FindActualCurrently(DateTime dateTime)
        {
            var utcDateTime = dateTime.Kind == DateTimeKind.Utc 
                ? dateTime 
                : dateTime.ToUniversalTime();

            if (!MaxPublishedDate.HasValue || MaxPublishedDate < utcDateTime)
                return null;

            var forecast = GetWeatherDataPoints(true);
            if (forecast.Count <= 1)
                return null;

            var first = forecast.LastOrDefault(p => p?.Date != null && p.Date <= utcDateTime);
            if (first == null)
                return null;

            var last = forecast.FirstOrDefault(p => p?.Date != null && p.Date > utcDateTime);
            if (last == null)
                return first;

            if (first.Date.HasValue && last.Date.HasValue)
            {
                var deltaFirst = (utcDateTime - first.Date.Value).TotalSeconds;
                var deltaLast = (last.Date.Value - utcDateTime).TotalSeconds;
                var currently = deltaFirst <= deltaLast ? first : last;
                return currently;
            }

            return null;
        }

        public Task<IWeatherDataPoint> FindActualCurrentlyAsync(DateTime dateTime)
        {
            return Task.Run(() => FindActualCurrently(dateTime));
        }

        public DateTime? GetMaxPublishedDate()
        {
            var result = new List<DateTime>();

            if (Currently?.Date != null)
                result.Add(Currently.Date.Value);

            var maxDate = GetMaxPublishedDate(Daily);
            if (maxDate.HasValue)
                result.Add(maxDate.Value);

            maxDate = GetMaxPublishedDate(Hourly);
            if (maxDate.HasValue)
                result.Add(maxDate.Value);

            return result.Count > 0 ? result.Max() : (DateTime?) null;
        }

        public Task<DateTime?> GetMaxPublishedDateAsync()
        {
            return Task.Run(() => GetMaxPublishedDate());
        }

        public DateTime? GetMaxPublishedDate(WeatherDataPoint[] dataPoints)
        {
            if (dataPoints == null || dataPoints.Length == 0)
                return null;

            var maxDate = dataPoints.Where(p => p?.Date != null).Max(p => p.Date.Value);
            return maxDate;
        }

        public IList<IWeatherDataPoint> GetWeatherDataPoints(bool smartIncludeDaily)
        {
            var result = new List<IWeatherDataPoint>();

            if (Currently != null)
                result.Add(Currently);

            if (!smartIncludeDaily && Daily != null && Daily.Length > 0)
                result.AddRange(Daily);

            if (Hourly != null && Hourly.Length > 0)
                result.AddRange(Hourly);
            else if (smartIncludeDaily && Daily != null && Daily.Length > 0)
                result.AddRange(Daily);

            result = result.Where(p => p?.Date != null).ToList();

            if (result.Count <= 1)
                return result;

            result = result.OrderBy(p => p.Date).ToList();
            return result;
        }

        public void Refresh()
        {
            ClearMaxPublishedDate();
        }

        private void ClearMaxPublishedDate()
        {
            _maxPublishedDate = null;
        }

        #region . IDisposable .

        private void OnDispose()
        {
            ClearMaxPublishedDate();
            Currently = null;
            Daily = null;
            Hourly = null;
        }

        public void Dispose()
        {
            OnDispose();
            GC.SuppressFinalize(this);
        }

        #endregion . IDisposable .
    }
}