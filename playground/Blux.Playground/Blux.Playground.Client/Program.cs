using Blux.Playground.Client;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddPlaygroundBlux();

await builder.Build().RunAsync();
