using System.Text.Json.Serialization;

namespace Blux.UI.Tools.Query;

/// <summary>A node of the filter tree: a <see cref="FilterRule"/> or a <see cref="DataListFilterGroup"/>.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(FilterRule), "rule")]
[JsonDerivedType(typeof(DataListFilterGroup), "group")]
public abstract record DataListFilterNode
{
    private protected DataListFilterNode() { }
}

/// <summary>
/// One condition on a field (or on a query-only filter, see <see cref="DataListSchemaBuilder{T}.QueryFilter"/>).
/// </summary>
/// <param name="Key">The filter key: the stable key of a field, or the name of a query-only filter.</param>
/// <param name="Operator">The comparison.</param>
/// <param name="Value">The operand; <see langword="null"/> for <see cref="FilterOperator.IsEmpty"/> and <see cref="FilterOperator.IsNotEmpty"/>.</param>
/// <param name="IncludeEmpty">Also match rows where the field is empty (null or an empty string). Replaces the <c>~none</c> entry of the Nuxt UI table.</param>
public sealed record FilterRule(string Key, FilterOperator Operator, FilterValue? Value = null, bool IncludeEmpty = false) : DataListFilterNode;

/// <summary>A group of filter nodes combined with AND or OR. An empty group matches every row.</summary>
public sealed record DataListFilterGroup : DataListFilterNode
{
    /// <summary>Creates a group.</summary>
    public DataListFilterGroup(FilterCombinator combinator, IReadOnlyList<DataListFilterNode> children)
    {
        Combinator = combinator;
        Children = children ?? [];
    }

    /// <summary>How the children combine.</summary>
    public FilterCombinator Combinator { get; init; }

    /// <summary>The children.</summary>
    public IReadOnlyList<DataListFilterNode> Children { get; init; }

    /// <summary>The group that matches everything.</summary>
    public static DataListFilterGroup Empty { get; } = new(FilterCombinator.And, []);

    /// <summary>An AND group of the given nodes.</summary>
    public static DataListFilterGroup And(params DataListFilterNode[] children) => new(FilterCombinator.And, children);

    /// <summary>An OR group of the given nodes.</summary>
    public static DataListFilterGroup Or(params DataListFilterNode[] children) => new(FilterCombinator.Or, children);

    /// <inheritdoc />
    public bool Equals(DataListFilterGroup? other) =>
        other is not null && Combinator == other.Combinator && SequenceEquality.Equal(Children, other.Children);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Combinator, SequenceEquality.Hash(Children));
}
