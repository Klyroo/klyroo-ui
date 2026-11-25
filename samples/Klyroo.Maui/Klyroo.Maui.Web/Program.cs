using Klyroo.Maui.Shared;
using Klyroo.Maui.Shared.Services;
using Klyroo.Maui.Web.Components;
using Klyroo.Maui.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Device specific services used by the shared project.
builder.Services.AddSingleton<IFormFactor, FormFactor>();
builder.Services.AddSampleKlyroo();
builder.Services.AddSingleton<ISystemBars, NoSystemBars>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(Klyroo.Maui.Shared._Imports).Assembly);

app.Run();
