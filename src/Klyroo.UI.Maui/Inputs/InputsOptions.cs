using System.Collections;

namespace Klyroo.UI.Maui;

/// <summary>An option of a list control, read from a <see cref="KlySelectItem"/> or from a plain object.</summary>
internal sealed record InputsOption(object? Value, string Label, bool Disabled, int? Count, string? Icon, string? Group)
{
    public bool HasValue(object? other) => Equals(Value, other);
}

internal static class InputsOptions
{
    /// <summary>The options of a list: a <see cref="KlySelectItem"/> as it is, any other object as its own value and its <c>ToString()</c>.</summary>
    public static List<InputsOption> From(IEnumerable? items)
    {
        var options = new List<InputsOption>();
        if (items is null) return options;
        foreach (var item in items)
        {
            switch (item)
            {
                case null:
                    break;
                case KlySelectItem select:
                    options.Add(new InputsOption(select.Value, select.Label, select.Disabled, select.Count, select.Icon, select.Group));
                    break;
                default:
                    options.Add(new InputsOption(item, item.ToString() ?? string.Empty, false, null, null, null));
                    break;
            }
        }
        return options;
    }

    /// <summary>
    /// The options under their headings: the ones without a group first, in their order and with no heading, then each group in the order of its
    /// first option with all its options, wherever they are in the list.
    /// </summary>
    public static List<(string? Heading, List<InputsOption> Options)> Sections(IEnumerable<InputsOption> options)
    {
        var sections = new List<(string? Heading, List<InputsOption> Options)>();
        var loose = new List<InputsOption>();
        var grouped = new List<(string Heading, List<InputsOption> Options)>();
        foreach (var option in options)
        {
            if (string.IsNullOrEmpty(option.Group))
            {
                loose.Add(option);
                continue;
            }
            var index = grouped.FindIndex(g => g.Heading == option.Group);
            if (index < 0) grouped.Add((option.Group, [option]));
            else grouped[index].Options.Add(option);
        }
        if (loose.Count > 0) sections.Add((null, loose));
        foreach (var (heading, list) in grouped) sections.Add((heading, list));
        return sections;
    }

    /// <summary>The options whose label contains the text, ignoring case: what a menu shows while the user types.</summary>
    public static List<InputsOption> Filter(IEnumerable<InputsOption> options, string? text)
    {
        var query = (text ?? string.Empty).Trim();
        return query.Length == 0
            ? [.. options]
            : [.. options.Where(o => o.Label.Contains(query, StringComparison.CurrentCultureIgnoreCase))];
    }
}
