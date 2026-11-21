using System.Net;
using System.Text;
using Changelog.Web.Releases;
using Microsoft.Extensions.Logging;

namespace Blux.Templates.Tests;

/// <summary>An <see cref="HttpMessageHandler"/> that answers from a function and records the requests it saw (with the headers as they were sent).</summary>
internal sealed class FakeGitHub(Func<HttpRequestMessage, int, HttpResponseMessage> respond) : HttpMessageHandler
{
    public List<(string Url, IReadOnlyDictionary<string, string> Headers)> Requests { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var headers = request.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value), StringComparer.OrdinalIgnoreCase);
        lock (Requests) Requests.Add((request.RequestUri!.ToString(), headers));
        await Task.Yield();
        int number;
        lock (Requests) number = Requests.Count;
        return respond(request, number);
    }

    public static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK, string? etag = null, string? link = null)
    {
        var response = new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        if (etag is not null) response.Headers.TryAddWithoutValidation("ETag", etag);
        if (link is not null) response.Headers.TryAddWithoutValidation("Link", link);
        return response;
    }

    public static HttpResponseMessage Status(HttpStatusCode status, params (string Name, string Value)[] headers)
    {
        var response = new HttpResponseMessage(status) { Content = new StringContent("{\"message\":\"x\"}", Encoding.UTF8, "application/json") };
        foreach (var (name, value) in headers) response.Headers.TryAddWithoutValidation(name, value);
        return response;
    }
}

/// <summary>A logger that keeps every line, so a test can check that something (a token) is in none of them.</summary>
internal sealed class RecordingLogger<T> : ILogger<T>
{
    public List<string> Lines { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        lock (Lines) Lines.Add(formatter(state, exception) + (exception is null ? "" : " " + exception));
    }
}

/// <summary>A clock a test moves by hand.</summary>
internal sealed class ManualTime(DateTimeOffset start) : TimeProvider
{
    private DateTimeOffset _now = start;

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan by) => _now += by;
}

internal static class ChangelogSamples
{
    public static string Release(string tag, string? name = null, string? body = "", bool draft = false, bool prerelease = false, string? login = "octo-fan", string? published = "2026-01-02T03:04:05Z", string? avatar = null) =>
        "{" + $"\"id\":{Math.Abs(tag.GetHashCode())},\"tag_name\":{Quote(tag)},\"name\":{(name is null ? "null" : Quote(name))},\"draft\":{(draft ? "true" : "false")},\"prerelease\":{(prerelease ? "true" : "false")},"
        + $"\"published_at\":{(published is null ? "null" : Quote(published))},\"body\":{(body is null ? "null" : Quote(body))},"
        + $"\"author\":{(login is null ? "null" : "{\"login\":" + Quote(login) + ",\"avatar_url\":" + (avatar is null ? "null" : Quote(avatar)) + "}")}" + "}";

    public static string Array(params string[] releases) => "[" + string.Join(",", releases) + "]";

    public static string Quote(string text) => System.Text.Json.JsonSerializer.Serialize(text);

    public static ReleaseEntry Entry(string tag, string body = "", string? slug = null, string? title = null) =>
        new(1, tag, title ?? tag, slug ?? ReleaseSlug.From(tag), body, DateTimeOffset.Parse("2026-01-02T03:04:05Z", System.Globalization.CultureInfo.InvariantCulture), false, null, null);
}
