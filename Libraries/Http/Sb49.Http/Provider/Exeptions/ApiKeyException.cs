using System;

namespace Sb49.Http.Provider.Exeptions
{
    public class ApiKeyException : ProviderServiceExceptionBase
    {
        public ApiKeyException() : base (Properties.Resources.UndefinedServerApiKey)
        {
        }

        public ApiKeyException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}