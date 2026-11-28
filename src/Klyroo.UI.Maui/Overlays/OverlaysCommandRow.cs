using System.Globalization;

namespace Klyroo.UI.Maui;

/// <summary>A command of the palette: icon, label and description, a count and a shortcut. Touch targets are at least 44 points high.</summary>
internal sealed class OverlaysCommandRow : Border
{
    private bool _highlighted;

    public OverlaysCommandRow(KlyCommandGroup group, KlyCommandItem item)
    {
        Group = group;
        Item = item;
        this.SetRadius(KlyTheme.RadiusOf(1));
        StrokeThickness = 0;
        Padding = new Thickness(10, 8);
        MinimumHeightRequest = 44;
        BackgroundColor = Colors.Transparent;

        var grid = new Grid { ColumnSpacing = 12, VerticalOptions = LayoutOptions.Center };
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        if (!string.IsNullOrEmpty(item.Icon)) grid.Add(new KlyIcon { Name = item.Icon, Size = KlySize.Sm, IconColor = KlyTheme.TextMuted, VerticalOptions = LayoutOptions.Center }, 0);

        var label = new Label { Text = item.Label, FontSize = 14, LineBreakMode = LineBreakMode.TailTruncation };
        label.SetThemeColor(Label.TextColorProperty, KlyTheme.TextHighlighted);
        var wrapper = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center, Children = { label } };
        if (!string.IsNullOrEmpty(item.Description))
        {
            var description = new Label { Text = item.Description, FontSize = 12, LineBreakMode = LineBreakMode.TailTruncation };
            description.SetThemeColor(Label.TextColorProperty, KlyTheme.TextMuted);
            wrapper.Add(description);
        }
        grid.Add(wrapper, 1);

        if (item.Count is { } count)
        {
            var text = new Label { Text = count.ToString("N0", CultureInfo.CurrentCulture), FontSize = 12, Padding = new Thickness(8, 0, 0, 0), VerticalOptions = LayoutOptions.Center };
            text.SetThemeColor(Label.TextColorProperty, KlyTheme.TextMuted);
            grid.Add(text, 2);
        }
        if (!string.IsNullOrEmpty(item.Shortcut))
        {
            var shortcut = new Label { Text = item.Shortcut, FontSize = 12, Padding = new Thickness(16, 0, 0, 0), VerticalOptions = LayoutOptions.Center };
            shortcut.SetThemeColor(Label.TextColorProperty, KlyTheme.TextMuted);
            grid.Add(shortcut, 3);
        }
        Content = grid;

        // A screen reader reads the label, then the count, as the hidden separator of the web row does.
        SemanticProperties.SetDescription(this, item.Count is { } n ? $"{item.Label}, {n.ToString("N0", CultureInfo.CurrentCulture)}" : item.Label);
        SemanticProperties.SetHint(this, item.Description);
        if (!item.IsEnabled)
        {
            IsEnabled = false;
            Opacity = 0.6;
        }

        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => Activated?.Invoke(this, EventArgs.Empty);
        GestureRecognizers.Add(tap);
        var pointer = new PointerGestureRecognizer();
        pointer.PointerEntered += (_, _) =>
        {
            if (item.IsEnabled) Hovered?.Invoke(this, EventArgs.Empty);
        };
        GestureRecognizers.Add(pointer);
    }

    public KlyCommandGroup Group { get; }

    public KlyCommandItem Item { get; }

    /// <summary>Whether the row is the one Enter activates.</summary>
    public bool Highlighted
    {
        get => _highlighted;
        set
        {
            if (_highlighted == value) return;
            _highlighted = value;
            if (value) this.SetThemeColor(BackgroundColorProperty, KlyTheme.BackgroundElevated);
            else BackgroundColor = Colors.Transparent;
        }
    }

    /// <summary>Raised when the user taps the row (also a disabled one: the palette ignores it).</summary>
    public event EventHandler? Activated;

    /// <summary>Raised when a pointer moves over an enabled row.</summary>
    public event EventHandler? Hovered;

    /// <summary>Activates the row as a tap does.</summary>
    public void Activate() => Activated?.Invoke(this, EventArgs.Empty);
}
