using System.Globalization;
using System.Text.RegularExpressions;

namespace Klyroo.UI.Maui;

/// <summary>What <see cref="KlyBlogPost"/> and <see cref="KlyChangelogVersion"/> share: the date and the authors.</summary>
internal static partial class PagesPost
{
    [GeneratedRegex(@"^\s*dddd[\s,.:/-]*|[\s,.:/-]*dddd\s*$")]
    private static partial Regex DayName();

    /// <summary>
    /// Formats the date with <paramref name="format"/>, or, without one, with the long date pattern of the culture minus the day name
    /// ("March 4, 2026" in en-US, "4 mars 2026" in fr-FR): the order of the culture, not the English one.
    /// </summary>
    public static string Date(DateTimeOffset date, string? format, CultureInfo? culture)
    {
        culture ??= CultureInfo.CurrentCulture;
        if (format is not null) return date.ToString(format, culture);
        var pattern = DayName().Replace(culture.DateTimeFormat.LongDatePattern, "").Trim();
        return date.ToString(pattern.Length == 0 ? "D" : pattern, culture);
    }

    /// <summary>One author: an avatar and the name, with the role under it.</summary>
    public static View Author(KlyPostAuthor author, KlySize size)
    {
        var avatar = new KlyAvatar { Source = author.AvatarSource, Text = author.Name, Alt = author.Name, Size = size, VerticalOptions = LayoutOptions.Center };
        var name = PagesUi.Text(14, KlyTheme.TextHighlighted);
        name.Text = author.Name;
        name.IsVisible = true;
        var description = PagesUi.Text(12, KlyTheme.TextMuted);
        PagesUi.Show(description, author.Description);
        var grid = new Grid
        {
            ColumnSpacing = 8,
            HorizontalOptions = LayoutOptions.Start,
            ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Auto) },
        };
        grid.Add(avatar, 0, 0);
        grid.Add(new VerticalStackLayout { VerticalOptions = LayoutOptions.Center, Children = { name, description } }, 1, 0);
        return grid;
    }

    /// <summary>Several authors: overlapping avatars, named together for a screen reader.</summary>
    public static View Group(IReadOnlyList<KlyPostAuthor> authors)
    {
        var group = new HorizontalStackLayout { Spacing = -8, HorizontalOptions = LayoutOptions.Start };
        foreach (var author in authors) group.Children.Add(new KlyAvatar { Source = author.AvatarSource, Text = author.Name, Size = KlySize.Md });
        SemanticProperties.SetDescription(group, string.Join(", ", authors.Select(a => a.Name)));
        return group;
    }
}
