namespace Klyroo.UI;

/// <summary>One metric of a <see cref="KlyStatGroup{TValue}"/>.</summary>
/// <typeparam name="TValue">A built-in number type or its nullable form (see <see cref="KlyStat{TValue}"/>).</typeparam>
/// <param name="Label">The metric label.</param>
/// <param name="Value">The metric value, formatted with <see cref="KlyStatGroup{TValue}.Format"/> and <see cref="KlyStatGroup{TValue}.Culture"/>. A dash for a nullable type with no value, or a value that is not finite.</param>
public sealed record KlyStatItem<TValue>(string Label, TValue Value)
{
    /// <summary>A line of text below the value.</summary>
    public string? Caption { get; init; }

    /// <summary>A color for the value, such as <see cref="KlyColor.Error"/> for a metric that went wrong.</summary>
    public KlyColor? Color { get; init; }
}

/// <summary>Several small metrics side by side under an optional title (the "row" of statistics). <typeparamref name="TValue"/> is the number type of every value.</summary>
/// <typeparam name="TValue">The number type of the values.</typeparam>
public partial class KlyStatGroup<TValue>;
