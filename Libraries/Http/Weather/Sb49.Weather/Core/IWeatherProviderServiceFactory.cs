namespace Sb49.Weather.Core
{
    public interface IWeatherProviderServiceFactory
    {
        bool Exists(int providerId);
        IWeatherProviderService GetProvider(int id);
    }
}