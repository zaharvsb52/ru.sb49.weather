using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Sb49.Common.Droid;
using Sb49.Security.Cryptography;

namespace Sb49.Weather.Droid.Common.Crypto
{
    internal sealed class Sb49Crypto : CryptoSymmetricBase, ISb49Crypto
    {
        private const int IterationsInternal = 10203;
        private const int SaltBitSizeInternal = 128; //64, 128

        private string _talk;

        public Sb49Crypto(string talk, int saltBitSize = SaltBitSizeInternal,
            int iterations = IterationsInternal) : base(saltBitSize, iterations)
        {
            if (string.IsNullOrEmpty(talk))
                throw new ArgumentNullException(nameof(talk));

            _talk = talk;
            ProviderType = ProviderType.None;
        }

        public ProviderType ProviderType { get; set; }

        protected override void OnCreateEncryptProvider()
        {
            GenerateTalk();
            Provider = CreateProvider(ProviderType);
        }

        protected override void WriteNonEncryptInfos(Stream stream, params byte[][] infos)
        {
            WriteNonEncryptInfo(Encoding.UTF8.GetBytes(ProviderType.ToString()), stream);
            base.WriteNonEncryptInfos(stream, infos);
        }

        protected override byte[][] ReadNonEncryptInfos(Stream stream, int count, out int offset)
        {
            var info = ReadNonEncryptInfo(stream);
            ProviderType = (ProviderType) Enum.Parse(typeof(ProviderType), Encoding.UTF8.GetString(info));

            var infos = base.ReadNonEncryptInfos(stream, count, out offset);
            offset += info.Length + InfoLengthSize;
            return infos;
        }

        protected override void OnCreateDecryptProvider()
        {
            OnCreateEncryptProvider();
        }

        public override string Decrypt(string encryptedMessage)
        {
            switch (ProviderType)
            {
                case ProviderType.Simple:
                    return string.IsNullOrEmpty(encryptedMessage)
                        ? string.Empty
                        : Encoding.UTF8.GetString(Convert.FromBase64String(encryptedMessage));
                case ProviderType.SimpleZip:
                    if (string.IsNullOrEmpty(encryptedMessage))
                        return string.Empty;
                    var src = Convert.FromBase64String(encryptedMessage);
                    var zipUtil = new ZipUtil();
                    return zipUtil.Decompress(src);
            }

            return base.Decrypt(encryptedMessage);
        }

        public override string Encrypt(string message)
        {
            switch (ProviderType)
            {
                case ProviderType.Simple:
                    return string.IsNullOrEmpty(message)
                        ? string.Empty
                        : Convert.ToBase64String(Encoding.UTF8.GetBytes(message));
                    case ProviderType.SimpleZip:
                        if (string.IsNullOrEmpty(message))
                            return string.Empty;
                        var zipUtil = new ZipUtil();
                        return Convert.ToBase64String(zipUtil.Compress(message));
            }

            return base.Encrypt(message);
        }

        protected override void OnDispose(bool disposing)
        {
            _talk = null;
            Provider?.Dispose();
            Provider = null;
            base.OnDispose(disposing);
        }

        private SymmetricAlgorithm CreateProvider(ProviderType type)
        {
            Provider?.Dispose();
            SymmetricAlgorithm result;

            switch (type)
            {
                case ProviderType.Simple:
                case ProviderType.SimpleZip:
                    return null;
                case ProviderType.Aes001:
                    result = new AesManaged
                    {
                        Mode = CipherMode.CBC,
                        Padding = PaddingMode.PKCS7
                    };
                    break;
                case ProviderType.Des001:
                    result = new TripleDESCryptoServiceProvider
                    {
                        Mode = CipherMode.CBC,
                        Padding = PaddingMode.PKCS7
                    };
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            result.KeySize = result.LegalKeySizes.Last().MaxSize;
            result.BlockSize = result.LegalBlockSizes.Last().MaxSize;
            return result;
        }

        private void GenerateTalk()
        {
            Talk = string.Format("{0}18a{0}b52S{1}Jk12{0}", ProviderType, _talk);
        }
    }

    public enum ProviderType
    {
        None,
        Default,
        Simple,
        SimpleZip,
        Aes001,
        Des001
    }
}