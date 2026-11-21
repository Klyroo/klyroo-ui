# Blux.UI.Server

The server side of [Blux UI](https://www.nuget.org/packages/Blux.UI), for apps that render on an ASP.NET Core server (static server rendering, Interactive Server and Auto prerendering).

A component that renders for a request and has no browser yet, a static render or a prerender, can read what the browser left in a cookie. Blux.UI is also loaded by
WebAssembly and MAUI apps, where there is no ASP.NET Core request, so it only knows a small abstraction, `IBxRequestCookies`, and this package implements it over
`IHttpContextAccessor`.

```csharp
// Program.cs of the server project (not the client project of a WebAssembly or Auto app)
builder.Services.AddBluxUi();
builder.Services.AddBluxUiTools();
builder.Services.AddBluxServer();
```

`AddBluxServer()` calls `AddHttpContextAccessor()` and registers `IBxRequestCookies`. Without it nothing breaks: a static or prerendered `BxDataList` (package `Blux.UI.Tools`)
cuts the days of its date filters in UTC until the browser is interactive, and logs one warning in Development that says so. With it, the list uses the time zone the browser stored in
its `blux-timezone` cookie from the second page onwards (the cookie is written by a script, so the first visit has none), so a static page and a prerender show the same days as the interactive render.

An interactive render after a prerender has no HTTP request (the circuit is not one), so `IBxRequestCookies` returns nothing there; components read cookies only while they
are not interactive.

The package version is the same as Blux.UI's: the package depends on exactly that version.
