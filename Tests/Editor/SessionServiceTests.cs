using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Deucarian.Session.Tests
{
    public sealed class SessionServiceTests
    {
        private static readonly DateTimeOffset Now = new DateTimeOffset(2026, 6, 4, 12, 0, 0, TimeSpan.Zero);

        [Test]
        public void LoginSuccessStoresSession()
        {
            RunAsync(async () =>
            {
            var store = new InMemorySessionStore();
            var expectedSession = CreateSession("login-token");
            var service = CreateService(store);
            var loginService = new StubLoginService<LoginRequest>(SessionResult.Success(expectedSession));

            SessionResult result = await service.LoginAsync(new LoginRequest(), loginService);
            SessionData storedSession = await store.LoadAsync();

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(SessionState.Authenticated, service.State);
            Assert.AreEqual(expectedSession, service.CurrentSession);
            Assert.AreEqual(expectedSession, storedSession);
            });
        }

        [Test]
        public void LogoutClearsSession()
        {
            RunAsync(async () =>
            {
            var store = new InMemorySessionStore();
            var service = CreateService(store);
            await service.LoginAsync(
                new LoginRequest(),
                new StubLoginService<LoginRequest>(SessionResult.Success(CreateSession("logout-token"))));

            SessionResult result = await service.LogoutAsync();
            SessionData storedSession = await store.LoadAsync();

            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(service.CurrentSession);
            Assert.IsNull(storedSession);
            Assert.AreEqual(SessionState.Unauthenticated, service.State);
            });
        }

        [Test]
        public void RestoreLoadsSession()
        {
            RunAsync(async () =>
            {
            SessionData expectedSession = CreateSession("restore-token");
            var store = new InMemorySessionStore(expectedSession);
            var service = CreateService(store);

            SessionResult result = await service.RestoreAsync();

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(expectedSession, result.Session);
            Assert.AreEqual(expectedSession, service.CurrentSession);
            Assert.AreEqual(SessionState.Authenticated, service.State);
            });
        }

        [Test]
        public void ExpiredTokenIsDetected()
        {
            RunAsync(async () =>
            {
            SessionData expiredSession = CreateSession("expired-token", Now.AddSeconds(-1));
            var store = new InMemorySessionStore(expiredSession);
            var service = CreateService(store);

            await service.RestoreAsync();

            Assert.AreEqual(SessionState.Expired, service.State);
            Assert.IsTrue(service.IsAccessTokenExpired);
            Assert.IsTrue(service.IsAccessTokenExpiringSoon);
            Assert.IsFalse(service.IsAuthenticated);
            });
        }

        [Test]
        public void RefreshUpdatesSession()
        {
            RunAsync(async () =>
            {
            SessionData originalSession = CreateSession("old-token");
            SessionData refreshedSession = CreateSession("new-token");
            var store = new InMemorySessionStore(originalSession);
            var refreshService = new StubRefreshService(SessionResult.Success(refreshedSession));
            var service = CreateService(store, refreshService);
            await service.RestoreAsync();

            SessionResult result = await service.RefreshAsync();
            SessionData storedSession = await store.LoadAsync();

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(refreshedSession, result.Session);
            Assert.AreEqual(refreshedSession, service.CurrentSession);
            Assert.AreEqual(refreshedSession, storedSession);
            });
        }

        [TestCase(SessionRefreshFailurePolicy.PreserveSession, false)]
        [TestCase(SessionRefreshFailurePolicy.ClearSession, true)]
        public void FailedRefreshClearsOrPreservesSessionBasedOnPolicy(
            SessionRefreshFailurePolicy policy,
            bool expectCleared)
        {
            RunAsync(async () =>
            {
            SessionData originalSession = CreateSession("refresh-failure-token");
            var store = new InMemorySessionStore(originalSession);
            var refreshService = new StubRefreshService(SessionResult.Failed("refresh_failed", "Refresh failed."));
            var service = CreateService(store, refreshService, policy);
            await service.RestoreAsync();

            SessionResult result = await service.RefreshAsync();
            SessionData storedSession = await store.LoadAsync();

            Assert.IsFalse(result.Succeeded);
            if (expectCleared)
            {
                Assert.IsNull(service.CurrentSession);
                Assert.IsNull(storedSession);
                Assert.AreEqual(SessionState.Unauthenticated, service.State);
            }
            else
            {
                Assert.AreEqual(originalSession, service.CurrentSession);
                Assert.AreEqual(originalSession, storedSession);
                Assert.AreEqual(SessionState.Authenticated, service.State);
            }
            });
        }

        [Test]
        public void SessionChangedEventFires()
        {
            RunAsync(async () =>
            {
            var store = new InMemorySessionStore();
            var service = CreateService(store);
            var expectedSession = CreateSession("event-token");
            SessionChangedEventArgs receivedArgs = null;
            int eventCount = 0;

            service.SessionChanged += (sender, args) =>
            {
                eventCount++;
                receivedArgs = args;
            };

            await service.LoginAsync(
                new LoginRequest(),
                new StubLoginService<LoginRequest>(SessionResult.Success(expectedSession)));

            Assert.AreEqual(1, eventCount);
            Assert.IsNotNull(receivedArgs);
            Assert.AreEqual(SessionState.Unauthenticated, receivedArgs.PreviousState);
            Assert.AreEqual(SessionState.Authenticated, receivedArgs.CurrentState);
            Assert.AreEqual(SessionChangeReason.LoggedIn, receivedArgs.Reason);
            Assert.AreEqual(expectedSession, receivedArgs.CurrentSession);
            });
        }

        private static void RunAsync(Func<Task> asyncTest)
        {
            asyncTest().GetAwaiter().GetResult();
        }

        private static SessionService CreateService(
            ISessionStore store,
            ISessionRefreshService refreshService = null,
            SessionRefreshFailurePolicy policy = SessionRefreshFailurePolicy.PreserveSession)
        {
            return new SessionService(
                store,
                refreshService,
                TimeSpan.FromMinutes(1),
                policy,
                () => Now);
        }

        private static SessionData CreateSession(string accessToken)
        {
            return CreateSession(accessToken, Now.AddMinutes(10));
        }

        private static SessionData CreateSession(string accessToken, DateTimeOffset expiresAtUtc)
        {
            return new SessionData(accessToken, "refresh-token", expiresAtUtc);
        }

        private sealed class LoginRequest
        {
        }

        private sealed class StubLoginService<TLoginRequest> : ISessionLoginService<TLoginRequest>
        {
            private readonly SessionResult result;

            public StubLoginService(SessionResult result)
            {
                this.result = result;
            }

            public Task<SessionResult> LoginAsync(
                TLoginRequest request,
                CancellationToken cancellationToken = default(CancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Task.FromResult(result);
            }
        }

        private sealed class StubRefreshService : ISessionRefreshService
        {
            private readonly SessionResult result;

            public StubRefreshService(SessionResult result)
            {
                this.result = result;
            }

            public Task<SessionResult> RefreshAsync(
                SessionData currentSession,
                CancellationToken cancellationToken = default(CancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Task.FromResult(result);
            }
        }
    }
}
