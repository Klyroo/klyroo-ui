using Blux.Playground;
using Blux.Playground.Client;
using Blux.Playground.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddPlaygroundBlux();
builder.Services.AddBluxAi();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
// Reads the culture cookie that BxLocaleSelect writes (the default provider list includes it).
app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture("en")
    .AddSupportedCultures("en", "fr", "es")
    .AddSupportedUICultures("en", "fr", "es"));
app.UseAntiforgery();

app.MapStaticAssets();
app.MapBluxAi();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Blux.Playground.Client._Imports).Assembly);

app.Run();
