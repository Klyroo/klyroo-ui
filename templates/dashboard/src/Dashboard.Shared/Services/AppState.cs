using Lux.UI;

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
/// The primary and the neutral color of the app, chosen in the appearance dialog. The neutral palette drives the text, background and border tokens, as in Nuxt UI.
/// </summary>
/// <remarks>
/// Where the choice is kept is the business of an <see cref="IAppearanceStore"/>: a cookie on the web (written by the browser, read by the server while it renders, so the
/// first paint already has the colors) and the local storage of the web view in the MAUI app. A render that has a request (a static render, a prerender) reads the cookie
/// through <see cref="ILxRequestCookies"/> when the constructor runs; the layout hands the result to the interactive render (<see cref="Restore"/>) so that it does not start
/// from the defaults, and <see cref="LoadAsync"/> reads the store once the browser is there. A value that is not the name of an offered palette is ignored.
/// </remarks>
public sealed class AppearanceState
{
    private readonly IAppearanceStore _store;
    private bool _loaded;

    /// <summary>The palettes the dialog offers, the first one is the default (the Lux colors).</summary>
    public static IReadOnlyList<LxPalette> Palettes { get; } = [LxPalette.Lux, LxPalette.Blue, LxPalette.Emerald, LxPalette.Violet, LxPalette.Rose, LxPalette.Orange];

    /// <summary>The neutral palettes the dialog offers, the first one is the default (the Lux neutrals).</summary>
    public static IReadOnlyList<LxPalette> Neutrals { get; } = [LxPalette.Lux, LxPalette.Slate, LxPalette.Gray, LxPalette.Zinc, LxPalette.Neutral, LxPalette.Stone];

    /// <param name="store">Where the choice is kept.</param>
    /// <param name="cookies">The cookies of the request, when there is one (the web host calls <c>AddLuxServer()</c>); null in WebAssembly and in the MAUI app.</param>
    public AppearanceState(IAppearanceStore store, ILxRequestCookies? cookies = null)
    {
        _store = store;
        if (cookies is not null)
        {
            Restore(cookies.Get(AppearanceCookies.Name(AppearanceCookies.PrimaryKey, cookies.PathBase)), cookies.Get(AppearanceCookies.Name(AppearanceCookies.NeutralKey, cookies.PathBase)));
        }
    }

    public LxPalette Primary { get; private set; } = Palettes[0];

    public LxPalette Neutral { get; private set; } = Neutrals[0];

    /// <summary>The semantic colors with the chosen palettes. A new object on each choice, because the options of a render freeze.</summary>
    public LxColorOptions Colors { get; private set; } = Build(Palettes[0], Neutrals[0]);

    /// <summary>Raised after the colors changed.</summary>
    public event Action? Changed;

    private static LxColorOptions Build(LxPalette primary, LxPalette neutral) => new() { Primary = primary, Neutral = neutral };

    /// <summary>
    /// Takes the names of the palettes as they were read from a cookie or handed over by a prerender. A name that is not on offer (a cookie that was edited) leaves that color
    /// as it is. Raises nothing: it is for the start of a render, before anything listens.
    /// </summary>
    public void Restore(string? primary, string? neutral)
    {
        Primary = Palettes.FirstOrDefault(p => p.Name == primary) ?? Primary;
        Neutral = Neutrals.FirstOrDefault(p => p.Name == neutral) ?? Neutral;
        Colors = Build(Primary, Neutral);
    }

    /// <summary>Reads the store, once, after the first render in the browser.</summary>
    public async Task LoadAsync()
    {
        if (_loaded) return;
        _loaded = true;
        try
        {
            var (primary, neutral) = await _store.LoadAsync();
            var before = (Primary, Neutral);
            Restore(primary, neutral);
            if ((Primary, Neutral) != before) Changed?.Invoke();
        }
        catch (Exception error) when (error is Microsoft.JSInterop.JSException or InvalidOperationException or Microsoft.JSInterop.JSDisconnectedException)
        {
            // No store (a private window, a blocked cookie): the defaults stay.
        }
    }

    public Task SetAsync(LxPalette primary) => SetAsync(primary, Neutral);

    /// <summary>Applies both colors and keeps them in the store.</summary>
    public async Task SetAsync(LxPalette primary, LxPalette neutral)
    {
        Primary = primary;
        Neutral = neutral;
        Colors = Build(primary, neutral);
        Changed?.Invoke();
        try
        {
            await _store.SaveAsync(primary.Name, neutral.Name);
        }
        catch (Exception error) when (error is Microsoft.JSInterop.JSException or InvalidOperationException or Microsoft.JSInterop.JSDisconnectedException)
        {
            // Not kept, but applied for this visit.
        }
    }
}
