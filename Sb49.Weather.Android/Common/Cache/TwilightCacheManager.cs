using Android.Content;
using Sb49.Common.Droid;
using Sb49.Common.Droid.Cache;

namespace Sb49.Weather.Droid.Common
{
    public sealed class TwilightCacheManager : CacheManager
    {
        public const int MaxCount = 100;

        public void CacheData(Context context, string key, string data)
        {
            var buffer = new ZipUtil().Compress(data);
            if(buffer?.Length > 0)
                CacheData(context, key, buffer, false);
        }

        public new string RetrieveData(Context context, string key)
        {
            var buffer = base.RetrieveData(context, key);
            if (buffer == null || buffer.Length == 0)
                return null;

            var result = new ZipUtil().Decompress(buffer);
            return result;
        }
    }
}