namespace Klyroo.UI.Maui;

/// <summary>
/// The button that opens a <see cref="KlyContentSearch"/> and shows its keyboard shortcut. It is a <see cref="KlyButton"/> with the search icon drawn over it, so it takes its color, variant
/// and size. Set <see cref="Search"/> to the search it opens; <see cref="Clicked"/> is raised whatever it is.
/// </summary>
public class KlyContentSearchButton : Grid
{
    /// <summary>Identifies <see cref="Label"/>.</summary>
    public static readonly BindableProperty LabelProperty = BindableProperty.Create(nameof(Label), typeof(string), typeof(KlyContentSearchButton), "Search", propertyChanged: (b, _, _) => ((KlyContentSearchButton)b).Arrange());
    /// <summary>Identifies <see cref="Collapsed"/>.</summary>
    public static readonly BindableProperty CollapsedProperty = BindableProperty.Create(nameof(Collapsed), typeof(bool), typeof(KlyContentSearchButton), true, propertyChanged: (b, _, _) => ((KlyContentSearchButton)b).Arrange());
    /// <summary>Identifies <see cref="Keys"/>.</summary>
    public static readonly BindableProperty KeysProperty = BindableProperty.Create(nameof(Keys), typeof(IReadOnlyList<string>), typeof(KlyContentSearchButton), null, propertyChanged: (b, _, _) => ((KlyContentSearchButton)b).Arrange());
    /// <summary>Identifies <see cref="Search"/>.</summary>
    public static readonly BindableProperty SearchProperty = BindableProperty.Create(nameof(Search), typeof(KlyContentSearch), typeof(KlyContentSearchButton), null);
    /// <summary>Identifies <see cref="Color"/>.</summary>
    public static readonly BindableProperty ColorProperty = BindableProperty.Create(nameof(Color), typeof(KlyColor), typeof(KlyContentSearchButton), KlyColor.Neutral, propertyChanged: (b, _, v) => ((KlyContentSearchButton)b).Restyle());
    /// <summary>Identifies <see cref="Variant"/>.</summary>
    public static readonly BindableProperty VariantProperty = BindableProperty.Create(nameof(Variant), typeof(ButtonVariant), typeof(KlyContentSearchButton), ButtonVariant.Outline, propertyChanged: (b, _, v) => ((KlyContentSearchButton)b).Restyle());
    /// <summary>Identifies <see cref="Size"/>.</summary>
    public static readonly BindableProperty SizeProperty = BindableProperty.Create(nameof(Size), typeof(KlySize), typeof(KlyContentSearchButton), KlySize.Md, propertyChanged: (b, _, v) => ((KlyContentSearchButton)b).Restyle());

    private readonly KlyButton _button = new() { Color = KlyColor.Neutral, Variant = ButtonVariant.Outline, HorizontalOptions = LayoutOptions.Fill };
    private readonly KlyIcon _icon = new() { Name = KlyIconNames.Search, Size = KlySize.Md, InputTransparent = true };
    private readonly Label _label = PagesUi.Text(14, KlyTheme.TextMuted);
    private readonly HorizontalStackLayout _keys = new() { Spacing = 2, InputTransparent = true, VerticalOptions = LayoutOptions.Center };
    private readonly Grid _overlay = new() { ColumnSpacing = 8, InputTransparent = true, VerticalOptions = LayoutOptions.Center, ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };

    /// <summary>Creates a search button.</summary>
    public KlyContentSearchButton()
    {
        HorizontalOptions = LayoutOptions.Start;
        _label.InputTransparent = true;
        _icon.VerticalOptions = LayoutOptions.Center;
        _label.VerticalOptions = LayoutOptions.Center;
        _overlay.Add(_icon, 0, 0);
        _overlay.Add(_label, 1, 0);
        _overlay.Add(_keys, 2, 0);
        Children.Add(_button);
        Children.Add(_overlay);
        _button.Clicked += (_, _) =>
        {
            Clicked?.Invoke(this, EventArgs.Empty);
            Search?.IsOpen = true;
        };
        Restyle();
        Arrange();
    }

    /// <summary>Raised when the user presses the button.</summary>
    public event EventHandler? Clicked;

    /// <summary>The text of the button, and its accessible name when <see cref="Collapsed"/>. "Search" by default, in the language of the app.</summary>
    public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    /// <summary>Shows only the icon, as a square button. Without it the button shows its label and its shortcut.</summary>
    public bool Collapsed { get => (bool)GetValue(CollapsedProperty); set => SetValue(CollapsedProperty, value); }

