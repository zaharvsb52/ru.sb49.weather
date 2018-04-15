using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Sb49.Http.Core;
using Sb49.Provider.YahooWeather.Model;
using Sb49.Rfc822;
using Sb49.Security.Core;
using Sb49.Weather.Code;
using Sb49.Weather.Core;
using Sb49.Weather.Impl;
using Sb49.Weather.Model;

namespace Sb49.Provider.YahooWeather
{
    /// <summary>
    /// Yahoo! weather provider service.
    /// </summary>
    public class WeatherProviderService : WeatherProviderServiceBase
    {
        private const string IconFormat = "https://s.yimg.com/zz/combo?a/i/us/we/52/{0}.gif";

        private WeatherProviderUnits _weatherProviderUnits;

        public WeatherProviderService(int providerId, RequestCounterBase requestCounter)
            : base(providerId, requestCounter)
        {
        }

        public WeatherProviderService(ISb49SecureString apiKey, int providerId, RequestCounterBase requestCounter)
            : base(apiKey, providerId, requestCounter)
        {
        }

        public override Uri BaseUri => new Uri("https://query.yahooapis.com/v1/public/");

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
                            // gleb PressureUnit = PressureUnit.Psi,
                            PressureUnit = PressureUnit.MilliBars,
                            // gleb VisibilityUnit = DistanceUnit.Miles,
                            VisibilityUnit = DistanceUnit.Kilometer,
                            WindSpeedUnit = SpeedUnit.MilesPerHour
                        };
                    default:
                        return new Units
                        {
                            TemperatureUnit = TemperatureUnit.Celsius,
                            PressureUnit = PressureUnit.MilliBars,
                            VisibilityUnit = DistanceUnit.Kilometer,
                            WindSpeedUnit = SpeedUnit.KmPerHour
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
                RequestCounter.Count = 0;

            return true;
        }

        protected override string BuildRequestUri(double latitude, double longitude)
        {
            var urlparams = string.Format(FormatProvider, "text='({0},{1})'", latitude, longitude);
            var result = BuildRequestUrl(urlparams);
            return result;
        }

        protected override string BuildRequestUri(string address)
        {
            var urlparams = string.Format("text='{0}'", address);
            var result = BuildRequestUrl(urlparams);
            return result;
        }

        protected override string BuildRequestUri(long id)
        {
            var urlparams = string.Format("w='{0}'", id);
            var result = BuildRequestUrl(urlparams);
            return result;
        }

        private string BuildRequestUrl(string urlparams)
        {
            var unit = OptionalParameters?.MeasurementUnits;
            _weatherProviderUnits = string.IsNullOrEmpty(unit)
                ? WeatherProviderUnits.Imperial // Right presure unit
                : (WeatherProviderUnits) Enum.Parse(typeof(WeatherProviderUnits), unit, true);
            var result =
                string.Format(
                    "yql?q=select * from weather.forecast where woeid in (select woeid from geo.places(1) where {0}) and u='{1}'&format=json",
                    urlparams, _weatherProviderUnits.ToValue());
            return result;
        }

