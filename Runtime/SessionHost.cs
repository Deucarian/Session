using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Deucarian.Session
{
    /// <summary>Scene access to an explicitly supplied authoritative session. The composition root owns the service.</summary>
    [DisallowMultipleComponent]
    public sealed class SessionHost : MonoBehaviour
    {
        private ISessionService service;
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private bool destroyed;
        public void Configure(ISessionService session)
        {
            if (destroyed) throw new ObjectDisposedException(nameof(SessionHost));
            if (service != null) throw new InvalidOperationException("This session host is already configured.");
            service = session ?? throw new ArgumentNullException(nameof(session));
        }
        public SessionState State => Service.State;
        public Task<SessionResult> RestoreAsync(CancellationToken cancellationToken = default) => Run(Service.RestoreAsync, cancellationToken);
        public Task<SessionResult> LogoutAsync(CancellationToken cancellationToken = default) => Run(Service.LogoutAsync, cancellationToken);
        public Task<SessionResult> RefreshAsync(CancellationToken cancellationToken = default) => Run(Service.RefreshAsync, cancellationToken);
        private ISessionService Service => !destroyed ? service ??
            throw new InvalidOperationException("SessionHost '" + name + "' is not configured. Supply the application's ISessionService once during startup before restoring, refreshing or signing out.") : throw new ObjectDisposedException(nameof(SessionHost));
        private async Task<SessionResult> Run(Func<CancellationToken, Task<SessionResult>> operation, CancellationToken token)
        {
            using (var cancellation = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token, token))
                return await operation(cancellation.Token);
        }
        private void OnDestroy() { destroyed = true; lifetime.Cancel(); lifetime.Dispose(); service = null; }
    }
}
