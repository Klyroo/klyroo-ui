namespace Klyroo.UI.Maui;

/// <summary>A message in a colored box, with a title, a description and an optional close button.</summary>
public class KlyAlert : Border
{
    /// <summary>Identifies <see cref="Title"/>.</summary>
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(KlyAlert), null, propertyChanged: (b, _, v) => ((KlyAlert)b).ShowText(((KlyAlert)b)._title, (string?)v));
    /// <summary>Identifies <see cref="Description"/>.</summary>
    public static readonly BindableProperty DescriptionProperty = BindableProperty.Create(nameof(Description), typeof(string), typeof(KlyAlert), null, propertyChanged: (b, _, v) => ((KlyAlert)b).ShowText(((KlyAlert)b)._description, (string?)v));
    /// <summary>Identifies <see cref="Color"/>.</summary>
    public static readonly BindableProperty ColorProperty = BindableProperty.Create(nameof(Color), typeof(KlyColor), typeof(KlyAlert), KlyColor.Primary, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="Variant"/>.</summary>
    public static readonly BindableProperty VariantProperty = BindableProperty.Create(nameof(Variant), typeof(KlyVariant), typeof(KlyAlert), KlyVariant.Solid, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="Closable"/>.</summary>
    public static readonly BindableProperty ClosableProperty = BindableProperty.Create(nameof(Closable), typeof(bool), typeof(KlyAlert), false, propertyChanged: (b, _, v) => ((KlyAlert)b)._close.IsVisible = (bool)v);
    /// <summary>Identifies <see cref="CloseLabel"/>.</summary>
    public static readonly BindableProperty CloseLabelProperty = BindableProperty.Create(nameof(CloseLabel), typeof(string), typeof(KlyAlert), "Close", propertyChanged: (b, _, v) => SemanticProperties.SetDescription(((KlyAlert)b)._close, (string?)v));

    private readonly Label _title = new() { FontSize = 14, FontAttributes = FontAttributes.Bold, IsVisible = false };
    private readonly Label _description = new() { FontSize = 14, IsVisible = false };
    private readonly Button _close = new()
    {
        Text = "✕",
        FontSize = 14,
        Padding = new Thickness(4),
        MinimumWidthRequest = 32,
        MinimumHeightRequest = 32,
        BackgroundColor = Colors.Transparent,
        BorderWidth = 0,
        VerticalOptions = LayoutOptions.Start,
        IsVisible = false,
    };

    /// <summary>Creates an alert.</summary>
    public KlyAlert()
    {
        Padding = new Thickness(16);
        this.SetRadius(KlyTheme.RadiusOf(1));
        SemanticProperties.SetDescription(_close, CloseLabel);
        _close.Clicked += (_, _) =>
        {
            IsVisible = false;
            Closed?.Invoke(this, EventArgs.Empty);
        };

        var texts = new VerticalStackLayout { Spacing = 4, Children = { _title, _description } };
        var grid = new Grid { ColumnSpacing = 10, ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
        grid.Add(texts, 0);
        grid.Add(_close, 1);
        Content = grid;
        ApplyStyle();
    }

    /// <summary>Raised when the user closes the alert with the close button. The alert hides itself first.</summary>
    public event EventHandler? Closed;

    /// <summary>Title of the alert, in bold.</summary>
    public string? Title { get => (string?)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    /// <summary>Text under the title.</summary>
    public string? Description { get => (string?)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }

    /// <summary>Semantic color of the alert.</summary>
    public KlyColor Color { get => (KlyColor)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }

    /// <summary>Look of the alert: <c>Solid</c>, <c>Outline</c>, <c>Soft</c> or <c>Subtle</c>.</summary>
    public KlyVariant Variant { get => (KlyVariant)GetValue(VariantProperty); set => SetValue(VariantProperty, value); }

    /// <summary>Shows a close button. Pressing it hides the alert and raises <see cref="Closed"/>.</summary>
    public bool Closable { get => (bool)GetValue(ClosableProperty); set => SetValue(ClosableProperty, value); }

    /// <summary>Accessible name of the close button, in the language of the app.</summary>
    public string CloseLabel { get => (string)GetValue(CloseLabelProperty); set => SetValue(CloseLabelProperty, value); }

    private static void Restyle(BindableObject bindable, object oldValue, object newValue) => ((KlyAlert)bindable).ApplyStyle();

    private void ShowText(Label label, string? text)
    {
        label.Text = text;
        label.IsVisible = !string.IsNullOrEmpty(text);
    }

    private void ApplyStyle()
    {
        var surface = Surface.For(Variant, KlyTheme.Resolve(Color));
        this.Apply(surface);
        _title.SetThemeColor(Label.TextColorProperty, surface.Text);
        _description.SetThemeColor(Label.TextColorProperty, surface.Text);
        _close.SetThemeColor(Button.TextColorProperty, surface.Text);
    }
}
