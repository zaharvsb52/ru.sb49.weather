using System;

namespace Sb49.Common.Logging
{
    public interface ILog : IDisposable
    {
        void Info(object message);
        void InfoTag(string tag, object message);
        void InfoFormatTag(string tag, string format, params object[] args);
        void InfoFormat(string format, params object[] args);

        void Warn(object message);
        void WarnTag(string tag, object message);
        void WarnFormat(string format, params object[] args);
        void WarnFormatTag(string tag, string format, params object[] args);
        
        void Debug(object message);
        void DebugTag(string tag, object message);
        void DebugFormat(string format, params object[] args);
        void DebugFormatTag(string tag, string format, params object[] args);

        void Error(object message);
        void ErrorTag(string tag, object message);
        void Error(object message, Exception ex);
        void ErrorTag(string tag, object message, Exception ex);
        void ErrorFormat(string format, params object[] args);
        void ErrorFormatTag(string tag, string format, params object[] args);
    }
}