using System.Collections.Generic;
using System.Threading;
using Sb49.Http.Provider.Core;
using Sb49.Security.Core;

namespace Sb49.Geocoder.Core
{
    public interface IGeocoder
    {
        ISb49SecureString ApiKey { get; set; }
        string LanguageCode { get; set; }

        IList<ILocationAddress> GetFromLocation(double latitude, double longitude, int maxResults, CancellationToken? token);
        IList<ILocationAddress> GetFromLocationName(string locationName, int maxResults, CancellationToken? token);
    }
}