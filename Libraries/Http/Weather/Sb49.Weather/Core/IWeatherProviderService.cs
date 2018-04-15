using System.Threading;
using Sb49.Http.Provider.Core;
using Sb49.Security.Core;
using Sb49.Weather.Model;

namespace Sb49.Weather.Core
{
    public interface IWeatherProviderService : IProviderService
    {
        int ProviderId { get; }
        ISb49SecureString ApiKey { get; set; }
        OptionalParameters OptionalParameters { get; set; }
        double? Latitude { get; }
        double? Longitude { get; }

        WeatherForecast GetForecast(double latitude, double longitude, OptionalParameters optionalParameters = null, CancellationToken? token = null);
        WeatherForecast GetForecast(string address, OptionalParameters optionalParameters = null, CancellationToken? token = null);
        WeatherForecast GetForecast(long id, OptionalParameters optionalParameters = null, CancellationToken? token = null);
        WeatherForecast GetForecast(ILocationAddress location, OptionalParameters optionalParameters = null, CancellationToken? token = null);

        string GetSunInfoCache(int? maxCount);
        void SetSunInfoCache(string json);
    }
}