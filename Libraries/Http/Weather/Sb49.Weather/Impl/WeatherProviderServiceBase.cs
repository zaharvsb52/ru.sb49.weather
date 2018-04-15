using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading;
using Newtonsoft.Json;
using Sb49.Http.Core;
using Sb49.Http.Provider.Core;
using Sb49.Security.Core;
using Sb49.Twilight;
using Sb49.Weather.Core;
using Sb49.Weather.Exceptions;
using Sb49.Weather.Model;
using Sb49.Weather.Model.Core;

namespace Sb49.Weather.Impl
{
    public abstract class WeatherProviderServiceBase : ProviderServiceBase, IWeatherProviderService
    {
        private TwilightPrivider _twilightPrivider;
        private IDictionary<int, ConcurrentDictionary<string, SunInfo>> _cacheSunInfo;

        protected WeatherProviderServiceBase(int providerId, RequestCounterBase requestCounter)
        {
            ProviderId = providerId;
            RequestCounter = requestCounter;
        }

        protected WeatherProviderServiceBase(ISb49SecureString apiKey, int providerId, RequestCounterBase requestCounter)
            : this(providerId, requestCounter)
        {
            ApiKey = apiKey;
        }

        public ISb49SecureString ApiKey { get; set; }
        public int ProviderId { get; }
        public OptionalParameters OptionalParameters { get; set; }
        protected abstract Units ResponseUnits { get; }
        public double? Latitude { get; protected set; }
        public double? Longitude { get; protected set; }
        protected virtual string LanguageDefault => "en";

        public Func<IWeatherProviderService, ILocationAddress, OptionalParameters, CancellationToken?, WeatherForecast> ForecastHandler { get; set; }

        public virtual WeatherForecast GetForecast(double latitude, double longitude, OptionalParameters optionalParameters, CancellationToken? token)
        {
            Initialize(optionalParameters);
            var requestString = BuildRequestUri(latitude, longitude);
            var result = GetForecast(requestString, token);
            return result;
        }

        public virtual WeatherForecast GetForecast(string address, OptionalParameters optionalParameters, CancellationToken? token)
        {
            Initialize(optionalParameters);
            var requestString = BuildRequestUri(address);
            var result = GetForecast(requestString, token);
            return result;
        }

        public virtual WeatherForecast GetForecast(long id, OptionalParameters optionalParameters, CancellationToken? token)
        {
            Initialize(optionalParameters);
            var requestString = BuildRequestUri(id);
            var result = GetForecast(requestString, token);
            return result;
        }

        public virtual WeatherForecast GetForecast(ILocationAddress location, OptionalParameters optionalParameters, CancellationToken? token)
        {
            Initialize(optionalParameters);
            return ForecastHandler?.Invoke(this, location, optionalParameters, token);
        }

        protected virtual void Initialize(OptionalParameters optionalParameters)
        {
            if (optionalParameters != null)
                OptionalParameters = optionalParameters;
        }

        protected abstract string BuildRequestUri(double latitude, double longitude);
        protected abstract string BuildRequestUri(string address);
        protected abstract string BuildRequestUri(long id);

        protected override object OnProcessResponse(HttpResponseHeaders responseHeaders, string json, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(json) || json == string.Empty)
                return null;
            return json;
        }

        protected abstract WeatherForecast OnProcessResponse(object responseResult, CancellationToken token);

        private WeatherForecast GetForecast(string requestString, CancellationToken? token)
        {
            if(!token.HasValue)
                token = CancellationToken.None;
            return GetForecast(requestString, token.Value);
        }

        protected virtual WeatherForecast GetForecast(string requestString, CancellationToken token)
        {
            var responseResult = GetStringAsync(requestString, token);
            var result = OnProcessResponse(responseResult, token);
            return result;
        }

        protected override bool OnValidateRequest(CancellationToken token)
        {
            ValidateApiKey();
            return ValidateRequestCount();
        }

        protected override void OnIncrementRequestCount()
        {
            RequestCounter++;
            RequestCounter.UpdatedDate = DateTime.UtcNow;
            RequestCounter?.Save();
        }

        protected override bool IfCancellationRequested(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return false;
        }

        protected virtual void ValidateApiKey()
        {
            if (ApiKey == null || !ApiKey.Validate())
                throw new WeatherApiKeyException();
        }

        protected abstract bool ValidateRequestCount();

        protected bool ValidateForecast(WeatherForecast weather)
        {
            if(weather == null)
                throw new Exception("Weather forecast is null.");

            if (weather.Currently == null)
                throw new Exception("Weather currently forecast is null.");

            if (!weather.Currently.Date.HasValue)
                throw new Exception("Weather currently forecast date is null.");

            if (weather.Daily == null || weather.Daily.Length == 0)
                throw new Exception("Weather daily forecast is unspecified.");

            return true;
        }

