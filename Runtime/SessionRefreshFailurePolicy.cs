namespace JorisHoef.SessionHelper
{
    /// <summary>
    /// Determines how <see cref="SessionService"/> handles a failed refresh attempt.
    /// </summary>
    public enum SessionRefreshFailurePolicy
    {
        /// <summary>Keep the current session in memory and storage when refresh fails.</summary>
        PreserveSession = 0,

        /// <summary>Clear the current session from memory and storage when refresh fails.</summary>
        ClearSession = 1
    }
}
