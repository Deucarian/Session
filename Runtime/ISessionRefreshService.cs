using System.Threading;
using System.Threading.Tasks;

namespace JorisHoef.SessionHelper
{
    /// <summary>
    /// Refreshes the current session through an application-specific backend flow.
    /// </summary>
    public interface ISessionRefreshService
    {
        /// <summary>
        /// Attempts to refresh the supplied session.
        /// </summary>
        /// <param name="currentSession">The current session data.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>A session result containing replacement session data on success.</returns>
        Task<SessionResult> RefreshAsync(SessionData currentSession, CancellationToken cancellationToken = default(CancellationToken));
    }
}
