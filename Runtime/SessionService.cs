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
        private const string InvalidAccessTokenCode = "invalid_access_token";
        private const string LoginExceptionCode = "login_exception";
        private const string RefreshExceptionCode = "refresh_exception";
        private const string RefreshServiceMissingCode = "refresh_service_missing";
        private const string NoSessionCode = "no_session";
        private readonly SessionCommitCoordinator commits;
        private readonly SessionRefreshCoalescer refreshes = new SessionRefreshCoalescer();
        private readonly ISessionRefreshService refreshService;
        private readonly Func<DateTimeOffset> utcNowProvider;

        private SessionData currentSession => commits.Current;
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

            commits = new SessionCommitCoordinator(sessionStore);
            commits.Changed += OnSessionChanged;
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
                SessionData session = currentSession;
                return session != null && session.IsExpired(GetUtcNow());
            }
        }

        /// <inheritdoc />
        public bool IsAccessTokenExpiringSoon
        {
            get
            {
                SessionData session = currentSession;
                return session != null && session.IsExpiredOrExpiringWithin(GetUtcNow(), ExpiryLeeway);
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
        public Task<SessionResult> RestoreAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            return commits.RestoreAsync(commits.Begin(), cancellationToken);
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

            long generation = commits.Begin();
            try
            {
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

            cancellationToken.ThrowIfCancellationRequested();
            if (!commits.IsCurrent(generation)) return SessionCommitCoordinator.Superseded();
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

            return await commits.CommitAsync(
                generation,
                loginResult.Session,
                SessionChangeReason.LoggedIn,
                cancellationToken);
            }
            finally { commits.Finish(generation); }
        }

        /// <inheritdoc />
        public Task<SessionResult> ReplaceAccessTokenAsync(
            string accessToken,
            DateTimeOffset? expiresAtUtc = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!SessionData.IsValidAccessToken(accessToken))
            {
                return Task.FromResult(
                    SessionResult.Failed(
                        InvalidAccessTokenCode,
                        "A valid access token is required."));
            }

            long generation = commits.Begin(out SessionData previous);
            string refreshToken = previous?.RefreshToken;
            var replacement = new SessionData(
                accessToken,
                refreshToken,
                expiresAtUtc);
            return commits.CommitAsync(
                generation,
                replacement,
                SessionChangeReason.AccessTokenReplaced,
                cancellationToken);
        }

        /// <inheritdoc />
        public Task<SessionResult> RefreshAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            long generation = commits.Capture(out SessionData session);
            if (!commits.IsCurrent(generation)) return Task.FromResult(SessionCommitCoordinator.Superseded());
            if (session == null)
            {
                return Task.FromResult(SessionResult.Failed(NoSessionCode, "No current session is available to refresh."));
            }

            if (refreshService == null)
            {
                return Task.FromResult(SessionResult.Failed(
                    RefreshServiceMissingCode,
                    "No session refresh service was configured."));
            }

            return refreshes.RunAsync(generation, token => RefreshCoreAsync(generation, session, token), cancellationToken);
        }

        private async Task<SessionResult> RefreshCoreAsync(long generation, SessionData session, CancellationToken cancellationToken)
        {
            SessionResult refreshResult;
            try
            {
                refreshResult = await refreshService.RefreshAsync(session, cancellationToken);
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

            cancellationToken.ThrowIfCancellationRequested();
            if (!commits.IsCurrent(generation)) return SessionCommitCoordinator.Superseded();
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
                    SessionResult clearResult = await commits.CommitAsync(
                        generation,
                        null,
                        SessionChangeReason.RefreshFailed,
                        cancellationToken);

                    if (clearResult.IsFailure)
                    {
                        return clearResult;
                    }
                }

                return refreshResult;
            }

            return await commits.CommitAsync(
                generation,
                refreshResult.Session,
                SessionChangeReason.Refreshed,
                cancellationToken);
        }

        /// <inheritdoc />
        public Task<SessionResult> LogoutAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            return commits.CommitAsync(commits.Begin(), null, SessionChangeReason.LoggedOut, cancellationToken);
        }

        private void OnSessionChanged(SessionData previousSession, SessionData session, SessionChangeReason reason)
        {
            SessionState previousState = CalculateState(previousSession);
            SessionState currentState = CalculateState(session);
            bool sessionChanged = !Equals(previousSession, session);
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
                        session,
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
