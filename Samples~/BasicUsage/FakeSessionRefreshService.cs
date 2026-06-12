using System;
using System.Threading;
using System.Threading.Tasks;

namespace Deucarian.Session.Samples
{
    /// <summary>
    /// Fake refresh service that replaces the access token without contacting a backend.
    /// </summary>
    public sealed class FakeSessionRefreshService : ISessionRefreshService
    {
        /// <inheritdoc />
        public Task<SessionResult> RefreshAsync(
            SessionData currentSession,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var refreshedSession = new SessionData(
                "sample-access-token-refreshed",
                currentSession.RefreshToken,
                DateTimeOffset.UtcNow.AddMinutes(15));

            return Task.FromResult(SessionResult.Success(refreshedSession));
        }
    }
}
