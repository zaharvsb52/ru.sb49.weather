using System;
using System.Threading.Tasks;
using Android;
using Android.Content;
using Android.Support.V4.Content;

namespace Sb49.Common.Support.v7.Droid.Permissions
{
    public class Sb49PermissionChecker
    {
        public string[] PermissionLocation => new[] { Manifest.Permission.AccessFineLocation, Manifest.Permission.AccessCoarseLocation };

        /// <summary>
        /// Determines whether this instance has permission the specified permission.
        /// </summary>
        public PermissionStatus CheckPermission(Context context, string[] permissions)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (permissions == null || permissions.Length == 0)
                return PermissionStatus.Unknown;

            foreach (var permission in permissions)
            {
                var status = GetPermissionStatus(PermissionChecker.CheckSelfPermission(context, permission));
                if (status == PermissionStatus.Granted)
                    continue;

                return status;
            }

            return PermissionStatus.Granted;
        }

        /// <summary>
        /// Determines whether this instance has permission the specified permission.
        /// </summary>
        public Task<PermissionStatus> CheckPermissionAsync(Context context, string[] permissions)
        {
            return Task.Run(() => CheckPermission(context, permissions));
        }

        public static PermissionStatus GetPermissionStatus(int permissionStatus)
        {
            switch (permissionStatus)
            {
                case PermissionChecker.PermissionGranted:
                    return PermissionStatus.Granted;
                case PermissionChecker.PermissionDenied:
                    return PermissionStatus.Denied;
                case PermissionChecker.PermissionDeniedAppOp:
                    return PermissionStatus.Disabled;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}