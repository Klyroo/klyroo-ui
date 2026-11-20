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
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Blux.Playground.Client._Imports).Assembly);

app.Run();
