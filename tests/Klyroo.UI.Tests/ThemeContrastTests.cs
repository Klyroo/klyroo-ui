using System.Text.RegularExpressions;
using Klyroo.UI.Theming;

namespace Klyroo.UI.Tests;

/// <summary>
/// Guards the resolved classes of the themes (not only the token values) against 1.4.11 regressions, in both themes
/// and for the 7 colors: the border of outline and subtle variants, the edge of solid controls and the mark of the
/// current or completed item must reach 3:1 against the page surfaces.
/// </summary>
public partial class PaletteContrastTests
{
    // Pseudo-states that only decorate a control; the state classes below are what carries a meaning.
    private static readonly string[] TransientStates = ["hover", "focus", "focus-visible", "focus-within", "active", "disabled", "aria-disabled"];

    // Prefixes that mark the current, selected, checked or completed item.
    private static readonly string[] StatePrefixes =
    [
        "aria-[current=page]", "aria-current", "aria-selected", "aria-pressed", "data-[state=active]", "data-[state=completed]",
        "data-active", "data-selected", "checked",
    ];

    private static readonly Regex ColorRing = new(@"^(?<kind>ring|border)-\(--kly-color(?<text>-text)?\)(?:/(?<alpha>\d+))?$");

    private static readonly IReadOnlyList<ComponentTheme> AllThemes = [.. new KlyrooOptions().AllThemes];

    private sealed record Entry(string Theme, string Slot, string Origin, string Classes);

    /// <summary>Every class string of every theme: base slots, variants and compound variants.</summary>
    private static IEnumerable<Entry> AllEntries() =>
        AllThemes.SelectMany(theme =>
            theme.Slots.Select(s => new Entry(theme.Name, s.Key, "slot", s.Value))
                .Concat(theme.Variants.SelectMany(v => v.Value.SelectMany(value =>
                    value.Value.Select(s => new Entry(theme.Name, s.Key, $"{v.Key}={value.Key}", s.Value)))))
                .Concat(theme.CompoundVariants.SelectMany(c =>
                    c.Classes.Select(s => new Entry(theme.Name, s.Key, "compound", s.Value)))));

    /// <summary>Splits "aria-[current=page]:hover:bg-x" into its prefixes and its utility, ignoring colons inside brackets.</summary>
    private static (string[] Prefixes, string Utility) Split(string token)
    {
        var parts = new List<string>();
        var depth = 0;
        var start = 0;
        for (var i = 0; i < token.Length; i++)
        {
            switch (token[i])
            {
                case '[': depth++; break;
                case ']': depth--; break;
                case ':' when depth == 0:
                    parts.Add(token[start..i]);
                    start = i + 1;
                    break;
            }
        }
        return ([.. parts], token[start..]);
    }

    private static IEnumerable<(string[] Prefixes, string Utility)> Tokens(string classes) =>
        classes.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(Split);

    private static string Describe(Entry e) => $"{e.Theme}.{e.Slot} [{e.Origin}]";

    [Fact]
    public void Outline_and_subtle_borders_reach_3_to_1_against_the_page_surfaces_for_every_color()
    {
        var checkedBorders = 0;
        foreach (var theme in AllThemes)
        {
            if (!theme.Variants.TryGetValue("variant", out var variants))
            {
                continue;
            }

            foreach (var look in new[] { "outline", "subtle" })
            {
                if (!variants.TryGetValue(look, out var slots))
                {
                    continue;
                }

                foreach (var (slot, classes) in slots)
                {
                    foreach (var (prefixes, utility) in Tokens(classes))
                    {
                        var m = ColorRing.Match(utility);
                        if (!m.Success || prefixes.Any(TransientStates.Contains))
                        {
                            continue;
                        }

                        var alpha = m.Groups["alpha"].Success ? int.Parse(m.Groups["alpha"].Value) / 100.0 : 1.0;
                        var suffix = m.Groups["text"].Success ? "-text" : "";
                        checkedBorders++;
                        foreach (var themeName in new[] { "light", "dark" })
                        {
                            foreach (var color in Colors)
                            {
                                foreach (var surface in Surfaces)
                                {
                                    var background = Token(themeName, surface);
                                    var border = Blend(Token(themeName, color + suffix), background, alpha);
                                    var ratio = Contrast(border, background);
                                    Assert.True(ratio >= 3.0,
                                        $"{theme.Name}.{slot} {look}: '{string.Join(':', prefixes.Append(utility))}' gives {ratio:0.00}:1 on {surface} for {themeName} {color}, needs 3:1");
                                }
                            }
                        }
                    }
                }
            }
        }

        Assert.True(checkedBorders > 10, "No outline or subtle border found: the rule is not checking anything.");
    }

