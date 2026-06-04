using System.Threading;
using System.Threading.Tasks;

namespace JorisHoef.SessionHelper
{
    /// <summary>
    /// Persists session data for restore, save, and clear operations.
    /// </summary>
    public interface ISessionStore
    {
        /// <summary>
        /// Loads the saved session.
        /// </summary>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>The saved session, or null when no session exists.</returns>
        Task<SessionData> LoadAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Saves the supplied session.
        /// </summary>
        /// <param name="session">The session to save.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>A task that completes when the session has been saved.</returns>
        Task SaveAsync(SessionData session, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Clears the saved session.
        /// </summary>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>A task that completes when the session has been cleared.</returns>
        Task ClearAsync(CancellationToken cancellationToken = default(CancellationToken));
    }
}
