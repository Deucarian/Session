using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Deucarian.Session.Samples
{
    /// <summary>
    /// Minimal MonoBehaviour sample for restore, fake login, refresh, and logout.
    /// </summary>
    public sealed class SessionSampleController : MonoBehaviour
    {
        private ISessionService sessionService;
        private FakeSessionLoginService loginService;

        private void Awake()
        {
            sessionService = new SessionService(
                new PlayerPrefsSessionStore("session.sample"),
                new FakeSessionRefreshService(),
                TimeSpan.FromMinutes(2),
                SessionRefreshFailurePolicy.PreserveSession);

            loginService = new FakeSessionLoginService();
            sessionService.SessionChanged += OnSessionChanged;
        }

        private async void Start()
        {
            await RestoreSessionAsync();
        }

        private void OnDestroy()
        {
            if (sessionService != null)
            {
                sessionService.SessionChanged -= OnSessionChanged;
            }
        }

        /// <summary>
        /// Restores the sample session from storage.
        /// </summary>
        [ContextMenu("Restore Session")]
        public async void RestoreSession()
        {
            await RestoreSessionAsync();
        }

        /// <summary>
        /// Runs a fake login and stores the returned session.
        /// </summary>
        [ContextMenu("Fake Login")]
        public async void FakeLogin()
        {
            SessionResult result = await sessionService.LoginAsync(
                new FakeLoginRequest
                {
                    Username = "demo",
                    Password = "password"
                },
                loginService);

            Debug.Log(result.Succeeded ? "Fake login succeeded." : result.Error.Message);
        }

        /// <summary>
        /// Refreshes the current sample session.
        /// </summary>
        [ContextMenu("Refresh Session")]
        public async void RefreshSession()
        {
            SessionResult result = await sessionService.RefreshAsync();
            Debug.Log(result.Succeeded ? "Refresh succeeded." : result.Error.Message);
        }

        /// <summary>
        /// Logs out and clears the sample session.
        /// </summary>
        [ContextMenu("Logout")]
        public async void Logout()
        {
            SessionResult result = await sessionService.LogoutAsync();
            Debug.Log(result.Succeeded ? "Logged out." : result.Error.Message);
        }

        private async Task RestoreSessionAsync()
        {
            SessionResult result = await sessionService.RestoreAsync();
            Debug.Log(result.Succeeded ? $"Restored session state: {sessionService.State}" : result.Error.Message);
        }

        private void OnSessionChanged(object sender, SessionChangedEventArgs args)
        {
            Debug.Log($"Session changed: {args.PreviousState} -> {args.CurrentState}");
        }
    }
}
