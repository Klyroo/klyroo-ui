using System.Diagnostics.CodeAnalysis;

namespace Blux.UI.Tools.Query;

/// <summary>
/// Converts a value to and from the text of a URL query parameter. The same codecs serve the components (URL-synced state) and an API
/// server that parses the URLs they produce, which is why they live in this Blazor-free package.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
public interface IQueryCodec<T>
{
    /// <summary>Parses the decoded text of a parameter. Returns false for text that is not a value of this codec: the caller then uses its default.</summary>
    bool TryParse(string raw, [MaybeNullWhen(false)] out T value);

    /// <summary>The text of a value, or null to leave the parameter out of the URL (an empty list, no value).</summary>
    string? Format(T value);
}
