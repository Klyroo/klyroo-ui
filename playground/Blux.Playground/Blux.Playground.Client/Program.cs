using Blux.Playground.Client;
using Blux.UI;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddPlaygroundBlux();

var host = builder.Build();
// The client has no request: take the culture the server read from the cookie, so both pick the same locale.
await host.Services.ApplyCultureCookieAsync();
await host.RunAsync();
