namespace Klyroo.UI.Maui;

/// <summary>A placeholder for an area without content: an icon, a title, a description, some content and actions.</summary>
[ContentProperty(nameof(Body))]
public class KlyEmpty : Border
{
    /// <summary>Identifies <see cref="Title"/>.</summary>
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(KlyEmpty), null, propertyChanged: (b, _, _) => ((KlyEmpty)b).Refresh());
    /// <summary>Identifies <see cref="Description"/>.</summary>
    public static readonly BindableProperty DescriptionProperty = BindableProperty.Create(nameof(Description), typeof(string), typeof(KlyEmpty), null, propertyChanged: (b, _, _) => ((KlyEmpty)b).Refresh());
    /// <summary>Identifies <see cref="Icon"/>.</summary>
    public static readonly BindableProperty IconProperty = BindableProperty.Create(nameof(Icon), typeof(View), typeof(KlyEmpty), null, propertyChanged: (b, _, _) => ((KlyEmpty)b).Refresh());
    /// <summary>Identifies <see cref="Actions"/>.</summary>
    public static readonly BindableProperty ActionsProperty = BindableProperty.Create(nameof(Actions), typeof(View), typeof(KlyEmpty), null, propertyChanged: (b, _, _) => ((KlyEmpty)b).Refresh());
    /// <summary>Identifies <see cref="ActionItems"/>.</summary>
    public static readonly BindableProperty ActionItemsProperty = BindableProperty.Create(nameof(ActionItems), typeof(IReadOnlyList<KlyActionItem>), typeof(KlyEmpty), null, propertyChanged: (b, _, _) => ((KlyEmpty)b).Refresh());
    /// <summary>Identifies <see cref="Body"/>.</summary>
    public static readonly BindableProperty BodyProperty = BindableProperty.Create(nameof(Body), typeof(View), typeof(KlyEmpty), null, propertyChanged: (b, _, _) => ((KlyEmpty)b).Refresh());
    /// <summary>Identifies <see cref="HeadingLevel"/>.</summary>
    public static readonly BindableProperty HeadingLevelProperty = BindableProperty.Create(nameof(HeadingLevel), typeof(int?), typeof(KlyEmpty), null, propertyChanged: (b, _, _) => ((KlyEmpty)b).Refresh());
    /// <summary>Identifies <see cref="Variant"/>.</summary>
    public static readonly BindableProperty VariantProperty = BindableProperty.Create(nameof(Variant), typeof(EmptyVariant), typeof(KlyEmpty), EmptyVariant.Outline, propertyChanged: (b, _, _) => ((KlyEmpty)b).ApplyStyle());
    /// <summary>Identifies <see cref="Size"/>.</summary>
    public static readonly BindableProperty SizeProperty = BindableProperty.Create(nameof(Size), typeof(KlySize), typeof(KlyEmpty), KlySize.Md, propertyChanged: (b, _, _) => ((KlyEmpty)b).ApplyStyle());

    private readonly VerticalStackLayout _root = new() { HorizontalOptions = LayoutOptions.Center };
    private readonly VerticalStackLayout _header = new() { HorizontalOptions = LayoutOptions.Center, MaximumWidthRequest = 448 };
    private readonly Label _title = DisplayParts.NewLabel(16, KlyTheme.TextHighlighted, FontAttributes.Bold);
    private readonly Label _description = DisplayParts.NewLabel(14, KlyTheme.TextMuted);
    private readonly FlexLayout _actions = new() { Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap, JustifyContent = Microsoft.Maui.Layouts.FlexJustify.Center, AlignItems = Microsoft.Maui.Layouts.FlexAlignItems.Center };

    /// <summary>Creates a placeholder.</summary>
    public KlyEmpty()
    {
        _title.HorizontalTextAlignment = TextAlignment.Center;
        _description.HorizontalTextAlignment = TextAlignment.Center;
        HorizontalOptions = LayoutOptions.Fill;
        Content = _root;
        Refresh();
        ApplyStyle();
    }

    /// <summary>Title of the placeholder, as a heading of level <see cref="HeadingLevel"/> for a screen reader.</summary>
    public string? Title { get => (string?)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    /// <summary>Text under the title.</summary>
    public string? Description { get => (string?)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }

    /// <summary>The illustration above the title: a <see cref="KlyIcon"/> or any view. It keeps its own size.</summary>
    public View? Icon { get => (View?)GetValue(IconProperty); set => SetValue(IconProperty, value); }

    /// <summary>Buttons under the text, as any view.</summary>
    public View? Actions { get => (View?)GetValue(ActionsProperty); set => SetValue(ActionsProperty, value); }

    /// <summary>Buttons under the text, from data (<see cref="KlyActionItem"/>). They are shown after <see cref="Actions"/>.</summary>
    public IReadOnlyList<KlyActionItem>? ActionItems { get => (IReadOnlyList<KlyActionItem>?)GetValue(ActionItemsProperty); set => SetValue(ActionItemsProperty, value); }

    /// <summary>Extra content between the description and the actions.</summary>
    public View? Body { get => (View?)GetValue(BodyProperty); set => SetValue(BodyProperty, value); }

    /// <summary>Makes the title a heading of this level (1 to 6) for a screen reader. Without it the title is plain text.</summary>
    public int? HeadingLevel { get => (int?)GetValue(HeadingLevelProperty); set => SetValue(HeadingLevelProperty, value); }

    /// <summary>Look of the placeholder: <c>Outline</c> (dashed border), <c>Soft</c>, <c>Subtle</c> or <c>Naked</c>.</summary>
    public EmptyVariant Variant { get => (EmptyVariant)GetValue(VariantProperty); set => SetValue(VariantProperty, value); }

    /// <summary>Size of the placeholder, from <c>Xs</c> to <c>Xl</c>: the space around and between the parts, and the size of the texts.</summary>
    public KlySize Size { get => (KlySize)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    private void Refresh()
    {
        DisplayParts.Show(_title, Title);
        DisplayParts.Show(_description, Description);
        if (HeadingLevel is { } level) SemanticProperties.SetHeadingLevel(_title, DisplayParts.HeadingLevel(level));
        else SemanticProperties.SetHeadingLevel(_title, SemanticHeadingLevel.None);

        DisplayParts.Detach(Icon);
        DisplayParts.Detach(Body);
        DisplayParts.Detach(Actions);
        _header.Children.Clear();
        if (Icon is not null || Title is not null || Description is not null)
        {
            if (Icon is not null)
            {
                Icon.HorizontalOptions = LayoutOptions.Center;
                AutomationProperties.SetIsInAccessibleTree(Icon, false);
                _header.Children.Add(Icon);
            }
            if (Title is not null) _header.Children.Add(_title);
            if (Description is not null) _header.Children.Add(_description);
        }

        _actions.Children.Clear();
        if (Actions is not null) _actions.Children.Add(Actions);
        foreach (var item in ActionItems ?? [])
        {
            var button = DisplayParts.ActionButton(item);
            button.Margin = new Thickness(4);
            _actions.Children.Add(button);
        }

        _root.Children.Clear();
        if (_header.Children.Count > 0) _root.Children.Add(_header);
        if (Body is not null) _root.Children.Add(Body);
        if (_actions.Children.Count > 0) _root.Children.Add(_actions);
    }

    private void ApplyStyle()
    {
        // Spacing, padding and text sizes follow the web theme of each size.
        var (gap, padding, headerGap, titleSize, descriptionSize) = Size switch
        {
            KlySize.Xs => (8d, 16d, 4d, 12d, 12d),
            KlySize.Sm => (12d, 16d, 4d, 14d, 12d),
            KlySize.Md => (16d, 24d, 6d, 16d, 14d),
            KlySize.Lg => (16d, 32d, 8d, 18d, 16d),
            KlySize.Xl => (20d, 40d, 8d, 20d, 16d),
            _ => throw new ArgumentOutOfRangeException(nameof(Size), Size, null),
        };
        _root.Spacing = gap;
        _header.Spacing = headerGap;
        Padding = new Thickness(padding);
        _title.FontSize = titleSize;
        _description.FontSize = descriptionSize;

        this.SetRadius(KlyTheme.RadiusOf(1));
        var tint = KlyTheme.BackgroundElevated.WithAlpha(0.5f);
        var transparent = KlyThemeColor.Transparent;
        this.Apply(Variant switch
        {
            EmptyVariant.Outline => new Surface(transparent, KlyTheme.BorderAccented, 1, KlyTheme.Text),
            EmptyVariant.Soft => new Surface(tint, transparent, 0, KlyTheme.Text),
            EmptyVariant.Subtle => new Surface(tint, KlyTheme.Border, 1, KlyTheme.Text),
            EmptyVariant.Naked => new Surface(transparent, transparent, 0, KlyTheme.Text),
            _ => throw new ArgumentOutOfRangeException(nameof(Variant), Variant, null),
        });
        StrokeDashArray = Variant == EmptyVariant.Outline ? new DoubleCollection { 4, 4 } : new DoubleCollection();
    }
}
