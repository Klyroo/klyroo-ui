namespace Klyroo.UI.Maui;

/// <summary>
/// Shows toasts in the newest <see cref="KlyToaster"/> that is in a page. Register it as a singleton for dependency injection
/// (<c>builder.Services.AddSingleton&lt;IToastService, KlyToastService&gt;()</c>), or use <see cref="Default"/>, which every <see cref="KlyToaster"/> serves unless it is given another service.
/// </summary>
public sealed class KlyToastService : IToastService
{
    private readonly List<KlyToaster> _toasters = [];
    private readonly object _lock = new();

    /// <summary>The service that toasters without their own <see cref="KlyToaster.Service"/> attach to.</summary>
    public static KlyToastService Default { get; } = new();

    /// <summary>The number of toasters that serve this service.</summary>
    internal int ToasterCount
    {
        get
        {
            lock (_lock) return _toasters.Count;
        }
    }

    /// <inheritdoc />
    public ValueTask Add(KlyToast toast)
    {
        ArgumentNullException.ThrowIfNull(toast);
        KlyToaster? toaster;
        lock (_lock) toaster = _toasters.Count > 0 ? _toasters[^1] : null;
        if (toaster is null)
        {
            throw new InvalidOperationException("No KlyToaster is attached: place a KlyToaster in a page, inside a KlyOverlayHost, before you add toasts.");
        }
        toaster.Show(toast);
        return ValueTask.CompletedTask;
    }

    internal void Attach(KlyToaster toaster)
    {
        lock (_lock)
        {
            _toasters.Remove(toaster);
            _toasters.Add(toaster);
        }
    }

    internal void Detach(KlyToaster toaster)
    {
        lock (_lock) _toasters.Remove(toaster);
    }
}
