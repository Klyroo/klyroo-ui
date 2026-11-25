# Klyroo.UI.Server

The server side of [Klyroo.UI](https://www.nuget.org/packages/Klyroo.UI), for apps that render on an ASP.NET Core server
(static server rendering, Interactive Server and Auto prerendering).

A component that renders for a request and has no browser yet, in a static render or a prerender, can read what the browser
left in a cookie, such as a time zone. Klyroo.UI is also loaded by WebAssembly and MAUI apps, where there is no ASP.NET Core
request, so it only knows the abstraction `IKlyRequestCookies`. This package implements it over `IHttpContextAccessor`.

```bash
dotnet add package Klyroo.UI.Server --prerelease
```

```csharp
using Klyroo.UI;
using Klyroo.UI.Server;

// Program.cs of the server project, not the client project of a WebAssembly or Auto app
builder.Services.AddKlyrooUi();
builder.Services.AddKlyrooServer();
```

`AddKlyrooServer()` calls `AddHttpContextAccessor()` and registers `IKlyRequestCookies`. Without it nothing breaks: components
keep their defaults until the browser is interactive. An interactive render after a prerender has no HTTP request, so
`IKlyRequestCookies.Get` returns null there.

Because a page that reads a request cookie produces HTML shaped for that user, `Get` marks the response `Cache-Control: private`
(unless `private` or `no-store` is already set) and adds `Cookie` to `Vary`. If your own middleware manages caching headers, turn
this off with `AddKlyrooServer(options => options.SetCacheHeaders = false)`.

The package has the same version as Klyroo.UI and depends on exactly that version. See the
[repository README](https://github.com/klyroo/klyroo-ui#readme).
