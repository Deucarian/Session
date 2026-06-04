using System;
using System.Globalization;
using System.Threading.Tasks;
using UnityEngine;

namespace JorisHoef.SessionHelper.Samples
{
    /// <summary>
    /// Minimal MonoBehaviour sample for restore, fake login, refresh, logout, and store clearing.
    /// </summary>
    public sealed class SessionHelperSampleController : MonoBehaviour
    {
        private const string SampleStoreKey = "session-helper.sample";

        private ISessionStore sessionStore;
        private ISessionService sessionService;
        private FakeSessionLoginService loginService;
        private GUIStyle titleStyle;
        private GUIStyle labelStyle;
        private GUIStyle boxStyle;
        private GUIStyle buttonStyle;
        private string lastEventText = "No session events yet.";
        private string lastResultText = "Ready.";
        private bool isBusy;

        private void Awake()
        {
            sessionStore = new PlayerPrefsSessionStore(SampleStoreKey);
            sessionService = new SessionService(
                sessionStore,
                new FakeSessionRefreshService(),
                TimeSpan.FromMinutes(2),
                SessionRefreshFailurePolicy.PreserveSession);

            loginService = new FakeSessionLoginService();
            sessionService.SessionChanged += OnSessionChanged;
        }

        private async void Start()
        {
            await RunOperationAsync("Restore", RestoreSessionCoreAsync);
        }

        private void OnGUI()
        {
            EnsureStyles();

            GUILayout.BeginArea(new Rect(20f, 20f, 620f, 360f), boxStyle);
            GUILayout.Label("SessionHelper Basic Usage", titleStyle);
            GUILayout.Space(8f);

            bool previousGuiEnabled = GUI.enabled;
            GUI.enabled = previousGuiEnabled && !isBusy;

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Login", buttonStyle, GUILayout.Height(36f)))
            {
                FakeLogin();
            }

            if (GUILayout.Button("Restore", buttonStyle, GUILayout.Height(36f)))
            {
                RestoreSession();
            }

            if (GUILayout.Button("Refresh", buttonStyle, GUILayout.Height(36f)))
            {
                RefreshSession();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Logout", buttonStyle, GUILayout.Height(36f)))
            {
                Logout();
            }

            if (GUILayout.Button("Clear Store", buttonStyle, GUILayout.Height(36f)))
            {
                ClearStore();
            }
            GUILayout.EndHorizontal();

            GUI.enabled = previousGuiEnabled;

            GUILayout.Space(12f);
            GUILayout.Label(BuildStatusText(), labelStyle);
            GUILayout.Space(8f);
            GUILayout.Label("Last session event: " + lastEventText, labelStyle);
            GUILayout.Label("Last operation: " + lastResultText, labelStyle);
            GUILayout.EndArea();
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
            await RunOperationAsync("Restore", RestoreSessionCoreAsync);
        }

        /// <summary>
        /// Runs a fake login and stores the returned session.
        /// </summary>
        [ContextMenu("Fake Login")]
        public async void FakeLogin()
        {
            await RunOperationAsync("Fake login", LoginCoreAsync);
        }

        /// <summary>
        /// Refreshes the current sample session.
        /// </summary>
        [ContextMenu("Refresh Session")]
        public async void RefreshSession()
        {
            await RunOperationAsync("Refresh", () => sessionService.RefreshAsync());
        }

        /// <summary>
        /// Logs out and clears the sample session.
        /// </summary>
        [ContextMenu("Logout")]
        public async void Logout()
        {
            await RunOperationAsync("Logout", () => sessionService.LogoutAsync());
        }

        /// <summary>
        /// Clears the persisted sample session, then restores from the empty store.
        /// </summary>
        [ContextMenu("Clear Store")]
        public async void ClearStore()
        {
            await RunOperationAsync("Clear store", ClearStoreCoreAsync);
        }

        private void OnSessionChanged(object sender, SessionChangedEventArgs args)
        {
            lastEventText = string.Format(
                CultureInfo.InvariantCulture,
                "{0}: {1} -> {2}",
                args.Reason,
                args.PreviousState,
                args.CurrentState);

            Debug.Log("Session changed: " + lastEventText);
        }

        private async Task<SessionResult> LoginCoreAsync()
        {
            return await sessionService.LoginAsync(
                new FakeLoginRequest
                {
                    Username = "demo",
                    Password = "password"
                },
                loginService);
        }

        private Task<SessionResult> RestoreSessionCoreAsync()
        {
            return sessionService.RestoreAsync();
        }

        private async Task<SessionResult> ClearStoreCoreAsync()
        {
            await sessionStore.ClearAsync();
            return await sessionService.RestoreAsync();
        }

        private async Task RunOperationAsync(string operationName, Func<Task<SessionResult>> operation)
        {
            if (isBusy)
            {
                return;
            }

            isBusy = true;
            lastResultText = operationName + " started.";

            try
            {
                SessionResult result = await operation();
                lastResultText = FormatResult(operationName, result);

                if (result.Succeeded)
                {
                    Debug.Log(operationName + " succeeded. State: " + sessionService.State);
                }
                else
                {
                    Debug.LogWarning(operationName + " failed: " + result.Error.Message);
                }
            }
            catch (Exception exception)
            {
                lastResultText = operationName + " failed: " + exception.Message;
                Debug.LogWarning(operationName + " failed: " + exception.Message);
            }
            finally
            {
                isBusy = false;
            }
        }

        private string BuildStatusText()
        {
            SessionData session = sessionService.CurrentSession;
            string expiry = session == null || !session.ExpiresAtUtc.HasValue
                ? "None"
                : session.ExpiresAtUtc.Value.UtcDateTime.ToString(
                    "yyyy-MM-dd HH:mm:ss 'UTC'",
                    CultureInfo.InvariantCulture);

            bool hasRefreshToken = session != null && session.HasRefreshToken;

            return string.Format(
                CultureInfo.InvariantCulture,
                "State: {0}\nAuthenticated: {1}\nAccess token expired: {2}\nAccess token expiring soon: {3}\nExpiry: {4}\nRefresh token present: {5}",
                sessionService.State,
                sessionService.IsAuthenticated,
                sessionService.IsAccessTokenExpired,
                sessionService.IsAccessTokenExpiringSoon,
                expiry,
                hasRefreshToken);
        }

        private static string FormatResult(string operationName, SessionResult result)
        {
            if (result == null)
            {
                return operationName + " returned no result.";
            }

            if (result.Succeeded)
            {
                return operationName + " succeeded.";
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                "{0} failed ({1}): {2}",
                operationName,
                result.Error.Code,
                result.Error.Message);
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                wordWrap = true
            };

            boxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(16, 16, 16, 16)
            };

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14
            };
        }
    }
}
