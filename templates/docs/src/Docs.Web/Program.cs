using Blux.Templates.Content;
using Blux.UI;
using Docs.Web.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBluxUi();
// Static server rendering only: no AddInteractiveServerComponents, no AddInteractiveWebAssemblyComponents. Add one of them (and a render mode on
// the component) the day a page needs a component that runs .NET in the browser.
builder.Services.AddRazorComponents();

// The pages: every .md file below the content folder. "Content:Folder" moves it, "Content:IncludeDrafts" serves the pages marked draft, and
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
    Watch = builder.Environment.IsDevelopment(),
    IncludeDrafts = builder.Configuration.GetValue<bool>("Content:IncludeDrafts"),
    AllowRawHtml = builder.Configuration.GetValue<bool>("Content:AllowRawHtml"),
});

var app = builder.Build();

// Read the content now: a page that cannot be used stops the start with the name of its file, not the first visitor.
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

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>();

app.Run();
