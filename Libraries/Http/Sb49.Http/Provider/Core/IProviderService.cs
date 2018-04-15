using System;
using Sb49.Http.Core;

namespace Sb49.Http.Provider.Core
{
    public interface IProviderService : IHttpClient, IDisposable
    {
        RequestCounterBase RequestCounter { get; }
    }
}