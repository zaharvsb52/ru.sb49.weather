using Sb49.Http.Core;
using Sb49.Weather.Droid.Common;

namespace Sb49.Weather.Droid.Model
{
    public sealed class RequestCounter : RequestCounterBase
    {
        public RequestCounter(int providerId) : base(providerId)
        {
        }

        public override void Save()
        {
            AppSettings.Default.SaveRequestCounter(ProviderId, this);
        }
    }
}