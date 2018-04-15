using System;
using System.Linq;
using System.Xml;
using log4net.Config;
using Sb49.Common.Logging.Exeptions;

namespace Sb49.Common.Logging.Log4Net.Droid
{
    public class Log4NetLoggerFactoryAdapter : ILoggerFactoryAdapter
    {
        private string _name;

        public void Configure()
        {
            Configure(null, (XmlElement) null);
        }

        public void Configure(string name, string xml)
        {
            var xmlDocument = new XmlDocument();
            if (!string.IsNullOrEmpty(xml))
                xmlDocument.LoadXml(xml);
            Configure(name, xmlDocument);
        }

        public void Configure(string name, XmlElement xml)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            _name = name;
            var loggerRepository = log4net.LogManager.CreateRepository(name);
            var result = XmlConfigurator.Configure(loggerRepository, xml);

            if (result?.Count > 0)
            {
                var messages = result.OfType<object>().Select(p => p.ToString()).ToArray();
                var message = string.Join(" ", messages);
                throw new LoggerConfigureException(string.Format("Logger configure failed. {0}", message));
            }
        }

        public void Configure(string name, XmlDocument xml)
        {
            Configure(name, xml?["log4net"]);
        }

        public ILog GetLogger(Type type, string tag)
        {
            if(type == null)
                throw new ArgumentNullException(nameof(type));

            var logger = string.IsNullOrEmpty(_name)
                ? log4net.LogManager.GetLogger(type)
                : log4net.LogManager.GetLogger(_name, type);

            if (!string.IsNullOrEmpty(tag))
            {
                var repository = logger.Logger.Repository;
                repository.Properties[GetTagParameterKey(type.FullName)] = tag;
            }

            return new LoggerWrapper(logger, type);
        }

        internal static string GetTagParameterKey(string loggerName)
        {
            return string.Format("{0}_tag", loggerName);
        }
    }
}