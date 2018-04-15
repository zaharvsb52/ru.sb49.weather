using System;
using System.Linq;
using Android.Content;

namespace Sb49.Common.Droid.Cache
{
    //http://stackoverflow.com/questions/9942560/when-to-clear-the-cache-dir-in-android/10069679#10069679
    public class CacheManager
    {
        protected const int MaxSizeByteDefault = 5242880; //5 MB

        public CacheManager(int maxSize = MaxSizeByteDefault)
        {
            if (maxSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxSize),
                    string.Format("{0} must be greater than 0", nameof(MaxSizeByte)));
            }

            MaxSizeByte = maxSize;
        }

        public int MaxSizeByte { get; }

        public void CacheData(Context context, string key, byte[] data, bool append)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            if (data == null || data.Length == 0)
                return;

            using (var cacheDir = GetCacheDir(context))
            {
                var size = cacheDir.TotalSpace - cacheDir.FreeSpace;
                var dataSize = data.Length;
                var newSize = dataSize + size;

                if (newSize > MaxSizeByte)
                    Clear(cacheDir, newSize - MaxSizeByte);

                if(dataSize > MaxSizeByte)
                    return;

                using (var file = new Java.IO.File(cacheDir, key))
                {
                    using (var stream = new Java.IO.FileOutputStream(file, append))
                    {
                        stream.Write(data);
                        stream.Flush();
                        stream.Close();
                    }
                }
            }
        }

        public bool ContainsKey(Context context, string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            using (var file = FindFile(context, key))
            {
                return file?.Exists() == true;
            }
        }

        public byte[] RetrieveData(Context context, string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            using (var file = FindFile(context, key))
            {
                if (file?.Exists() != true)
                    return null;

                var data = new byte[(int) file.Length()];
                using (var stream = new Java.IO.FileInputStream(file))
                {
                    stream.Read(data);
                    return data;
                }
            }
        }

        public void Remove(Context context, string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            using (var file = FindFile(context, key))
            {
                if (file?.Exists() == true)
                    file.Delete();
            }
        }

        public void Clear(Context context)
        {
            using (var cacheDir = GetCacheDir(context))
            {
                Clear(cacheDir, null);
            }
        }

        protected void Clear(Java.IO.File dir, long? bytes)
        {
            var bytesDeleted = 0L;
            var files = dir.ListFiles();

            foreach (var file in files)
            {
                bytesDeleted += file.Length();
                file.Delete();
                if (bytes.HasValue && bytesDeleted >= bytes)
                    break;
            }
        }

        protected Java.IO.File GetCacheDir(Context context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            return context.CacheDir;
        }

        protected Java.IO.File FindFile(Context context, string key)
        {
            using (var cacheDir = GetCacheDir(context))
            {
                return new Java.IO.File(cacheDir, key);
            }
        }
    }
}