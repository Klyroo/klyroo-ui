namespace Klyroo.UI.Maui;

/// <summary>A list of label and value rows, flat or in titled groups, with badges, links, copy buttons and loading placeholders.</summary>
public class KlyFacts : KlyBlockBase
{
    /// <summary>Identifies <see cref="Items"/>.</summary>
    public static readonly BindableProperty ItemsProperty = BindableProperty.Create(nameof(Items), typeof(IReadOnlyList<KlyFact>), typeof(KlyFacts), null, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Groups"/>.</summary>
    public static readonly BindableProperty GroupsProperty = BindableProperty.Create(nameof(Groups), typeof(IReadOnlyList<KlyFactGroup>), typeof(KlyFacts), null, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Max"/>.</summary>
    public static readonly BindableProperty MaxProperty = BindableProperty.Create(nameof(Max), typeof(int?), typeof(KlyFacts), null, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Step"/>.</summary>
    public static readonly BindableProperty StepProperty = BindableProperty.Create(nameof(Step), typeof(int?), typeof(KlyFacts), null, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="ShowMoreFormat"/>.</summary>
    public static readonly BindableProperty ShowMoreFormatProperty = BindableProperty.Create(nameof(ShowMoreFormat), typeof(string), typeof(KlyFacts), "Show more · {0}", propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="CopyLabelFormat"/>.</summary>
    public static readonly BindableProperty CopyLabelFormatProperty = BindableProperty.Create(nameof(CopyLabelFormat), typeof(string), typeof(KlyFacts), "Copy {0}", propertyChanged: Rebuild);

    private readonly VerticalStackLayout _stack = new() { Spacing = 12 };
    private readonly DisplayReveal _reveal = new();

    /// <summary>Creates a block of facts.</summary>
    public KlyFacts() : base(KlySurface.Bordered)
    {
        Body.Add(_stack);
        Rebuild();
    }

    /// <summary>The label and value rows, flat. They are ignored when <see cref="Groups"/> is set.</summary>
    public IReadOnlyList<KlyFact>? Items { get => (IReadOnlyList<KlyFact>?)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }

    /// <summary>The titled groups of rows. When set, they replace <see cref="Items"/>, and <see cref="Max"/> does not apply.</summary>
    public IReadOnlyList<KlyFactGroup>? Groups { get => (IReadOnlyList<KlyFactGroup>?)GetValue(GroupsProperty); set => SetValue(GroupsProperty, value); }

    /// <summary>The number of rows shown before a Show more button (all rows when <see langword="null"/>). It applies to <see cref="Items"/> only.</summary>
    public int? Max { get => (int?)GetValue(MaxProperty); set => SetValue(MaxProperty, value); }

    /// <summary>The number of rows each press of Show more reveals (all the remaining ones when <see langword="null"/>).</summary>
    public int? Step { get => (int?)GetValue(StepProperty); set => SetValue(StepProperty, value); }

    /// <summary>The text of the Show more button, where <c>{0}</c> is the number of rows the next press reveals, in the language of the app.</summary>
    public string ShowMoreFormat { get => (string)GetValue(ShowMoreFormatProperty); set => SetValue(ShowMoreFormatProperty, value); }

    /// <summary>The accessible name of a copy button, where <c>{0}</c> is the label of the row it copies, in the language of the app.</summary>
    public string CopyLabelFormat { get => (string)GetValue(CopyLabelFormatProperty); set => SetValue(CopyLabelFormatProperty, value); }

    /// <summary>Copies a text instead of the platform clipboard; for tests.</summary>
    internal Func<string, Task>? Copier { get; set; }

    /// <summary>The stack of groups or rows, for tests.</summary>
    internal VerticalStackLayout Stack => _stack;

    private static void Rebuild(BindableObject bindable, object oldValue, object newValue) => ((KlyFacts)bindable).Rebuild();

    private void Rebuild()
    {
        _reveal.Sync(Max);
        _stack.Children.Clear();

        if (Groups is { Count: > 0 } groups)
        {
            foreach (var group in groups) _stack.Children.Add(BuildGroup(group));
        }
        else if (Items is { Count: > 0 } items)
        {
            var (visible, remaining) = _reveal.Apply(items, Max, Step);
            _stack.Children.Add(BuildList(visible));
            if (remaining > 0)
            {
                _stack.Children.Add(DisplayParts.TextButton(string.Format(ShowMoreFormat, remaining), KlyTheme.TextMuted, 12, () =>
                {
                    _reveal.More(items.Count, Max, Step);
                    Rebuild();
                }));
            }
        }
        else
        {
            var none = DisplayParts.NewLabel(12, KlyTheme.TextMuted);
            none.Text = "—";
            _stack.Children.Add(none);
        }
    }

    private View BuildGroup(KlyFactGroup group)
    {
        var box = new VerticalStackLayout { Spacing = 8 };
        if (!string.IsNullOrEmpty(group.Title) || !string.IsNullOrEmpty(group.Aside))
        {
            var header = new Grid { ColumnSpacing = 8 };
            header.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            header.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            var title = DisplayParts.NewLabel(12, KlyTheme.TextHighlighted, FontAttributes.Bold);
            title.Text = group.Title;
            header.Add(title, 0);
            if (!string.IsNullOrEmpty(group.Aside))
            {
                var aside = DisplayParts.NewLabel(12, KlyTheme.TextMuted);
                aside.Text = group.Aside;
                header.Add(aside, 1);
            }
            box.Children.Add(header);
            box.Children.Add(DisplayParts.Divider(KlyTheme.Border));
        }
        box.Children.Add(BuildList(group.Items));
        return box;
    }

    private View BuildList(IReadOnlyList<KlyFact> facts)
    {
        var list = new VerticalStackLayout { Spacing = 8 };
        for (var i = 0; i < facts.Count; i++)
        {
            if (i > 0) list.Children.Add(DisplayParts.Divider(KlyTheme.Border));
            list.Children.Add(BuildRow(facts[i]));
        }
        return list;
    }

    private View BuildRow(KlyFact fact)
    {
        var row = new Grid { ColumnSpacing = 12 };
        row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

        // The label, with its icon and its hint.
        var labelRow = new HorizontalStackLayout { Spacing = 6 };
        if (!string.IsNullOrEmpty(fact.Icon))
        {
            var icon = DisplayParts.SmallIcon(fact.Icon, KlyTheme.TextMuted);
            AutomationProperties.SetIsInAccessibleTree(icon, false);
            labelRow.Children.Add(icon);
        }
        var label = DisplayParts.NewLabel(12, KlyTheme.TextMuted);
        label.Text = fact.Label;
        labelRow.Children.Add(label);
        var term = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center, Children = { labelRow } };
        if (!string.IsNullOrEmpty(fact.Hint))
        {
            var hint = DisplayParts.NewLabel(12, KlyTheme.TextDimmed);
            hint.Text = fact.Hint;
            term.Children.Add(hint);
        }
        row.Add(term, 0);

        // The value: a placeholder, a badge, a dash, a link or the text.
        var isEmpty = string.IsNullOrWhiteSpace(fact.Value) && fact.Badge is null;
        var value = new HorizontalStackLayout { Spacing = 6, HorizontalOptions = LayoutOptions.End, VerticalOptions = LayoutOptions.Center };
        if (fact.Loading)
        {
            value.Children.Add(new KlySkeleton { WidthRequest = 80, HeightRequest = 16, HorizontalOptions = LayoutOptions.End });
        }
        else if (fact.Badge is { } badge)
        {
            value.Children.Add(DisplayParts.Badge(badge));
        }
        else if (isEmpty)
        {
            var empty = DisplayParts.NewLabel(14, KlyTheme.TextDimmed);
            empty.Text = "—";
            value.Children.Add(empty);
        }
        else if (!string.IsNullOrEmpty(fact.Href))
        {
            value.Children.Add(DisplayParts.LinkLabel(fact.Value, DisplayParts.PrimaryText, 14, fact.Href, null));
        }
        else
        {
            var text = fact.Emphasis ? KlyTheme.TextHighlighted : fact.Color is { } color ? KlyTheme.Resolve(color).Text : KlyTheme.Text;
            var label2 = DisplayParts.NewLabel(14, text, fact.Emphasis ? FontAttributes.Bold : FontAttributes.None);
            label2.Text = fact.Value;
            label2.HorizontalTextAlignment = TextAlignment.End;
            label2.VerticalTextAlignment = TextAlignment.Center;
            if (fact.Mono) label2.FontFamily = DisplayParts.MonoFont();
            value.Children.Add(label2);
        }

        if ((fact.Copy || !string.IsNullOrEmpty(fact.CopyText)) && !isEmpty)
        {
            var text = fact.CopyText ?? fact.Value;
            var copy = new DisplayIconButton(KlyIconNames.Copy, string.Format(CopyLabelFormat, fact.Label)) { Margin = new Thickness(0, -8) };
            copy.Clicked += (_, _) => DisplayClipboard.Copy(text, Copier);
            value.Children.Add(copy);
        }
        row.Add(value, 1);
        return row;
    }
}
