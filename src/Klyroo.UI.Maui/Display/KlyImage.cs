namespace Klyroo.UI.Maui;

/// <summary>
/// A picture with a text alternative, an optional caption and a fallback. The fallback sits behind the picture, so it shows while the picture loads and stays when the picture cannot be loaded.
/// A picture with no <see cref="Alt"/> is decorative and hidden from assistive technology.
/// </summary>
public class KlyImage : VerticalStackLayout
{
    /// <summary>Identifies <see cref="Source"/>.</summary>
    public static readonly BindableProperty SourceProperty = BindableProperty.Create(nameof(Source), typeof(ImageSource), typeof(KlyImage), null, propertyChanged: (b, _, v) => ((KlyImage)b)._picture.Source = (ImageSource?)v);
    /// <summary>Identifies <see cref="Fallback"/>.</summary>
    public static readonly BindableProperty FallbackProperty = BindableProperty.Create(nameof(Fallback), typeof(ImageSource), typeof(KlyImage), null, propertyChanged: (b, _, v) => ((KlyImage)b)._fallback.Source = (ImageSource?)v);
    /// <summary>Identifies <see cref="Alt"/>.</summary>
    public static readonly BindableProperty AltProperty = BindableProperty.Create(nameof(Alt), typeof(string), typeof(KlyImage), null, propertyChanged: (b, _, _) => ((KlyImage)b).ApplyAlt());
    /// <summary>Identifies <see cref="Caption"/>.</summary>
    public static readonly BindableProperty CaptionProperty = BindableProperty.Create(nameof(Caption), typeof(string), typeof(KlyImage), null, propertyChanged: (b, _, v) => DisplayParts.Show(((KlyImage)b)._caption, (string?)v));
    /// <summary>Identifies <see cref="Aspect"/>.</summary>
    public static readonly BindableProperty AspectProperty = BindableProperty.Create(nameof(Aspect), typeof(Aspect), typeof(KlyImage), Aspect.AspectFit, propertyChanged: (b, _, v) => ((KlyImage)b).ShowAspect((Aspect)v));
    /// <summary>Identifies <see cref="ImageWidth"/>.</summary>
    public static readonly BindableProperty ImageWidthProperty = BindableProperty.Create(nameof(ImageWidth), typeof(double), typeof(KlyImage), -1d, propertyChanged: (b, _, v) => ((KlyImage)b)._frame.WidthRequest = (double)v);
    /// <summary>Identifies <see cref="ImageHeight"/>.</summary>
    public static readonly BindableProperty ImageHeightProperty = BindableProperty.Create(nameof(ImageHeight), typeof(double), typeof(KlyImage), -1d, propertyChanged: (b, _, v) => ((KlyImage)b)._frame.HeightRequest = (double)v);

    private readonly Image _fallback = new() { Aspect = Aspect.AspectFit };
    private readonly Image _picture = new() { Aspect = Aspect.AspectFit };
    private readonly Grid _frame = new();
    private readonly Label _caption = DisplayParts.NewLabel(12, KlyTheme.TextMuted);

    /// <summary>Creates an empty image: set <see cref="Source"/>.</summary>
    public KlyImage()
    {
        Spacing = 8;
        _caption.IsVisible = false;
        _frame.Add(_fallback);
        _frame.Add(_picture);
        Add(_frame);
        Add(_caption);
        ApplyAlt();
    }

    /// <summary>The picture: a file, a resource, a stream or an address. A string or a <see cref="Uri"/> converts to an <see cref="ImageSource"/>.</summary>
    public ImageSource? Source { get => (ImageSource?)GetValue(SourceProperty); set => SetValue(SourceProperty, value); }

    /// <summary>The picture shown behind <see cref="Source"/>: while it loads, and when it cannot be loaded.</summary>
    public ImageSource? Fallback { get => (ImageSource?)GetValue(FallbackProperty); set => SetValue(FallbackProperty, value); }

    /// <summary>What a screen reader says for the picture. Without it, or empty, the picture is decorative.</summary>
    public string? Alt { get => (string?)GetValue(AltProperty); set => SetValue(AltProperty, value); }

    /// <summary>A caption under the picture.</summary>
    public string? Caption { get => (string?)GetValue(CaptionProperty); set => SetValue(CaptionProperty, value); }

    /// <summary>How the picture fills its frame (<see cref="Microsoft.Maui.Aspect.AspectFit"/> by default).</summary>
    public Aspect Aspect { get => (Aspect)GetValue(AspectProperty); set => SetValue(AspectProperty, value); }

    /// <summary>The width of the picture, in device-independent units. It reserves the space before the picture loads. Unset by default: the picture takes the width it is given.</summary>
    public double ImageWidth { get => (double)GetValue(ImageWidthProperty); set => SetValue(ImageWidthProperty, value); }

    /// <summary>The height of the picture, in device-independent units, with the same rules as <see cref="ImageWidth"/>.</summary>
    public double ImageHeight { get => (double)GetValue(ImageHeightProperty); set => SetValue(ImageHeightProperty, value); }

    private void ShowAspect(Aspect aspect)
    {
        _picture.Aspect = aspect;
        _fallback.Aspect = aspect;
    }

    private void ApplyAlt()
    {
        var decorative = string.IsNullOrEmpty(Alt);
        SemanticProperties.SetDescription(_frame, decorative ? null : Alt);
        AutomationProperties.SetIsInAccessibleTree(_frame, !decorative);
    }
}
