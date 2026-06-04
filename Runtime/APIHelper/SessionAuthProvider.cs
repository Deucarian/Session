using System.Threading;
using System.Threading.Tasks;
using JorisHoef.APIHelper.Authentication;

namespace JorisHoef.SessionHelper.APIHelper
{
    /// <summary>
    /// Exposes the current Session Helper access token through APIHelper's <see cref="IApiAuthProvider"/> interface.
    /// </summary>
    public sealed class SessionAuthProvider : IApiAuthProvider
    {
        private readonly ISessionService sessionService;
        private readonly bool refreshIfExpiredOrExpiringSoon;

        /// <summary>
        /// Creates an APIHelper auth provider backed by a session service.
        /// </summary>
        /// <param name="sessionService">Session service that owns the current token.</param>
        /// <param name="refreshIfExpiredOrExpiringSoon">Whether to attempt refresh before returning an expired or soon-expiring token.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="sessionService"/> is null.</exception>
        public SessionAuthProvider(ISessionService sessionService, bool refreshIfExpiredOrExpiringSoon = true)
        {
            if (sessionService == null)
            {
                throw new System.ArgumentNullException(nameof(sessionService));
            }

            this.sessionService = sessionService;
            this.refreshIfExpiredOrExpiringSoon = refreshIfExpiredOrExpiringSoon;
        }

        /// <inheritdoc />
        public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            SessionData session = sessionService.CurrentSession;
            if (session == null)
            {
                return null;
            }

            if (refreshIfExpiredOrExpiringSoon
                && (sessionService.IsAccessTokenExpired || sessionService.IsAccessTokenExpiringSoon))
            {
                await sessionService.RefreshAsync(cancellationToken);
                session = sessionService.CurrentSession;
            }

            if (!sessionService.IsAuthenticated || session == null)
            {
                return null;
            }

            return session.AccessToken;
        }
    }
}