        protected override WeatherForecast GetForecast(string requestString, CancellationToken token)
        {
            WeatherForecast result = null;

            for (var i = 0; i < 20; i++)
            {
                if (IfCancellationRequested(token))
                    break;

                result = base.GetForecast(requestString, token);
                if (result != null)
                    break;

                Task.Delay(2000, token).Wait(token);
            }
            return result;
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

            var forecastResponse = JsonConvert.DeserializeObject<WeatherForecastResponse>(json);
            if (forecastResponse?.Query?.Results?.Channel?.Item?.Forecast == null)
            {
                //if (forecastResponse.Query?.CreatedDate != null)
                //    throw new HttpRequestException(string.Format("Post empty forecast request. Weather provider is {0}.", ProviderId));

                return null;
            }

            var forecast = forecastResponse.Query.Results.Channel;
            var forecastLink = forecast.Link;
            if (!string.IsNullOrEmpty(forecastLink))
            {
                var index = forecastLink.LastIndexOf("http", StringComparison.OrdinalIgnoreCase); //HARDCODE:
                if (index > 0)
                    forecastLink = forecastLink.Substring(index);
            }

            if (IfCancellationRequested(token))
                return null;

            if (!Latitude.HasValue && !Longitude.HasValue &&
                forecast?.Item?.Latitude != null && forecast.Item?.Longitude != null)
            {
                Latitude = forecast.Item.Latitude;
                Longitude = forecast.Item.Longitude;
            }

            // weather forecast
            if (forecast.Item == null)
                return null;
            var weather = new WeatherForecast(providerId: ProviderId, units: ResponseUnits, latitude: Latitude,
                longitude: Longitude, languageCode: OptionalParameters?.LanguageCode, link: forecastLink, hasIcons: true);

            // Currently
            var currentlyDateOffset = ConvertRfc822ToDateTimeOffset(forecast.Item.Condition?.Date);
            if (currentlyDateOffset.HasValue)
            {
                var currentlyDateTime = currentlyDateOffset.Value.UtcDateTime;

                // sunrise, sunset
                //var currentlySunrise = GetUtcDateTime(forecast.Astronomy.Sunrise, currentlyDateOffset);
                //var currentlySunset = GetUtcDateTime(forecast.Astronomy.Sunset, currentlyDateOffset);

                var astronomy = GetSunInfo(currentlyDateTime, token);
                var temperature = forecast.Item.Condition?.Temp;
                var conditionCode = forecast.Item.Condition?.Code;
                var currently = new WeatherDataPoint
                {
                    Date = currentlyDateTime,
                    Astronomy = astronomy,
                    ApparentTemperature = forecast.Wind?.Chill,
                    Temperature = temperature,
                    Pressure = forecast.Atmosphere?.Pressure,
                    Humidity = forecast.Atmosphere?.Humidity / 100,
                    Icon = string.Format(IconFormat, conditionCode),
                    Visibility = forecast.Atmosphere?.Visibility,
                    WindDirection = forecast.Wind?.Direction,
                    WindSpeed = forecast.Wind?.Speed,
                    Condition = forecast.Item.Condition?.Text,
                    WeatherCode = ConvertToWeatherCode(conditionCode)
                };

                if (currently.WeatherCode == WeatherCodes.Error)
                    currently.WeatherUnrecognizedCode = conditionCode.HasValue ? conditionCode.ToString() : null;

                weather.Currently = currently;
            }
            else
            {
                throw new FormatException("Can't define currently forecast date.");
            }

            if (IfCancellationRequested(token))
                return null;

            // daily
            var daily = CreateItems(forecast.Item.Forecast, token);
            weather.Daily = daily;

            // error
            if (!ValidateForecast(weather))
                return null;

            // validate weather forecast properties
            var currentlyDate = weather.Currently.Date.Value.Date;
            var currentDaily = weather.Daily.FirstOrDefault(p => p.Date.HasValue && p.Date.Value.Date == currentlyDate);
            if (currentDaily != null)
            {
                if (!currentDaily.WindSpeed.HasValue)
                    currentDaily.WindSpeed = weather.Currently.WindSpeed;
                if (!currentDaily.WindDirection.HasValue)
                    currentDaily.WindDirection = weather.Currently.WindDirection;
            }

            if (IfCancellationRequested(token))
                return null;

            weather.UpdatedDate = DateTime.UtcNow;
            return weather;
        }

        private WeatherDataPoint CreateWeatherDataPoint(Forecast item, CancellationToken token)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            var itemCode = item.Code;
            var date = ConvertStringToDateTime(item.Date, DateTimeUniversalStyles);
            if (!date.HasValue)
                throw new FormatException("Can't define forecast date.");

