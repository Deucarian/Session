using System;
using System.Threading;
using System.Threading.Tasks;

namespace Deucarian.Session
{
    internal sealed class SessionCommitCoordinator
    {
        private readonly ISessionStore store;
        private readonly object stateGate = new object();
        private readonly SemaphoreSlim storageGate = new SemaphoreSlim(1, 1);
        private long generation;
        private long committedGeneration;
        private SessionData current;

        public SessionCommitCoordinator(ISessionStore store) => this.store = store;

        public event Action<SessionData, SessionData, SessionChangeReason> Changed;
        public SessionData Current { get { lock (stateGate) return current; } }

        public long Begin()
        {
            lock (stateGate) return ++generation;
        }

        public long Begin(out SessionData previous)
        {
            lock (stateGate) { previous = current; return ++generation; }
        }

        public long Capture(out SessionData session)
        {
            lock (stateGate) { session = current; return committedGeneration; }
        }

        public bool IsCurrent(long expected)
        {
            lock (stateGate) return expected == generation;
        }

        public void Finish(long expected)
        {
            lock (stateGate)
            {
                if (generation == expected) committedGeneration = ++generation;
            }
        }

        public Task<SessionResult> RestoreAsync(long expected, CancellationToken token) =>
            ExecuteAsync(expected, null, SessionChangeReason.Restored, true, token);

        public Task<SessionResult> CommitAsync(long expected, SessionData session, SessionChangeReason reason, CancellationToken token) =>
            ExecuteAsync(expected, session, reason, false, token);

        public static SessionResult Superseded() =>
            SessionResult.Failed("operation_superseded", "A newer session operation replaced this request.");

        private async Task<SessionResult> ExecuteAsync(long expected, SessionData session,
            SessionChangeReason reason, bool restore, CancellationToken token)
        {
            try { await storageGate.WaitAsync(token); }
            catch { Finish(expected); throw; }
            SessionData previous;
            try
            {
                if (!IsCurrent(expected)) return Superseded();
                try
                {
                    if (restore) session = await store.LoadAsync(token);
                    else if (session == null) await store.ClearAsync(token);
                    else await store.SaveAsync(session, token);
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception exception)
                {
                    string operation = restore ? "load" : session == null ? "clear" : "save";
                    return SessionResult.Failed("store_" + operation + "_exception",
                        "Failed to " + operation + " the session.", exception);
                }

                bool applied;
                lock (stateGate)
                {
                    previous = current;
                    applied = expected == generation && !token.IsCancellationRequested;
                    if (applied)
                    {
                        current = session;
                        committedGeneration = ++generation;
                    }
                }
                if (!applied)
                {
                    if (!restore)
                    {
                        try
                        {
                            if (previous == null) await store.ClearAsync(CancellationToken.None);
                            else await store.SaveAsync(previous, CancellationToken.None);
                        }
                        catch (Exception exception)
                        {
                            return SessionResult.Failed("store_rollback_exception", "A superseded session write could not restore the previously committed session.", exception);
                        }
                    }
                    token.ThrowIfCancellationRequested();
                    return Superseded();
                }
            }
            finally { Finish(expected); storageGate.Release(); }

            if (!Equals(previous, session)) Changed?.Invoke(previous, session, reason);
            return session == null ? SessionResult.Success() : SessionResult.Success(session);
        }
    }
}
