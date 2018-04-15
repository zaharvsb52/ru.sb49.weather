using System;

namespace Sb49.Http.Provider.Exeptions
{
    public class ExceededServerRequestCountsException : ProviderServiceExceptionBase
    {
        public ExceededServerRequestCountsException() : base (Properties.Resources.ExceededServerRequestCounts)
        {
        }

        public ExceededServerRequestCountsException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}