namespace Klyroo.UI.Maui;

/// <summary>
/// A text link: a <see cref="Label"/> that opens <see cref="Href"/> in the browser (or in the app registered for its scheme) when it is tapped, and raises <see cref="Clicked"/>.
/// It is muted until it is <see cref="Active"/>, then it takes the primary color.
/// </summary>
public class KlyLink : Label
{
    /// <summary>Identifies <see cref="Href"/>.</summary>
    public static readonly BindableProperty HrefProperty = BindableProperty.Create(nameof(Href), typeof(string), typeof(KlyLink), null, propertyChanged: (b, _, _) => ((KlyLink)b).ApplyHint());
    /// <summary>Identifies <see cref="Target"/>.</summary>
    public static readonly BindableProperty TargetProperty = BindableProperty.Create(nameof(Target), typeof(KlyLinkTarget?), typeof(KlyLink), null, propertyChanged: (b, _, _) => ((KlyLink)b).ApplyHint());
    /// <summary>Identifies <see cref="Active"/>.</summary>
    public static readonly BindableProperty ActiveProperty = BindableProperty.Create(nameof(Active), typeof(bool), typeof(KlyLink), false, propertyChanged: (b, _, _) => ((KlyLink)b).ApplyStyle());
    /// <summary>Identifies <see cref="OpensInNewTabLabel"/>.</summary>
    public static readonly BindableProperty OpensInNewTabLabelProperty = BindableProperty.Create(nameof(OpensInNewTabLabel), typeof(string), typeof(KlyLink), "Opens in a new tab", propertyChanged: (b, _, _) => ((KlyLink)b).ApplyHint());

    /// <summary>Creates a link.</summary>
    public KlyLink()
    {
        FontSize = 14;
        HorizontalOptions = LayoutOptions.Start;
        VerticalTextAlignment = TextAlignment.Center;
        // A touch target of 40, even for a link that is one line of text.
        MinimumHeightRequest = 40;
        DisplayParts.Tap(this, Tap);
        ApplyStyle();
    }

    /// <summary>Raised when the link is tapped, before <see cref="Href"/> opens.</summary>
    public event EventHandler? Clicked;

    /// <summary>The address to open: <c>https://</c> in the browser, <c>mailto:</c>, <c>tel:</c> or another scheme in the app registered for it. A link without an absolute address only raises <see cref="Clicked"/>.</summary>
    public string? Href { get => (string?)GetValue(HrefProperty); set => SetValue(HrefProperty, value); }

    /// <summary>The address as a <see cref="System.Uri"/>: the same value as <see cref="Href"/>.</summary>
    public Uri? Uri
    {
        get => DisplayLaunch.TryParse(Href, out var uri) ? uri : null;
        set => Href = value?.OriginalString;
    }

    /// <summary>Where the address opens: <see cref="KlyLinkTarget.Blank"/> in the external browser, anything else in the in-app browser when the platform has one.</summary>
    public KlyLinkTarget? Target { get => (KlyLinkTarget?)GetValue(TargetProperty); set => SetValue(TargetProperty, value); }

    /// <summary>Marks the link as the current page: it takes the primary color instead of the muted one. There is no address to compare with on a device, so the app says when.</summary>
    public bool Active { get => (bool)GetValue(ActiveProperty); set => SetValue(ActiveProperty, value); }

    /// <summary>What a screen reader adds for a link that opens in the external browser, in the language of the app.</summary>
    public string OpensInNewTabLabel { get => (string)GetValue(OpensInNewTabLabelProperty); set => SetValue(OpensInNewTabLabelProperty, value); }

    /// <summary>Opens addresses instead of the platform; for tests.</summary>
    internal Func<Uri, KlyLinkTarget?, Task>? Opener { get; set; }

    /// <inheritdoc />
    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        // Same dimming as the web link: disabled:opacity-60.
        if (propertyName == nameof(IsEnabled)) Opacity = IsEnabled ? 1 : 0.6;
    }

    /// <summary>What a tap does, for tests: nothing while the link is disabled.</summary>
    internal void SendTapped()
    {
        if (IsEnabled) Tap();
    }

    private void Tap()
    {
        Clicked?.Invoke(this, EventArgs.Empty);
        DisplayLaunch.Open(Href, Target, Opener);
    }

    private void ApplyStyle() => this.SetThemeColor(TextColorProperty, Active ? DisplayParts.PrimaryText : KlyTheme.TextMuted);

    private void ApplyHint() =>
        SemanticProperties.SetHint(this, Href is not null && Target is { IsBlank: true } ? OpensInNewTabLabel : null);
}
