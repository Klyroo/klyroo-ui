using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;

namespace Klyroo.Templates.Content;

/// <summary>
/// The search index of the site as a JSON file, for <c>KlyContentSearch IndexUrl</c>: the browser fetches it the first time the search opens, so the
/// pages of the site do not each carry a copy. The file is the list of pages of <see cref="MarkdownSite.Search"/>, built once per version of the content
/// and sent with an <c>ETag</c>, so a visitor who has it asks only whether it changed.
/// </summary>
public static class MarkdownSearchIndex
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    // One entry per version of the content: a reload makes a new site, and the old entry goes with it.
    private static readonly ConditionalWeakTable<MarkdownSite, Entry> Entries = new();

    private sealed record Entry(byte[] Body, string ETag);

    /// <summary>The JSON of the index: an array of pages, each with <c>title</c>, <c>href</c> and, when they have one, <c>description</c>, <c>group</c>, <c>keywords</c> and <c>headings</c>.</summary>
    public static byte[] Serialize(MarkdownSite site) => JsonSerializer.SerializeToUtf8Bytes(site.Search, Json);

    /// <summary>Serves the index at <paramref name="pattern"/> (<c>/search-index.json</c> by default), below the path base like every other address.</summary>
    public static IEndpointConventionBuilder MapSearchIndex(this IEndpointRouteBuilder endpoints, string pattern = "/search-index.json") =>
        endpoints.MapMethods(pattern, ["GET", "HEAD"], (HttpContext context, MarkdownStore store) =>
        {
            var entry = Entries.GetValue(store.Site, site =>
            {
                var body = Serialize(site);
                return new Entry(body, $"\"{Convert.ToHexString(SHA256.HashData(body))[..16]}\"");
            });
            var headers = context.Response.GetTypedHeaders();
            // Cached by the browser, but asked about every time: the content can change in development, and a request that finds the same ETag costs no body.
            headers.CacheControl = new CacheControlHeaderValue { NoCache = true };
            headers.ETag = new EntityTagHeaderValue(entry.ETag);
            if (context.Request.Headers.IfNoneMatch.Contains(entry.ETag))
            {
                return Results.StatusCode(StatusCodes.Status304NotModified);
            }
            return Results.Bytes(entry.Body, "application/json");
        });
}
