using Blux.Playground;
using Blux.Playground.Client;
using Blux.UI;
using Blux.Playground.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddPlaygroundBlux();
builder.Services.AddBluxAi(builder.Configuration);
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
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Blux.Playground.Client._Imports).Assembly);

app.Run();
