# Starter

A minimal Blazor app on Lux.UI: a header with navigation, a language select and a color mode button, a home page with a hero, a features grid and an interactive demo, an about page and a footer. The same pages run on the web (server first, then WebAssembly) and in a native app (MAUI Blazor Hybrid).

<!-- rename:start -->
## Make it yours

This repository is a GitHub template. After you create your repository from it, give the app its own name once, from the root of the repository:

```bash
dotnet run rename.cs MyApp
```

`rename.cs` is a .NET 10 file-based app. It renames the solution, the projects and their folders, the namespaces, the MAUI application title and id, and every other occurrence of the template name, then deletes itself. The new name is one identifier: letters, digits and underscores, starting with a letter.

<!-- rename:end -->
## Run

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```bash
dotnet run --project src/Starter.Web
```

The first build downloads three things and keeps them in caches: the Tailwind CLI (once per machine, checked against a SHA-256 that ships with the Lux.UI package), the fonts of `Starter.Web.csproj` and the icons your pages name. Later builds, and builds without a network, reuse them.

## What is in it

```text
src/
  Starter.Shared/      Razor class library: pages, layout, styles (Styles/app.css) and the AddStarterUi() setup
  Starter.Web/         ASP.NET Core host, global Auto interactivity (server, then WebAssembly)
  Starter.Web.Client/  the WebAssembly half of the web host
  Starter.Maui/        MAUI Blazor Hybrid app for Android, iOS, Mac Catalyst and Windows
```

- Replace `Pages/Home.razor` with your own home page and edit `Layout/MainLayout.razor` for the header, the footer and the navigation.
- `StarterServices.cs` configures Lux for the three hosts in one place (colors, locales, default sizes).
- Tailwind is compiled by the build from `src/Starter.Shared/Styles/app.css` (`<LuxTailwind>true</LuxTailwind>` in `Starter.Shared.csproj`), with no Node and no `package.json`. The result is served at `_content/Starter.Shared/app.css`, which both the web host and the MAUI app link.
- Package versions are in `Directory.Packages.props`. The Lux.UI version is pinned there.

## Develop

Run the app with `dotnet watch`, and Tailwind in a second terminal, so a new utility class shows up at the next reload:

```bash
dotnet watch --project src/Starter.Web
dotnet build src/Starter.Shared -t:LuxTailwindWatch
```

In VS Code, the `dev` task (Terminal, Run Task) builds once and starts both. `dotnet watch` does not follow the generated CSS by itself, and it does not reload the page for a CSS-only edit: reload by hand after an edit.

## Native app

The MAUI project needs the MAUI workload of the platform you build for:

```bash
dotnet workload install maui-android      # or maui-ios, maui-maccatalyst, maui-windows
dotnet build src/Starter.Maui -f net10.0-android
```

Because `Starter.slnx` includes the MAUI project, building the solution needs a workload too. To build only the web app, build `src/Starter.Web`, as above. The app targets phones and desktops; Android TV is not set up.

## Serve below a path

Set the `PathBase` setting (an environment variable `PathBase=/starter`, or `appsettings.json`) to serve the whole app below a prefix, behind a reverse proxy for example. Links in the pages are relative, so they follow. The host does not redirect to HTTPS, so that a proxy that terminates TLS works as is; if the app is exposed directly, add `app.UseHttpsRedirection()` and `app.UseHsts()` to `Program.cs`.

### Behind a reverse proxy

A proxy that terminates TLS hands the app plain HTTP on its own address, and tells it the real ones in `X-Forwarded-For`, `X-Forwarded-Proto` and `X-Forwarded-Host`. The app ignores those headers unless you turn them on, because a visitor could send them too. Set `ForwardedHeaders:Enabled` to `true` (the environment variable `ForwardedHeaders__Enabled=true`) and list the addresses of your proxies in `ForwardedHeaders:KnownProxies` (`ForwardedHeaders__KnownProxies__0=10.0.0.5`); a proxy on the same machine (loopback) is trusted without being listed. Headers from any other sender are not believed. If the proxy passes the original `Host` header, also set `AllowedHosts` in `appsettings.json` to your domain.

## License

MIT, see `LICENSE`.
