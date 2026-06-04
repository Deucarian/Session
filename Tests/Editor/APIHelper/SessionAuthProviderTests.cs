using System;
using System.Threading;
using System.Threading.Tasks;
using JorisHoef.SessionHelper.APIHelper;
using NUnit.Framework;

namespace JorisHoef.SessionHelper.APIHelper.Tests
{
    public sealed class SessionAuthProviderTests
    {
        private static readonly DateTimeOffset Now = new DateTimeOffset(2026, 6, 4, 12, 0, 0, TimeSpan.Zero);

        [Test]
        public async Task ApiHelperAdapterReturnsBearerTokenWhenAuthenticated()
        {
            var store = new InMemorySessionStore(
                new SessionData("adapter-token", "refresh-token", Now.AddMinutes(5)));
            var service = new SessionService(store, utcNowProvider: () => Now);
            await service.RestoreAsync();
            var provider = new SessionAuthProvider(service);

            string token = await provider.GetAccessTokenAsync(default(CancellationToken));

            Assert.AreEqual("adapter-token", token);
        }

        [Test]
        public async Task ApiHelperAdapterReturnsNoAuthHeaderWhenUnauthenticated()
        {
            var service = new SessionService(new InMemorySessionStore(), utcNowProvider: () => Now);
            var provider = new SessionAuthProvider(service);

            string token = await provider.GetAccessTokenAsync(default(CancellationToken));

            Assert.IsNull(token);
        }
    }
}