            var astronomy = GetSunInfo(date, token);
            var result = new WeatherDataPoint
            {
                Date = date,
                Astronomy = astronomy,
                Temperature = (item.Low + item.High) / 2.0, //HARDCODE:
                MaxTemperature = item.High,
                MinTemperature = item.Low,
                Condition = item.Text,
                WeatherCode = ConvertToWeatherCode(itemCode),
                Icon = string.Format(IconFormat, itemCode) 
            };

            if (result.WeatherCode == WeatherCodes.Error)
                result.WeatherUnrecognizedCode = itemCode.HasValue ? itemCode.ToString() : null;

            return result;
        }

        private WeatherDataPoint[] CreateItems(ICollection<Forecast> items, CancellationToken token)
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

        private DateTimeOffset? ConvertRfc822ToDateTimeOffset(string source)
        {
            if (string.IsNullOrEmpty(source))
                return null;

            var result = new DateTimeRfc822(source,
                DateTimeSyntax.WithDayName | DateTimeSyntax.FourDigitYear | DateTimeSyntax.UseAmPm);
            return result.Instant;
        }

        //private DateTime? GetUtcDateTime(string time, DateTimeOffset? dateTimeOffset)
        //{
        //    if (string.IsNullOrEmpty(time) || !dateTimeOffset.HasValue)
        //        return null;

        //    var datestr = dateTimeOffset.Value.ToString(DateFormat);
        //    var date = ConvertStringToDateTime(string.Format("{0} {1}", datestr, time), DateTimeUniversalStyles);
        //    return date?.Subtract(dateTimeOffset.Value.Offset);
        //}

        #region WeatherCode

        private WeatherCodes ConvertToWeatherCode(int? code)
        {
            if (!code.HasValue)
                return WeatherCodes.Undefined;

            switch (code)
            {
                case 0:
                case 1:
                case 2:
                    return WeatherCodes.Extreme;
                case 3:
                    return WeatherCodes.Thunderstorm | WeatherCodes.HeavyRain;
                case 4:
                    return WeatherCodes.Thunderstorm | WeatherCodes.Rain;
                case 5:
                case 6:
                case 7:
                    return WeatherCodes.RainAndSnow;
                case 8:
                    return WeatherCodes.Clouds;
                case 9:
                    return WeatherCodes.Clouds;
                case 10:
                    return WeatherCodes.FreezingRain;
                case 11:
                case 12:
                    return WeatherCodes.HeavyRain;
                case 13:
                    return WeatherCodes.Snow;
                case 14:
                    return WeatherCodes.LightSnow;
                case 15:
                    return WeatherCodes.HeavySnow;
                case 16:
                    return WeatherCodes.Snow;
                case 17:
                    return WeatherCodes.Hail;
                case 18:
                    return WeatherCodes.RainAndSnow;
                case 19:
                case 20:
                case 21:
                case 22:
                    return WeatherCodes.Fog;
                case 23:
                case 24:
                    return WeatherCodes.Clouds;
                //case 25:
                case 26:
                case 27:
                case 28:
                case 29:
                case 30:
                    return WeatherCodes.Clouds;
                case 31:
                case 32:
                case 33:
                case 34:
                    return WeatherCodes.ClearSky;
                case 35:
                    return WeatherCodes.Hail | WeatherCodes.Rain;
                //case 36:
                case 37:
                case 38:
                case 39:
                    return WeatherCodes.Thunderstorm | WeatherCodes.Rain;
                case 40:
                    return WeatherCodes.HeavyRain;
                case 41:
                    return WeatherCodes.HeavySnow;
                case 42:
                    return WeatherCodes.Snow;
                case 43:
                    return WeatherCodes.HeavySnow;
                case 44:
                    return WeatherCodes.FewClouds;
                case 45:
                    return WeatherCodes.Rain | WeatherCodes.Thunderstorm;
                case 46:
                    return WeatherCodes.HeavySnow;
                case 47:
                    return WeatherCodes.Rain | WeatherCodes.Thunderstorm;

                //case 3200:
                default:
                    return WeatherCodes.Error;
            }
        }

        #endregion WeatherCode
    }
}