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
// Reads the culture cookie that BxLocaleSelect writes (the default provider list includes it). Blux components follow
// the UI culture on their own: the supported cultures are the codes of the built-in locales.
var cultures = BxLocales.All.Select(locale => locale.Code).ToArray();
app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture("en")
    .AddSupportedCultures(cultures)
    .AddSupportedUICultures(cultures));
app.UseAntiforgery();

app.MapStaticAssets();
app.MapBluxAi();
app.MapBluxImages();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Blux.Playground.Client._Imports).Assembly);

app.Run();
