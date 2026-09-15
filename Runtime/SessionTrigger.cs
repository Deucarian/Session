using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
namespace Deucarian.Session
{
    public sealed class SessionTrigger : MonoBehaviour
    {
        [SerializeField] private SessionHost host;
        [SerializeField] private UnityEvent succeeded = new UnityEvent();
        [SerializeField] private UnityEvent failed = new UnityEvent();
        public SessionResult LastResult { get; private set; }
        private SessionHost Host => host != null ? host : throw new InvalidOperationException("Assign a configured SessionHost to this SessionTrigger.");
        public async void Restore() => Complete(await Host.RestoreAsync());
        public async void Refresh() => Complete(await Host.RefreshAsync());
        public async void Logout() => Complete(await Host.LogoutAsync());
        private void Complete(SessionResult result)
        {
            if (this == null) return;
            LastResult = result;
            if (result.Succeeded) succeeded.Invoke(); else failed.Invoke();
        }
    }
}
