using System.Globalization;

namespace Klyroo.UI.Maui;

/// <summary>A row of an <see cref="InputsOptionList"/>: the leading icon, the label, the count and the check mark of a selected option.</summary>
internal sealed class InputsOptionRow : Border
{
    private readonly Label _label;
    private readonly KlyIcon _check;

    public InputsOptionRow(InputsOption option, bool selected, KlyColor color, double fontSize)
    {
        Option = option;
        Selected = selected;
        StrokeThickness = 0;
        BackgroundColor = Colors.Transparent;
        this.SetRadius(KlyTheme.RadiusOf(1) - 2);
        Padding = new Thickness(8, 6);
        MinimumHeightRequest = 40;

        var grid = new Grid { ColumnSpacing = 6, VerticalOptions = LayoutOptions.Center };
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

        if (option.Icon is { } icon)
        {
            grid.Add(new KlyIcon { Name = icon, Size = KlySize.Sm, IconColor = KlyTheme.TextMuted, VerticalOptions = LayoutOptions.Center }, 0);
        }

        _label = new Label { Text = option.Label, FontSize = fontSize, LineBreakMode = LineBreakMode.TailTruncation, VerticalOptions = LayoutOptions.Center };
        _label.SetThemeColor(Label.TextColorProperty, KlyTheme.Text);
        grid.Add(_label, 1);

        if (option.Count is { } count)
        {
            var countLabel = new Label { Text = count.ToString("N0", CultureInfo.CurrentCulture), FontSize = 12, VerticalOptions = LayoutOptions.Center };
            countLabel.SetThemeColor(Label.TextColorProperty, KlyTheme.TextMuted);
            grid.Add(countLabel, 2);
        }

        // The check keeps its place when hidden, so the labels do not move when an option is selected.
        _check = new KlyIcon { Name = KlyIconNames.Check, Size = KlySize.Sm, IconColor = KlyTheme.Resolve(color).Text, VerticalOptions = LayoutOptions.Center, Opacity = selected ? 1 : 0 };
        grid.Add(_check, 3);
        Content = grid;

        SemanticProperties.SetDescription(this, Describe());
        if (option.Disabled) Opacity = 0.6;
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => Choose();
        GestureRecognizers.Add(tap);
    }

    public InputsOption Option { get; }

    public bool Selected { get; }

    public string LabelText => _label.Text;

    public bool CheckVisible => _check.Opacity > 0;

    public event EventHandler<InputsOption>? Chosen;

    public void Choose()
    {
        if (IsEnabled && !Option.Disabled) Chosen?.Invoke(this, Option);
    }

    private string Describe()
    {
        var text = Option.Label;
        if (Option.Count is { } count) text += ", " + count.ToString("N0", CultureInfo.CurrentCulture);
        return Selected ? text + ", selected" : text;
    }
}

/// <summary>The rows of a list of options under their headings, for a listbox and for the lists of the menus. It draws the rows and reports a tap, and keeps no selection of its own.</summary>
internal sealed class InputsOptionList : VerticalStackLayout
{
    private readonly List<InputsOptionRow> _rows = [];

    public InputsOptionList()
    {
        Spacing = 0;
    }

    public IReadOnlyList<InputsOptionRow> Rows => _rows;

    public event EventHandler<InputsOption>? Chosen;

    /// <summary>Builds the rows again.</summary>
    /// <param name="options">The options to list.</param>
    /// <param name="isSelected">Says whether an option shows the check mark.</param>
    /// <param name="color">Semantic color of the check mark.</param>
    /// <param name="fontSize">Font size of the labels.</param>
    public void Show(IReadOnlyList<InputsOption> options, Func<InputsOption, bool> isSelected, KlyColor color, double fontSize)
    {
        Children.Clear();
        _rows.Clear();
        foreach (var (heading, list) in InputsOptions.Sections(options))
        {
            if (heading is not null)
            {
                var title = new Label { Text = heading, FontSize = 12, FontAttributes = FontAttributes.Bold, Padding = new Thickness(8, 6) };
                title.SetThemeColor(Label.TextColorProperty, KlyTheme.TextMuted);
                SemanticProperties.SetHeadingLevel(title, SemanticHeadingLevel.Level2);
                Children.Add(title);
            }
            foreach (var option in list)
            {
                var row = new InputsOptionRow(option, isSelected(option), color, fontSize);
                row.Chosen += (_, chosen) => Chosen?.Invoke(this, chosen);
                _rows.Add(row);
                Children.Add(row);
            }
        }
    }
}
