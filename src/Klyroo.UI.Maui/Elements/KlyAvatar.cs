namespace Klyroo.UI.Maui;

/// <summary>A round picture of a person or a thing. When there is no picture, or while it loads, the initials of <see cref="Text"/> show instead.</summary>
public class KlyAvatar : Border
{
    /// <summary>Identifies <see cref="Source"/>.</summary>
    public static readonly BindableProperty SourceProperty = BindableProperty.Create(nameof(Source), typeof(ImageSource), typeof(KlyAvatar), null, propertyChanged: (b, _, v) => ((KlyAvatar)b)._image.Source = (ImageSource?)v);
    /// <summary>Identifies <see cref="Text"/>.</summary>
    public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(KlyAvatar), null, propertyChanged: (b, _, v) => ((KlyAvatar)b)._initials.Text = Initials((string?)v));
    /// <summary>Identifies <see cref="Alt"/>.</summary>
    public static readonly BindableProperty AltProperty = BindableProperty.Create(nameof(Alt), typeof(string), typeof(KlyAvatar), null, propertyChanged: (b, _, v) => SemanticProperties.SetDescription((KlyAvatar)b, (string?)v));
    /// <summary>Identifies <see cref="Color"/>.</summary>
    public static readonly BindableProperty ColorProperty = BindableProperty.Create(nameof(Color), typeof(KlyColor?), typeof(KlyAvatar), null, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="Size"/>.</summary>
    public static readonly BindableProperty SizeProperty = BindableProperty.Create(nameof(Size), typeof(KlySize), typeof(KlyAvatar), KlySize.Md, propertyChanged: Restyle);

    private readonly Label _initials = new() { HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center, LineBreakMode = LineBreakMode.NoWrap };
    private readonly Image _image = new() { Aspect = Aspect.AspectFill };

    /// <summary>Creates an avatar.</summary>
    public KlyAvatar()
    {
        StrokeThickness = 0;
        HorizontalOptions = LayoutOptions.Start;
        VerticalOptions = LayoutOptions.Center;
        Content = new Grid { Children = { _initials, _image } };
        ApplyStyle();
    }

    /// <summary>The picture. It covers the initials once it has loaded.</summary>
    public ImageSource? Source { get => (ImageSource?)GetValue(SourceProperty); set => SetValue(SourceProperty, value); }

    /// <summary>Text the initials come from: the first letter of its first two words, in upper case.</summary>
    public string? Text { get => (string?)GetValue(TextProperty); set => SetValue(TextProperty, value); }

    /// <summary>What a screen reader says for the avatar, such as the name of the person.</summary>
    public string? Alt { get => (string?)GetValue(AltProperty); set => SetValue(AltProperty, value); }

    /// <summary>Semantic color of the initials. When not set, the avatar is a gray accent.</summary>
    public KlyColor? Color { get => (KlyColor?)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }

    /// <summary>Size of the avatar, from <c>Xs</c> (24) to <c>Xl</c> (48).</summary>
    public KlySize Size { get => (KlySize)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    internal static string Initials(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        var words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(words.Take(2).Select(w => char.ToUpperInvariant(w[0])));
    }

    private static void Restyle(BindableObject bindable, object oldValue, object newValue) => ((KlyAvatar)bindable).ApplyStyle();

    private void ApplyStyle()
    {
        var (diameter, fontSize) = Size switch
        {
            KlySize.Xs => (24d, 10d),
            KlySize.Sm => (28d, 12d),
            KlySize.Md => (32d, 14d),
            KlySize.Lg => (40d, 16d),
            KlySize.Xl => (48d, 18d),
            _ => throw new ArgumentOutOfRangeException(nameof(Size), Size, null),
        };
        WidthRequest = diameter;
        HeightRequest = diameter;
        this.SetRadius(diameter / 2);
        _initials.FontSize = fontSize;
        _initials.FontAttributes = FontAttributes.Bold;

        if (Color is { } color)
        {
            var set = KlyTheme.Resolve(color);
            this.SetThemeColor(BackgroundColorProperty, set.Fill.WithAlpha(0.15f));
            _initials.SetThemeColor(Label.TextColorProperty, set.Text);
        }
        else
        {
            this.SetThemeColor(BackgroundColorProperty, KlyTheme.BackgroundAccented);
            _initials.SetThemeColor(Label.TextColorProperty, KlyTheme.Text);
        }
    }
}
