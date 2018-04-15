using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;

namespace Sb49.Common.Droid
{
    public sealed class ZipUtil
    {
        public void Zip(string[] files, string zipFilePath, string comment = null, int bufferSize = 1024, CancellationToken? token = null)
        {
            if (string.IsNullOrEmpty(zipFilePath))
                throw new ArgumentNullException(nameof(zipFilePath));

            if(token == null)
                token = CancellationToken.None;

            using (var fileOutputStream = File.Create(zipFilePath))
            {
                using (var zipOutputStream = new Java.Util.Zip.ZipOutputStream(fileOutputStream))
                {
                    if (!string.IsNullOrEmpty(comment))
                        zipOutputStream.SetComment(comment);

                    var buffer = new byte[bufferSize];
                    foreach (var file in files.Where(p => !string.IsNullOrEmpty(p)))
                    {
                        if(token.Value.IsCancellationRequested)
                            break;

                        using (var entry = new Java.Util.Zip.ZipEntry(Path.GetFileName(file)))
                        {
                            zipOutputStream.PutNextEntry(entry);

                            using (var readStream = File.OpenRead(file))
                            {
                                using (var bufferedStream = new BufferedStream(readStream))
                                {
                                    int count;
                                    while ((count = bufferedStream.Read(buffer, 0, bufferSize)) > 0)
                                    {
                                        zipOutputStream.Write(buffer, 0, count);
                                    }
                                }
                            }

                            zipOutputStream.CloseEntry();
                        }
                    }

                    zipOutputStream.Close();
                }
            }
        }

        public byte[] Compress(string src, CompressionLevel? compressionLevel = null)
        {
            if (string.IsNullOrEmpty(src))
                return null;

            var byteArray = Encoding.UTF8.GetBytes(src);
            var byteArrayLength = byteArray.Length;
            byte[] zip;

            using (var ms = new MemoryStream())
            {
                using (var zs = GetGZipStream(ms, CompressionMode.Compress, compressionLevel))
                {
                    zs.Write(byteArray, 0, byteArrayLength);
                    zs.Close();
                    zip = ms.ToArray();
                }
            }

            if (zip.Length <= 0)
                return null;

            var sizeArray = BitConverter.GetBytes(byteArrayLength);
            var buffer = new List<byte>(sizeArray);
            buffer.AddRange(zip);
            return buffer.ToArray();
        }

        public string Decompress(byte[] src, CompressionLevel? compressionLevel = null)
        {
            if (src == null)
                return null;

            var srcLength = src.Length;
            if (srcLength == 0)
                return null;

            const int intSize = sizeof(int);
            if (srcLength <= intSize)
                return null;

            var zip = new byte[srcLength - intSize];
            Array.Copy(src, intSize, zip, 0, zip.Length);

            var unzipLength = BitConverter.ToInt32(src, 0);
            if (unzipLength <= 0)
                throw new ArgumentOutOfRangeException(nameof(unzipLength), "zip lenght must be greater than 0");

            var unzip = new byte[unzipLength];

            using (var ms = new MemoryStream(zip))
            {
                using (var zs = GetGZipStream(ms, CompressionMode.Decompress, compressionLevel))
                {
                    unzipLength = zs.Read(unzip, 0, unzipLength);
                    if (unzipLength <= 0)
                        return null;

                    zs.Close();
                }
            }

            var result = Encoding.UTF8.GetString(unzip);
            return result;
        }

        private GZipStream GetGZipStream(Stream strem, CompressionMode compressionMode, CompressionLevel? compressionLevel)
        {
            return compressionLevel.HasValue
                ? new GZipStream(strem, compressionLevel.Value)
                : new GZipStream(strem, compressionMode);
        }
    }
}