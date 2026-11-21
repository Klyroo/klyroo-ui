using System.Text.Json.Serialization;

namespace Blux.UI.Tools.Query;

/// <summary>
/// The value of a <see cref="FilterRule"/>. A closed hierarchy: <see cref="Text"/>, <see cref="Number"/>, <see cref="Bool"/>,
/// <see cref="Date"/> (a calendar day), <see cref="Moment"/> (an instant, for hour, minute and second granularity),
/// <see cref="List"/> and <see cref="Range"/>.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(Text), "text")]
[JsonDerivedType(typeof(Number), "number")]
[JsonDerivedType(typeof(Bool), "bool")]
[JsonDerivedType(typeof(Date), "date")]
[JsonDerivedType(typeof(Moment), "moment")]
[JsonDerivedType(typeof(List), "list")]
[JsonDerivedType(typeof(Range), "range")]
public abstract record FilterValue
{
    private protected FilterValue() { }

    /// <summary>A string.</summary>
    public sealed record Text(string Value) : FilterValue;

    /// <summary>A number. Always a <see cref="decimal"/> so that every numeric field type compares exactly.</summary>
    public sealed record Number(decimal Value) : FilterValue;

    /// <summary>A boolean.</summary>
    public sealed record Bool(bool Value) : FilterValue;

    /// <summary>A calendar day. Compared against <see cref="DateTimeOffset"/> and <see cref="DateTime"/> fields as the whole UTC day.</summary>
    public sealed record Date(DateOnly Value) : FilterValue;

    /// <summary>An instant, compared exactly (to <see cref="DateOnly"/> fields it reduces to its UTC day).</summary>
    public sealed record Moment(DateTimeOffset Value) : FilterValue;

    /// <summary>A list of option values. Values are strings and are converted to the type of the field (enum name, number, bool, Guid, ISO date).</summary>
    public sealed record List : FilterValue
    {
        /// <summary>Creates a list.</summary>
        public List(IReadOnlyList<string> values) => Values = values ?? [];

        /// <summary>The values.</summary>
        public IReadOnlyList<string> Values { get; init; }

        /// <inheritdoc />
        public bool Equals(List? other) => other is not null && SequenceEquality.Equal(Values, other.Values);

        /// <inheritdoc />
        public override int GetHashCode() => SequenceEquality.Hash(Values);
    }

    /// <summary>A range with inclusive bounds, either of which may be missing. Bounds are <see cref="Number"/>, <see cref="Date"/> or <see cref="Moment"/>.</summary>
    public sealed record Range(FilterValue? From, FilterValue? To) : FilterValue;
}
