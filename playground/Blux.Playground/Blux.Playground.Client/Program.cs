using Blux.Playground.Client;
using Blux.UI;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddPlaygroundBlux();

var host = builder.Build();
// The client has no request: take the culture the server rendered with (meta element, cookie, lang attribute), so both pick the same locale.
await host.Services.ApplyServerCultureAsync(BxLocales.All.Select(locale => locale.Code).Concat(["en-GB", "fr-CA"]));
await host.RunAsync();
