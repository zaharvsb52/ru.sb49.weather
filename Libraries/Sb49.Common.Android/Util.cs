using System;
using System.Security.Cryptography;
using System.Text;
using Android.Content;

namespace Sb49.Common.Droid
{
    public static class Util
    {
        //http://stackoverflow.com/questions/27842430/service-intent-must-be-explicit-intent
        public static Intent CreateExplicitFromImplicitIntent(Context context, Intent implicitIntent)
        {
            // Retrieve all services that can match the given intent
            var packageManager = context.PackageManager;
            var resolveInfo = packageManager.QueryIntentServices(implicitIntent, 0);

            // Make sure only one match was found
            if (resolveInfo == null || resolveInfo.Count != 1)
                return null;

            // Get component info and create ComponentName
            var serviceInfo = resolveInfo[0];
            var packageName = serviceInfo.ServiceInfo.PackageName;
            var className = serviceInfo.ServiceInfo.Name;
            var component = new ComponentName(packageName, className);

            // Create a new intent. Use the old one for extras and such reuse
            var explicitIntent = new Intent(implicitIntent);

            // Set the component to be explicit
            explicitIntent.SetComponent(component);

            return explicitIntent;
        }

        public static bool IsNetworkAvailable(Android.Net.ConnectivityManager connectivityManager)
        {
            if (connectivityManager == null)
                throw new ArgumentNullException(nameof(connectivityManager));

            //When on API 21+ need to use getAllNetworks, else fall base to GetAllNetworkInfo
            //https://developer.android.com/reference/android/net/ConnectivityManager.html#getAllNetworks()
            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Lollipop)
            {
                foreach (var network in connectivityManager.GetAllNetworks())
                {
                    var info = connectivityManager.GetNetworkInfo(network);

                    if (info?.IsConnected ?? false)
                        return true;
                }
            }
            else
            {
                var info = connectivityManager.ActiveNetworkInfo;
                return info?.IsConnected ?? false;
            }

            return false;
        }

        public static int GetAppWidgetCell(int size)
        {
            return (size + 30) / 70;
        }

        public static bool ExistsExternalStorage()
        {
            return Android.OS.Environment.ExternalStorageState == Android.OS.Environment.MediaMounted ||
                   Android.OS.Environment.ExternalStorageState == Android.OS.Environment.MediaMountedReadOnly;
        }

        public static string GetDeviceName()
        {
            var manufacturer = Android.OS.Build.Manufacturer;
            var model = Android.OS.Build.Model;
            if (model.StartsWith(manufacturer))
                return model.ToCapital();
            return manufacturer.ToCapital() + " " + model;
        }

        public static string GetAndroidVersion()
        {
            var result = string.Format("Android {0} (API Level {1} - {2})",
                Android.OS.Build.VERSION.Release,
                Android.OS.Build.VERSION.Sdk,
                Android.OS.Build.VERSION.SdkInt);
            return result;
        }

        public static string GetMd5Code(string text, bool upperCase)
        {
            var bytetext = Encoding.UTF8.GetBytes(text);
            using (var md5 = MD5.Create())
            {
                var byteHashed = md5.ComputeHash(bytetext);
                return Common.Util.ToHex(byteHashed, upperCase);
            }
        }
    }
}