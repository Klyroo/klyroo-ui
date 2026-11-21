using System.Text.Json.Serialization;
using Blux.UI.Tools.Query;

namespace Blux.UI.Tools.Tests;

[JsonSerializable(typeof(DataListResult<Person>))]
public sealed partial class PersonJson : JsonSerializerContext
{
}
