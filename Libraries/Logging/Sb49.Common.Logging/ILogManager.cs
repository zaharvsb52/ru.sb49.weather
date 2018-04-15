using System;

namespace Sb49.Common.Logging
{
    public interface ILogManager
    {
        ILoggerFactoryAdapter Adapter { get; set; }

        ILog GetLogger<T>(string tag = null);
        ILog GetLogger(Type type, string tag = null);
        void Reset();
    }
}