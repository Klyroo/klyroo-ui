using System.Diagnostics;

/// <summary>
/// Startup hook of the template demos (the runtime looks for a type named <c>StartupHook</c> in the global namespace). The docs host that started a
/// demo may die without a chance to stop it (SIGKILL, the out of memory killer), and a child process outlives its parent, so the demo watches the host:
/// it exits when its standard input, a pipe the host holds open, reaches the end (the operating system closes it with the host), and, as a fallback,
/// when the process whose id is in <c>BLUX_WATCHDOG_PARENT_PID</c> is gone. It does nothing when neither variable is set.
/// </summary>
internal static class StartupHook
{
    private static Timer? _killer;

    public static void Initialize()
    {
        if (Environment.GetEnvironmentVariable("BLUX_WATCHDOG_STDIN") == "1")
        {
            Start("blux-watchdog-stdin", WatchStandardInput);
        }
        if (int.TryParse(Environment.GetEnvironmentVariable("BLUX_WATCHDOG_PARENT_PID"), out var parent) && parent > 0)
        {
            Start("blux-watchdog-parent", () => WatchParent(parent));
        }
    }

    private static void Start(string name, ThreadStart body) =>
        new Thread(body) { IsBackground = true, Name = name }.Start();

    private static void WatchStandardInput()
    {
        try
        {
            using var input = Console.OpenStandardInput();
            var buffer = new byte[256];
            while (input.Read(buffer, 0, buffer.Length) > 0)
            {
                // Nothing is expected on it: reading is only a way to see the pipe close.
            }
        }
        catch (Exception)
        {
            // A broken pipe is the same news as the end of it.
        }
        Exit();
    }

    private static void WatchParent(int parent)
    {
        while (true)
        {
            Thread.Sleep(2000);
            try
            {
                using var process = Process.GetProcessById(parent);
                if (process.HasExited) break;
            }
            catch (ArgumentException)
            {
                break;
            }
            catch (InvalidOperationException)
            {
                break;
            }
        }
        Exit();
    }

    /// <summary>A graceful exit, and the kill that follows when something keeps the process from ending.</summary>
    private static void Exit()
    {
        _killer = new Timer(_ => Process.GetCurrentProcess().Kill(), null, TimeSpan.FromSeconds(5), Timeout.InfiniteTimeSpan);
        Environment.Exit(1);
    }
}
