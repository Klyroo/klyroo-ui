namespace Klyroo.UI.Maui;

/// <summary>A small indicator placed on a corner of its content: a dot, a count or a status. Put the element it sits on (an avatar, a button) in <see cref="Content"/>.</summary>
[ContentProperty(nameof(Content))]
public class KlyChip : Grid
{
    /// <summary>Identifies <see cref="Content"/>.</summary>
    public static readonly BindableProperty ContentProperty = BindableProperty.Create(nameof(Content), typeof(View), typeof(KlyChip), null, propertyChanged: (b, o, n) => ((KlyChip)b).ShowContent((View?)o, (View?)n));
    /// <summary>Identifies <see cref="Text"/>.</summary>
    public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(KlyChip), null, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="AccessibleName"/>.</summary>
    public static readonly BindableProperty AccessibleNameProperty = BindableProperty.Create(nameof(AccessibleName), typeof(string), typeof(KlyChip), null, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Color"/>.</summary>
    public static readonly BindableProperty ColorProperty = BindableProperty.Create(nameof(Color), typeof(KlyColor), typeof(KlyChip), KlyColor.Primary, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Size"/>.</summary>
    public static readonly BindableProperty SizeProperty = BindableProperty.Create(nameof(Size), typeof(KlySize), typeof(KlyChip), KlySize.Md, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Position"/>.</summary>
    public static readonly BindableProperty PositionProperty = BindableProperty.Create(nameof(Position), typeof(KlyChipPosition), typeof(KlyChip), KlyChipPosition.TopRight, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Inset"/>.</summary>
    public static readonly BindableProperty InsetProperty = BindableProperty.Create(nameof(Inset), typeof(bool), typeof(KlyChip), false, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Standalone"/>.</summary>
    public static readonly BindableProperty StandaloneProperty = BindableProperty.Create(nameof(Standalone), typeof(bool), typeof(KlyChip), false, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Show"/>.</summary>
    public static readonly BindableProperty ShowProperty = BindableProperty.Create(nameof(Show), typeof(bool), typeof(KlyChip), true, propertyChanged: Rebuild);

    private readonly Border _ring = new() { StrokeThickness = 0, Padding = 2 };
    private readonly Border _indicator = new() { StrokeThickness = 1, Padding = 0 };
    private readonly Label _label = new() { FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center, LineBreakMode = LineBreakMode.NoWrap };

    /// <summary>Creates a chip.</summary>
    public KlyChip()
    {
        HorizontalOptions = LayoutOptions.Start;
        VerticalOptions = LayoutOptions.Center;
        _ring.Content = _indicator;
        _ring.SizeChanged += (_, _) => Reposition(_ring.Width, _ring.Height);
        this.Add(_ring);
        Apply();
    }

    /// <summary>The element the indicator sits on, an avatar or a button for example.</summary>
    public View? Content { get => (View?)GetValue(ContentProperty); set => SetValue(ContentProperty, value); }

    /// <summary>Text inside the indicator, a count for example. Without it the indicator is a dot.</summary>
    public string? Text { get => (string?)GetValue(TextProperty); set => SetValue(TextProperty, value); }

    /// <summary>
    /// What a screen reader says for the indicator ("3 unread messages"). Without it a dot is hidden from assistive technology
    /// and a text is read as it is: never rely on the color alone to convey a meaning.
    /// </summary>
    public string? AccessibleName { get => (string?)GetValue(AccessibleNameProperty); set => SetValue(AccessibleNameProperty, value); }

    /// <summary>Semantic color of the chip.</summary>
    public KlyColor Color { get => (KlyColor)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }

    /// <summary>Size of the chip, from <c>Xs</c> to <c>Xl</c>.</summary>
    public KlySize Size { get => (KlySize)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    /// <summary>Corner of the content where the chip sits. Defaults to the top right.</summary>
    public KlyChipPosition Position { get => (KlyChipPosition)GetValue(PositionProperty); set => SetValue(PositionProperty, value); }

    /// <summary>Keeps the indicator inside the corner of the element instead of straddling it.</summary>
    public bool Inset { get => (bool)GetValue(InsetProperty); set => SetValue(InsetProperty, value); }

    /// <summary>Draws the indicator on its own, in the flow, without an element to sit on.</summary>
    public bool Standalone { get => (bool)GetValue(StandaloneProperty); set => SetValue(StandaloneProperty, value); }

    /// <summary>Shows the indicator. Set it to false to hide it without removing the element it sits on.</summary>
    public bool Show { get => (bool)GetValue(ShowProperty); set => SetValue(ShowProperty, value); }

    private bool IsCount => !string.IsNullOrEmpty(Text);

    private static void Rebuild(BindableObject bindable, object oldValue, object newValue) => ((KlyChip)bindable).Apply();

    private void ShowContent(View? old, View? content)
    {
        if (old is not null) Children.Remove(old);
        if (content is not null) Children.Insert(0, content);
    }

    private void Apply()
    {
        var set = KlyTheme.Resolve(Color);

        // The ring around the indicator is the page background (ring-2 ring-bg), so the indicator reads against what is under it.
        _ring.SetThemeColor(BackgroundColorProperty, KlyTheme.Background);
        _ring.SetRadius(9999);
        _ring.IsVisible = Show;
        _indicator.SetThemeColor(BackgroundColorProperty, set.Fill);
        _indicator.SetStroke(set.Text);
        _indicator.SetRadius(9999);
        _label.SetThemeColor(Label.TextColorProperty, set.Contrast);

        if (IsCount)
        {
            var (height, padding, fontSize) = Size switch
            {
                KlySize.Xs => (14d, 2d, 10d),
                KlySize.Sm => (16d, 4d, 10d),
                KlySize.Md => (16d, 4d, 10d),
                KlySize.Lg => (20d, 4d, 12d),
                KlySize.Xl => (24d, 6d, 14d),
                _ => throw new ArgumentOutOfRangeException(nameof(Size), Size, null),
            };
            _label.Text = Text;
            _label.FontSize = fontSize;
            _indicator.Content = _label;
            _indicator.Padding = new Thickness(padding, 0);
            _indicator.HeightRequest = height;
            _indicator.WidthRequest = -1;
            _indicator.MinimumWidthRequest = height;
        }
        else
        {
            var dot = Size switch
            {
                KlySize.Xs => 6d,
                KlySize.Sm => 8d,
                KlySize.Md => 10d,
                KlySize.Lg => 12d,
                KlySize.Xl => 14d,
                _ => throw new ArgumentOutOfRangeException(nameof(Size), Size, null),
            };
            _indicator.Content = null;
            _indicator.Padding = new Thickness(0);
            _indicator.HeightRequest = dot;
            _indicator.WidthRequest = dot;
            _indicator.MinimumWidthRequest = -1;
        }

        // A dot means nothing without a name, so it is hidden from a screen reader; a count is read as it is.
        SemanticProperties.SetDescription(_ring, AccessibleName);
        AutomationProperties.SetIsInAccessibleTree(_ring, AccessibleName is not null || IsCount);

        if (Standalone)
        {
            _ring.HorizontalOptions = LayoutOptions.Center;
            _ring.VerticalOptions = LayoutOptions.Center;
        }
        else
        {
            _ring.HorizontalOptions = Position is KlyChipPosition.TopLeft or KlyChipPosition.BottomLeft ? LayoutOptions.Start : LayoutOptions.End;
            _ring.VerticalOptions = Position is KlyChipPosition.TopLeft or KlyChipPosition.TopRight ? LayoutOptions.Start : LayoutOptions.End;
        }

        Reposition(_ring.Width, _ring.Height);
    }

    /// <summary>
    /// Moves the indicator half of its own size over the corner so that it straddles it, as the web chip does (translate by 50%). An inset or standalone chip stays where it is.
    /// The size is the measured one once the view has a layout, else the one from the table.
    /// </summary>
    internal void Reposition(double width, double height)
    {
        if (Standalone || Inset)
        {
            _ring.TranslationX = 0;
            _ring.TranslationY = 0;
            return;
        }

        if (width <= 0 || height <= 0)
        {
            width = _indicator.WidthRequest > 0 ? _indicator.WidthRequest + 4 : _indicator.MinimumWidthRequest + 4;
            height = _indicator.HeightRequest + 4;
        }

        var right = Position is KlyChipPosition.TopRight or KlyChipPosition.BottomRight;
        var top = Position is KlyChipPosition.TopLeft or KlyChipPosition.TopRight;
        var toEnd = FlowDirection == FlowDirection.RightToLeft ? !right : right;
        _ring.TranslationX = toEnd ? width / 2 : -width / 2;
        _ring.TranslationY = top ? -height / 2 : height / 2;
    }
}
