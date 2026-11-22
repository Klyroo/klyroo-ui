using System.Text.Json.Serialization;

namespace Blux.UI.Tools.Query;

/// <summary>
/// System.Text.Json source-generated metadata for the wire records (request, filter tree, values, facets, state), so they serialize without
/// reflection and survive trimming. Polymorphic types carry a <c>type</c> discriminator; enums are strings; names are camelCase.
/// <see cref="DataListResult{T}"/> is generic: declare <c>[JsonSerializable(typeof(DataListResult&lt;MyRow&gt;))]</c> in the context of your own app,
/// which can reuse these types.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(DataListRequest))]
[JsonSerializable(typeof(DataListPaging))]
[JsonSerializable(typeof(DataListSort))]
[JsonSerializable(typeof(DataListSearch))]
[JsonSerializable(typeof(DataListFacetRequest))]
[JsonSerializable(typeof(DataListFacetResult))]
[JsonSerializable(typeof(DataListFacetOption))]
[JsonSerializable(typeof(DataListFilterNode))]
[JsonSerializable(typeof(DataListFilterGroup))]
[JsonSerializable(typeof(FilterRule))]
[JsonSerializable(typeof(FilterValue))]
[JsonSerializable(typeof(FilterValue.Boolean), TypeInfoPropertyName = "FilterValueBoolean")]
[JsonSerializable(typeof(DataListState))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(Dictionary<string, Dictionary<string, string>>))]
public sealed partial class DataListJsonContext : JsonSerializerContext
{
}
