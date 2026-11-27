namespace Klyroo.UI.Maui;

/// <summary>
/// A button of the editor toolbar: the Lucide icon in a rounded face the size of the web button, and a transparent native <see cref="Button"/>
/// over it that takes the tap, the focus and the screen reader. The cell is at least 40 wide and high, whatever the face.
/// </summary>
internal sealed class EditorToolbarButton : Grid
{
    /// <summary>The smallest tappable cell.</summary>
    internal const double TouchTarget = 40;

    private readonly Border _face = new() { StrokeThickness = 0, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
    private readonly KlyIcon _icon = new() { HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center, InputTransparent = true };
    private readonly Button _hit = new()
    {
        BackgroundColor = Colors.Transparent,
        BorderWidth = 0,
        Padding = new Thickness(0),
        MinimumWidthRequest = 0,
        MinimumHeightRequest = 0,
        HorizontalOptions = LayoutOptions.Fill,
        VerticalOptions = LayoutOptions.Fill,
    };

    internal EditorToolbarButton(EditorToolbarItem item)
    {
        Item = item;
        _icon.Name = EditorActionInfo.IconName(item.Action, item.Level);
        _face.SetRadius(KlyTheme.RadiusOf(1) - 2);
        _face.Content = _icon;
        _hit.Clicked += (_, _) => Clicked?.Invoke(this, EventArgs.Empty);

        var label = item.Label ?? EditorActionInfo.Label(item.Action, item.Level);
        SemanticProperties.SetDescription(_hit, label);
        ToolTipProperties.SetText(this, label);

        Children.Add(_face);
        Children.Add(_hit);
    }

    internal EditorToolbarItem Item { get; }

    internal bool Pressed { get; private set; }

    internal bool Enabled => _hit.IsEnabled;

    internal event EventHandler? Clicked;

    internal Button Hit => _hit;

    /// <summary>Size of the face, as the web <c>size-6</c> to <c>size-10</c> of the button.</summary>
    internal static double FaceOf(KlySize size) => size switch
    {
        KlySize.Xs => 24,
        KlySize.Sm => 28,
        KlySize.Md => 32,
        KlySize.Lg => 36,
        KlySize.Xl => 40,
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, null),
    };

    internal void Update(KlySize size, KlyColor color, bool pressed, bool enabled)
    {
        Pressed = pressed;
        var face = FaceOf(size);
        WidthRequest = HeightRequest = Math.Max(TouchTarget, face);
        _face.WidthRequest = _face.HeightRequest = face;
        // The web icons are 14 to 20: the closest native size.
        _icon.Size = size is KlySize.Lg or KlySize.Xl ? KlySize.Md : KlySize.Xs;

        var set = KlyTheme.Resolve(color);
        if (pressed)
        {
            // aria-pressed on the web: the tint and the ring of a subtle surface.
            _face.Apply(Surface.For(ButtonVariant.Subtle, set));
            _icon.IconColor = set.Text;
        }
        else
        {
            _face.Apply(new Surface(KlyThemeColor.Transparent, KlyThemeColor.Transparent, 0, KlyTheme.TextMuted));
            _icon.IconColor = KlyTheme.TextMuted;
        }

        _hit.IsEnabled = enabled;
        // disabled:opacity-40 on the web.
        Opacity = enabled ? 1 : 0.4;
        if (EditorActionInfo.IsToggle(Item.Action)) SemanticProperties.SetHint(_hit, pressed ? "Active" : null);
    }
}
