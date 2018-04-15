using System;
using Sb49.Http.Provider.Core;

namespace Sb49.Weather.Droid.Model
{
    public interface IWeatherLocationAddress : ILocationAddress, IDisposable
    {
        string GetMd5Code();
    }
}