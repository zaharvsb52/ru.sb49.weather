using System;

namespace Sb49.Common.Logging
{
    public interface ILoggerFactoryAdapter
    {
        ILog GetLogger(Type type, string tag);

        void Configure();
        void Configure(string name, string xml);
        //void Configure(string name, XmlElement xml);
        //void Configure(string name, XmlDocument xml);
    }
}