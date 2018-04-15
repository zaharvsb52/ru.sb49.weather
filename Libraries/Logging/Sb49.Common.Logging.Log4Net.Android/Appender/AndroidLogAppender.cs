using System.Linq;
using Android.Util;
using log4net;
using log4net.Appender;
using log4net.Core;

namespace Sb49.Common.Logging.Log4Net.Droid.Appender
{
    public class AndroidLogAppender : AppenderSkeleton
    {
        private string _tagPropertyKey;

        protected override void Append(LoggingEvent loggingEvent)
        {
            var message = RenderLoggingEvent(loggingEvent);
            if (string.IsNullOrEmpty(message))
                return;

            var tag = GetTag(loggingEvent);

            if (loggingEvent.Level.Name == Level.Info.Name)
            {
                Log.Info(tag, message);
            }
            else if (loggingEvent.Level.Name == Level.Warn.Name)
            {
                Log.Warn(tag, message);
            }
            else if (loggingEvent.Level.Name == Level.Debug.Name)
            {
                Log.Debug(tag, message);
            }
            else if (loggingEvent.Level.Name == Level.Error.Name)
            {
                Log.Error(tag, message);
            }
            else
            {
                Log.Verbose(tag, message);
            }
        }

        protected override void Append(LoggingEvent[] loggingEvents)
        {
            foreach (var loggingEvent in loggingEvents)
            {
                Append(loggingEvent);
            }
        }

        private string GetTag(LoggingEvent loggingEvent)
        {
            if (string.IsNullOrEmpty(_tagPropertyKey))
                _tagPropertyKey = Log4NetLoggerFactoryAdapter.GetTagParameterKey(loggingEvent.LoggerName);

            string tag = null;
            if (ThreadContext.Properties.GetKeys()?.Any(p => p == _tagPropertyKey) == true)
            {
                tag = ThreadContext.Properties[_tagPropertyKey]?.ToString();
            }
            else if (loggingEvent.Repository.Properties.Contains(_tagPropertyKey))
            {
                tag = loggingEvent.Repository.Properties[_tagPropertyKey]?.ToString();
            }

            if (string.IsNullOrEmpty(tag))
                tag = loggingEvent.Repository.Name;

            return tag;
        }
    }
}