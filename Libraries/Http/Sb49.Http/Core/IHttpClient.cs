using System;

namespace Sb49.Http.Core
{
    public interface IHttpClient
    {
        Uri BaseUri { get; }
        TimeSpan? Timeout { get; set; }
    }
}