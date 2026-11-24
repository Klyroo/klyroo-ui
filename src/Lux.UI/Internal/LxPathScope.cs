using System.Globalization;
using System.Text;

namespace Lux.UI;

/// <summary>
/// What an app remembers in the browser, scoped to the path base it is served under (<c>UsePathBase("/x")</c> with <c>&lt;base href="/x/"&gt;</c>).
/// Apps on one origin share cookies and localStorage: a cookie is scoped by its path and by its name (a server keeps the last of two
/// cookies of the same name, and the one of another app can come after), a storage key by a prefix. An app at the root keeps the plain
/// names, so nothing changes for it. The browser side of the same rules is <c>js/base-path.js</c>. The path base is compared as written
/// (case-sensitive), so use the same casing in <c>UsePathBase</c> and in <c>&lt;base href&gt;</c>.
/// </summary>
public static class LxPathScope
{
    /// <summary>
    /// The name of the cookie that <c>LxSidebar</c> writes for <paramref name="id"/>: <c>lux-sidebar-{id}</c> at the root and
    /// <c>lux-sidebar.{slug}~~{id}</c> below a path base. The width is kept under the same name followed by <c>-width</c>.
    /// Read the cookie on the server with it: <c>Request.Cookies[LxPathScope.SidebarCookieName("app", Request.PathBase)]</c>.
    /// </summary>
    /// <param name="id">The <c>Id</c> of the sidebar (a dashboard group uses its <c>StorageKey</c>).</param>
    /// <param name="pathBase">The path base of the app, <c>Request.PathBase</c>; null or empty at the root.</param>
    public static string SidebarCookieName(string id, string? pathBase = null) => SidebarCookiePrefix(pathBase) + Uri.EscapeDataString(id);

    /// <summary>
    /// The name of a cookie that an app keeps under <paramref name="name"/> (<c>lux-timezone</c>): the name itself at the root and <c>{name}.{slug}~~</c> below a path base,
    /// the same rule as <see cref="SidebarCookieName"/> without an id. The cookie path is the path base, "/" at the root.
    /// </summary>
    /// <param name="name">The name at the root.</param>
    /// <param name="pathBase">The path base of the app, <c>Request.PathBase</c>; null or empty at the root.</param>
    public static string CookieName(string name, string? pathBase = null)
    {
        var slug = Slug(pathBase);
        return slug.Length == 0 ? name : $"{name}.{slug}~~";
    }

    internal static string SidebarCookiePrefix(string? pathBase)
    {
        var slug = Slug(pathBase);
        return slug.Length == 0 ? "lux-sidebar-" : $"lux-sidebar.{slug}~~";
    }

    /// <summary>The path base as a token that is safe in a cookie name: segments joined by ".", other characters as "~" and the hex of their UTF-8 bytes. Empty at the root.</summary>
    internal static string Slug(string? pathBase)
    {
        if (string.IsNullOrEmpty(pathBase)) return "";
        var builder = new StringBuilder();
        var first = true;
        foreach (var segment in pathBase.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            if (!first) builder.Append('.');
            first = false;
            foreach (var rune in segment.EnumerateRunes())
            {
                if (rune.Value < 128 && (char.IsAsciiLetterOrDigit((char)rune.Value) || rune.Value is '_' or '-'))
                {
                    builder.Append((char)rune.Value);
                    continue;
                }
                var bytes = new byte[4];
                var length = rune.EncodeToUtf8(bytes);
                foreach (var b in bytes.AsSpan(0, length)) builder.Append('~').Append(b.ToString("x2", CultureInfo.InvariantCulture));
            }
        }
        return builder.ToString();
    }

    /// <summary>The decoded path of a base URI (<c>NavigationManager.BaseUri</c>) without its trailing slash: "" at the root.</summary>
    internal static string PathBaseOf(string baseUri) =>
        Uri.TryCreate(baseUri, UriKind.Absolute, out var uri) ? Uri.UnescapeDataString(uri.AbsolutePath).TrimEnd('/') : "";
}
