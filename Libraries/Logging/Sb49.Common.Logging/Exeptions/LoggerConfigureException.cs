using System;

namespace Sb49.Common.Logging.Exeptions
{
    public class LoggerConfigureException : Exception
    {
        public LoggerConfigureException()
        {
        }

        public LoggerConfigureException(string message) : base(message)
        {
        }

        public LoggerConfigureException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}