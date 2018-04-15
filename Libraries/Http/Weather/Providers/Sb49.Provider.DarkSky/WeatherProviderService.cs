using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Sb49.Http.Core;
using Sb49.Http.Provider.Exeptions;
using Sb49.Provider.DarkSky.Model;
using Sb49.Security.Core;
using Sb49.Weather.Code;
using Sb49.Weather.Core;
using Sb49.Weather.Impl;
using Sb49.Weather.Model;

namespace Sb49.Provider.DarkSky
{
    /// <summary>
    /// Dark Sky weather provider service.
    /// </summary>
    public class WeatherProviderService : WeatherProviderServiceBase
    {
        protected const int MaxRequestCountPerDay = 900; //1000
        private WeatherProviderUnits _weatherProviderUnits;

        public WeatherProviderService(int providerId, RequestCounterBase requestCounter)
            : base(providerId, requestCounter)
        {
        }

        public WeatherProviderService(ISb49SecureString apiKey, int providerId, RequestCounterBase requestCounter)
            : base(apiKey, providerId, requestCounter)
        {
        }

        public override Uri BaseUri => new Uri("https://api.darksky.net/");

        protected override string LanguageDefault => Language.English.ToValue();

        protected override Units ResponseUnits
        {
            get
            {
                switch (_weatherProviderUnits)
                {
                    case WeatherProviderUnits.Us:
                        return new Units
                        {
                            TemperatureUnit = TemperatureUnit.Fahrenheit,
                            PressureUnit = PressureUnit.MilliBars,
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

        protected override bool ValidateRequestCount()
        {
            if (RequestCounter == null)
                throw new ArgumentNullException(nameof(RequestCounter));

            var updatedDate = RequestCounter.UpdatedDate;
            if (!updatedDate.HasValue)
                return true;

            var date = updatedDate.Value.Date;
            var today = DateTime.UtcNow.Date;
            if ((today - date).TotalDays >= 1)
            {
                RequestCounter.Count = 0;
                return true;
            }

            var result = RequestCounter.Count < MaxRequestCountPerDay;
            if (!result)
                throw new ExceededServerRequestCountsException();

            return true;
        }

        public override WeatherForecast GetForecast(string address, OptionalParameters optionalParameters,
            CancellationToken? token)
        {
            throw new NotImplementedException();
        }

        public override WeatherForecast GetForecast(long id, OptionalParameters optionalParameters,
            CancellationToken? token)
        {
            throw new NotImplementedException();
        }

        protected override string BuildRequestUri(double latitude, double longitude)
        {
            Latitude = latitude;
            Longitude = longitude;

            var queryString =
                new StringBuilder(string.Format(FormatProvider, "forecast/{0}/{1:N4},{2:N4}", ApiKey?.Decrypt(),
                    latitude,
                    longitude));

            if (OptionalParameters?.UnixTimeInSeconds != null)
                queryString.AppendFormat(",{0}", OptionalParameters.UnixTimeInSeconds);

            if (OptionalParameters != null)
                queryString.Append("?");

            if (OptionalParameters?.DataBlocksToExclude != null && OptionalParameters.DataBlocksToExclude.Count > 0)
                queryString.AppendFormat("&exclude={0}", string.Join(",", OptionalParameters.DataBlocksToExclude));

            if (OptionalParameters?.ExtendHourly != null && OptionalParameters.ExtendHourly.Value)
                queryString.Append("&extend=hourly");

            var languageCode = GetValidLanguage(OptionalParameters?.LanguageCode);
            queryString.AppendFormat("&lang={0}", languageCode);

            var unit = OptionalParameters?.MeasurementUnits;
            _weatherProviderUnits = string.IsNullOrEmpty(unit)
                ? WeatherProviderUnits.Si
                : (WeatherProviderUnits) Enum.Parse(typeof(WeatherProviderUnits), unit, true);
            queryString.AppendFormat("&units={0}", _weatherProviderUnits.ToValue());

            return queryString.ToString();
        }

        protected override string BuildRequestUri(string address)
        {
            throw new NotImplementedException();
        }

        protected override string BuildRequestUri(long id)
        {
            throw new NotImplementedException();
        }

        private object GetValidLanguage(string languageCode)
        {
            if (string.IsNullOrEmpty(languageCode))
                return LanguageDefault;

            var languages = Enum.GetValues(typeof(Language)).Cast<Language>().Select(p => p.ToValue()).ToArray();
            var result =
                languages.FirstOrDefault(p => string.Equals(languageCode, p, StringComparison.OrdinalIgnoreCase));
            return result ?? LanguageDefault;
        }

        protected override object OnProcessResponse(HttpResponseHeaders responseHeaders, string json,
            CancellationToken token)
        {
            if (IfCancellationRequested(token))
                return null;

            var result = new DarkSkyResponse
            {
                Response = string.IsNullOrWhiteSpace(json) ? null : json,
                Headers = new ResponseHeaders
                {
                    CacheControl = responseHeaders.CacheControl,
                    ApiCalls =
            long.TryParse(responseHeaders.GetValues("X-Forecast-API-Calls")?.FirstOrDefault(),
                out long callsParsed)
                ? (long?)callsParsed
                : null,
                    ResponseTime = responseHeaders.GetValues("X-Response-Time")?.FirstOrDefault()
                }
            };
            return result;
        }

        protected override WeatherForecast OnProcessResponse(object responseResult, CancellationToken token)
        {
            if (IfCancellationRequested(token))
                return null;

            if (responseResult == null)
                return null;

            var response = responseResult as DarkSkyResponse;
            if (string.IsNullOrEmpty(response?.Response) || string.IsNullOrWhiteSpace(response.Response))
                return null;

            var forecast = JsonConvert.DeserializeObject<Forecast>(response.Response);
            if (forecast == null)
                return null;

            // weather forecast
            var weather = new WeatherForecast(providerId: ProviderId, latitude: Latitude, longitude: Longitude,
                units: ResponseUnits, languageCode: OptionalParameters?.LanguageCode,
                link: string.Format(FormatProvider, "https://darksky.net/forecast/{0:N4},{1:N4}/{2}",
                    Latitude, Longitude, _weatherProviderUnits.ToValue() + "12"), hasIcons: false)
            {
                Currently = CreateWeatherDataPoint(forecast.Currently, token),
                Daily = CreateItems(forecast.Daily?.Data, token),
                Hourly = CreateItems(forecast.Hourly?.Data, token)
            };

            if (IfCancellationRequested(token))
                return null;

            // error
            if (!ValidateForecast(weather))
                return null;

            if (IfCancellationRequested(token))
                return null;

            weather.UpdatedDate = DateTime.UtcNow;
            return weather;
        }

        private WeatherDataPoint CreateWeatherDataPoint(DataPoint item, CancellationToken token)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            var date = ConvertUnixTimeToUtcDateTime(item.Time);
            if (!date.HasValue)
                throw new FormatException("Can't define forecast date.");

            //var sunrise = ConvertUnixTimeToUtcDateTime(item.SunriseTime);
            //var sunset = ConvertUnixTimeToUtcDateTime(item.SunsetTime);

            var astronomy = GetSunInfo(date, token);
            var itemIcon = item.Icon;

            var result = new WeatherDataPoint
            {
                Date = date,
                Astronomy = astronomy,
                ApparentTemperature = item.ApparentTemperature,
                Temperature = item.Temperature,
                MaxTemperature = item.TemperatureMax,
                MinTemperature = item.TemperatureMin,
                Pressure = item.Pressure,
                Humidity = item.Humidity,
                Condition = item.Summary,
                Icon = itemIcon,
                WeatherCode = ConvertToWeatherCode(itemIcon),
                Visibility = item.Visibility,
                WindDirection = item.WindBearing,
                WindSpeed = item.WindSpeed,
                DewPoint = item.DewPoint
            };

            if (result.WeatherCode == WeatherCodes.Error)
                result.WeatherUnrecognizedCode = itemIcon;

            return result;
        }

        private WeatherDataPoint[] CreateItems(ICollection<DataPoint> items, CancellationToken token)
        {
            if (items == null || items.Count == 0)
                throw new ArgumentNullException(nameof(items));

            var data = new List<WeatherDataPoint>();
            foreach (var item in items)
            {
                if (IfCancellationRequested(token))
                    return null;

                var weatherdp = CreateWeatherDataPoint(item, token);
                if (weatherdp?.Date != null)
                    data.Add(weatherdp);
            }

            return data.ToArray();
        }

        #region WeatherCode

        private WeatherCodes ConvertToWeatherCode(string code)
        {
            if (string.IsNullOrEmpty(code))
                return WeatherCodes.Undefined;

            switch (code.ToLower())
            {
                case "clear-day":
                case "clear-night":
                    return WeatherCodes.ClearSky;
                case "rain":
                    return WeatherCodes.Rain;
                case "snow":
                    return WeatherCodes.Snow;
                case "sleet":
                    return WeatherCodes.RainAndSnow;
                case "fog":
                    return WeatherCodes.Fog;
                case "cloudy":
                    return WeatherCodes.Clouds;
                case "partly-cloudy-day":
                case "partly-cloudy-night":
                    return WeatherCodes.FewClouds;
                case "thunderstorm":
                    return WeatherCodes.Thunderstorm;
                case "tornado":
                    return WeatherCodes.Extreme;
                default:
                    return WeatherCodes.Error;
            }
        }

        #endregion WeatherCode
    }
}