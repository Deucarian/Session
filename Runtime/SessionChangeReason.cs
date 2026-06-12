namespace Deucarian.Session
{
    /// <summary>
    /// Identifies the operation that caused the session to change.
    /// </summary>
    public enum SessionChangeReason
    {
        /// <summary>The session was restored from storage.</summary>
        Restored = 0,

        /// <summary>The session was created by a successful login.</summary>
        LoggedIn = 1,

        /// <summary>The session was cleared by logout.</summary>
        LoggedOut = 2,

        /// <summary>The session was replaced by a successful refresh.</summary>
        Refreshed = 3,

        /// <summary>The session was cleared because refresh failed and the clear policy was active.</summary>
        RefreshFailed = 4
    }
}
