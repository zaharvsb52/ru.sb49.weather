using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using Sb49.Http.Core;
using Sb49.Http.Provider.Exeptions;
using Sb49.Provider.OpenWeatherMap.Model;
using Sb49.Provider.OpenWeatherMap.Model.Core;
using Sb49.Security.Core;
using Sb49.Weather.Code;
using Sb49.Weather.Core;
using Sb49.Weather.Impl;
using Sb49.Weather.Model;

namespace Sb49.Provider.OpenWeatherMap
{
    /// <summary>
    /// OpenWeatherMap weather provider service.
    /// </summary>
    public class WeatherProviderService : WeatherProviderServiceBase
    {
        private const int MaxRequestCountPerMinute = 50; //60
        private const string IconFormat = "http://openweathermap.org/img/w/{0}.png";

        private WeatherProviderUnits _weatherProviderUnits;
        private WeatherDataResponseType _weatherDataType;

        public WeatherProviderService(int providerId, RequestCounterBase requestCounter)
            : base(providerId, requestCounter)
        {
        }

        public WeatherProviderService(ISb49SecureString apiKey, int providerId, RequestCounterBase requestCounter)
            : base(apiKey, providerId, requestCounter)
        {
        }

        public override Uri BaseUri => new Uri("http://api.openweathermap.org/data/2.5/");

        protected override Units ResponseUnits
        {
            get
            {
                switch (_weatherProviderUnits)
                {
                    case WeatherProviderUnits.Imperial:
                        return new Units
                        {
                            TemperatureUnit = TemperatureUnit.Fahrenheit,
                            PressureUnit = PressureUnit.HectoPa,
                            VisibilityUnit = DistanceUnit.Miles,
                            WindSpeedUnit = SpeedUnit.MilesPerHour
                        };
                    default:
                        return new Units
                        {
                            TemperatureUnit = TemperatureUnit.Celsius,
                            PressureUnit = PressureUnit.HectoPa,
                            VisibilityUnit = DistanceUnit.Kilometer,
                            WindSpeedUnit = SpeedUnit.MeterPerSec
                        };
                }
            }
        }

        protected override void Initialize(OptionalParameters optionalParameters)
        {
            base.Initialize(optionalParameters);
            _weatherDataType = WeatherDataResponseType.Forecast;
        }

        protected override bool ValidateRequestCount()
        {
            if (RequestCounter == null)
                throw new ArgumentNullException(nameof(RequestCounter));

            if (!RequestCounter.UpdatedDate.HasValue)
                return true;

            var minutes = (DateTime.UtcNow - RequestCounter.UpdatedDate.Value).TotalMinutes;
            if (minutes >= 1)
            {
                RequestCounter.Count = 0;
                return true;
            }

            if (RequestCounter.Count < MaxRequestCountPerMinute)
                return true;

            var count = RequestCounter.Count / minutes;
            var result = count < MaxRequestCountPerMinute;
            if (!result)
                throw new ExceededServerRequestCountsException();

            return true;
        }

        protected override string BuildRequestUri(double latitude, double longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
            var urlparams = string.Format(FormatProvider, "lat={0}&lon={1}", latitude, longitude);
            var result = BuildRequestUrl(urlparams);
            return result;
        }

        protected override string BuildRequestUri(string address)
        {
            var urlparams = string.Format("q={0}", address);
            var result = BuildRequestUrl(urlparams);
            return result;
        }

        protected override string BuildRequestUri(long id)
        {
            var urlparams = string.Format("id={0}", id);
            var result = BuildRequestUrl(urlparams);
            return result;
        }

        private string BuildRequestUrl(string urlparams)
        {
            var unit = OptionalParameters?.MeasurementUnits;
            _weatherProviderUnits = string.IsNullOrEmpty(unit)
                ? WeatherProviderUnits.Metric
                : (WeatherProviderUnits) Enum.Parse(typeof(WeatherProviderUnits), unit, true);
            var urlbase = string.Format("{0}?appid={1}", _weatherDataType.ToValue(), ApiKey?.Decrypt());
            var lang = GetValidLanguage(OptionalParameters?.LanguageCode);
            var result = string.Format("{0}&{1}&units={2}&lang={3}", urlbase, urlparams,
                _weatherProviderUnits.ToValue(),
                lang);
            return result;
        }

        private string GetValidLanguage(string languageCode)
        {
            if (string.IsNullOrEmpty(languageCode))
                return LanguageDefault;

            var languages = new[]
            {
                LanguageDefault, "ru", "it", "es", "sp", "uk", "ua", "de", "pt", "ro", "pl", "fi", "nl", "fr",
                "bg", "sv", "se", "zh_tw", "zh", "zh_cn",
                "tr", "hr", "ca"
            };
            var result =
                languages.FirstOrDefault(p => string.Equals(languageCode, p, StringComparison.OrdinalIgnoreCase));
            return result ?? LanguageDefault;
        }

