using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace Deucarian.Session.Tests
{
    public sealed class SessionHostTests
    {
        [Test]
        public async Task HostUsesSuppliedSessionAndLeavesItUsableAfterDestruction()
        {
            var service = new SessionService(new InMemorySessionStore());
            var go = new GameObject("session");
            try
            {
                var host = go.AddComponent<SessionHost>();
                host.Configure(service);
                Assert.That((await host.RestoreAsync()).Succeeded, Is.True);
                await service.ReplaceAccessTokenAsync("test-token");
                Assert.That((await host.LogoutAsync()).Succeeded, Is.True);
                Assert.That(service.IsAuthenticated, Is.False);
                Object.DestroyImmediate(go);
                Assert.That((await service.RestoreAsync()).Succeeded, Is.True);
            }
            finally { if (go != null) Object.DestroyImmediate(go); }
        }
    }
}
