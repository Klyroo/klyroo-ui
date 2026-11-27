using System.Diagnostics;

namespace Klyroo.UI.Maui;

/// <summary>
/// The auto-dismiss timer of a toast: it can be paused while the pointer or a finger is on the toast, and resumes with at least one second left, as the web toaster does.
/// The wait is injectable so the tests need no clock.
/// </summary>
internal sealed class OverlaysCountdown
{
    private static readonly TimeSpan MinimumResume = TimeSpan.FromSeconds(1);

    private readonly Action _elapsed;
    private readonly Func<TimeSpan, CancellationToken, Task> _delay;
    private CancellationTokenSource? _running;
    private long _started;
    private bool _finished;

    public OverlaysCountdown(TimeSpan duration, Action elapsed, Func<TimeSpan, CancellationToken, Task>? delay = null)
    {
        Remaining = duration;
        _elapsed = elapsed;
        _delay = delay ?? Task.Delay;
    }

    /// <summary>Time left when the countdown was last paused or started.</summary>
    public TimeSpan Remaining { get; private set; }

    /// <summary>True while the countdown runs (started, not paused, not finished).</summary>
    public bool IsRunning => _running is not null;

    public void Start() => Run();

    public void Pause()
    {
        if (_running is null) return;
        Remaining -= Stopwatch.GetElapsedTime(_started);
        Cancel();
    }

    public void Resume()
    {
        if (_running is not null || _finished) return;
        if (Remaining < MinimumResume) Remaining = MinimumResume;
        Run();
    }

    public void Stop()
    {
        _finished = true;
        Cancel();
    }

    private void Run()
    {
        if (_finished || _running is not null) return;
        var source = _running = new CancellationTokenSource();
        _started = Stopwatch.GetTimestamp();
        _ = WaitAsync(Remaining, source);
    }

    private async Task WaitAsync(TimeSpan span, CancellationTokenSource source)
    {
        try
        {
            await _delay(span > TimeSpan.Zero ? span : TimeSpan.Zero, source.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        if (source.IsCancellationRequested || !ReferenceEquals(_running, source)) return;
        _running = null;
        _finished = true;
        source.Dispose();
        _elapsed();
    }

    private void Cancel()
    {
        var source = _running;
        _running = null;
        source?.Cancel();
        source?.Dispose();
    }
}
