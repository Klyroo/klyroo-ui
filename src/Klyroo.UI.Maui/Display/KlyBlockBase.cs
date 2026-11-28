using SurfaceStyle = Klyroo.UI.Maui.Surface;

namespace Klyroo.UI.Maui;

/// <summary>
/// The frame of the read-only blocks (<see cref="KlyFacts"/>, <see cref="KlyStat{TValue}"/>, <see cref="KlyList"/>...): a <see cref="Surface"/> (border, background and padding),
/// and a skeleton shown instead of the content while <see cref="Loading"/>. The content is built by the derived control.
/// </summary>
public abstract class KlyBlockBase : Border
{
    /// <summary>Identifies <see cref="Surface"/>.</summary>
    public static readonly BindableProperty SurfaceProperty = BindableProperty.Create(nameof(Surface), typeof(KlySurface?), typeof(KlyBlockBase), null, propertyChanged: (b, _, _) => ((KlyBlockBase)b).ApplySurface());
    /// <summary>Identifies <see cref="Loading"/>.</summary>
    public static readonly BindableProperty LoadingProperty = BindableProperty.Create(nameof(Loading), typeof(bool), typeof(KlyBlockBase), false, propertyChanged: (b, _, v) => ((KlyBlockBase)b).ShowLoading((bool)v));

    private readonly KlySurface _defaultSurface;
    private readonly VerticalStackLayout _body;
    private readonly VerticalStackLayout _skeleton;

    /// <summary>Creates the frame.</summary>
    /// <param name="defaultSurface">The surface used while <see cref="Surface"/> is not set.</param>
    /// <param name="spacing">The space between the children of the content, 12 on the web (<c>gap-3</c>).</param>
    protected KlyBlockBase(KlySurface defaultSurface, double spacing = 12)
    {
        _defaultSurface = defaultSurface;
        _body = new VerticalStackLayout { Spacing = spacing };
        _skeleton = new VerticalStackLayout { Spacing = 10, IsVisible = false };
        _skeleton.Add(SkeletonRow(20, 1, 2));
        _skeleton.Add(SkeletonRow(16, 2, 1));
        _skeleton.Add(SkeletonRow(16, 1, 1));
        AutomationProperties.SetIsInAccessibleTree(_skeleton, false);
        Content = new Grid { Children = { _body, _skeleton } };
        HorizontalOptions = LayoutOptions.Fill;
        ApplySurface();
    }

    /// <summary>Border, background and padding of the block. When not set, each block has its own default (<see cref="KlySurface.Plain"/>, <see cref="KlySurface.Muted"/> or <see cref="KlySurface.Bordered"/>).</summary>
    public KlySurface? Surface { get => (KlySurface?)GetValue(SurfaceProperty); set => SetValue(SurfaceProperty, value); }

    /// <summary>Shows a skeleton placeholder instead of the content.</summary>
    public bool Loading { get => (bool)GetValue(LoadingProperty); set => SetValue(LoadingProperty, value); }

    /// <summary>The surface in effect: <see cref="Surface"/>, or the default of the block.</summary>
    internal KlySurface EffectiveSurface => Surface ?? _defaultSurface;

    /// <summary>The stack the derived control fills with its content.</summary>
    protected VerticalStackLayout Body => _body;

    private static Grid SkeletonRow(double height, int filled, int empty)
    {
        var row = new Grid { ColumnDefinitions = { new ColumnDefinition(new GridLength(filled, GridUnitType.Star)), new ColumnDefinition(new GridLength(empty, GridUnitType.Star)) } };
        row.Add(new KlySkeleton { HeightRequest = height }, 0);
        return row;
    }

    private void ShowLoading(bool loading)
    {
        _body.IsVisible = !loading;
        _skeleton.IsVisible = loading;
    }

    private void ApplySurface()
    {
        var transparent = KlyThemeColor.Transparent;
        var surface = EffectiveSurface switch
        {
            KlySurface.Plain => new SurfaceStyle(transparent, transparent, 0, KlyTheme.Text),
            KlySurface.Muted => new SurfaceStyle(KlyTheme.BackgroundMuted, transparent, 0, KlyTheme.Text),
            KlySurface.Bordered => new SurfaceStyle(KlyTheme.Background, KlyTheme.Border, 1, KlyTheme.Text),
            KlySurface.Elevated => new SurfaceStyle(KlyTheme.BackgroundElevated, KlyTheme.BorderAccented, 1, KlyTheme.Text),
            _ => throw new ArgumentOutOfRangeException(nameof(Surface), EffectiveSurface, null),
        };
        this.Apply(surface);
        this.SetRadius(KlyTheme.RadiusOf(1));
        Padding = EffectiveSurface == KlySurface.Plain ? new Thickness(0) : new Thickness(16);

        // shadow-sm: a soft shadow in the darkest token of the theme, which only shows on a light page.
        Shadow = EffectiveSurface == KlySurface.Elevated
            ? new Shadow { Brush = new SolidColorBrush(KlyTheme.TextHighlighted.Light), Offset = new Point(0, 1), Radius = 2, Opacity = 0.1f }
            : null!;
    }
}
