namespace Klyroo.UI.Internal;

/// <summary>The <c>rel</c> of a link: the protection a link that opens a new tab needs, merged with the <c>rel</c> the caller passed.</summary>
internal static class LinkRel
{
    /// <summary>
    /// <c>noopener noreferrer</c> for a <see cref="KlyLinkTarget.Blank"/> target, added to the tokens of the <c>rel</c> in <paramref name="attributes"/> that it does not hold yet;
    /// the caller's own <c>rel</c> (or nothing) for any other target.
    /// </summary>
    public static string? For(KlyLinkTarget? target, IReadOnlyDictionary<string, object>? attributes = null)
    {
        var own = attributes is not null && attributes.TryGetValue("rel", out var value) ? value?.ToString() : null;
        if (target is not { IsBlank: true })
        {
            return own;
        }
        var tokens = (own ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        foreach (var required in new[] { "noopener", "noreferrer" })
        {
            if (!tokens.Contains(required, StringComparer.OrdinalIgnoreCase))
            {
                tokens.Add(required);
            }
        }
        return string.Join(' ', tokens);
    }
}
