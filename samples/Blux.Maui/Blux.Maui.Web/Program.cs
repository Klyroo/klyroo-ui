using Blux.Maui.Shared;
using Blux.Maui.Shared.Services;
using Blux.Maui.Web.Components;
using Blux.Maui.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Device specific services used by the shared project.
builder.Services.AddSingleton<IFormFactor, FormFactor>();
builder.Services.AddSampleBlux();

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
    .AddAdditionalAssemblies(typeof(Blux.Maui.Shared._Imports).Assembly);

app.Run();
