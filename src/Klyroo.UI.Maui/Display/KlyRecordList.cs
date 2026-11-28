using System.Globalization;

namespace Klyroo.UI.Maui;

/// <summary>A list of entity rows, each with an image, avatar or icon, a reference, a title, a secondary line, badges and a trailing value.</summary>
public class KlyRecordList : KlyBlockBase
{
    /// <summary>Identifies <see cref="Title"/>.</summary>
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(KlyRecordList), null, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Count"/>.</summary>
    public static readonly BindableProperty CountProperty = BindableProperty.Create(nameof(Count), typeof(int?), typeof(KlyRecordList), null, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Items"/>.</summary>
    public static readonly BindableProperty ItemsProperty = BindableProperty.Create(nameof(Items), typeof(IReadOnlyList<KlyRecordItem>), typeof(KlyRecordList), null, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Max"/>.</summary>
    public static readonly BindableProperty MaxProperty = BindableProperty.Create(nameof(Max), typeof(int?), typeof(KlyRecordList), null, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Step"/>.</summary>
    public static readonly BindableProperty StepProperty = BindableProperty.Create(nameof(Step), typeof(int?), typeof(KlyRecordList), null, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="ShowMoreFormat"/>.</summary>
    public static readonly BindableProperty ShowMoreFormatProperty = BindableProperty.Create(nameof(ShowMoreFormat), typeof(string), typeof(KlyRecordList), "Show more · {0}", propertyChanged: Rebuild);

    private readonly VerticalStackLayout _stack = new() { Spacing = 10 };
    private readonly DisplayReveal _reveal = new();

    /// <summary>Creates a record list.</summary>
    public KlyRecordList() : base(KlySurface.Bordered, 10)
    {
        Body.Add(_stack);
        Rebuild();
    }

    /// <summary>The title above the rows.</summary>
    public string? Title { get => (string?)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    /// <summary>A count shown beside the title.</summary>
    public int? Count { get => (int?)GetValue(CountProperty); set => SetValue(CountProperty, value); }

    /// <summary>The rows, as <see cref="KlyRecordItem"/>.</summary>
    public IReadOnlyList<KlyRecordItem>? Items { get => (IReadOnlyList<KlyRecordItem>?)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }

    /// <summary>The number of rows shown before a Show more button (all rows when <see langword="null"/>).</summary>
    public int? Max { get => (int?)GetValue(MaxProperty); set => SetValue(MaxProperty, value); }

    /// <summary>The number of rows each press of Show more reveals (all the remaining ones when <see langword="null"/>).</summary>
    public int? Step { get => (int?)GetValue(StepProperty); set => SetValue(StepProperty, value); }

    /// <summary>The text of the Show more button, where <c>{0}</c> is the number of rows the next press reveals, in the language of the app.</summary>
    public string ShowMoreFormat { get => (string)GetValue(ShowMoreFormatProperty); set => SetValue(ShowMoreFormatProperty, value); }

    internal VerticalStackLayout Stack => _stack;

    private static void Rebuild(BindableObject bindable, object oldValue, object newValue) => ((KlyRecordList)bindable).Rebuild();

    private void Rebuild()
    {
        _reveal.Sync(Max);
        _stack.Children.Clear();

        if (!string.IsNullOrEmpty(Title))
        {
            var header = new Grid { ColumnSpacing = 8 };
            header.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            header.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            var title = DisplayParts.NewLabel(14, KlyTheme.TextHighlighted, FontAttributes.Bold);
            title.Text = Title;
            header.Add(title, 0);
            if (Count is { } count)
            {
                var number = DisplayParts.NewLabel(12, KlyTheme.TextMuted);
                number.Text = count.ToString("N0", CultureInfo.CurrentCulture);
                number.VerticalOptions = LayoutOptions.Center;
                header.Add(number, 1);
            }
            _stack.Children.Add(header);
        }

        var (visible, remaining) = _reveal.Apply(Items, Max, Step);
        foreach (var record in visible) _stack.Children.Add(Row(record));

        if (remaining > 0 && Items is { } items)
        {
            _stack.Children.Add(DisplayParts.TextButton(string.Format(ShowMoreFormat, remaining), KlyTheme.TextMuted, 12, () =>
            {
                _reveal.More(items.Count, Max, Step);
                Rebuild();
            }));
        }
    }

    private static View? Leading(KlyRecordItem record)
    {
        var radius = KlyTheme.RadiusOf(1);
        if (record.ImageSrc is not null)
        {
            // The picture is decorative: the title names the row.
            var frame = DisplayParts.Tile(32, radius, KlyTheme.BackgroundMuted);
            frame.Content = new Image { Source = record.ImageSrc, Aspect = Aspect.AspectFill };
            AutomationProperties.SetIsInAccessibleTree(frame, false);
            return frame;
        }

        if (string.IsNullOrEmpty(record.AvatarText) && string.IsNullOrEmpty(record.Icon)) return null;
        var set = record.Color is { } color ? KlyTheme.Resolve(color) : (KlyColorSet?)null;
        var tile = DisplayParts.Tile(32, radius, set?.Fill.WithAlpha(0.15f) ?? KlyTheme.BackgroundMuted);
        var foreground = set?.Text ?? KlyTheme.TextHighlighted;
        if (!string.IsNullOrEmpty(record.Icon))
        {
            tile.Content = new KlyIcon { Name = record.Icon, Size = KlySize.Sm, IconColor = foreground, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
        }
        else
        {
            var text = DisplayParts.NewLabel(12, foreground, FontAttributes.Bold, LineBreakMode.NoWrap);
            text.Text = record.AvatarText;
            text.HorizontalOptions = LayoutOptions.Center;
            text.VerticalOptions = LayoutOptions.Center;
            tile.Content = text;
        }
        AutomationProperties.SetIsInAccessibleTree(tile, false);
        return tile;
    }

    private static View Row(KlyRecordItem record)
    {
        var row = new Grid { ColumnSpacing = 12 };
        row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

        var lead = new HorizontalStackLayout { Spacing = 12, VerticalOptions = LayoutOptions.Center };
        if (Leading(record) is { } visual) lead.Children.Add(visual);

        var body = new VerticalStackLayout { VerticalOptions = LayoutOptions.Center };
        if (!string.IsNullOrEmpty(record.Reference))
        {
            var reference = DisplayParts.NewLabel(11, KlyTheme.TextMuted, lineBreak: LineBreakMode.TailTruncation);
            reference.Text = record.Reference;
            reference.FontFamily = DisplayParts.MonoFont();
            body.Children.Add(reference);
        }
        if (!string.IsNullOrEmpty(record.Href) || record.OnClick is not null)
        {
            body.Children.Add(DisplayParts.LinkLabel(record.Title, KlyTheme.TextHighlighted, 14, record.Href, record.OnClick, FontAttributes.Bold));
        }
        else
        {
            var title = DisplayParts.NewLabel(14, KlyTheme.TextHighlighted, FontAttributes.Bold, LineBreakMode.TailTruncation);
            title.Text = record.Title;
            body.Children.Add(title);
        }
        if (!string.IsNullOrEmpty(record.Description))
        {
            var meta = new HorizontalStackLayout { Spacing = 4 };
            if (!string.IsNullOrEmpty(record.DescriptionIcon))
            {
                var icon = DisplayParts.SmallIcon(record.DescriptionIcon, KlyTheme.TextMuted);
                AutomationProperties.SetIsInAccessibleTree(icon, false);
                meta.Children.Add(icon);
            }
            var description = DisplayParts.NewLabel(12, KlyTheme.TextMuted, lineBreak: LineBreakMode.TailTruncation);
            description.Text = record.Description;
            meta.Children.Add(description);
            body.Children.Add(meta);
        }
        lead.Children.Add(body);
        row.Add(lead, 0);

        var trail = new HorizontalStackLayout { Spacing = 8, VerticalOptions = LayoutOptions.Center };
        foreach (var badge in record.Badges ?? []) trail.Children.Add(DisplayParts.Badge(badge));
        if (record.Badge is { } single) trail.Children.Add(DisplayParts.Badge(single));
        if (!string.IsNullOrEmpty(record.Trailing) || !string.IsNullOrEmpty(record.TrailingCaption))
        {
            var block = new VerticalStackLayout { HorizontalOptions = LayoutOptions.End };
            if (!string.IsNullOrEmpty(record.Trailing))
            {
                var value = DisplayParts.NewLabel(14, record.Color is { } color ? KlyTheme.Resolve(color).Text : KlyTheme.TextHighlighted, FontAttributes.Bold, LineBreakMode.NoWrap);
                value.Text = record.Trailing;
                value.HorizontalTextAlignment = TextAlignment.End;
                block.Children.Add(value);
            }
            if (!string.IsNullOrEmpty(record.TrailingCaption))
            {
                var caption = DisplayParts.NewLabel(12, KlyTheme.TextMuted, lineBreak: LineBreakMode.NoWrap);
                caption.Text = record.TrailingCaption;
                caption.HorizontalTextAlignment = TextAlignment.End;
                block.Children.Add(caption);
            }
            trail.Children.Add(block);
        }
        row.Add(trail, 1);
        return row;
    }
}