        protected override WeatherForecast OnProcessResponse(object responseResult, CancellationToken token)
        {
            if (IfCancellationRequested(token))
                return null;

            if (responseResult == null)
                return null;

            var json = responseResult as string;
            if (string.IsNullOrEmpty(json) || string.IsNullOrWhiteSpace(json))
                return null;

            var forecast = JsonConvert.DeserializeObject<WeatherForecastResponse>(json);
            if (forecast?.City == null || forecast.Items == null)
                return null;

            if (!Latitude.HasValue && !Longitude.HasValue &&
                forecast.City?.Coord?.Latitude != null && forecast.City?.Coord?.Longitude != null)
            {
                Latitude = forecast.City.Coord.Latitude;
                Longitude = forecast.City.Coord.Longitude;
            }

            var currentlyResponse = GetCurrently(token);
            if (currentlyResponse == null)
                return null;

            // weather forecast
            var weather = new WeatherForecast(providerId: ProviderId, latitude: Latitude, longitude: Longitude,
                units: ResponseUnits, languageCode: OptionalParameters?.LanguageCode,
                link: string.Format("http://openweathermap.org/city/{0}", forecast.City.Id), hasIcons: true)
            {
                Currently = CreateWeatherDataPoint(currentlyResponse, token),
                Hourly = CreateItems(forecast.Items.Cast<WeatherResponseBase>().ToArray(), token)
            };

            if (IfCancellationRequested(token))
                return null;

            // daily
            if (weather.Hourly != null && weather.Hourly.Length > 0)
            {
                var daily = new List<WeatherDataPoint>();

                // ReSharper disable once PossibleInvalidOperationException
                var hourlyGroup =
                    weather.Hourly.Where(p => p.Date.HasValue).GroupBy(key => key.Date.Value.Date).ToArray();

                foreach (var group in hourlyGroup)
                {
                    if (IfCancellationRequested(token))
                        return null;

                    var date = group.Key;
                    var astronomy = group.FirstOrDefault()?.Astronomy;

                    var point = new WeatherDataPoint
                    {
                        Date = date,
                        Astronomy = astronomy,
                        MinTemperature = group.Min(p => p.MinTemperature),
                        MaxTemperature = group.Max(p => p.MaxTemperature)
                    };
                    daily.Add(point);
                }

                if (daily.Count > 0)
                    weather.Daily = daily.ToArray();
            }

            // error
            if (!ValidateForecast(weather))
                return null;

            if (IfCancellationRequested(token))
                return null;

            weather.UpdatedDate = DateTime.UtcNow;
            return weather;
        }

        private WeatherResponse GetCurrently(CancellationToken token)
        {
            if (!Latitude.HasValue || !Longitude.HasValue)
                return null;

            var weatherDataType = _weatherDataType;
            try
            {
                _weatherDataType = WeatherDataResponseType.Current;

                var requestString = BuildRequestUri(Latitude.Value, Longitude.Value);
                var responseResult = GetStringAsync(requestString, token);
                if (responseResult == null)
                    return null;

                var json = responseResult as string;
                if (string.IsNullOrEmpty(json) || string.IsNullOrWhiteSpace(json))
                    return null;

                return JsonConvert.DeserializeObject<WeatherResponse>(json);
            }
            finally
            {
                _weatherDataType = weatherDataType;
            }
        }

        private WeatherDataPoint CreateWeatherDataPoint(WeatherResponseBase item, CancellationToken token)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            DateTime? date = null;

            switch (item)
            {
                case WeatherResponse weatherResponse:
                    date = ConvertUnixTimeToUtcDateTime(weatherResponse.DateUnixTime);
                    //sunrise = ConvertUnixTimeToUtcDateTime(weatherResponse.WeatherSys?.SunriseUnixTime);
                    //sunset = ConvertUnixTimeToUtcDateTime(weatherResponse.WeatherSys?.SunsetUnixTime);
                    break;
                case WeatherForecastItem weatherForecastItem:
                    date = ConvertStringToDateTime(weatherForecastItem.Date, DateTimeUniversalStyles);
                    break;
            }

            if (!date.HasValue)
                throw new FormatException("Can't define forecast date.");

            var astronomy = GetSunInfo(date, token);
            var weatherInfo = item.WeatherInfos?.FirstOrDefault();
            var conditionId = weatherInfo?.ConditionId;