    /// <summary>
    /// The keys of the shortcut to show, one key cap each. Left out, the hint is the modifier of the platform (the command key on Apple devices, Ctrl elsewhere) and K.
    /// An empty list shows no keys. Match the shortcut your app registers: this control does not listen to the keyboard.
    /// </summary>
    public IReadOnlyList<string>? Keys { get => (IReadOnlyList<string>?)GetValue(KeysProperty); set => SetValue(KeysProperty, value); }

    /// <summary>The search this button opens: its <see cref="KlyContentSearch.IsOpen"/> becomes <c>true</c>.</summary>
    public KlyContentSearch? Search { get => (KlyContentSearch?)GetValue(SearchProperty); set => SetValue(SearchProperty, value); }

    /// <summary>Semantic color of the button. Neutral by default.</summary>
    public KlyColor Color { get => (KlyColor)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }

    /// <summary>Look of the button: <c>Solid</c>, <c>Outline</c>, <c>Soft</c>, <c>Subtle</c>, <c>Ghost</c> or <c>Link</c>. Outline by default.</summary>
    public ButtonVariant Variant { get => (ButtonVariant)GetValue(VariantProperty); set => SetValue(VariantProperty, value); }

    /// <summary>Size of the button, from <c>Xs</c> to <c>Xl</c>.</summary>
    public KlySize Size { get => (KlySize)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    internal KlyButton Button => _button;

    internal Label LabelView => _label;

    internal HorizontalStackLayout KeyCaps => _keys;

    /// <summary>The key caps shown: the given keys, or the modifier of the platform and K.</summary>
    internal IReadOnlyList<string> KeyTexts => Keys ?? [ModifierKey(), "K"];

    internal static string ModifierKey()
    {
        try
        {
            return DeviceInfo.Platform == DevicePlatform.iOS || DeviceInfo.Platform == DevicePlatform.MacCatalyst ? "⌘" : "Ctrl";
        }
        catch (Exception)
        {
            // No platform to ask (a unit test, a design-time host): the key of most desktops.
            return "Ctrl";
        }
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        // Same dimming as the web button: disabled:opacity-60.
        if (propertyName == nameof(IsEnabled))
        {
            _button.IsEnabled = IsEnabled;
            Opacity = IsEnabled ? 1 : 0.6;
        }
    }

    private void Restyle()
    {
        _button.Color = Color;
        _button.Variant = Variant;
        _button.Size = Size;
        _icon.IconColor = KlyTheme.TextMuted;
        Arrange();
    }

    private void Arrange()
    {
        _button.Text = "";
        SemanticProperties.SetDescription(_button, Label);
        SemanticProperties.SetHint(_button, null);
        _label.Text = Label;
        _label.IsVisible = !Collapsed;
        _label.FontSize = Size is KlySize.Xs or KlySize.Sm ? 12 : 14;

        _keys.Children.Clear();
        _keys.IsVisible = !Collapsed && KeyTexts.Count > 0;
        if (!Collapsed)
        {
            foreach (var key in KeyTexts) _keys.Children.Add(KeyCap(key));
        }

        // A collapsed button is a square of the touch size; an open one is as wide as its content.
        var side = _button.MinimumHeightRequest;
        _button.MinimumWidthRequest = Collapsed ? side : -1;
        _overlay.HorizontalOptions = Collapsed ? LayoutOptions.Center : LayoutOptions.Fill;
        _overlay.Margin = Collapsed ? new Thickness(0) : new Thickness(12, 0, 6, 0);
        HorizontalOptions = Collapsed ? LayoutOptions.Start : LayoutOptions.Fill;
    }

    private static Border KeyCap(string text)
    {
        var label = PagesUi.Text(10, KlyTheme.TextMuted);
        PagesUi.Show(label, text);
        var cap = new Border { StrokeThickness = 1, Padding = new Thickness(4, 2), Content = label };
        cap.SetRadius(KlyTheme.RadiusOf(0.5));
        cap.SetStroke(KlyTheme.Border);
        cap.SetThemeColor(BackgroundColorProperty, KlyTheme.Background);
        AutomationProperties.SetIsInAccessibleTree(cap, false);
        return cap;
    }
}
