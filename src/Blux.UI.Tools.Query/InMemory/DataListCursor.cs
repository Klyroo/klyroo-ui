using System.Buffers.Text;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Blux.UI.Tools.Query;

/// <summary>
/// The cursor of the in-memory executor: URL-safe base64 of a JSON array <c>[version, sortSignature, rowKey, sortValue...]</c> describing the last
/// row of the page. The next page is every row that sorts strictly after that tuple, so it survives rows being added or removed in between.
/// It is opaque but not signed: it carries no secret and no permission, and filters and authorization are applied again to every request, so
/// editing a cursor can only move the position inside the caller's own result. A cursor that does not decode, or that was made for another sort,
/// is read as "no cursor" (the first page).
/// </summary>
internal static class DataListCursor
{
    private const string Version = "1";
    private const int MaxLength = 4096;

    public static string Encode(string signature, string rowKey, IReadOnlyList<string?> values)
    {
        var parts = new string?[3 + values.Count];
        parts[0] = Version;
        parts[1] = signature;
        parts[2] = rowKey;
        for (var i = 0; i < values.Count; i++) parts[3 + i] = values[i];
        return Base64Url.EncodeToString(JsonSerializer.SerializeToUtf8Bytes(parts, CursorJsonContext.Default.StringArray));
    }

    public static bool TryDecode(string cursor, string signature, int valueCount, out string rowKey, out string?[] values)
    {
        rowKey = "";
        values = [];
        if (string.IsNullOrEmpty(cursor) || cursor.Length > MaxLength) return false;
        try
        {
            var bytes = Base64Url.DecodeFromChars(cursor);
            var parts = JsonSerializer.Deserialize(Encoding.UTF8.GetString(bytes), CursorJsonContext.Default.StringArray);
            if (parts is null || parts.Length != 3 + valueCount || parts[0] != Version || parts[1] != signature || parts[2] is null) return false;
            rowKey = parts[2]!;
            values = parts[3..];
            return true;
        }
        catch (Exception e) when (e is FormatException or JsonException or ArgumentException or DecoderFallbackException)
        {
            return false;
        }
    }
}

/// <summary>
/// The JSON metadata of the cursor alone. The cursor does not use <see cref="DataListJsonContext"/>, so that running a request (cursor paging included)
/// never pulls the whole wire model into a NativeAOT image.
/// </summary>
[JsonSerializable(typeof(string[]))]
internal sealed partial class CursorJsonContext : JsonSerializerContext
{
}
