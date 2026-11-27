using System.Globalization;

namespace Klyroo.UI.Maui;

/// <summary>Before and after values per field, with unchanged fields hidden unless <see cref="ShowUnchanged"/> is set.</summary>
public class KlyDiff : KlyBlockBase
{
    /// <summary>Identifies <see cref="Title"/>.</summary>
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(KlyDiff), null, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Items"/>.</summary>
    public static readonly BindableProperty ItemsProperty = BindableProperty.Create(nameof(Items), typeof(IReadOnlyList<KlyDiffItem>), typeof(KlyDiff), null, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Culture"/>.</summary>
    public static readonly BindableProperty CultureProperty = BindableProperty.Create(nameof(Culture), typeof(CultureInfo), typeof(KlyDiff), null, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="ShowUnchanged"/>.</summary>
    public static readonly BindableProperty ShowUnchangedProperty = BindableProperty.Create(nameof(ShowUnchanged), typeof(bool), typeof(KlyDiff), false, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="ChangedFormat"/>.</summary>
    public static readonly BindableProperty ChangedFormatProperty = BindableProperty.Create(nameof(ChangedFormat), typeof(string), typeof(KlyDiff), "{0}: was {1}, now {2}", propertyChanged: Rebuild);

    private readonly VerticalStackLayout _stack = new() { Spacing = 8 };

    /// <summary>Creates a diff.</summary>
    public KlyDiff() : base(KlySurface.Plain, 8)
    {
        Body.Add(_stack);
        Rebuild();
    }

    /// <summary>The title of the block.</summary>
    public string? Title { get => (string?)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    /// <summary>The fields, as <see cref="KlyDiffItem"/>.</summary>
    public IReadOnlyList<KlyDiffItem>? Items { get => (IReadOnlyList<KlyDiffItem>?)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }

    /// <summary>The culture that formats the values of the items built with <see cref="KlyDiffItem.Compare{T}(string, T, T, string, IEqualityComparer{T})"/>. Defaults to the culture of the current thread.</summary>
    public CultureInfo? Culture { get => (CultureInfo?)GetValue(CultureProperty); set => SetValue(CultureProperty, value); }

    /// <summary>Shows the fields whose <see cref="KlyDiffItem.Changed"/> is <see langword="false"/> (<see langword="false"/> by default).</summary>
    public bool ShowUnchanged { get => (bool)GetValue(ShowUnchangedProperty); set => SetValue(ShowUnchangedProperty, value); }

    /// <summary>What a screen reader says for a changed field, where <c>{0}</c> is the label, <c>{1}</c> the previous value and <c>{2}</c> the new one, in the language of the app. The strike-through and the colors say it to the eyes only.</summary>
    public string ChangedFormat { get => (string)GetValue(ChangedFormatProperty); set => SetValue(ChangedFormatProperty, value); }

    internal VerticalStackLayout Stack => _stack;

    private CultureInfo ItemCulture => Culture ?? CultureInfo.CurrentCulture;

    // An item built from typed values formats them here, with the culture of the block; a plain item shows its text.
    private string? BeforeText(KlyDiffItem item) => item.BeforeFor is { } format ? format(ItemCulture) : item.Before;

    private string? AfterText(KlyDiffItem item) => item.AfterFor is { } format ? format(ItemCulture) : item.After;

    private static void Rebuild(BindableObject bindable, object oldValue, object newValue) => ((KlyDiff)bindable).Rebuild();

    private void Rebuild()
    {
        _stack.Children.Clear();
        if (!string.IsNullOrEmpty(Title))
        {
            var title = DisplayParts.NewLabel(14, KlyTheme.TextHighlighted, FontAttributes.Bold);
            title.Text = Title;
            _stack.Children.Add(title);
        }

        var visible = (Items ?? []).Where(d => ShowUnchanged || d.Changed).ToList();
        for (var i = 0; i < visible.Count; i++)
        {
            if (i > 0) _stack.Children.Add(DisplayParts.Divider(KlyTheme.Border));
            _stack.Children.Add(Row(visible[i]));
        }
    }

    private View Row(KlyDiffItem item)
    {
        var row = new Grid { ColumnSpacing = 8 };
        row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

        var label = new HorizontalStackLayout { Spacing = 6, VerticalOptions = LayoutOptions.Center };
        var text = DisplayParts.NewLabel(12, KlyTheme.TextMuted);
        text.Text = item.Label;
        label.Children.Add(text);
        if (item.Note is { } note) label.Children.Add(DisplayParts.Badge(note));
        row.Add(label, 0);

        var before = BeforeText(item);
        var after = AfterText(item);
        var values = new HorizontalStackLayout { Spacing = 8, VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.End };
        if (item.Changed)
        {
            if (!string.IsNullOrEmpty(before))
            {
                var old = DisplayParts.NewLabel(12, KlyTheme.Resolve(KlyColor.Error).Text);
                old.Text = before;
                old.TextDecorations = TextDecorations.Strikethrough;
                values.Children.Add(old);
            }
            if (!string.IsNullOrEmpty(before) && !string.IsNullOrEmpty(after))
            {
                var arrow = DisplayParts.NewLabel(12, KlyTheme.TextMuted);
                arrow.Text = "→";
                AutomationProperties.SetIsInAccessibleTree(arrow, false);
                values.Children.Add(arrow);
            }
            var now = DisplayParts.NewLabel(12, KlyTheme.Resolve(KlyColor.Success).Text, FontAttributes.Bold);
            now.Text = after ?? "—";
            values.Children.Add(now);
            SemanticProperties.SetDescription(row, string.Format(ChangedFormat, item.Label, before ?? "—", after ?? "—"));
        }
        else
        {
            var same = DisplayParts.NewLabel(12, KlyTheme.TextMuted);
            same.Text = after ?? before ?? "—";
            values.Children.Add(same);
        }
        row.Add(values, 1);
        return row;
    }
}
