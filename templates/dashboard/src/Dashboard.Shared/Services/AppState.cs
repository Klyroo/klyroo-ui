using Blux.UI;
using Microsoft.JSInterop;

namespace Dashboard.Shared.Services;

/// <summary>
/// What the panels of the layout share: the notifications slideover and the appearance dialog are in the layout (one each), and the buttons of every page, the user menu
/// and the command palette open them through this service. One per user session.
/// </summary>
public sealed class AppState
{
    /// <summary>Raised when something asks to open the notifications slideover.</summary>
    public event Action? NotificationsRequested;

    /// <summary>Raised when something asks to open the appearance dialog.</summary>
    public event Action? AppearanceRequested;

    public void OpenNotifications() => NotificationsRequested?.Invoke();

    public void OpenAppearance() => AppearanceRequested?.Invoke();
}

/// <summary>
/// The primary color of the app, chosen in the appearance dialog. The choice is kept in the <c>localStorage</c> of the browser and applied once the page is interactive
/// (a prerendered page has the default color until then). Everything else about the colors (the neutral palette, the semantic colors) is set once, in <c>AddDashboardUi</c>.
/// </summary>
public sealed class AppearanceState(IJSRuntime js)
{
    private const string StorageKey = "dashboard-primary";
    private bool _loaded;

    /// <summary>The palettes the dialog offers, the first one is the default (the Blux colors).</summary>
    public static IReadOnlyList<BxPalette> Palettes { get; } = [BxPalette.Blux, BxPalette.Blue, BxPalette.Emerald, BxPalette.Violet, BxPalette.Rose, BxPalette.Orange];

    public BxPalette Primary { get; private set; } = Palettes[0];

    /// <summary>The semantic colors with the chosen primary. A new object on each choice, because the options of a render freeze.</summary>
    public BxColorOptions Colors { get; private set; } = Build(Palettes[0]);

    /// <summary>Raised after the primary color changed.</summary>
    public event Action? Changed;

    private static BxColorOptions Build(BxPalette primary) => new() { Primary = primary };

    /// <summary>Reads the saved color, once, after the first render in the browser.</summary>
    public async Task LoadAsync()
    {
        if (_loaded) return;
        _loaded = true;
        try
        {
            var saved = await js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            var palette = Palettes.FirstOrDefault(p => p.Name == saved);
            if (palette is not null && palette != Primary) Apply(palette);
        }
        catch (Exception error) when (error is JSException or InvalidOperationException or JSDisconnectedException)
        {
            // No storage (a private window, a blocked store): the default color stays.
        }
    }

    public async Task SetAsync(BxPalette palette)
    {
        Apply(palette);
        try
        {
            await js.InvokeVoidAsync("localStorage.setItem", StorageKey, palette.Name);
        }
        catch (Exception error) when (error is JSException or InvalidOperationException or JSDisconnectedException)
        {
            // Not kept, but applied for this visit.
        }
    }

    private void Apply(BxPalette palette)
    {
        Primary = palette;
        Colors = Build(palette);
        Changed?.Invoke();
    }
}
