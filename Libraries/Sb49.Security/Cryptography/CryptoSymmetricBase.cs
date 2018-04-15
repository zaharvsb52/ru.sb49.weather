using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Sb49.Security.Core;

namespace Sb49.Security.Cryptography
{
    //http://stackoverflow.com/questions/15162293/aesmanaged-file-encryption-and-decryption-and-prepending-initiaization-vector
    //http://stackoverflow.com/questions/202011/encrypt-and-decrypt-a-string/10366194#10366194

    public abstract class CryptoSymmetricBase : ICryptoSymmetric, IDisposable
    {
        protected const int InfoLengthSize = 4;

        protected CryptoSymmetricBase(int saltBitSize, int iterations)
        {
            SaltBitSize = saltBitSize;
            Iterations = iterations;
        }

        //protected CryptoSymmetricBase(SymmetricAlgorithm provider, byte[] talk, int saltBitSize, int iterations) : this(saltBitSize, iterations)
        //{
        //    if (provider == null)
        //        throw new ArgumentNullException(nameof(provider));
        //    if (talk == null)
        //        throw new ArgumentNullException(nameof(talk));

        //    Provider = provider;
        //    Talk = Convert.ToBase64String(talk);
        //}

        ~CryptoSymmetricBase()
        {
            OnDispose(false);
        }

        protected SymmetricAlgorithm Provider { get; set; }
        protected string Talk { get; set; }
        protected int SaltBitSize { get; }
        protected int Iterations { get; }

        public virtual string Encrypt(string message)
        {
            using (var ms = new MemoryStream())
            {
                using (Encrypt(message, ms))
                {
                    var encryptedMessageBytes = new byte[ms.Length];
                    ms.Position = 0;
                    ms.Read(encryptedMessageBytes, 0, encryptedMessageBytes.Length);

                    var encryptedMessage = Convert.ToBase64String(encryptedMessageBytes);
                    return encryptedMessage;
                }
            }
        }

        public virtual string Decrypt(string encryptedMessage)
        {
            using (var ms = new MemoryStream())
            {
                using (Decrypt(encryptedMessage, ms))
                {
                    var decryptedMessageBytes = new byte[ms.Length];
                    ms.Position = 0;
                    ms.Read(decryptedMessageBytes, 0, decryptedMessageBytes.Length);

                    var message = Encoding.UTF8.GetString(decryptedMessageBytes);
                    return message;
                }
            }
        }

        protected virtual CryptoStream Encrypt(string message, Stream stream)
        {
            var messageBytes = Encoding.UTF8.GetBytes(message);
            OnCreateEncryptProvider();
            var salt = CreateSalt();
            var iv = CreateIv(salt);
            WriteNonEncryptInfos(stream, salt, iv);

            var key = CreateKey(salt);

            using (var transform = Provider.CreateEncryptor(key, iv))
            {
                var cs = new CryptoStream(stream, transform, CryptoStreamMode.Write);
                cs.Write(messageBytes, 0, messageBytes.Length);
                cs.FlushFinalBlock();
                return cs;
            }
        }

        protected abstract void OnCreateEncryptProvider();

        protected virtual byte[] CreateSalt()
        {
            using (var generator = new Rfc2898DeriveBytes(Talk, SaltBitSize / 8, Iterations))
            {
                return generator.Salt;
            }
        }

        protected virtual byte[] CreateKey(byte[] salt)
        {
            //Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, IV); 
            using (var generator = new Rfc2898DeriveBytes(Talk, salt, Iterations))
            {
                return generator.GetBytes(Provider.KeySize / 8);
            }
        }

        protected virtual byte[] CreateIv(byte[] salt)
        {
            Provider.GenerateIV();
            return Provider.IV;
        }

        protected virtual CryptoStream Decrypt(string encryptedMessage, Stream stream)
        {
            var encryptedMessageBytes = Convert.FromBase64String(encryptedMessage);

            byte[][] infos;
            int offset;
            using (var ms = new MemoryStream(encryptedMessageBytes))
            {
                infos = ReadNonEncryptInfos(ms, 2, out offset);
            }

            OnCreateDecryptProvider();
            var salt = infos[0];
            var key = CreateKey(salt);
            var iv = infos[1];

            using (var transform = Provider.CreateDecryptor(key, iv))
            {
                var cs = new CryptoStream(stream, transform, CryptoStreamMode.Write);
                cs.Write(encryptedMessageBytes, offset, encryptedMessageBytes.Length - offset);
                cs.FlushFinalBlock();
                return cs;
            }
        }

        protected abstract void OnCreateDecryptProvider();

        protected virtual void WriteNonEncryptInfos(Stream stream, params byte[][] infos)
        {
            if (infos == null)
                return;

            foreach (var info in infos)
            {
                WriteNonEncryptInfo(info, stream);
            }
        }

        protected virtual byte[][] ReadNonEncryptInfos(Stream stream, int count, out int offset)
        {
            offset = 0;
            var result = new List<byte[]>();
            for (var i = 0; i < count; i++)
            {
                var info = ReadNonEncryptInfo(stream);
                result.Add(info);
                offset += info.Length + InfoLengthSize;
            }

            return result.ToArray();
        }

        protected virtual void WriteNonEncryptInfo(byte[] info, Stream stream)
        {
            var length = info.Length;
            stream.Write(BitConverter.GetBytes(length), 0, InfoLengthSize);
            stream.Write(info, 0, length);
        }

        protected virtual byte[] ReadNonEncryptInfo(Stream stream)
        {
            var rawLength = new byte[InfoLengthSize];
            if (stream.Read(rawLength, 0, rawLength.Length) != rawLength.Length)
                throw new Exception("Stream did not contain properly formatted byte array.");

            var len = BitConverter.ToInt32(rawLength, 0);
            var buffer = new byte[len];
            if (stream.Read(buffer, 0, buffer.Length) != buffer.Length)
                throw new Exception("Did not read byte array properly.");

            return buffer;
        }

        #region . IDisposable .

        protected virtual void OnDispose(bool disposing)
        {
            Talk = null;
        }

        public void Dispose()
        {
            OnDispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion . IDisposable .
    }
}