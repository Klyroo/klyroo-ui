namespace Klyroo.UI.Maui;

/// <summary>One step of a track: the marker, the color of the line that leads to the next step, and the text beside or under the marker.</summary>
/// <param name="Indicator">The marker.</param>
/// <param name="Separator">Color of the line to the next step.</param>
/// <param name="Text">The title and the description.</param>
internal sealed record NavigationTrackEntry(View Indicator, KlyThemeColor Separator, View Text);

/// <summary>
/// The layout of <see cref="KlyStepper"/> and the default layout of <see cref="KlyTimeline"/>: markers joined by a line, with the text under the markers
/// of a horizontal track and beside those of a vertical one.
/// </summary>
internal static class NavigationTrack
{
    /// <summary>Gap under a vertical step: the web <c>pb-6</c>.</summary>
    public const double VerticalGap = 24;

    /// <summary>Thickness of the line between two markers: the web <c>h-0.5</c> and <c>w-0.5</c>.</summary>
    public const double LineThickness = 2;

    public static View Build(IReadOnlyList<NavigationTrackEntry> entries, bool vertical) => vertical ? Vertical(entries) : Horizontal(entries);

    private static Grid Horizontal(IReadOnlyList<NavigationTrackEntry> entries)
    {
        // Every step but the last takes an equal share (flex-1) so the lines fill the width; the last one is as wide as its marker (flex-none).
        var grid = new Grid { ColumnSpacing = 0 };
        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            var last = i == entries.Count - 1;
            grid.ColumnDefinitions.Add(new ColumnDefinition(last ? GridLength.Auto : GridLength.Star));

            var track = new Grid { ColumnSpacing = 8, ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) } };
            track.Add(entry.Indicator, 0);
            if (!last)
            {
                var line = NavigationLayout.Line(entry.Separator, height: LineThickness);
                line.MinimumWidthRequest = 16;
                line.VerticalOptions = LayoutOptions.Center;
                track.Add(line, 1);
            }
            entry.Text.HorizontalOptions = LayoutOptions.Start;
            var column = new VerticalStackLayout { Spacing = 8, Children = { track, entry.Text } };
            grid.Add(column, i);
        }
        return grid;
    }

    private static VerticalStackLayout Vertical(IReadOnlyList<NavigationTrackEntry> entries)
    {
        var stack = new VerticalStackLayout { Spacing = 0 };
        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            var last = i == entries.Count - 1;
            var row = new Grid
            {
                ColumnSpacing = 12,
                ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) },
                RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star) },
            };
            row.Add(entry.Indicator, 0, 0);
            if (!last)
            {
                var line = NavigationLayout.Line(entry.Separator, width: LineThickness);
                line.MinimumHeightRequest = 24;
                line.HorizontalOptions = LayoutOptions.Center;
                row.Add(line, 0, 1);
            }
            entry.Text.VerticalOptions = LayoutOptions.Start;
            entry.Text.Margin = new Thickness(0, 0, 0, last ? 0 : VerticalGap);
            row.Add(entry.Text, 1, 0);
            Grid.SetRowSpan(entry.Text, 2);
            stack.Children.Add(row);
        }
        return stack;
    }
}