    [Fact]
    public void Solid_fills_that_cannot_reach_3_to_1_are_edged_by_the_text_color()
    {
        // The light signal yellow is 1.24:1 on the page, so every solid fill needs an edge that is.
        var fillAlone = new[] { "light", "dark" }.All(t => Colors.All(c => Surfaces.All(s => Contrast(Token(t, c), Token(t, s)) >= 3.0)));
        Assert.False(fillAlone, "The palette now passes without an edge: this rule and the edges can be dropped.");

        var solids = 0;
        foreach (var entry in AllEntries())
        {
            var tokens = Tokens(entry.Classes).ToList();
            if (!tokens.Any(t => t.Prefixes.Length == 0 && t.Utility == "bg-(--kly-color)"))
            {
                continue;
            }

            solids++;
            var edged = tokens.Any(t => t.Prefixes.Length == 0 && Regex.IsMatch(t.Utility, @"^(inset-)?ring(-\d+)?-\(--kly-color-text\)$"));
            Assert.True(edged, $"{Describe(entry)} has a solid fill and no unprefixed ring-(--kly-color-text) edge: '{entry.Classes}'");
        }

        Assert.True(solids >= 8, $"Only {solids} solid fills found: the rule is not checking enough.");
    }

    [Fact]
    public void The_current_or_selected_fill_is_edged_by_the_text_color()
    {
        var states = 0;
        foreach (var entry in AllEntries())
        {
            var tokens = Tokens(entry.Classes).ToList();
            foreach (var prefix in StatePrefixes)
            {
                var fills = tokens.Where(t => t.Prefixes.Length == 1 && t.Prefixes[0] == prefix && Regex.IsMatch(t.Utility, @"^bg-\(--kly-color\)(/\d+)?$"));
                if (!fills.Any())
                {
                    continue;
                }

                states++;
                var edged = tokens.Any(t => t.Prefixes.Length == 1 && t.Prefixes[0] == prefix && Regex.IsMatch(t.Utility, @"^(inset-)?(ring|border)(-\d+)?-\(--kly-color-text\)(/([7-9]\d|100))?$"));
                Assert.True(edged, $"{Describe(entry)}: '{prefix}:' fills with the color and has no '{prefix}:ring-(--kly-color-text)' or border edge");
            }
        }

        Assert.True(states >= 6, $"Only {states} state fills found: the rule is not checking enough.");
    }

    [Fact]
    public void The_current_item_mark_uses_the_text_color_not_the_fill_color()
    {
        // A line, border or inset shadow of the signal yellow is 1.24:1 on the page; the -text color is 5.6:1.
        foreach (var entry in AllEntries())
        {
            foreach (var (prefixes, utility) in Tokens(entry.Classes))
            {
                if (prefixes.Length == 0 || prefixes.Any(TransientStates.Contains) || !prefixes.Any(StatePrefixes.Contains))
                {
                    continue;
                }

                var bad = Regex.IsMatch(utility, @"^border(-[a-z])?-\(--kly-color\)$") || (utility.StartsWith("shadow-[") && utility.Contains("var(--kly-color)"));
                Assert.False(bad, $"{Describe(entry)}: '{string.Join(':', prefixes.Append(utility))}' draws the mark with the fill color");
            }
        }
    }

    [Fact]
    public void Content_navigation_marks_the_current_page_with_the_text_color()
    {
        var link = AllThemes.Single(t => t.Name == "contentNavigation").Slots["link"];
        Assert.DoesNotContain("var(--kly-color)", link);
        Assert.Contains("var(--kly-color-text)", link);
    }

    [Fact]
    public void Completed_connector_lines_use_the_text_color()
    {
        foreach (var name in new[] { "stepper", "timeline" })
        {
            var separator = AllThemes.Single(t => t.Name == name).Slots["separator"] + " ";
            Assert.DoesNotContain(":bg-(--kly-color) ", separator);
            Assert.Contains(":bg-(--kly-color-text) ", separator);
        }
    }

    [Fact]
    public void The_selected_pill_tab_is_edged_with_a_ring_that_reaches_3_to_1_on_its_track()
    {
        var trigger = AllThemes.Single(t => t.Name == "tabs").Variants["variant"]["pill"]["trigger"];
        Assert.Contains("aria-selected:ring-(--klyroo-text-muted)", trigger);
        foreach (var theme in new[] { "light", "dark" })
        {
            var ratio = Contrast(Token(theme, "text-muted"), Token(theme, "bg-elevated"));
            Assert.True(ratio >= 3.0, $"{theme}: text-muted on bg-elevated is {ratio:0.00}:1, needs 3:1");
        }
    }

    [Fact]
    public void Switch_error_ring_sits_inside_the_track_whether_it_is_checked_or_not()
    {
        // checked:ring-inset must not make a checked invalid switch differ from an unchecked one: the error ring is
        // inset in both states and, when checked, wins over the 1px text-color ring.
        var tokens = Tokens(AllThemes.Single(t => t.Name == "switch").Slots["input"]).Select(t => string.Join(':', t.Prefixes.Append(t.Utility))).ToList();
        Assert.Contains("aria-invalid:ring-inset", tokens);
        Assert.Contains("aria-invalid:ring-2", tokens);
        Assert.Contains("aria-invalid:ring-(--klyroo-error)", tokens);
        Assert.Contains("checked:aria-invalid:ring-2", tokens);
        Assert.Contains("checked:aria-invalid:ring-(--klyroo-error)", tokens);
    }
}
