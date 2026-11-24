using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using Lux.UI;
using Lux.UI.Server;
using Dashboard.Shared;
using Dashboard.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Behind a reverse proxy the request the app sees is the proxy's: its own address, plain HTTP. With ForwardedHeaders:Enabled the app reads X-Forwarded-For,
// -Proto and -Host from the proxies it trusts, so the scheme and host of generated addresses are the visitor's. Loopback is trusted; list other proxies in
// ForwardedHeaders:KnownProxies (addresses). Off by default: a header from a client that is not a trusted proxy must not be believed.
var forwardedHeaders = builder.Configuration.GetValue<bool>("ForwardedHeaders:Enabled");
if (forwardedHeaders)
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
        foreach (var proxy in builder.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [])
        {
            options.KnownProxies.Add(IPAddress.TryParse(proxy, out var address)
                ? address
                : throw new InvalidOperationException($"ForwardedHeaders:KnownProxies: '{proxy}' is not an IP address."));
        }
    });
}

builder.Services.AddDashboardUi();
// The cookies of the request, for the components that render before the browser is there: the color choice of the app, and the time zone of the customers table (its
// days are the user's from the first byte). Server only: the WebAssembly client and the MAUI app have no request.
builder.Services.AddLuxServer();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

var app = builder.Build();

// First, so that everything after it (the path base, redirects, links) sees the address the visitor used.
if (forwardedHeaders)
{
    app.UseForwardedHeaders();
}

// PathBase serves the whole app below a prefix ("/dashboard"), as a reverse proxy does. UseRouting has to come right after UsePathBase, because
// WebApplication would otherwise add it first and the prefix would never be stripped before routing. App.razor writes the matching <base href>.
if (builder.Configuration["PathBase"] is { Length: > 0 } pathBase)
{
    app.UsePathBase("/" + pathBase.Trim('/'));
    app.UseRouting();
}

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

// Reads the query string, the culture cookie that LxLocaleSelect writes and Accept-Language. Lux components follow the UI culture
// on their own: the supported UI cultures are the codes of the built-in locales.
var cultures = LxLocales.All.Select(locale => locale.Code).ToArray();
app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture("en")
    .AddSupportedCultures(cultures)
    .AddSupportedUICultures(cultures));
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Dashboard.Shared.DashboardServices).Assembly);

app.Run();
