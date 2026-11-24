namespace Lux.UI.Images.Local;

/// <summary>
/// Runs one computation per key however many callers ask for it at the same time; the others await the same result.
/// The work gets its own cancellation token, canceled only when every caller has gone away, so one client disconnecting
/// does not abort a result that others wait for, and a result nobody waits for any more stops early.
/// </summary>
internal sealed class LxFlights<T>
{
    private sealed class Flight
    {
        public Task<T> Task = null!;
        public readonly CancellationTokenSource Source = new();
        public int Waiters;
    }

    private readonly Dictionary<string, Flight> _flights = new(StringComparer.Ordinal);
    private readonly object _gate = new();

    public async Task<T> RunAsync(string key, Func<CancellationToken, Task<T>> work, CancellationToken caller)
    {
        Flight flight;
        lock (_gate)
        {
            if (!_flights.TryGetValue(key, out flight!))
            {
                var created = flight = new Flight();
                _flights[key] = created;
                created.Task = Task.Run(async () =>
                {
                    try
                    {
                        return await work(created.Source.Token).ConfigureAwait(false);
                    }
                    finally
                    {
                        lock (_gate)
                        {
                            if (_flights.TryGetValue(key, out var current) && ReferenceEquals(current, created)) _flights.Remove(key);
                        }
                    }
                });
                _ = created.Task.ContinueWith(static t => _ = t.Exception, TaskContinuationOptions.OnlyOnFaulted);
            }
            flight.Waiters++;
        }

        try
        {
            return await flight.Task.WaitAsync(caller).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (caller.IsCancellationRequested)
        {
            lock (_gate)
            {
                if (--flight.Waiters == 0)
                {
                    if (_flights.TryGetValue(key, out var current) && ReferenceEquals(current, flight)) _flights.Remove(key);
                    flight.Source.Cancel();
                }
            }
            throw;
        }
    }
}
