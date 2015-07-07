namespace HockeyApp
{
    /// <summary>
    /// Defines the members for notifying the user about previous session crashing due to unhandled exception
    /// </summary>
    public interface IAppCrashNotifier
    {
        /// <summary>
        /// Confirm with the user as to whether they want to submit application crash data
        /// </summary>
        /// <returns><seealso cref="bool"/></returns>
        bool ConfirmUploadCrashData();
    }
}
