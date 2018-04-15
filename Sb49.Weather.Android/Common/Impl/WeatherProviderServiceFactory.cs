using System;
using Microsoft.Practices.Unity;
using Sb49.Weather.Core;

namespace Sb49.Weather.Droid.Common.Impl
{
    public class WeatherProviderServiceFactory : IWeatherProviderServiceFactory, IDisposable
    {
        private readonly IUnityContainer _container;

        public WeatherProviderServiceFactory(IUnityContainer container)
        {
            _container = container;
        }

        public bool Exists(int providerId)
        {
            return _container.IsRegistered<IWeatherProviderService>(providerId.ToString());
        }

        public IWeatherProviderService GetProvider(int providerId)
        {
            return Exists(providerId) 
                ? _container.Resolve<IWeatherProviderService>(providerId.ToString()) 
                : null;
        }

        #region . IDisposable .

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        #endregion . IDisposable .
    }
}