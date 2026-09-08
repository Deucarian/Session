using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Deucarian.Session.Tests
{
    public sealed class SessionConcurrencyTests
    {
        [Test]
        public async Task RefreshCompletingAfterLogoutCannotRestoreAuthenticationOrStorage()
        {
            var store = new InMemorySessionStore(new SessionData("initial"));
            var backend = new DeferredBackend();
            var service = new SessionService(store, backend);
            await service.RestoreAsync();
            var refresh = service.RefreshAsync();
            await service.LogoutAsync();
            backend.Completion.SetResult(SessionResult.Success(new SessionData("obsolete")));
            AssertSuperseded(await refresh);
            Assert.That(service.CurrentSession, Is.Null);
            Assert.That(await store.LoadAsync(), Is.Null);
        }

        [Test]
        public async Task LateRefreshFailureCannotClearANewerLogin()
        {
            var store = new InMemorySessionStore(new SessionData("initial"));
            var backend = new DeferredBackend();
            var service = new SessionService(store, backend, refreshFailurePolicy: SessionRefreshFailurePolicy.ClearSession);
            await service.RestoreAsync();
            var refresh = service.RefreshAsync();
            await service.ReplaceAccessTokenAsync("new-login");
            backend.Completion.SetResult(SessionResult.Failed("expired", "Old refresh token expired."));
            AssertSuperseded(await refresh);
            Assert.That(service.CurrentSession.AccessToken, Is.EqualTo("new-login"));
            Assert.That((await store.LoadAsync()).AccessToken, Is.EqualTo("new-login"));
        }

        [Test]
        public async Task LateLoginCannotUndoLogout()
        {
            var store = new InMemorySessionStore();
            var backend = new DeferredBackend();
            var service = new SessionService(store);
            var login = service.LoginAsync("request", backend);
            await service.LogoutAsync();
            backend.Completion.SetResult(SessionResult.Success(new SessionData("obsolete")));
            AssertSuperseded(await login);
            Assert.That(await store.LoadAsync(), Is.Null);
        }

        [Test]
        public async Task LogoutWaitsForInFlightStorageThenClearsIt()
        {
            var store = new DeferredStore { PauseSave = true };
            var service = new SessionService(store);
            var save = service.ReplaceAccessTokenAsync("obsolete");
            await store.Entered.Task;
            var logout = service.LogoutAsync();
            Assert.That(logout.IsCompleted, Is.False);
            store.Resume.SetResult(true);
            AssertSuperseded(await save);
            Assert.That((await logout).Succeeded, Is.True);
            Assert.That(service.CurrentSession, Is.Null);
            Assert.That(await store.LoadAsync(), Is.Null);
        }

        [Test]
        public async Task RestoreInFlightCannotUndoLogout()
        {
            var store = new DeferredStore { PauseLoad = true, Value = new SessionData("obsolete") };
            var service = new SessionService(store);
            var restore = service.RestoreAsync();
            await store.Entered.Task;
            var logout = service.LogoutAsync();
            store.Resume.SetResult(true);
            AssertSuperseded(await restore);
            await logout;
            Assert.That(service.CurrentSession, Is.Null);
            Assert.That(await store.LoadAsync(), Is.Null);
        }

        [Test]
        public async Task ConcurrentRefreshesShareOneBackendCallAndOneCommit()
        {
            var backend = new DeferredBackend();
            var service = new SessionService(new InMemorySessionStore(new SessionData("initial")), backend);
            await service.RestoreAsync();
            int changes = 0;
            service.SessionChanged += (_, __) => changes++;
            var first = service.RefreshAsync();
            var second = service.RefreshAsync();
            Assert.That(backend.Calls, Is.EqualTo(1));
            backend.Completion.SetResult(SessionResult.Success(new SessionData("refreshed")));
            Assert.That((await first).Succeeded, Is.True);
            Assert.That((await second).Succeeded, Is.True);
            Assert.That(changes, Is.EqualTo(1));
        }

        [Test]
        public async Task CancelingOneRefreshWaiterDoesNotCancelAnother()
        {
            var backend = new DeferredBackend();
            var service = new SessionService(new InMemorySessionStore(new SessionData("initial")), backend);
            await service.RestoreAsync();
            using (var cancellation = new CancellationTokenSource())
            {
                var first = service.RefreshAsync(cancellation.Token);
                var second = service.RefreshAsync();
                cancellation.Cancel();
                try { await first; Assert.Fail("Canceled caller must stop waiting."); }
                catch (OperationCanceledException) { }
                Assert.That(backend.Token.IsCancellationRequested, Is.False);
                backend.Completion.SetResult(SessionResult.Success(new SessionData("refreshed")));
                Assert.That((await second).Succeeded, Is.True);
            }
        }

        private static void AssertSuperseded(SessionResult result)
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Error.Code, Is.EqualTo("operation_superseded"));
        }

        [Test]
        public async Task AlreadyCanceledRefreshDoesNotStartBackendWork()
        {
            var backend = new DeferredBackend();
            var service = new SessionService(new InMemorySessionStore(new SessionData("initial")), backend);
            await service.RestoreAsync();
            using (var canceled = new CancellationTokenSource())
            {
                canceled.Cancel();
                try { await service.RefreshAsync(canceled.Token); Assert.Fail("Expected cancellation."); }
                catch (OperationCanceledException) { }
                Assert.That(backend.Calls, Is.Zero);
            }
        }

        [Test]
        public async Task CancelingLastWaiterContainsThrowingBackendCancellationCallback()
        {
            var backend = new DeferredBackend();
            var service = new SessionService(new InMemorySessionStore(new SessionData("initial")), backend);
            await service.RestoreAsync();
            using (var canceled = new CancellationTokenSource())
            {
                var refresh = service.RefreshAsync(canceled.Token);
                using (backend.Token.Register(() => throw new InvalidOperationException("Bad backend callback")))
                {
                    canceled.Cancel();
                    try { await refresh; Assert.Fail("Expected cancellation."); }
                    catch (OperationCanceledException) { }
                    Assert.That(backend.Token.IsCancellationRequested, Is.True);
                    backend.Completion.SetResult(SessionResult.Success(new SessionData("obsolete")));
                }
            }
            Assert.That(service.CurrentSession.AccessToken, Is.EqualTo("initial"));
        }

        [Test]
        public async Task FailedLoginDoesNotPermanentlyBlockRefreshingTheExistingSession()
        {
            var backend = new DeferredBackend();
            var service = new SessionService(new InMemorySessionStore(new SessionData("initial")), backend);
            await service.RestoreAsync();
            var login = service.LoginAsync("request", backend);
            backend.Completion.SetResult(SessionResult.Failed("rejected", "Login rejected"));
            Assert.That((await login).Succeeded, Is.False);
            await service.RefreshAsync();
            Assert.That(backend.Calls, Is.EqualTo(1));
        }

        [Test]
        public async Task SupersededWriteCannotRemainPersistedWhenNewerLoginFails()
        {
            var store = new DeferredStore { PauseSave = true };
            var backend = new DeferredBackend();
            var service = new SessionService(store);
            var write = service.ReplaceAccessTokenAsync("superseded");
            await store.Entered.Task;
            var login = service.LoginAsync("newer request", backend);
            backend.Completion.SetResult(SessionResult.Failed("rejected", "Login rejected"));
            await login;
            store.Resume.SetResult(true);
            AssertSuperseded(await write);
            Assert.That(await store.LoadAsync(), Is.Null);
            Assert.That(service.CurrentSession, Is.Null);
        }

        private sealed class DeferredBackend : ISessionRefreshService, ISessionLoginService<string>
        {
            public readonly TaskCompletionSource<SessionResult> Completion = new TaskCompletionSource<SessionResult>();
            public int Calls;
            public CancellationToken Token;
            public Task<SessionResult> LoginAsync(string request, CancellationToken cancellationToken = default) => Completion.Task;
            public Task<SessionResult> RefreshAsync(SessionData currentSession, CancellationToken cancellationToken = default)
            {
                Calls++;
                Token = cancellationToken;
                return Completion.Task;
            }
        }

        private sealed class DeferredStore : ISessionStore
        {
            public SessionData Value;
            public bool PauseSave;
            public bool PauseLoad;
            public readonly TaskCompletionSource<bool> Entered = new TaskCompletionSource<bool>();
            public readonly TaskCompletionSource<bool> Resume = new TaskCompletionSource<bool>();
            public async Task SaveAsync(SessionData session, CancellationToken cancellationToken = default)
            {
                if (PauseSave) { Entered.TrySetResult(true); await Resume.Task; }
                Value = session;
            }
            public async Task<SessionData> LoadAsync(CancellationToken cancellationToken = default)
            {
                var value = Value;
                if (PauseLoad) { PauseLoad = false; Entered.TrySetResult(true); await Resume.Task; }
                return value;
            }
            public Task ClearAsync(CancellationToken cancellationToken = default)
            {
                Value = null;
                return Task.CompletedTask;
            }
        }
    }
}
