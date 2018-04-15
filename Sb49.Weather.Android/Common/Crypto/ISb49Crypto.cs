using Sb49.Security.Core;

namespace Sb49.Weather.Droid.Common.Crypto
{
    public interface ISb49Crypto : ICryptoSymmetric
    {
        ProviderType ProviderType { get; set; }
    }
}