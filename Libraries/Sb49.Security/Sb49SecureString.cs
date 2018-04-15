using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using Sb49.Security.Core;

namespace Sb49.Security
{
    public sealed class Sb49SecureString : ISb49SecureString
    {
        private SecureString _secure;

        public Sb49SecureString(string src)
        {
            if (!string.IsNullOrEmpty(src))
                _secure = ToSecureString(src);
        }

        ~Sb49SecureString()
        {
            OnDispose(false);    
        }

        public bool IsDisposed { get; private set; }

        public string Decrypt()
        {
            return IsDisposed ? null : ConvertToString(_secure);
        }

        public bool Validate()
        {
            try
            {
                return !IsDisposed && _secure != null && !string.IsNullOrEmpty(Decrypt());
            }
            catch
            {
                return false;
            }
        }

        private static SecureString ToSecureString(string src, bool makeReadOnly = true)
        {
            if (string.IsNullOrEmpty(src))
                return null;

            var result = new SecureString();
            src.ToCharArray().ToList().ForEach(p => result.AppendChar(p));
            if (makeReadOnly)
                result.MakeReadOnly();
            return result;
        }

        private static string ConvertToString(SecureString src)
        {
            if (src == null)
                return null;

            //https://blogs.msdn.microsoft.com/fpintos/2009/06/12/how-to-properly-convert-securestring-to-string/
            var unmanagedString = IntPtr.Zero;
            try
            {
                //unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(src);
                unmanagedString = SecureStringMarshal.SecureStringToGlobalAllocUnicode(src);
                return Marshal.PtrToStringUni(unmanagedString);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
            }
        }

        #region . IDisposable .
        private void OnDispose(bool disposing)
        {
            if (!IsDisposed)
                _secure?.Dispose();
            _secure = null;
            IsDisposed = disposing;
        }

        public void Dispose()
        {
            OnDispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion . IDisposable .
    }
}