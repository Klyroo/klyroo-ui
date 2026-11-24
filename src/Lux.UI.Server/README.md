# Lux.UI.Server

The server side of [Lux.UI](https://www.nuget.org/packages/Lux.UI), for apps that render on an ASP.NET Core server (static server rendering, Interactive Server and Auto prerendering).

A component that renders for a request and has no browser yet, a static render or a prerender, can read what the browser left in a cookie. Lux.UI is also loaded by
WebAssembly and MAUI apps, where there is no ASP.NET Core request, so it only knows a small abstraction, `ILxRequestCookies`, and this package implements it over
`IHttpContextAccessor`.

```csharp
// Program.cs of the server project (not the client project of a WebAssembly or Auto app)
builder.Services.AddLuxUi();
builder.Services.AddLuxUiTools();
builder.Services.AddLuxServer();
```

`AddLuxServer()` calls `AddHttpContextAccessor()` and registers `ILxRequestCookies`. Without it nothing breaks: a static or prerendered `LxDataList` (package `Lux.UI.Tools`)
cuts the days of its date filters in UTC until the browser is interactive, and logs one warning in Development that says so. With it, the list uses the time zone the browser stored in
its `lux-timezone` cookie from the second page onwards (the cookie is written by a script, so the first visit has none), so a static page and a prerender show the same days as the interactive render.

The same goes for the columns a user chose in a `LxDataList` with a `PreferencesKey` (visibility, order, pins, widths, grouped in the cookie `lux-dl-cols`): with `AddLuxServer()` the first HTML
already has them, without it a static render or a prerender paints the default columns (one warning in Development) and the interactive render applies the cookie once it can read it.

Because a page that reads a request cookie produces HTML shaped for that user, `ILxRequestCookies.Get` automatically marks the HTTP response `Cache-Control: private` (unless `private` or `no-store` is already set) and adds `Cookie` to `Vary`, so a shared cache or CDN does not serve one user's rendered HTML to another. If your own middleware manages caching headers, you can disable this:

```csharp
builder.Services.AddLuxServer(options => options.SetCacheHeaders = false);
```

An interactive render after a prerender has no HTTP request (the circuit is not one), so `ILxRequestCookies` returns nothing there; components read cookies only while they
are not interactive.

The package version is the same as Lux.UI's: the package depends on exactly that version.
