# Simple usage

Create the authoritative SessionService once with the application's chosen store and refresh provider, then call sessionHost.Configure(service). The host delegates restore/logout/refresh to that exact session and returns SessionResult. Host destruction cancels its outstanding calls and releases its reference; the composition root continues to own the service. This does not choose a storage security policy, introduce another session, or add backend-specific login data.

Import the **Simple Usage** sample from Unity Package Manager. Its caller script is:

Definition fields now use named, domain-specific keys. Select an existing definition from the Inspector dropdown or pass the same named key in code. Declare each project key once in a marked key set; ordinary caller methods do not accept raw IDs. Generated keys for asset-authored definitions require no asset reference in the caller. Owner-issued selection and row handles represent runtime instances.

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
