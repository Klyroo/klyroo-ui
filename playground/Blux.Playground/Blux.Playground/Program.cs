using Blux.Playground;
using Blux.Playground.Client;
using Blux.UI;
using Blux.Playground.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddPlaygroundBlux();
builder.Services.AddBluxAi(builder.Configuration);
// Resizes and converts the images of wwwroot on demand, so the docs show real srcset candidates. No remote host is allowed.
builder.Services.AddBluxImagesLocal(o => o.Endpoint = PlaygroundServices.ImageEndpoint);
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
// PathBase serves the whole site below a prefix ("/sub"), as a reverse proxy or the live demos of the templates do: the E2E suite runs a second
// instance this way to check that the library never assumes it lives at "/". UseRouting has to come right after UsePathBase, because
// WebApplication would otherwise add it first and the prefix would never be stripped before routing. App.razor writes the matching <base href>.
if (builder.Configuration["PathBase"] is { Length: > 0 } pathBase)
{
    app.UsePathBase("/" + pathBase.Trim('/'));
    app.UseRouting();
}
app.UseBluxAi();
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
// Reads the query string, the culture cookie that BxLocaleSelect writes and Accept-Language (the default provider list).
// Blux components follow the UI culture on their own: the supported UI cultures are the codes of the built-in locales.
// The supported cultures add two regional ones, so that dates and clocks keep the format of an en-GB or fr-CA user.
var uiCultures = BxLocales.All.Select(locale => locale.Code).ToArray();
app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture("en")
    .AddSupportedCultures([.. uiCultures, "en-GB", "fr-CA"])
    .AddSupportedUICultures(uiCultures));
app.UseAntiforgery();

app.MapStaticAssets();
app.MapBluxAi();
app.MapBluxImages();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Blux.Playground.Client._Imports).Assembly);

app.Run();
