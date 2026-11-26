namespace Klyroo.UI;

/// <summary>
/// The <c>target</c> of a link: where the address opens. <see cref="Self"/>, <see cref="Blank"/>, <see cref="Parent"/> and <see cref="Top"/> are the
/// keywords of the HTML standard; <see cref="Named(string)"/> targets a frame or a window by its name. A link that opens in <see cref="Blank"/>
/// gets <c>rel="noopener noreferrer"</c>.
/// </summary>
public readonly struct KlyLinkTarget : IEquatable<KlyLinkTarget>
{
    private readonly string? _value;

    private KlyLinkTarget(string value) => _value = value;

    /// <summary>The same browsing context (<c>_self</c>, the default of a link).</summary>
    public static KlyLinkTarget Self { get; } = new("_self");

    /// <summary>A new tab or window (<c>_blank</c>).</summary>
    public static KlyLinkTarget Blank { get; } = new("_blank");

    /// <summary>The parent of the current frame (<c>_parent</c>).</summary>
    public static KlyLinkTarget Parent { get; } = new("_parent");

    /// <summary>The top-level browsing context (<c>_top</c>).</summary>
    public static KlyLinkTarget Top { get; } = new("_top");

    /// <summary>The frame or window with this name, created when it does not exist.</summary>
    /// <param name="name">The name. It must not be empty or start with an underscore: the underscore names are the keywords above.</param>
    /// <exception cref="ArgumentException">The name is empty, has white space only or starts with an underscore.</exception>
    public static KlyLinkTarget Named(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (name[0] == '_') throw new ArgumentException($"A frame name must not start with an underscore: use KlyLinkTarget.Self, Blank, Parent or Top for \"{name}\".", nameof(name));
        return new KlyLinkTarget(name);
    }

    /// <summary>The value of the <c>target</c> attribute. A default instance is <see cref="Self"/>.</summary>
    public string Value => _value ?? "_self";

    /// <summary>Whether the link opens a new tab or window, which makes it need <c>rel="noopener noreferrer"</c>.</summary>
    public bool IsBlank => Value == "_blank";

    /// <inheritdoc />
    public bool Equals(KlyLinkTarget other) => string.Equals(Value, other.Value, StringComparison.Ordinal);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is KlyLinkTarget other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);

    /// <inheritdoc />
    public override string ToString() => Value;

    /// <summary>Whether two targets have the same value.</summary>
    public static bool operator ==(KlyLinkTarget left, KlyLinkTarget right) => left.Equals(right);

    /// <summary>Whether two targets have different values.</summary>
    public static bool operator !=(KlyLinkTarget left, KlyLinkTarget right) => !left.Equals(right);
}
