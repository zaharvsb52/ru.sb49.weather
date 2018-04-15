using System;

namespace Sb49.Http.Provider.Exeptions
{
    public class ProviderServiceExceptionBase : Exception
    {
        public ProviderServiceExceptionBase()
        {
        }

        public ProviderServiceExceptionBase(string message) : base(message)
        {
        }

        public ProviderServiceExceptionBase(string message, Exception inner) : base(message, inner)
        {
        }
    }
}