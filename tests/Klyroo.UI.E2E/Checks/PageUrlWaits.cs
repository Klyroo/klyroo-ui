namespace Klyroo.UI.E2E;

/// <summary>
/// Waits for the address of a page the way a test needs it for an enhanced or same-document navigation: polled in the page, never
/// by <c>WaitForURLAsync</c> (the browser tests of klyroo-stack wrap it as <c>PlaygroundTest.WaitForUrl</c>). <c>WaitForURLAsync</c> stays only where a full document load is awaited.
/// </summary>
internal static class PageUrlWaits
{
    /// <summary>Until a JavaScript expression of <c>url</c> (a <c>URL</c>) holds.</summary>
    public static Task WaitForUrlAsync(this IPage page, string expression, float timeout = 15_000) =>
        page.WaitForFunctionAsync($"() => {{ const url = new URL(location.href); return {expression}; }}", null, new() { Timeout = timeout });

    /// <summary>Until the full address matches a JavaScript regular expression (the same text Playwright's regex wait tests).</summary>
    public static Task WaitForUrlMatchingAsync(this IPage page, string pattern, float timeout = 15_000) =>
        page.WaitForFunctionAsync("pattern => new RegExp(pattern).test(location.href)", pattern, new() { Timeout = timeout });

    /// <summary>Until the full address ends with <paramref name="suffix"/>.</summary>
    public static Task WaitForUrlEndingWithAsync(this IPage page, string suffix, float timeout = 15_000) =>
        page.WaitForFunctionAsync("suffix => location.href.endsWith(suffix)", suffix, new() { Timeout = timeout });
}
