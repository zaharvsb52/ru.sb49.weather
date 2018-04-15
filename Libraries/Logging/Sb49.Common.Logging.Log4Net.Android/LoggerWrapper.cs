using System;
using log4net;

namespace Sb49.Common.Logging.Log4Net.Droid
{
    internal sealed class LoggerWrapper : ILog
    {
        private readonly log4net.ILog _logger;
        private readonly Type _type;

        public LoggerWrapper(log4net.ILog logger, Type type)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _type = type ?? throw new ArgumentNullException(nameof(type));
        }

        ~LoggerWrapper()
        {
            OnDispose();
        }

        #region Info
        public void Info(object message)
        {
            _logger.Info(message);
        }

        public void InfoTag(string tag, object message)
        {
            WriteTagParameter(tag);
            Info(message);
        }

        public void InfoFormat(string format, params object[] args)
        {
            _logger.InfoFormat(format, args);
        }

        public void InfoFormatTag(string tag, string format, params object[] args)
        {
            WriteTagParameter(tag);
            InfoFormat(format, args);
        }
        #endregion Info

        #region Warn
        public void Warn(object message)
        {
            _logger.Warn(message);
        }

        public void WarnTag(string tag, object message)
        {
            WriteTagParameter(tag);
            Warn(message);
        }

        public void WarnFormat(string format, params object[] args)
        {
            _logger.WarnFormat(format, args);
        }

        public void WarnFormatTag(string tag, string format, params object[] args)
        {
            WriteTagParameter(tag);
            WarnFormat(format, args);
        }
        #endregion Warn

        #region Debug
        public void Debug(object message)
        {
            _logger.Debug(message);
        }

        public void DebugTag(string tag, object message)
        {
            WriteTagParameter(tag);
        }

        public void DebugFormat(string format, params object[] args)
        {
            _logger.DebugFormat(format, args);
        }

        public void DebugFormatTag(string tag, string format, params object[] args)
        {
            WriteTagParameter(tag);
            DebugFormat(format, args);
        }
        #endregion Debug

        #region Error
        public void Error(object message)
        {
            _logger.Error(message);
        }

        public void ErrorTag(string tag, object message)
        {
            WriteTagParameter(tag);
            Error(message);
        }

        public void Error(object message, Exception ex)
        {
            _logger.Error(message, ex);
        }

        public void ErrorTag(string tag, object message, Exception ex)
        {
            WriteTagParameter(tag);
            Error(message, ex);
        }

        public void ErrorFormat(string format, params object[] args)
        {
            _logger.ErrorFormat(format, args);
        }

        public void ErrorFormatTag(string tag, string format, params object[] args)
        {
            WriteTagParameter(tag);
            ErrorFormat(format, args);
        }
        #endregion Error

        private void WriteTagParameter(string tag)
        {
            if(string.IsNullOrEmpty(tag))
                return;

            var key = Log4NetLoggerFactoryAdapter.GetTagParameterKey(_type.FullName);
            ThreadContext.Properties[key] = tag;
        }

        #region . IDisposable .
        private void OnDispose()
        {
        }

        public void Dispose()
        {
            OnDispose();
            GC.SuppressFinalize(this);
        }
        #endregion . IDisposable .
    }
}