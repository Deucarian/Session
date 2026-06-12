using System;
using System.Threading;
using System.Threading.Tasks;

namespace Deucarian.Session.Samples
{
    /// <summary>
    /// Fake login service that creates session data without contacting a backend.
    /// </summary>
    public sealed class FakeSessionLoginService : ISessionLoginService<FakeLoginRequest>
    {
        /// <inheritdoc />
        public Task<SessionResult> LoginAsync(
            FakeLoginRequest request,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var session = new SessionData(
                "sample-access-token",
                "sample-refresh-token",
                DateTimeOffset.UtcNow.AddMinutes(15));

            return Task.FromResult(SessionResult.Success(session));
        }
    }
}
