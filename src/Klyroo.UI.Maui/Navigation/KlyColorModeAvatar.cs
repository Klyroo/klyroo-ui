namespace Klyroo.UI.Maui;

/// <summary>
/// An avatar with a picture for the light theme and one for the dark theme: the picture follows the theme of the app live. It is a <see cref="KlyAvatar"/>
/// (<c>Text</c>, <c>Alt</c>, <c>Color</c> and <c>Size</c> work as on any avatar); set <see cref="Light"/> and <see cref="Dark"/> instead of <see cref="KlyAvatar.Source"/>.
/// </summary>
public class KlyColorModeAvatar : KlyAvatar
{
    /// <summary>Identifies <see cref="Light"/>.</summary>
    public static readonly BindableProperty LightProperty = BindableProperty.Create(nameof(Light), typeof(ImageSource), typeof(KlyColorModeAvatar), null, propertyChanged: (b, _, _) => ((KlyColorModeAvatar)b).ShowSource());
    /// <summary>Identifies <see cref="Dark"/>.</summary>
    public static readonly BindableProperty DarkProperty = BindableProperty.Create(nameof(Dark), typeof(ImageSource), typeof(KlyColorModeAvatar), null, propertyChanged: (b, _, _) => ((KlyColorModeAvatar)b).ShowSource());

    /// <summary>Image in light mode.</summary>
    public ImageSource? Light { get => (ImageSource?)GetValue(LightProperty); set => SetValue(LightProperty, value); }

    /// <summary>Image in dark mode. Defaults to <see cref="Light"/>.</summary>
    public ImageSource? Dark { get => (ImageSource?)GetValue(DarkProperty); set => SetValue(DarkProperty, value); }

    private void ShowSource()
    {
        if (Light is null && Dark is null)
        {
            Source = null;
            return;
        }
        this.SetAppTheme<ImageSource?>(SourceProperty, Light ?? Dark, Dark ?? Light);
    }
}
