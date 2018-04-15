namespace Sb49.Common.Support.v7.Droid.Permissions
{
    /// <summary>
    /// Status of a permission.
    /// </summary>
    public enum PermissionStatus
    {
        /// <summary>
        /// Permission is in an unknown state.
        /// </summary>
        Unknown,

        /// <summary>
        /// Denied by user.
        /// </summary>
        Denied,

        /// <summary>
        /// Feature is disabled on device.
        /// </summary>
        Disabled,

        /// <summary>
        /// Granted by user.
        /// </summary>
        Granted
    }
}