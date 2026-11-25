# Klyroo.UI.Server

The server side of [Klyroo.UI](https://www.nuget.org/packages/Klyroo.UI), for apps that render on an ASP.NET Core server (static server rendering, Interactive Server and Auto prerendering).

A component that renders for a request and has no browser yet, a static render or a prerender, can read what the browser left in a cookie. Klyroo.UI is also loaded by
WebAssembly and MAUI apps, where there is no ASP.NET Core request, so it only knows a small abstraction, `IKlyRequestCookies`, and this package implements it over
`IHttpContextAccessor`.

```csharp
// Program.cs of the server project (not the client project of a WebAssembly or Auto app)
builder.Services.AddKlyrooUi();
builder.Services.AddKlyrooUiTools();
builder.Services.AddKlyrooServer();
```

`AddKlyrooServer()` calls `AddHttpContextAccessor()` and registers `IKlyRequestCookies`. Without it nothing breaks: a static or prerendered `KlyDataList` (package `Klyroo.UI.Tools`)
cuts the days of its date filters in UTC until the browser is interactive, and logs one warning in Development that says so. With it, the list uses the time zone the browser stored in
its `klyroo-timezone` cookie from the second page onwards (the cookie is written by a script, so the first visit has none), so a static page and a prerender show the same days as the interactive render.

The same goes for the columns a user chose in a `KlyDataList` with a `PreferencesKey` (visibility, order, pins, widths, grouped in the cookie `klyroo-dl-cols`): with `AddKlyrooServer()` the first HTML
already has them, without it a static render or a prerender paints the default columns (one warning in Development) and the interactive render applies the cookie once it can read it.

Because a page that reads a request cookie produces HTML shaped for that user, `IKlyRequestCookies.Get` automatically marks the HTTP response `Cache-Control: private` (unless `private` or `no-store` is already set) and adds `Cookie` to `Vary`, so a shared cache or CDN does not serve one user's rendered HTML to another. If your own middleware manages caching headers, you can disable this:

```csharp
builder.Services.AddKlyrooServer(options => options.SetCacheHeaders = false);
```

An interactive render after a prerender has no HTTP request (the circuit is not one), so `IKlyRequestCookies` returns nothing there; components read cookies only while they
are not interactive.

The package version is the same as Klyroo.UI's: the package depends on exactly that version.
