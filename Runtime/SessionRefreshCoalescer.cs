using System;
using System.Threading;
using System.Threading.Tasks;

namespace Deucarian.Session
{
    internal sealed class SessionRefreshCoalescer
    {
        private readonly object gate = new object();
        private Flight active;

        public Task<SessionResult> RunAsync(long generation, Func<CancellationToken, Task<SessionResult>> refresh, CancellationToken token)
        {
            if (token.IsCancellationRequested) return Task.FromCanceled<SessionResult>(token);
            Flight flight;
            bool start;
            lock (gate)
            {
                start = active == null || active.Generation != generation || active.Abandoned || active.Completion.Task.IsCompleted;
                if (start) active = new Flight(generation);
                flight = active;
                flight.Waiters++;
            }
            if (start) _ = CompleteAsync(flight, refresh);
            return AwaitAsync(flight, token);
        }

        private async Task CompleteAsync(Flight flight, Func<CancellationToken, Task<SessionResult>> refresh)
        {
            try { flight.Completion.TrySetResult(await refresh(flight.Cancellation.Token)); }
            catch (OperationCanceledException) { flight.Completion.TrySetCanceled(); }
            catch (Exception exception)
            {
                flight.Completion.TrySetException(exception);
                _ = flight.Completion.Task.Exception;
            }
            finally
            {
                lock (gate)
                {
                    if (ReferenceEquals(active, flight)) active = null;
                    flight.Completed = true;
                    DisposeIfIdle(flight);
                }
            }
        }

        private async Task<SessionResult> AwaitAsync(Flight flight, CancellationToken token)
        {
            try
            {
                if (!token.CanBeCanceled) return await flight.Completion.Task;
                var canceled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                using (token.Register(() => canceled.TrySetResult(true)))
                {
                    await Task.WhenAny(flight.Completion.Task, canceled.Task);
                    token.ThrowIfCancellationRequested();
                    return await flight.Completion.Task;
                }
            }
            finally
            {
                bool cancel = false;
                lock (gate)
                {
                    if (--flight.Waiters == 0)
                    {
                        if (!flight.Completion.Task.IsCompleted)
                        {
                            flight.Abandoned = true;
                            flight.Canceling = cancel = true;
                        }
                        DisposeIfIdle(flight);
                    }
                }
                if (cancel)
                {
                    try { flight.Cancellation.Cancel(); }
                    catch (AggregateException exception)
                    {
                        flight.Completion.TrySetException(exception);
                        _ = flight.Completion.Task.Exception;
                    }
                    finally
                    {
                        lock (gate)
                        {
                            flight.Canceling = false;
                            DisposeIfIdle(flight);
                        }
                    }
                }
            }
        }

        private static void DisposeIfIdle(Flight flight)
        {
            if (!flight.Completed || flight.Waiters != 0 || flight.Canceling || flight.Disposed) return;
            flight.Disposed = true;
            flight.Cancellation.Dispose();
        }

        private sealed class Flight
        {
            public readonly long Generation;
            public readonly CancellationTokenSource Cancellation = new CancellationTokenSource();
            public readonly TaskCompletionSource<SessionResult> Completion = new TaskCompletionSource<SessionResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            public int Waiters;
            public bool Abandoned;
            public bool Completed;
            public bool Canceling;
            public bool Disposed;
            public Flight(long generation) => Generation = generation;
        }
    }
}
