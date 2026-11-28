namespace Klyroo.UI.Maui;

/// <summary>
/// An image with a version for the light theme and one for the dark theme: the picture follows the theme of the app live. It is the native <see cref="Image"/>
/// (<c>Aspect</c>, <c>WidthRequest</c> and <c>HeightRequest</c> work as on any image); set <see cref="Light"/> and <see cref="Dark"/> instead of <see cref="Image.Source"/>.
/// </summary>
public class KlyColorModeImage : Image
{
    /// <summary>Identifies <see cref="Light"/>.</summary>
    public static readonly BindableProperty LightProperty = BindableProperty.Create(nameof(Light), typeof(ImageSource), typeof(KlyColorModeImage), null, propertyChanged: (b, _, _) => ((KlyColorModeImage)b).ShowSource());
    /// <summary>Identifies <see cref="Dark"/>.</summary>
    public static readonly BindableProperty DarkProperty = BindableProperty.Create(nameof(Dark), typeof(ImageSource), typeof(KlyColorModeImage), null, propertyChanged: (b, _, _) => ((KlyColorModeImage)b).ShowSource());
    /// <summary>Identifies <see cref="Alt"/>.</summary>
    public static readonly BindableProperty AltProperty = BindableProperty.Create(nameof(Alt), typeof(string), typeof(KlyColorModeImage), null, propertyChanged: (b, _, _) => ((KlyColorModeImage)b).ShowAlt());

    /// <summary>Creates an image. Without <see cref="Alt"/> it is decorative.</summary>
    public KlyColorModeImage()
    {
        ShowAlt();
    }

    /// <summary>Image in light mode.</summary>
    public ImageSource? Light { get => (ImageSource?)GetValue(LightProperty); set => SetValue(LightProperty, value); }

    /// <summary>Image in dark mode. Defaults to <see cref="Light"/>.</summary>
    public ImageSource? Dark { get => (ImageSource?)GetValue(DarkProperty); set => SetValue(DarkProperty, value); }

    /// <summary>Text alternative. Leave empty for a decorative image, which a screen reader skips.</summary>
    public string? Alt { get => (string?)GetValue(AltProperty); set => SetValue(AltProperty, value); }

    private void ShowSource()
    {
        if (Light is null && Dark is null)
        {
            Source = null;
            return;
        }
        this.SetAppTheme<ImageSource?>(SourceProperty, Light ?? Dark, Dark ?? Light);
    }

    private void ShowAlt()
    {
        SemanticProperties.SetDescription(this, Alt);
        AutomationProperties.SetIsInAccessibleTree(this, !string.IsNullOrEmpty(Alt));
    }
}
