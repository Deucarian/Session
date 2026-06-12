using System;
using System.Threading;
using System.Threading.Tasks;

namespace Deucarian.Session
{
    /// <summary>
    /// Coordinates session state, persistence, login, logout, restore, and refresh operations.
    /// </summary>
    public interface ISessionService
    {
        /// <summary>
        /// Raised when the current session data or calculated session state changes.
        /// </summary>
        event EventHandler<SessionChangedEventArgs> SessionChanged;

        /// <summary>
        /// Gets the current session data, or null when no session has been loaded.
        /// </summary>
        SessionData CurrentSession { get; }

        /// <summary>
        /// Gets the current session state.
        /// </summary>
        SessionState State { get; }

        /// <summary>
        /// Gets whether the current session has an access token that is not expired.
        /// </summary>
        bool IsAuthenticated { get; }

        /// <summary>
        /// Gets whether the current access token has expired.
        /// </summary>
        bool IsAccessTokenExpired { get; }

        /// <summary>
        /// Gets whether the current access token has expired or is within the configured expiry leeway.
        /// </summary>
        bool IsAccessTokenExpiringSoon { get; }

        /// <summary>
        /// Gets or sets the leeway used to determine whether an access token is close to expiring.
        /// </summary>
        TimeSpan ExpiryLeeway { get; set; }

        /// <summary>
        /// Gets or sets how failed refresh attempts affect the current session.
        /// </summary>
        SessionRefreshFailurePolicy RefreshFailurePolicy { get; set; }

        /// <summary>
        /// Loads a saved session from the configured store.
        /// </summary>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>A successful result containing the restored session, or no session when storage is empty.</returns>
        Task<SessionResult> RestoreAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Logs in through the supplied backend-specific login service, then saves and activates the returned session.
        /// </summary>
        /// <typeparam name="TLoginRequest">The application-specific login request type.</typeparam>
        /// <param name="request">The login request.</param>
        /// <param name="loginService">The service that maps the request into session data.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>A session result containing the active session on success.</returns>
        Task<SessionResult> LoginAsync<TLoginRequest>(
            TLoginRequest request,
            ISessionLoginService<TLoginRequest> loginService,
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Refreshes the current session through the configured refresh service.
        /// </summary>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>A session result containing the refreshed session on success.</returns>
        Task<SessionResult> RefreshAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Clears the current session from memory and storage.
        /// </summary>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>A successful result when the session has been cleared.</returns>
        Task<SessionResult> LogoutAsync(CancellationToken cancellationToken = default(CancellationToken));
    }
}
