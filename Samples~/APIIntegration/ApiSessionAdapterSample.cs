using System;
using System.Threading;
using System.Threading.Tasks;
using Deucarian.API.Configuration;
using Deucarian.API.Services;
using Deucarian.Session.API;
using UnityEngine;

namespace Deucarian.Session.API.Samples
{
    /// <summary>
    /// Minimal sample showing how to configure API with a Session auth provider.
    /// </summary>
    public sealed class ApiSessionAdapterSample : MonoBehaviour
    {
        [SerializeField] private ApiClientConfig apiClientConfig;

        private ISessionService sessionService;

        private void Awake()
        {
            sessionService = new SessionService(
                new PlayerPrefsSessionStore("session.apihelper.sample"),
                new ApiSampleRefreshService());

            var authProvider = new SessionAuthProvider(sessionService);
            ApiServices.Configure(apiClientConfig, authProvider);
        }

        private sealed class ApiSampleRefreshService : ISessionRefreshService
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