        /// <summary>
        /// Get sunrise, sunset.
        /// </summary>
        /// <returns></returns>
        protected IAstronomy GetSunInfo(DateTime? utcDate, CancellationToken token)
        {
            if (IfCancellationRequested(token) || !utcDate.HasValue || !Latitude.HasValue || !Longitude.HasValue)
                return null;

            var sunInfo = GetSunInfo(utcDate.Value, token);
            if (sunInfo?.Validate() != true)
                return null;

            var result = new Astronomy
            {
                Sunrise = sunInfo.Sunrise,
                Sunset = sunInfo.Sunset
            };

            if (utcDate < sunInfo.Sunrise)
            {
                var sunInfoPrevious = GetSunInfo(utcDate.Value.AddDays(-1), token);
                if (sunInfoPrevious?.Validate() == true)
                {
                    result.PreviousInfo = new AstronomyInfo
                    {
                        Sunrise = sunInfoPrevious.Sunrise,
                        Sunset = sunInfoPrevious.Sunset
                    };
                }
            }
            else if(utcDate > sunInfo.Sunset)
            {
                var sunInfoNext = GetSunInfo(utcDate.Value.AddDays(1), token);
                if (sunInfoNext?.Validate() == true)
                {
                    result.PreviousInfo = new AstronomyInfo
                    {
                        Sunrise = sunInfoNext.Sunrise,
                        Sunset = sunInfoNext.Sunset
                    };
                }
            }

            return result;
        }

        private SunInfo GetSunInfo(DateTime utcDate, CancellationToken token)
        {
            if (IfCancellationRequested(token) || !Latitude.HasValue || !Longitude.HasValue)
                return null;

            var latitude = Latitude.Value;
            var longitude = Longitude.Value;

            if (_cacheSunInfo == null)
                _cacheSunInfo = new ConcurrentDictionary<int, ConcurrentDictionary<string, SunInfo>>();

            var keyByDate = GetSunInfoCacheKey(utcDate);
            var key = GetSunInfoCacheKey();
            if (_cacheSunInfo.ContainsKey(keyByDate))
            {
                var cache = _cacheSunInfo[keyByDate] ?? new ConcurrentDictionary<string, SunInfo>();
                if (!cache.ContainsKey(key))
                {
                    var sunInfo = GetSunInfo(utcDate, latitude, longitude, token);
                    if (sunInfo?.Validate() != true)
                        return null;

                    cache[key] = sunInfo;
                    _cacheSunInfo[keyByDate] = cache;
                    return sunInfo;
                }

                return cache[key];
            }
            else
            {
                var sunInfo = GetSunInfo(utcDate, latitude, longitude, token);
                if (sunInfo?.Validate() != true)
                    return null;

                var cache = new ConcurrentDictionary<string, SunInfo> { [key] = sunInfo};
                _cacheSunInfo[keyByDate] = cache;
                return sunInfo;
            }
        }

        private SunInfo GetSunInfo(DateTime utcDate, double latitude, double longitude, CancellationToken token)
        {
            if (IfCancellationRequested(token))
                return null;

            if (_twilightPrivider == null)
                _twilightPrivider = new TwilightPrivider();

            return _twilightPrivider.Get(latitude, longitude, utcDate, token);
        }

        public string GetSunInfoCache(int? maxCount)
        {
            if (_cacheSunInfo == null || _cacheSunInfo.Count == 0)
                return null;

            if (maxCount <= 0)
            {
                var maxCountName = nameof(maxCount);
                throw new ArgumentOutOfRangeException(maxCountName,
                    string.Format("{0} must be greater than 0", maxCountName));
            }

            if (maxCount.HasValue)
            {
                var cache = new Dictionary<int, ConcurrentDictionary<string, SunInfo>>(_cacheSunInfo);
                if (cache.Count > maxCount)
                {
                    while (cache.Count > maxCount)
                    {
                        var key = cache.Min(p => p.Key);
                        cache.Remove(key);
                    }
                }

                return JsonConvert.SerializeObject(cache);
            }

            return JsonConvert.SerializeObject(_cacheSunInfo);
        }

        public void SetSunInfoCache(string json)
        {
            if (string.IsNullOrEmpty(json) || string.IsNullOrWhiteSpace(json))
                return;

            var cache = JsonConvert.DeserializeObject<Dictionary<int, Dictionary<string, SunInfo>>>(json);
            if (cache != null)
            {
                ClearSunInfoCache();
                _cacheSunInfo = new ConcurrentDictionary<int, ConcurrentDictionary<string, SunInfo>>();
                foreach (var c in cache.Where(p => p.Value != null))
                {
                    _cacheSunInfo[c.Key] = new ConcurrentDictionary<string, SunInfo>(c.Value);
                }
            }
        }

        private int GetSunInfoCacheKey(DateTime date)
        {
            return int.Parse(string.Format(CultureInfo.InvariantCulture, "{0:yyyyMMdd}", date));
        }

        private string GetSunInfoCacheKey()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:N4}:{1:N4}", Latitude, Longitude);
        }

        protected void ClearSunInfoCache()
        {
            _cacheSunInfo?.Clear();
            _cacheSunInfo = null;
        }

        protected override void OnDispose(bool disposing)
        {
            _twilightPrivider = null;
            ClearSunInfoCache();

            base.OnDispose(disposing);
        }
    }
}