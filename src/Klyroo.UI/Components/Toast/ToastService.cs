using Microsoft.JSInterop;

namespace Klyroo.UI;

/// <summary>A notification shown by <see cref="KlyToaster"/>.</summary>
public sealed record KlyToast
{
    /// <summary>Title of the toast.</summary>
    public string? Title { get; init; }
    /// <summary>Text under the title.</summary>
    public string? Description { get; init; }
    /// <summary>Semantic color of the toast. Defaults to neutral.</summary>
    public KlyColor Color { get; init; } = KlyColor.Neutral;

    /// <summary>Time before the toast closes itself; hovering or focusing it pauses the countdown. Zero keeps it open.</summary>
    public TimeSpan? Duration { get; init; }
}

/// <summary>Shows toasts in the page's <see cref="KlyToaster"/>. Needs an interactive render mode.</summary>
public interface IToastService
{
    /// <summary>Shows a toast in the <see cref="KlyToaster"/> of the page.</summary>
    ValueTask Add(KlyToast toast);
}

internal sealed class ToastService(IJSRuntime js) : IToastService
{
    // Primitive arguments only: they serialize without reflection, so trimming cannot break them.
    public ValueTask Add(KlyToast toast) =>
        js.InvokeVoidAsync("Klyroo.toast", toast.Title, toast.Description, toast.Color.Value,
            toast.Duration is { } duration ? (int)duration.TotalMilliseconds : null);
}
