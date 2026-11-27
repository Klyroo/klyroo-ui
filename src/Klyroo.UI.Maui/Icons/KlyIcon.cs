namespace Klyroo.UI.Maui;

/// <summary>An icon by name, drawn as vector paths: the Lucide icons built into the library, and the ones registered with <see cref="KlyIcons.Register"/>.</summary>
public class KlyIcon : ThemedGraphicsView
{
    /// <summary>Identifies <see cref="Name"/>.</summary>
    public static readonly BindableProperty NameProperty = BindableProperty.Create(nameof(Name), typeof(string), typeof(KlyIcon), "", propertyChanged: (b, _, _) => ((KlyIcon)b).Refresh());
    /// <summary>Identifies <see cref="Size"/>.</summary>
    public static readonly BindableProperty SizeProperty = BindableProperty.Create(nameof(Size), typeof(KlySize), typeof(KlyIcon), KlySize.Md, propertyChanged: (b, _, _) => ((KlyIcon)b).Refresh());
    /// <summary>Identifies <see cref="IconColor"/>.</summary>
    public static readonly BindableProperty IconColorProperty = BindableProperty.Create(nameof(IconColor), typeof(KlyThemeColor?), typeof(KlyIcon), null, propertyChanged: (b, _, _) => ((KlyIcon)b).Invalidate());
    /// <summary>Identifies <see cref="Label"/>.</summary>
    public static readonly BindableProperty LabelProperty = BindableProperty.Create(nameof(Label), typeof(string), typeof(KlyIcon), null, propertyChanged: (b, _, _) => ((KlyIcon)b).Refresh());

    private PathF[] _paths = [];

    /// <summary>Creates an empty icon: set <see cref="Name"/>.</summary>
    public KlyIcon()
    {
        HorizontalOptions = LayoutOptions.Start;
        VerticalOptions = LayoutOptions.Center;
        Refresh();
    }

    /// <summary>The icon: a Lucide name such as <c>search</c> (also <c>i-lucide-search</c> and <c>lucide:search</c>), or a name registered with <see cref="KlyIcons.Register"/>. An unknown name draws nothing. See <see cref="KlyIconNames"/>.</summary>
    public string Name { get => (string)GetValue(NameProperty); set => SetValue(NameProperty, value); }

    /// <summary>Size of the icon: 16 for <c>Xs</c> and <c>Sm</c>, 20 for <c>Md</c> and <c>Lg</c>, 24 for <c>Xl</c>, as the web icon.</summary>
    public KlySize Size { get => (KlySize)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    /// <summary>Color of the strokes, in the light and the dark theme. When not set, the text color of the theme.</summary>
    public KlyThemeColor? IconColor { get => (KlyThemeColor?)GetValue(IconColorProperty); set => SetValue(IconColorProperty, value); }

    /// <summary>What a screen reader says for the icon. Without it the icon is decorative and hidden from assistive technology.</summary>
    public string? Label { get => (string?)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    internal static double SizeOf(KlySize size) => size switch
    {
        KlySize.Xs or KlySize.Sm => 16,
        KlySize.Md or KlySize.Lg => 20,
        KlySize.Xl => 24,
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, null),
    };

    /// <inheritdoc />
    public override void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (_paths.Length == 0) return;
        var scale = (float)(SizeOf(Size) / BuiltinIcons.Size);
        canvas.Scale(scale, scale);
        canvas.StrokeColor = (IconColor ?? KlyTheme.Text).Resolve();
        canvas.StrokeSize = (float)BuiltinIcons.StrokeWidth;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.StrokeLineJoin = LineJoin.Round;
        foreach (var path in _paths) canvas.DrawPath(path);
    }

    private void Refresh()
    {
        _paths = KlyIcons.TryGet(Name, out var data) ? data.Select(PathBuilder.Build).ToArray() : [];
        var size = SizeOf(Size);
        WidthRequest = size;
        HeightRequest = size;
        var label = Label;
        SemanticProperties.SetDescription(this, label);
        AutomationProperties.SetIsInAccessibleTree(this, !string.IsNullOrEmpty(label));
        Invalidate();
    }
}
