namespace JorisHoef.SessionHelper
{
    /// <summary>
    /// Describes the authentication state represented by the current session data.
    /// </summary>
    public enum SessionState
    {
        /// <summary>No usable session is available.</summary>
        Unauthenticated = 0,

        /// <summary>An access token is available and is not expired.</summary>
        Authenticated = 1,

        /// <summary>An access token is available, but its expiry time has passed.</summary>
        Expired = 2
    }
}
