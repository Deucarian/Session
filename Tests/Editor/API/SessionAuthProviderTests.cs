using System;
using System.Threading;
using System.Threading.Tasks;
using Deucarian.Session.API;
using NUnit.Framework;

namespace Deucarian.Session.API.Tests
{
    public sealed class SessionAuthProviderTests
    {
        private static readonly DateTimeOffset Now = new DateTimeOffset(2026, 6, 4, 12, 0, 0, TimeSpan.Zero);

        [Test]
        public void ApiAdapterReturnsBearerTokenWhenAuthenticated()
        {
            RunAsync(async () =>
            {
            var store = new InMemorySessionStore(
                new SessionData("adapter-token", "refresh-token", Now.AddMinutes(5)));
            var service = new SessionService(store, utcNowProvider: () => Now);
            await service.RestoreAsync();
            var provider = new SessionAuthProvider(service);

            string token = await provider.GetAccessTokenAsync(default(CancellationToken));

            Assert.AreEqual("adapter-token", token);
            });
        }

        [Test]
        public void ApiAdapterReturnsNoAuthHeaderWhenUnauthenticated()
        {
            RunAsync(async () =>
            {
            var service = new SessionService(new InMemorySessionStore(), utcNowProvider: () => Now);
            var provider = new SessionAuthProvider(service);

            string token = await provider.GetAccessTokenAsync(default(CancellationToken));

            Assert.IsNull(token);
            });
        }

        private static void RunAsync(Func<Task> asyncTest)
        {
            asyncTest().GetAwaiter().GetResult();
        }
    }
}
