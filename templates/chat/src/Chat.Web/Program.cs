using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using Blux.UI;
using Chat.Shared;
using Chat.Shared.Assistant;
using Chat.Web;
using Chat.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Behind a reverse proxy the request the app sees is the proxy's: its own address, plain HTTP. With ForwardedHeaders:Enabled the app reads X-Forwarded-For,
// -Proto and -Host from the proxies it trusts, so the scheme and host of generated addresses are the visitor's. Loopback is trusted; list other proxies in
// ForwardedHeaders:KnownProxies (addresses) or ForwardedHeaders:KnownNetworks (CIDR ranges). Off by default: a header from a client that is not a trusted proxy must not be believed.
var forwardedHeaders = builder.Configuration.GetValue<bool>("ForwardedHeaders:Enabled");
if (forwardedHeaders)
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
        foreach (var proxy in builder.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [])
        {
            options.KnownProxies.Add(IPAddress.TryParse(proxy, out var address)
                ? address
                : throw new InvalidOperationException($"ForwardedHeaders:KnownProxies: '{proxy}' is not an IP address."));
        }
        // Whole networks, as CIDR ("10.0.0.0/8"): for proxies that come and go in a range (a load balancer, a cluster).
        foreach (var network in builder.Configuration.GetSection("ForwardedHeaders:KnownNetworks").Get<string[]>() ?? [])
        {
            options.KnownIPNetworks.Add(System.Net.IPNetwork.TryParse(network, out var parsed)
                ? parsed
                : throw new InvalidOperationException($"ForwardedHeaders:KnownNetworks: '{network}' is not a network in CIDR notation (10.0.0.0/8)."));
        }
    });
}

builder.Services.AddChatUi();

// The assistant. The "Assistant" section of the configuration says which provider answers: "OpenAi" (any service that speaks the OpenAI completions API,
// with the key and the address below) or "Simulated" (canned replies, no network: what the live demo runs). The key and the address stay in this process, the
// browser only calls api/assistant/* below. Put the key in user secrets (dotnet user-secrets set "Assistant:ApiKey" ...) or in the environment (Assistant__ApiKey),
// never in appsettings.json.
var assistant = builder.Configuration.GetSection("Assistant").Get<AssistantOptions>() ?? new AssistantOptions();
// The client does not follow redirects, so the key is only ever sent to the address of the configuration, and has no timeout of its own: a reply streams for a while
// (the provider enforces Assistant:TimeoutSeconds).
var serviceClient = new HttpClient(new SocketsHttpHandler
{
    AllowAutoRedirect = false,
    PooledConnectionLifetime = TimeSpan.FromMinutes(5),
    ConnectTimeout = TimeSpan.FromSeconds(15),
})
{
    Timeout = Timeout.InfiniteTimeSpan,
};
builder.Services.AddAssistantProvider(assistant, serviceClient);
// Who may use the assistant. Without Assistant:AuthorizationPolicy anybody who reaches the app can, within the limits of AssistantGuard.
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<Microsoft.AspNetCore.Components.Server.Circuits.CircuitHandler, CallerCircuitHandler>();
if (!string.IsNullOrWhiteSpace(assistant.AuthorizationPolicy))
{
    builder.Services.AddCascadingAuthenticationState();
    builder.Services.AddScoped<IAssistantAuthorizer, PolicyAuthorizer>();
}

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

var app = builder.Build();

foreach (var problem in assistant.Problems())
{
    // The app starts anyway and says so in the page: the operator reads it here and in the page.
    app.Logger.LogWarning("Assistant is not usable: {Problem}", problem);
}
if (!string.IsNullOrWhiteSpace(assistant.AuthorizationPolicy))
{
    // A policy that does not exist would fail every request: say so now.
    var policies = app.Services.GetRequiredService<Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider>();
    if (await policies.GetPolicyAsync(assistant.AuthorizationPolicy) is null)
    {
        throw new InvalidOperationException($"Assistant:AuthorizationPolicy is '{assistant.AuthorizationPolicy}', but no such policy is defined: add it with builder.Services.AddAuthorization(options => options.AddPolicy(...)).");
    }
}
else if (assistant.IsOpenAi && !string.IsNullOrEmpty(assistant.ApiKey))
{
    app.Logger.LogWarning(
        "A real Assistant:ApiKey is configured and Assistant:AuthorizationPolicy is not: anybody who can reach this app can spend the key, limited only by the per-caller and daily limits of the Assistant section " +
        "(counted in memory, per process). Put the app behind authentication and name the policy in Assistant:AuthorizationPolicy before you expose it to the internet.");
}
app.Logger.LogInformation("Assistant provider: {Provider}", assistant.IsSimulated ? AssistantOptions.Simulated : AssistantOptions.OpenAi);

// First, so that everything after it (the path base, redirects, links) sees the address the visitor used.
if (forwardedHeaders)
{
    app.UseForwardedHeaders();
}

// PathBase serves the whole app below a prefix ("/chat"), as a reverse proxy does. UseRouting has to come right after UsePathBase, because
// WebApplication would otherwise add it first and the prefix would never be stripped before routing. App.razor writes the matching <base href>.
if (builder.Configuration["PathBase"] is { Length: > 0 } pathBase)
{
    app.UsePathBase("/" + pathBase.Trim('/'));
    app.UseRouting();
}

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

// Reads the query string, the culture cookie that BxLocaleSelect writes and Accept-Language. Blux components follow the UI culture
// on their own: the supported UI cultures are the codes of the built-in locales.
var cultures = BxLocales.All.Select(locale => locale.Code).ToArray();
app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture("en")
    .AddSupportedCultures(cultures)
    .AddSupportedUICultures(cultures));
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapAssistantEndpoints();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Chat.Shared.ChatServices).Assembly);

app.Run();
