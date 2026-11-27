namespace Klyroo.UI.Maui;

/// <summary>An item of <see cref="KlyAccordion"/>: a header label and a content that opens and closes.</summary>
/// <param name="Label">Text of the header button of the item.</param>
public sealed record KlyAccordionItem(string Label)
{
    /// <summary>Identifies the item in <see cref="KlyAccordion.Value"/>. Defaults to its index.</summary>
    public string? Value { get; init; }

    /// <summary>Icon shown before the label: a Lucide name such as <see cref="KlyIconNames.Info"/>.</summary>
    public string? Icon { get; init; }

    /// <summary>Content of the panel that opens under the header.</summary>
    public View? Content { get; init; }

    /// <summary>Text of the panel, used when <see cref="Content"/> is not set.</summary>
    public string? Text { get; init; }

    /// <summary>Whether the item starts open when <see cref="KlyAccordion.Value"/> is not set. With a single open item (<see cref="KlyAccordion.Multiple"/> off), only the first such item opens.</summary>
    public bool DefaultOpen { get; init; }

    /// <summary>Prevents the user from opening or closing the item.</summary>
    public bool Disabled { get; init; }
}
