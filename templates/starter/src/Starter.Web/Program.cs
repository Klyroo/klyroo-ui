using Blux.UI;
using Starter.Shared;
using Starter.Web.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddStarterUi();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

var app = builder.Build();

// PathBase serves the whole app below a prefix ("/starter"), as a reverse proxy does. UseRouting has to come right after UsePathBase, because
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

// Reads the query string, the culture cookie that BxLocaleSelect writes and Accept-Language. Blux components follow the UI culture
// on their own: the supported UI cultures are the codes of the built-in locales.
var cultures = BxLocales.All.Select(locale => locale.Code).ToArray();
app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture("en")
    .AddSupportedCultures(cultures)
    .AddSupportedUICultures(cultures));
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Starter.Shared.StarterServices).Assembly);

app.Run();
