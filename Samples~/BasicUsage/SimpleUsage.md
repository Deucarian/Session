# Simple usage

Create the authoritative SessionService once with the application's chosen store and refresh provider, then call sessionHost.Configure(service). The host delegates restore/logout/refresh to that exact session and returns SessionResult. Host destruction cancels its outstanding calls and releases its reference; the composition root continues to own the service. This does not choose a storage security policy, introduce another session, or add backend-specific login data.

Import the **Simple Usage** sample from Unity Package Manager. Its caller script is:

```csharp
using UnityEngine;

namespace Deucarian.Session.Samples.SimpleUsage
{
    public sealed class SimpleUsageExample : MonoBehaviour
    {
        [SerializeField] private SessionHost session;
        public System.Threading.Tasks.Task Restore() => session.RestoreAsync();
        public System.Threading.Tasks.Task Logout() => session.LogoutAsync();
    }
}
```
