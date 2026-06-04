using System;
using System.Threading;
using System.Threading.Tasks;
using JorisHoef.APIHelper.Configuration;
using JorisHoef.APIHelper.Services;
using JorisHoef.SessionHelper.APIHelper;
using UnityEngine;

namespace JorisHoef.SessionHelper.APIHelper.Samples
{
    /// <summary>
    /// Minimal sample showing how to configure APIHelper with a Session Helper auth provider.
    /// </summary>
    public sealed class ApiHelperSessionAdapterSample : MonoBehaviour
    {
        [SerializeField] private ApiClientConfig apiClientConfig;

        private ISessionService sessionService;

        private void Awake()
        {
            sessionService = new SessionService(
                new PlayerPrefsSessionStore("session-helper.apihelper.sample"),
                new ApiHelperSampleRefreshService());

            var authProvider = new SessionAuthProvider(sessionService);
            ApiServices.Configure(apiClientConfig, authProvider);
        }

        private sealed class ApiHelperSampleRefreshService : ISessionRefreshService
        {
            public Task<SessionResult> RefreshAsync(
                SessionData currentSession,
                CancellationToken cancellationToken = default(CancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                return Task.FromResult(
                    SessionResult.Success(
                        new SessionData(
                            "sample-apihelper-access-token-refreshed",
                            currentSession.RefreshToken,
                            DateTimeOffset.UtcNow.AddMinutes(15))));
            }
        }
    }
}
