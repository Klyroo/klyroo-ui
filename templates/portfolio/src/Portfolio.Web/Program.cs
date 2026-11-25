using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using Klyroo.Templates.Content;
using Klyroo.UI;
using Portfolio.Web.Components;
using Portfolio.Web.Site;

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

builder.Services.AddKlyrooUi();
// Static server rendering only: no AddInteractiveServerComponents, no AddInteractiveWebAssemblyComponents. Add one of them (and a render mode on
// the component) the day a page needs a component that runs .NET in the browser.
builder.Services.AddRazorComponents();

// The pages: every .md file below the content folder (work/, writing/ and the plain pages). "Content:Folder" moves it, "Content:IncludeDrafts" serves the pages marked draft, and
// "Content:AllowRawHtml" lets raw HTML in Markdown through: read the README before turning that on. In development a changed file is read again.
var contentFolder = builder.Configuration["Content:Folder"] is { Length: > 0 } configured ? configured : "content";
var contentRoot = Path.GetFullPath(contentFolder, builder.Environment.ContentRootPath);
if (!Directory.Exists(contentRoot) && !Path.IsPathRooted(contentFolder))
{
    // Started from another folder than the project's or the publish output's: the content next to the assembly is the one that was built.
    contentRoot = Path.GetFullPath(contentFolder, AppContext.BaseDirectory);
}
builder.Services.AddMarkdownContent(new ContentOptions
{
    Root = contentRoot,
    // Images are served from wwwroot: one that is not there is reported in the log.
    WebRoot = builder.Environment.WebRootPath,
    Watch = builder.Environment.IsDevelopment(),
    IncludeDrafts = builder.Configuration.GetValue<bool>("Content:IncludeDrafts"),
    AllowRawHtml = builder.Configuration.GetValue<bool>("Content:AllowRawHtml"),
    // What only this site knows: a project needs a valid year, an article a valid date, and a cover is a file of wwwroot. Run at every read, so that a file saved
    // with a mistake in Development keeps the previous content and logs the error instead of breaking the pages that read the projects and the articles.
    Validate = site => Library.Validate(site, builder.Environment.WebRootPath),
});

var app = builder.Build();

// First, so that everything after it (the path base, redirects, links) sees the address the visitor used.
if (forwardedHeaders)
{
    app.UseForwardedHeaders();
}

// Read the content now: a page that cannot be used stops the start with the name of its file, not the first visitor. That includes a project or an
// article with no valid year or date (see Validate above).
_ = app.Services.GetRequiredService<MarkdownStore>();

// PathBase serves the whole site below a prefix ("/site"), as a reverse proxy does. UseRouting has to come right after UsePathBase, because
// WebApplication would otherwise add it first and the prefix would never be stripped before routing. App.razor writes the matching <base href>.
if (builder.Configuration["PathBase"] is { Length: > 0 } pathBase)
{
    app.UsePathBase("/" + pathBase.Trim('/'));
    app.UseRouting();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

// Reads the query string, the culture cookie that KlyLocaleSelect writes and Accept-Language. Klyroo components follow the UI culture
// on their own: the supported UI cultures are the codes of the built-in locales. The text of the site is English: the language select
// translates what Klyroo writes itself (labels, menus, the form messages) and switches the layout to right-to-left where needed.
var cultures = KlyLocales.All.Select(locale => locale.Code).ToArray();
app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture("en")
    .AddSupportedCultures(cultures)
    .AddSupportedUICultures(cultures));
app.UseAntiforgery();

app.MapStaticAssets();
// The search index, fetched by the browser the first time the search opens (IndexUrl in MainLayout.razor).
app.MapSearchIndex();
app.MapRazorComponents<App>();

app.Run();
