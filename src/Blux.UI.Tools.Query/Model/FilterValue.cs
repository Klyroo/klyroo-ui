using System.Text.Json.Serialization;

namespace Blux.UI.Tools.Query;

/// <summary>
/// The value of a <see cref="FilterRule"/>. A closed hierarchy: <see cref="Text"/>, <see cref="Number"/>, <see cref="Boolean"/>,
/// <see cref="Date"/> (a calendar day), <see cref="Moment"/> (an instant, for hour, minute and second granularity),
/// <see cref="List"/> and <see cref="Range"/>.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(Text), "text")]
[JsonDerivedType(typeof(Number), "number")]
[JsonDerivedType(typeof(Boolean), "boolean")]
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
    public sealed record Boolean(bool Value) : FilterValue;

    /// <summary>A calendar day. On <see cref="DateTimeOffset"/> and <see cref="DateTime"/> fields it is that day in the time zone of the request (<see cref="DataListRequest.TimeZone"/>, UTC when absent), as the half-open range from its first instant to the next day's. <see cref="DateOnly"/> fields compare the day itself.</summary>
    public sealed record Date(DateOnly Value) : FilterValue;

    /// <summary>An instant. A date filter matches the whole hour, minute or second (the filter's <see cref="DateGranularity"/>) that contains it, cut in the request's time zone; against a <see cref="DateOnly"/> field it reduces to its local day.</summary>
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
