namespace Klyroo.UI.Maui;

/// <summary>A list of items: plain, as numbered steps, or as a checklist. Done items say so to assistive technology.</summary>
public class KlyList : KlyBlockBase
{
    /// <summary>Identifies <see cref="Title"/>.</summary>
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(KlyList), null, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Items"/>.</summary>
    public static readonly BindableProperty ItemsProperty = BindableProperty.Create(nameof(Items), typeof(IReadOnlyList<KlyListItem>), typeof(KlyList), null, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Variant"/>.</summary>
    public static readonly BindableProperty VariantProperty = BindableProperty.Create(nameof(Variant), typeof(KlyListVariant), typeof(KlyList), KlyListVariant.Plain, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Max"/>.</summary>
    public static readonly BindableProperty MaxProperty = BindableProperty.Create(nameof(Max), typeof(int?), typeof(KlyList), null, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Step"/>.</summary>
    public static readonly BindableProperty StepProperty = BindableProperty.Create(nameof(Step), typeof(int?), typeof(KlyList), null, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="ShowMoreFormat"/>.</summary>
    public static readonly BindableProperty ShowMoreFormatProperty = BindableProperty.Create(nameof(ShowMoreFormat), typeof(string), typeof(KlyList), "Show more · {0}", propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="StepCompletedLabel"/>.</summary>
    public static readonly BindableProperty StepCompletedLabelProperty = BindableProperty.Create(nameof(StepCompletedLabel), typeof(string), typeof(KlyList), "Step completed", propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="CurrentStepLabel"/>.</summary>
    public static readonly BindableProperty CurrentStepLabelProperty = BindableProperty.Create(nameof(CurrentStepLabel), typeof(string), typeof(KlyList), "Current step", propertyChanged: Rebuild);

    private readonly VerticalStackLayout _stack = new() { Spacing = 8 };
    private readonly DisplayReveal _reveal = new();

    /// <summary>Creates a list.</summary>
    public KlyList() : base(KlySurface.Plain, 8)
    {
        Body.Add(_stack);
        Rebuild();
    }

    /// <summary>The title above the list.</summary>
    public string? Title { get => (string?)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    /// <summary>The items, as <see cref="KlyListItem"/>.</summary>
    public IReadOnlyList<KlyListItem>? Items { get => (IReadOnlyList<KlyListItem>?)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }

    /// <summary>The layout: <see cref="KlyListVariant.Plain"/> (bullets or icons), <see cref="KlyListVariant.Steps"/> (a numbered list) or <see cref="KlyListVariant.Checks"/> (a checklist).</summary>
    public KlyListVariant Variant { get => (KlyListVariant)GetValue(VariantProperty); set => SetValue(VariantProperty, value); }

    /// <summary>The number of items shown before a Show more button (all items when <see langword="null"/>).</summary>
    public int? Max { get => (int?)GetValue(MaxProperty); set => SetValue(MaxProperty, value); }

    /// <summary>The number of items each press of Show more reveals (all the remaining ones when <see langword="null"/>).</summary>
    public int? Step { get => (int?)GetValue(StepProperty); set => SetValue(StepProperty, value); }

    /// <summary>The text of the Show more button, where <c>{0}</c> is the number of items the next press reveals, in the language of the app.</summary>
    public string ShowMoreFormat { get => (string)GetValue(ShowMoreFormatProperty); set => SetValue(ShowMoreFormatProperty, value); }

    /// <summary>What a screen reader adds for a done item of the steps and checks layouts, in the language of the app.</summary>
    public string StepCompletedLabel { get => (string)GetValue(StepCompletedLabelProperty); set => SetValue(StepCompletedLabelProperty, value); }

    /// <summary>What a screen reader adds for the current item of the steps and checks layouts, in the language of the app.</summary>
    public string CurrentStepLabel { get => (string)GetValue(CurrentStepLabelProperty); set => SetValue(CurrentStepLabelProperty, value); }

    /// <summary>The rows, for tests.</summary>
    internal IReadOnlyList<View> Rows => _stack.Children.OfType<Grid>().Cast<View>().ToList();

    /// <summary>The Show more button, when there is one.</summary>
    internal Button? MoreButton => _stack.Children.OfType<Button>().FirstOrDefault();

    private static void Rebuild(BindableObject bindable, object oldValue, object newValue) => ((KlyList)bindable).Rebuild();

    private void Rebuild()
    {
        _reveal.Sync(Max);
        _stack.Children.Clear();

        if (!string.IsNullOrEmpty(Title))
        {
            var title = DisplayParts.NewLabel(14, KlyTheme.TextHighlighted, FontAttributes.Bold);
            title.Text = Title;
            _stack.Children.Add(title);
        }

        var (visible, remaining) = _reveal.Apply(Items, Max, Step);
        var numbered = Variant is KlyListVariant.Steps or KlyListVariant.Checks;
        for (var i = 0; i < visible.Count; i++) _stack.Children.Add(BuildRow(visible[i], i, numbered));

        if (remaining > 0 && Items is { } items)
        {
            _stack.Children.Add(DisplayParts.TextButton(string.Format(ShowMoreFormat, remaining), KlyTheme.TextMuted, 12, () =>
            {
                _reveal.More(items.Count, Max, Step);
                Rebuild();
            }));
        }
    }

    private Grid BuildRow(KlyListItem item, int index, bool numbered)
    {
        var row = new Grid { ColumnSpacing = 12 };
        row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

        var lead = new HorizontalStackLayout { Spacing = 10, VerticalOptions = LayoutOptions.Center };
        lead.Children.Add(numbered ? Marker(item, index) : Bullet(item));

        var body = new VerticalStackLayout { VerticalOptions = LayoutOptions.Center };
        if (!string.IsNullOrEmpty(item.Href))
        {
            body.Children.Add(DisplayParts.LinkLabel(item.Text, KlyTheme.TextHighlighted, 14, item.Href, null, FontAttributes.Bold));
        }
        else
        {
            var text = DisplayParts.NewLabel(14, KlyTheme.Text, lineBreak: LineBreakMode.TailTruncation);
            text.Text = item.Text;
            body.Children.Add(text);
        }
        if (!string.IsNullOrEmpty(item.Description))
        {
            var description = DisplayParts.NewLabel(12, KlyTheme.TextMuted);
            description.Text = item.Description;
            body.Children.Add(description);
        }
        lead.Children.Add(body);
        row.Add(lead, 0);

        if (!string.IsNullOrEmpty(item.Meta))
        {
            var meta = DisplayParts.NewLabel(12, KlyTheme.TextMuted);
            meta.Text = item.Meta;
            meta.VerticalOptions = LayoutOptions.Center;
            row.Add(meta, 1);
        }

        // A done step says so, and the current one too: the marker alone is a color and a number.
        if (numbered && (item.Done || item.Current))
        {
            var parts = new List<string> { item.Text };
            if (item.Done) parts.Add(StepCompletedLabel);
            if (item.Current) parts.Add(CurrentStepLabel);
            SemanticProperties.SetDescription(row, string.Join(", ", parts));
        }
        return row;
    }

    private static View Bullet(KlyListItem item)
    {
        var color = item.Color is { } c ? KlyTheme.Resolve(c) : (KlyColorSet?)null;
        View visual;
        if (!string.IsNullOrEmpty(item.Icon))
        {
            visual = new KlyIcon { Name = item.Icon, Size = KlySize.Sm, IconColor = color?.Text ?? KlyTheme.TextMuted, VerticalOptions = LayoutOptions.Center };
        }
        else
        {
            var dot = DisplayParts.Tile(6, 3, color?.Fill ?? KlyTheme.TextMuted);
            dot.Margin = new Thickness(5, 0);
            visual = dot;
        }
        AutomationProperties.SetIsInAccessibleTree(visual, false);
        return visual;
    }

    private static View Marker(KlyListItem item, int index)
    {
        var success = KlyTheme.Resolve(KlyColor.Success);
        var primary = KlyTheme.Resolve(KlyColor.Primary);
        var (background, foreground) = item.Done
            ? (success.Fill.WithAlpha(0.15f), success.Text)
            : item.Current ? (primary.Fill, primary.Contrast) : (KlyTheme.BackgroundMuted, KlyTheme.TextMuted);
        var marker = DisplayParts.Tile(20, 10, background);
        if (item.Done)
        {
            marker.Content = DisplayParts.SmallIcon(KlyIconNames.Check, foreground);
        }
        else
        {
            var number = DisplayParts.NewLabel(12, foreground, FontAttributes.Bold, LineBreakMode.NoWrap);
            number.Text = (index + 1).ToString(System.Globalization.CultureInfo.CurrentCulture);
            number.HorizontalOptions = LayoutOptions.Center;
            number.VerticalOptions = LayoutOptions.Center;
            marker.Content = number;
        }
        AutomationProperties.SetIsInAccessibleTree(marker, false);
        return marker;
    }
}