            var result = new WeatherDataPoint
            {
                Date = date,
                Astronomy = astronomy,
                Temperature = item.WeatherMain?.Temperature,
                MaxTemperature = item.WeatherMain?.MaxTemperature,
                MinTemperature = item.WeatherMain?.MinTemperature,
                Pressure = item.WeatherMain?.Pressure,
                Humidity = item.WeatherMain?.Humidity / 100,
                Condition = weatherInfo?.Description,
                Icon = string.Format(IconFormat, weatherInfo?.Icon),
                WindDirection = item.Wind?.DirectionDegrees,
                WindSpeed = item.Wind?.Speed,
                WeatherCode = ConvertToWeatherCode(conditionId)
            };

            if (result.WeatherCode == WeatherCodes.Error)
                result.WeatherUnrecognizedCode = conditionId.HasValue ? conditionId.ToString() : null;

            return result;
        }

        private WeatherDataPoint[] CreateItems(ICollection<WeatherResponseBase> items, CancellationToken token)
        {
            if (items == null || items.Count == 0)
                return null;

            var result = new List<WeatherDataPoint>();

            foreach (var item in items)
            {
                if (IfCancellationRequested(token))
                    return null;
                var weatherdp = CreateWeatherDataPoint(item, token);
                if (weatherdp?.Date != null)
                    result.Add(weatherdp);
            }

            return result.ToArray();
        }

        #region WeatherCode

        private WeatherCodes ConvertToWeatherCode(int? code)
        {
            if (!code.HasValue)
                return WeatherCodes.Undefined;

            if (code >= 200 && code <= 299)
            {
                return WeatherCodes.Thunderstorm | WeatherCodes.Clouds | WeatherCodes.Rain;
            }

            if (code == 300)
            {
                return WeatherCodes.Light | WeatherCodes.Rain | WeatherCodes.Clouds;
            }
            if (code == 301)
            {
                return WeatherCodes.Rain | WeatherCodes.Clouds;
            }
            if (code >= 302 && code <= 399)
            {
                return WeatherCodes.HeavyRain | WeatherCodes.Clouds;
            }

            if (code == 500)
            {
                return WeatherCodes.LightRain | WeatherCodes.ClearSky;
            }
            if (code == 501)
            {
                return WeatherCodes.Rain | WeatherCodes.ClearSky;
            }
            if (code >= 502 && code <= 504)
            {
                return WeatherCodes.HeavyRain | WeatherCodes.ClearSky;
            }
            if (code == 511)
            {
                return WeatherCodes.FreezingRain | WeatherCodes.Clouds;
            }
            if (code == 520)
            {
                return WeatherCodes.LightRain | WeatherCodes.Clouds;
            }
            if (code == 521)
            {
                return WeatherCodes.Rain | WeatherCodes.Clouds;
            }
            if (code >= 522 && code <= 599)
            {
                return WeatherCodes.HeavyRain | WeatherCodes.Clouds;
            }

            if (code == 600)
            {
                return WeatherCodes.LightSnow | WeatherCodes.ClearSky;
            }
            if (code == 601)
            {
                return WeatherCodes.Snow | WeatherCodes.ClearSky;
            }
            if (code == 602)
            {
                return WeatherCodes.HeavySnow | WeatherCodes.ClearSky;
            }
            if (code == 611 || code == 612)
            {
                return WeatherCodes.Snow | WeatherCodes.Clouds;
            }
            if (code == 615)
            {
                return WeatherCodes.RainAndSnow | WeatherCodes.Clouds;
            }
            if (code == 616)
            {
                return WeatherCodes.HeavyRainAndSnow | WeatherCodes.Clouds;
            }
            if (code == 620)
            {
                return WeatherCodes.LightSnow | WeatherCodes.Clouds;
            }
            if (code == 621)
            {
                return WeatherCodes.Snow | WeatherCodes.Clouds;
            }
            if (code >= 622 && code <= 699)
            {
                return WeatherCodes.HeavySnow | WeatherCodes.Clouds;
            }

            if (code >= 700 && code <= 799)
            {
                return WeatherCodes.Fog;
            }

            if (code == 800)
            {
                return WeatherCodes.ClearSky;
            }
            if (code == 801)
            {
                return WeatherCodes.FewClouds;
            }
            if (code >= 802 && code <= 899)
            {
                return WeatherCodes.Clouds;
            }

            if (code == 900 || code == 901 || code == 902)
            {
                return WeatherCodes.Extreme;
            }
            if (code == 906)
            {
                return WeatherCodes.Hail;
            }

            return WeatherCodes.Error;
        }

        #endregion WeatherCode
    }
}