using System;
using System.Threading;
using System.Threading.Tasks;

namespace Deucarian.Session
{
    /// <summary>
    /// Default implementation of <see cref="ISessionService"/>.
    /// </summary>
    public sealed class SessionService : ISessionService
    {
        private const string InvalidSessionCode = "invalid_session";
        private const string LoginExceptionCode = "login_exception";
        private const string RefreshExceptionCode = "refresh_exception";
        private const string RefreshServiceMissingCode = "refresh_service_missing";
        private const string NoSessionCode = "no_session";
        private const string StoreLoadExceptionCode = "store_load_exception";
        private const string StoreSaveExceptionCode = "store_save_exception";
        private const string StoreClearExceptionCode = "store_clear_exception";

        private readonly ISessionStore sessionStore;
        private readonly ISessionRefreshService refreshService;
        private readonly Func<DateTimeOffset> utcNowProvider;

        private SessionData currentSession;
        private TimeSpan expiryLeeway;

        /// <summary>
        /// Creates a session service.
        /// </summary>
        /// <param name="sessionStore">Store used to persist session data.</param>
        /// <param name="refreshService">Optional service used to refresh session data.</param>
        /// <param name="expiryLeeway">Optional leeway used to detect tokens that are close to expiring.</param>
        /// <param name="refreshFailurePolicy">Policy used when refresh fails.</param>
        /// <param name="utcNowProvider">Optional UTC time provider for tests.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="sessionStore"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="expiryLeeway"/> is negative.</exception>
        public SessionService(
            ISessionStore sessionStore,
            ISessionRefreshService refreshService = null,
            TimeSpan? expiryLeeway = null,
            SessionRefreshFailurePolicy refreshFailurePolicy = SessionRefreshFailurePolicy.PreserveSession,
            Func<DateTimeOffset> utcNowProvider = null)
        {
            if (sessionStore == null)
            {
                throw new ArgumentNullException(nameof(sessionStore));
            }

            if (expiryLeeway.HasValue && expiryLeeway.Value < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(expiryLeeway), "Expiry leeway cannot be negative.");
            }

            this.sessionStore = sessionStore;
            this.refreshService = refreshService;
            this.expiryLeeway = expiryLeeway ?? TimeSpan.FromMinutes(1);
            RefreshFailurePolicy = refreshFailurePolicy;
            this.utcNowProvider = utcNowProvider ?? (() => DateTimeOffset.UtcNow);
        }

        /// <inheritdoc />
        public event EventHandler<SessionChangedEventArgs> SessionChanged;

        /// <inheritdoc />
        public SessionData CurrentSession
        {
            get { return currentSession; }
        }

        /// <inheritdoc />
        public SessionState State
        {
            get { return CalculateState(currentSession); }
        }

        /// <inheritdoc />
        public bool IsAuthenticated
        {
            get { return State == SessionState.Authenticated; }
        }

        /// <inheritdoc />
        public bool IsAccessTokenExpired
        {
            get
            {
                return currentSession != null && currentSession.IsExpired(GetUtcNow());
            }
        }

        /// <inheritdoc />
        public bool IsAccessTokenExpiringSoon
        {
            get
            {
                return currentSession != null && currentSession.IsExpiredOrExpiringWithin(GetUtcNow(), ExpiryLeeway);
            }
        }

