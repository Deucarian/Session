using System.Threading;
using System.Threading.Tasks;

namespace JorisHoef.SessionHelper
{
    /// <summary>
    /// Stores session data in memory for tests, tools, and temporary sessions.
    /// </summary>
    public sealed class InMemorySessionStore : ISessionStore
    {
        private SessionData session;

        /// <summary>
        /// Creates an empty in-memory session store.
        /// </summary>
        public InMemorySessionStore()
        {
        }

        /// <summary>
        /// Creates an in-memory session store with an initial session.
        /// </summary>
        /// <param name="initialSession">Initial session data.</param>
        public InMemorySessionStore(SessionData initialSession)
        {
            session = initialSession;
        }

        /// <inheritdoc />
        public Task<SessionData> LoadAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(session);
        }

        /// <inheritdoc />
        public Task SaveAsync(SessionData sessionData, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            session = sessionData;
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task ClearAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            session = null;
            return Task.CompletedTask;
        }
    }
}
