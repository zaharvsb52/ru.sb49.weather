using System;

namespace Sb49.Security.Core
{
    public interface ISb49SecureString : IDisposable
    {
        bool IsDisposed { get; }
        string Decrypt();
        bool Validate();
    }
}
