using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using Blux.UI;
using Changelog.Web.Components;
using Changelog.Web.ReleaseNotes;
using Changelog.Web.Site;

var builder = WebApplication.CreateBuilder(args);

// Behind a reverse proxy the request the app sees is the proxy's: its own address, plain HTTP. With ForwardedHeaders:Enabled the app reads X-Forwarded-For,
// -Proto and -Host from the proxies it trusts, so the scheme and host of generated addresses (the links of the feed) are the visitor's. Loopback is trusted;
// list other proxies in ForwardedHeaders:KnownProxies (addresses). Off by default: a header from a client that is not a trusted proxy must not be believed.
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

builder.Services.AddBluxUi();
// Static server rendering only: no AddInteractiveServerComponents, no AddInteractiveWebAssemblyComponents. Add one of them (and a render mode on
// the component) the day a page needs a component that runs .NET in the browser.
builder.Services.AddRazorComponents();

// The releases: the GitHub API or a file (Changelog:Source), kept in memory for Changelog:CacheDuration. See ReleaseNotes/ReleaseService.cs.
builder.Services.AddChangelog(builder.Configuration);

var app = builder.Build();

// First, so that everything after it (the path base, redirects, links) sees the address the visitor used.
if (forwardedHeaders)
{
    app.UseForwardedHeaders();
}

// A setting that cannot work is reported here, once; the pages tell the visitor that the releases are not available instead of failing.
app.ReportChangelogSettings();
// Ask for the releases now, in the background, so the first visitor does not wait for GitHub. Nothing waits for it and nothing can fail here.
app.Lifetime.ApplicationStarted.Register(() => _ = app.Services.GetRequiredService<ReleaseService>().RefreshNowAsync());

// PathBase serves the whole site below a prefix ("/site"), as a reverse proxy does. UseRouting has to come right after UsePathBase, because
// WebApplication would otherwise add it first and the prefix would never be stripped before routing. App.razor writes the matching <base href>.
if (app.Configuration["PathBase"] is { Length: > 0 } pathBase)
{
    app.UsePathBase("/" + pathBase.Trim('/'));
    app.UseRouting();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

// What the pages show comes from outside (release notes), so the browser is told not to guess types and not to leak the address of the page it came from.
app.Use(async (context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next(context);
});

app.UseAntiforgery();

app.MapStaticAssets();
app.MapReleaseFeed(SiteInfo.FeedTitle, SiteInfo.Description);
app.MapRazorComponents<App>();

app.Run();
