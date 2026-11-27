namespace Klyroo.UI.Maui;

/// <summary>A row of a menu: the icon, the label, the shortcut and, for an item with a submenu, a chevron. Touch targets are at least 40 points high.</summary>
internal sealed class OverlaysMenuRow : Border
{
    public OverlaysMenuRow(KlyMenuItem item, bool hasSubmenu)
    {
        Item = item;
        HasSubmenu = hasSubmenu;
        this.SetRadius(KlyTheme.RadiusOf(1) - 2);
        StrokeThickness = 0;
        Padding = new Thickness(8, 6);
        MinimumHeightRequest = 40;
        BackgroundColor = Colors.Transparent;

        var error = item.Color == KlyColor.Error;
        var tint = error ? KlyTheme.Resolve(KlyColor.Error).Text : KlyTheme.Text;
        var muted = error ? tint : KlyTheme.TextMuted;
        var grid = new Grid { ColumnSpacing = 6, VerticalOptions = LayoutOptions.Center };
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        if (!string.IsNullOrEmpty(item.Icon)) grid.Add(new KlyIcon { Name = item.Icon, Size = KlySize.Sm, IconColor = muted }, 0);
        var label = new Label { Text = item.Label, FontSize = 14, LineBreakMode = LineBreakMode.TailTruncation, VerticalOptions = LayoutOptions.Center };
        label.SetThemeColor(Label.TextColorProperty, tint);
        grid.Add(label, 1);
        if (!string.IsNullOrEmpty(item.Shortcut))
        {
            var shortcut = new Label { Text = item.Shortcut, FontSize = 12, Margin = new Thickness(16, 0, 0, 0), VerticalOptions = LayoutOptions.Center };
            shortcut.SetThemeColor(Label.TextColorProperty, KlyTheme.TextDimmed);
            grid.Add(shortcut, 2);
        }
        if (hasSubmenu) grid.Add(new KlyIcon { Name = KlyIconNames.ChevronRight, Size = KlySize.Sm, IconColor = muted }, 3);
        Content = grid;

        SemanticProperties.SetDescription(this, item.Label);
        SemanticProperties.SetHint(this, item.Description);
        if (!item.IsEnabled)
        {
            IsEnabled = false;
            Opacity = 0.6;
        }

        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => Activate();
        GestureRecognizers.Add(tap);
        var pointer = new PointerGestureRecognizer();
        pointer.PointerEntered += (_, _) =>
        {
            if (item.IsEnabled) this.SetThemeColor(BackgroundColorProperty, KlyTheme.BackgroundElevated);
        };
        pointer.PointerExited += (_, _) => BackgroundColor = Colors.Transparent;
        GestureRecognizers.Add(pointer);
    }

    public KlyMenuItem Item { get; }

    /// <summary>True for an item that opens a submenu instead of being selected.</summary>
    public bool HasSubmenu { get; }

    /// <summary>Raised when the user activates an enabled row.</summary>
    public event EventHandler? Activated;

    /// <summary>Activates the row, as a tap does. A disabled row ignores it.</summary>
    public void Activate()
    {
        if (Item.IsEnabled) Activated?.Invoke(this, EventArgs.Empty);
    }
}

/// <summary>The panel of <see cref="KlyDropdownMenu"/> and <see cref="KlyContextMenu"/>: headings, separators and rows on a raised surface.</summary>
internal sealed class OverlaysMenu : Border
{
    private readonly List<OverlaysMenuRow> _rows = [];

    public OverlaysMenu(IReadOnlyList<KlyMenuItem> items, bool submenus, string label)
    {
        Padding = new Thickness(4);
        MinimumWidthRequest = 176;
        this.ApplySurface(KlyTheme.RadiusOf(1));
        SemanticProperties.SetDescription(this, label);

        var stack = new VerticalStackLayout { Spacing = 0 };
        foreach (var item in items)
        {
            switch (item.Kind)
            {
                case KlyMenuItemKind.Separator:
                    var line = new BoxView { HeightRequest = 1, Margin = new Thickness(-4, 4) };
                    line.SetThemeColor(BoxView.ColorProperty, KlyTheme.Border);
                    AutomationProperties.SetIsInAccessibleTree(line, false);
                    stack.Add(line);
                    break;
                case KlyMenuItemKind.Heading:
                    var heading = new Label { Text = item.Label, FontSize = 12, FontAttributes = FontAttributes.Bold, Padding = new Thickness(8, 6) };
                    heading.SetThemeColor(Label.TextColorProperty, KlyTheme.TextMuted);
                    SemanticProperties.SetHeadingLevel(heading, SemanticHeadingLevel.Level3);
                    stack.Add(heading);
                    break;
                default:
                    var row = new OverlaysMenuRow(item, submenus && HasChildren(item));
                    row.Activated += (_, _) => ItemActivated?.Invoke(this, row);
                    _rows.Add(row);
                    stack.Add(row);
                    break;
            }
        }
        Content = new ScrollView { Content = stack };
    }

    /// <summary>The rows of the items, in order (not the headings and separators).</summary>
    public IReadOnlyList<OverlaysMenuRow> Rows => _rows;

    /// <summary>Raised with the row the user activated.</summary>
    public event EventHandler<OverlaysMenuRow>? ItemActivated;

    public static bool HasChildren(KlyMenuItem item) => item.Kind == KlyMenuItemKind.Item && item.Children is { Count: > 0 };
}
