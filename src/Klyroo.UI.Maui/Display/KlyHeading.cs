using System.Globalization;

namespace Klyroo.UI.Maui;

/// <summary>A section heading: an optional icon tile, the title as a heading for a screen reader, a count in a pill, a description and one action button.</summary>
public class KlyHeading : KlyBlockBase
{
    /// <summary>Identifies <see cref="Title"/>.</summary>
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(KlyHeading), null, propertyChanged: (b, _, _) => ((KlyHeading)b).Refresh());
    /// <summary>Identifies <see cref="Description"/>.</summary>
    public static readonly BindableProperty DescriptionProperty = BindableProperty.Create(nameof(Description), typeof(string), typeof(KlyHeading), null, propertyChanged: (b, _, _) => ((KlyHeading)b).Refresh());
    /// <summary>Identifies <see cref="Icon"/>.</summary>
    public static readonly BindableProperty IconProperty = BindableProperty.Create(nameof(Icon), typeof(View), typeof(KlyHeading), null, propertyChanged: (b, _, _) => ((KlyHeading)b).Refresh());
    /// <summary>Identifies <see cref="Count"/>.</summary>
    public static readonly BindableProperty CountProperty = BindableProperty.Create(nameof(Count), typeof(int?), typeof(KlyHeading), null, propertyChanged: (b, _, _) => ((KlyHeading)b).Refresh());
    /// <summary>Identifies <see cref="Action"/>.</summary>
    public static readonly BindableProperty ActionProperty = BindableProperty.Create(nameof(Action), typeof(KlyActionItem), typeof(KlyHeading), null, propertyChanged: (b, _, _) => ((KlyHeading)b).Refresh());
    /// <summary>Identifies <see cref="HeadingLevel"/>.</summary>
    public static readonly BindableProperty HeadingLevelProperty = BindableProperty.Create(nameof(HeadingLevel), typeof(int), typeof(KlyHeading), 3, propertyChanged: (b, _, v) => SemanticProperties.SetHeadingLevel(((KlyHeading)b)._title, DisplayParts.HeadingLevel((int)v)));

    private readonly Label _title = DisplayParts.NewLabel(16, KlyTheme.TextHighlighted, FontAttributes.Bold, LineBreakMode.TailTruncation);
    private readonly Label _description = DisplayParts.NewLabel(12, KlyTheme.TextMuted);
    private readonly Label _count = DisplayParts.NewLabel(12, KlyTheme.TextMuted);
    private readonly Border _countPill = new() { StrokeThickness = 1, Padding = new Thickness(8, 2), VerticalOptions = LayoutOptions.Center, IsVisible = false };
    private readonly Border _iconTile = DisplayParts.Tile(32, KlyTheme.RadiusOf(1), KlyTheme.BackgroundMuted);
    private readonly Border _actionHost = new() { StrokeThickness = 0, Padding = 0, VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.End };

    /// <summary>Creates a heading.</summary>
    public KlyHeading() : base(KlySurface.Plain)
    {
        _iconTile.Padding = new Thickness(8);
        _iconTile.IsVisible = false;
        _description.IsVisible = false;
        _countPill.SetStroke(KlyTheme.BorderAccented);
        _countPill.SetRadius(9999);
        _countPill.Content = _count;
        SemanticProperties.SetHeadingLevel(_title, DisplayParts.HeadingLevel(HeadingLevel));

        var titleRow = new HorizontalStackLayout { Spacing = 8, Children = { _title, _countPill } };
        var text = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center, Children = { titleRow, _description } };
        var lead = new HorizontalStackLayout { Spacing = 10, VerticalOptions = LayoutOptions.Center, Children = { _iconTile, text } };
        var row = new Grid { ColumnSpacing = 8 };
        row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        row.Add(lead, 0);
        row.Add(_actionHost, 1);
        Body.Add(row);
        Refresh();
    }

    /// <summary>The title, a heading for a screen reader of the level <see cref="HeadingLevel"/>.</summary>
    public string? Title { get => (string?)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    /// <summary>A line of smaller text under the title.</summary>
    public string? Description { get => (string?)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }

    /// <summary>The icon in the tile before the title, such as a <see cref="KlyIcon"/>. Give it <see cref="KlyIcon.IconColor"/> to color it.</summary>
    public View? Icon { get => (View?)GetValue(IconProperty); set => SetValue(IconProperty, value); }

    /// <summary>A count shown in a pill beside the title.</summary>
    public int? Count { get => (int?)GetValue(CountProperty); set => SetValue(CountProperty, value); }

    /// <summary>The button at the end of the row.</summary>
    public KlyActionItem? Action { get => (KlyActionItem?)GetValue(ActionProperty); set => SetValue(ActionProperty, value); }

    /// <summary>The level of the heading for a screen reader, from <c>1</c> to <c>6</c>; <c>3</c> by default, and values outside are clamped. Choose it from the outline of the page: the look does not change with the level.</summary>
    public int HeadingLevel { get => (int)GetValue(HeadingLevelProperty); set => SetValue(HeadingLevelProperty, value); }

    private void Refresh()
    {
        DisplayParts.Show(_title, Title);
        DisplayParts.Show(_description, Description);

        DisplayParts.Detach(Icon);
        _iconTile.Content = Icon;
        _iconTile.IsVisible = Icon is not null;
        AutomationProperties.SetIsInAccessibleTree(_iconTile, false);

        _count.Text = Count?.ToString("N0", CultureInfo.CurrentCulture);
        _countPill.IsVisible = Count is not null;

        _actionHost.Content = Action is { } action ? DisplayParts.ActionButton(action) : null;
    }
}
