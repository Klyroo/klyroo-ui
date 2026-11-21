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
/// The primary and the neutral color of the app, chosen in the appearance dialog. The choices are kept in the <c>localStorage</c> of the browser and applied once the
/// page is interactive (a prerendered page has the default colors until then). The neutral palette drives the text, background and border tokens, as in Nuxt UI.
/// </summary>
public sealed class AppearanceState(IJSRuntime js)
{
    private const string PrimaryKey = "dashboard-primary";
    private const string NeutralKey = "dashboard-neutral";
    private bool _loaded;

    /// <summary>The palettes the dialog offers, the first one is the default (the Blux colors).</summary>
    public static IReadOnlyList<BxPalette> Palettes { get; } = [BxPalette.Blux, BxPalette.Blue, BxPalette.Emerald, BxPalette.Violet, BxPalette.Rose, BxPalette.Orange];

    /// <summary>The neutral palettes the dialog offers, the first one is the default (the Blux neutrals).</summary>
    public static IReadOnlyList<BxPalette> Neutrals { get; } = [BxPalette.Blux, BxPalette.Slate, BxPalette.Gray, BxPalette.Zinc, BxPalette.Neutral, BxPalette.Stone];

    public BxPalette Primary { get; private set; } = Palettes[0];

    public BxPalette Neutral { get; private set; } = Neutrals[0];

    /// <summary>The semantic colors with the chosen palettes. A new object on each choice, because the options of a render freeze.</summary>
    public BxColorOptions Colors { get; private set; } = Build(Palettes[0], Neutrals[0]);

    /// <summary>Raised after the primary color changed.</summary>
    public event Action? Changed;

    private static BxColorOptions Build(BxPalette primary, BxPalette neutral) => new() { Primary = primary, Neutral = neutral };

    /// <summary>Reads the saved color, once, after the first render in the browser.</summary>
    public async Task LoadAsync()
    {
        if (_loaded) return;
        _loaded = true;
        try
        {
            var savedPrimary = await js.InvokeAsync<string?>("localStorage.getItem", PrimaryKey);
            var savedNeutral = await js.InvokeAsync<string?>("localStorage.getItem", NeutralKey);
            var primary = Palettes.FirstOrDefault(p => p.Name == savedPrimary) ?? Primary;
            var neutral = Neutrals.FirstOrDefault(p => p.Name == savedNeutral) ?? Neutral;
            if (primary != Primary || neutral != Neutral) Apply(primary, neutral);
        }
        catch (Exception error) when (error is JSException or InvalidOperationException or JSDisconnectedException)
        {
            // No storage (a private window, a blocked store): the default color stays.
        }
    }

    public Task SetAsync(BxPalette primary) => SetAsync(primary, Neutral);

    /// <summary>Applies both colors and keeps them in the browser.</summary>
    public async Task SetAsync(BxPalette primary, BxPalette neutral)
    {
        Apply(primary, neutral);
        try
        {
            await js.InvokeVoidAsync("localStorage.setItem", PrimaryKey, primary.Name);
            await js.InvokeVoidAsync("localStorage.setItem", NeutralKey, neutral.Name);
        }
        catch (Exception error) when (error is JSException or InvalidOperationException or JSDisconnectedException)
        {
            // Not kept, but applied for this visit.
        }
    }

    private void Apply(BxPalette primary, BxPalette neutral)
    {
        Primary = primary;
        Neutral = neutral;
        Colors = Build(primary, neutral);
        Changed?.Invoke();
    }
}