        /// <inheritdoc />
        public TimeSpan ExpiryLeeway
        {
            get { return expiryLeeway; }
            set
            {
                if (value < TimeSpan.Zero)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Expiry leeway cannot be negative.");
                }

                expiryLeeway = value;
            }
        }

        /// <inheritdoc />
        public SessionRefreshFailurePolicy RefreshFailurePolicy { get; set; }

        /// <inheritdoc />
        public async Task<SessionResult> RestoreAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            SessionData restoredSession;
            try
            {
                restoredSession = await sessionStore.LoadAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                return SessionResult.Failed(
                    StoreLoadExceptionCode,
                    "Failed to load the saved session.",
                    exception);
            }

            ApplySession(restoredSession, SessionChangeReason.Restored);
            return restoredSession == null ? SessionResult.Success() : SessionResult.Success(restoredSession);
        }

        /// <inheritdoc />
        public async Task<SessionResult> LoginAsync<TLoginRequest>(
            TLoginRequest request,
            ISessionLoginService<TLoginRequest> loginService,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (loginService == null)
            {
                throw new ArgumentNullException(nameof(loginService));
            }

            cancellationToken.ThrowIfCancellationRequested();

            SessionResult loginResult;
            try
            {
                loginResult = await loginService.LoginAsync(request, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                return SessionResult.Failed(
                    LoginExceptionCode,
                    "Login failed with an exception.",
                    exception);
            }

            if (loginResult == null)
            {
                return SessionResult.Failed(InvalidSessionCode, "Login returned no result.");
            }

            if (loginResult.IsFailure)
            {
                return loginResult;
            }

            if (!IsValidSession(loginResult.Session))
            {
                return SessionResult.Failed(InvalidSessionCode, "Login succeeded without valid session data.");
            }

            return await SaveAndApplySessionAsync(
                loginResult.Session,
                SessionChangeReason.LoggedIn,
                cancellationToken);
        }

        /// <inheritdoc />
        public async Task<SessionResult> RefreshAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (currentSession == null)
            {
                return SessionResult.Failed(NoSessionCode, "No current session is available to refresh.");
            }

            if (refreshService == null)
            {
                return SessionResult.Failed(
                    RefreshServiceMissingCode,
                    "No session refresh service was configured.");
            }

            SessionResult refreshResult;
            try
            {
                refreshResult = await refreshService.RefreshAsync(currentSession, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                refreshResult = SessionResult.Failed(
                    RefreshExceptionCode,
                    "Refresh failed with an exception.",
                    exception);
            }

            if (refreshResult == null)
            {
                refreshResult = SessionResult.Failed(InvalidSessionCode, "Refresh returned no result.");
            }

            if (refreshResult.Succeeded && !IsValidSession(refreshResult.Session))
            {
                refreshResult = SessionResult.Failed(InvalidSessionCode, "Refresh succeeded without valid session data.");
            }

            if (refreshResult.IsFailure)
            {
                if (RefreshFailurePolicy == SessionRefreshFailurePolicy.ClearSession)
                {
                    SessionResult clearResult = await ClearAndApplySessionAsync(
                        SessionChangeReason.RefreshFailed,
                        cancellationToken);

                    if (clearResult.IsFailure)
                    {
                        return clearResult;
                    }
                }

                return refreshResult;
            }

            return await SaveAndApplySessionAsync(
                refreshResult.Session,
                SessionChangeReason.Refreshed,
                cancellationToken);
        }

        /// <inheritdoc />
        public Task<SessionResult> LogoutAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return ClearAndApplySessionAsync(SessionChangeReason.LoggedOut, cancellationToken);
        }

        private async Task<SessionResult> SaveAndApplySessionAsync(
            SessionData session,
            SessionChangeReason reason,
            CancellationToken cancellationToken)
        {
            try
            {
                await sessionStore.SaveAsync(session, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                return SessionResult.Failed(
                    StoreSaveExceptionCode,
                    "Failed to save the session.",
                    exception);
            }

            ApplySession(session, reason);
            return SessionResult.Success(session);
        }

        private async Task<SessionResult> ClearAndApplySessionAsync(
            SessionChangeReason reason,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await sessionStore.ClearAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                return SessionResult.Failed(
                    StoreClearExceptionCode,
                    "Failed to clear the session.",
                    exception);
            }

            ApplySession(null, reason);
            return SessionResult.Success();
        }

        private void ApplySession(SessionData session, SessionChangeReason reason)
        {
            SessionData previousSession = currentSession;
            SessionState previousState = CalculateState(previousSession);

            currentSession = session;

            SessionState currentState = CalculateState(currentSession);
            bool sessionChanged = !Equals(previousSession, currentSession);
            bool stateChanged = previousState != currentState;

            if (!sessionChanged && !stateChanged)
            {
                return;
            }

            EventHandler<SessionChangedEventArgs> handler = SessionChanged;
            if (handler != null)
            {
                handler(
                    this,
                    new SessionChangedEventArgs(
                        previousSession,
                        currentSession,
                        previousState,
                        currentState,
                        reason));
            }
        }

        private SessionState CalculateState(SessionData session)
        {
            if (session == null)
            {
                return SessionState.Unauthenticated;
            }

            return session.IsExpired(GetUtcNow()) ? SessionState.Expired : SessionState.Authenticated;
        }

        private DateTimeOffset GetUtcNow()
        {
            return utcNowProvider().ToUniversalTime();
        }

        private static bool IsValidSession(SessionData session)
        {
            return session != null && !string.IsNullOrWhiteSpace(session.AccessToken);
        }
    }
}
