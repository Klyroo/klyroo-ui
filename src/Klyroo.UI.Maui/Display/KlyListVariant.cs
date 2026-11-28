namespace Klyroo.UI.Maui;

/// <summary>The layout of a <see cref="KlyList"/>.</summary>
public enum KlyListVariant
{
    /// <summary>A bullet or an icon before each item.</summary>
    Plain,
    /// <summary>A numbered list that marks done items and the current one.</summary>
    Steps,
    /// <summary>A checklist: numbered items that mark done ones and the current one.</summary>
    Checks,
}
