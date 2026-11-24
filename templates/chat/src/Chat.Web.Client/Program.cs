using Lux.UI;
using Chat.Shared;
using Chat.Shared.Assistant;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddChatUi();
// The browser never talks to the AI service: the replies come from the web host (api/assistant/stream), which holds the key. The host address is the <base href> of
// the page, so the calls go below the path base the app is served under.
builder.Services.AddRemoteAssistant(new Uri(builder.HostEnvironment.BaseAddress));

var host = builder.Build();
// The client has no request: take the culture the server rendered with (meta element, cookie, lang attribute), so both pick the same locale.
await host.Services.ApplyServerCultureAsync(BxLocales.All.Select(locale => locale.Code));
await host.RunAsync();
