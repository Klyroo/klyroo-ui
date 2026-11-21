using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Blux.UI.Tools;

/// <summary>
/// The time zone the browser told the server in its cookie (<c>blux-timezone</c>, see <c>js/time-zone-cookie.js</c>), for the renders that have a request and no browser: a static render and a
/// prerender. The library is also loaded by WebAssembly apps, where the types of ASP.NET Core are not part of the runtime (a framework reference would stop their build), so the request is reached by
/// reflection, only outside the browser and only to read one cookie: <c>IHttpContextAccessor</c> to the request, then its path base and its cookies. Without an ASP.NET Core host or a request the zone is unknown.
/// </summary>
internal static class ServerTimeZone
{
    /// <summary>The name of the cookie at the root of the site; below a path base it carries the slug of the base (<see cref="BxPathScope.CookieName"/>).</summary>
    public const string CookieName = "blux-timezone";

    private const string AccessorType = "Microsoft.AspNetCore.Http.IHttpContextAccessor, Microsoft.AspNetCore.Http.Abstractions";
    private const string ExtensionsType = "Microsoft.Extensions.DependencyInjection.HttpServiceCollectionExtensions, Microsoft.AspNetCore.Http";

    /// <summary>Lets the components read the request they render for (<c>AddHttpContextAccessor</c>). A no-op in the browser and without ASP.NET Core.</summary>
    [UnconditionalSuppressMessage("Trimming", "IL2057", Justification = "Looked up by name on a server, where the ASP.NET Core assemblies are not trimmed; when they are absent nothing is registered.")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Public method of a framework type, see above.")]
    public static void Register(IServiceCollection services)
    {
        if (OperatingSystem.IsBrowser()) return;
        Type.GetType(ExtensionsType)?.GetMethod("AddHttpContextAccessor", BindingFlags.Public | BindingFlags.Static, [typeof(IServiceCollection)])?.Invoke(null, [services]);
    }

    /// <summary>The valid IANA id the cookie of the current request holds, or <see langword="null"/>: no request, no cookie (the first visit, before any script ran) or an id this machine does not know.</summary>
    public static string? Read(IServiceProvider services)
    {
        if (OperatingSystem.IsBrowser()) return null;
        return ReadCookie(services) is { } id ? DataListDates.ValidId(id) : null;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2057", Justification = "Looked up by name on a server, where the ASP.NET Core assemblies are not trimmed.")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Public members of framework types, see above.")]
    private static string? ReadCookie(IServiceProvider services)
    {
        try
        {
            if (Type.GetType(AccessorType) is not { } accessorType || services.GetService(accessorType) is not { } accessor) return null;
            if (accessorType.GetProperty("HttpContext")?.GetValue(accessor) is not { } context) return null;
            if (context.GetType().GetProperty("Request")?.GetValue(context) is not { } request) return null;
            var requestType = request.GetType();
            var pathBase = requestType.GetProperty("PathBase")?.GetValue(request)?.ToString();
            if (requestType.GetProperty("Cookies")?.GetValue(request) is not { } cookies) return null;
            var arguments = new object?[] { BxPathScope.CookieName(CookieName, pathBase), null };
            return cookies.GetType().GetMethod("TryGetValue", [typeof(string), typeof(string).MakeByRefType()])?.Invoke(cookies, arguments) is true ? arguments[1] as string : null;
        }
        catch (Exception e) when (e is TargetInvocationException or InvalidOperationException or AmbiguousMatchException or NotSupportedException)
        {
            // A host that is not what this expects: no zone, UTC.
            return null;
        }
    }
}
