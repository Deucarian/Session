using System.Threading;
using System.Threading.Tasks;

namespace JorisHoef.SessionHelper
{
    /// <summary>
    /// Authenticates a backend-specific login request and maps the response into session data.
    /// </summary>
    /// <typeparam name="TLoginRequest">The application-specific login request type.</typeparam>
    public interface ISessionLoginService<in TLoginRequest>
    {
        /// <summary>
        /// Attempts to log in and return session data.
        /// </summary>
        /// <param name="request">The application-specific login request.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>A session result containing session data on success.</returns>
        Task<SessionResult> LoginAsync(TLoginRequest request, CancellationToken cancellationToken = default(CancellationToken));
    }
}
