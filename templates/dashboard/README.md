# Dashboard

An admin app on Blux UI: a resizable sidebar with a team switcher, a user menu and a command palette, a home page with figures, charts and recent sales, a customers table with search, filters, sorting, pagination, selection and row actions, an inbox with a reading pane, and four settings pages. The same pages run on the web (server first, then WebAssembly) and in a native app (MAUI Blazor Hybrid). The data is made up and lives in memory: replace it with your own source.

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
dotnet run --project src/Dashboard.Web
```

The first build downloads three things and keeps them in caches: the Tailwind CLI (once per machine, checked against a SHA-256 that ships with the Blux.UI package), the fonts of `Dashboard.Web.csproj` and the icons your pages name. Later builds, and builds without a network, reuse them.

## What is in it

```text
src/
  Dashboard.Shared/      Razor class library: pages, layout, sample data, styles (Styles/app.css) and the AddDashboardUi() setup
  Dashboard.Web/         ASP.NET Core host, global Auto interactivity (server, then WebAssembly)
  Dashboard.Web.Client/  the WebAssembly half of the web host
  Dashboard.Maui/        MAUI Blazor Hybrid app for Android, iOS, Mac Catalyst and Windows
```

| Page | What it shows |
| --- | --- |
| `Pages/Home.razor` | Four figures with their change since the period before, a revenue chart, a donut by channel, recent sales and a bar chart. The period is in the address (`?period=week`). |
| `Pages/Customers.razor` | A `BxDataList` of 240 customers: search, text, option, number, date and yes/no filters, sortable columns, pages, a ticked selection that survives the next page, a menu of actions per row and a form for a new customer (the country is searched in a remote source). |
| `Pages/Inbox.razor` | A list of messages and a reading pane side by side, with a resizable divider; on a phone one panel at a time. Opening a message reads it, and it can be answered and archived. |
| `Pages/Settings*.razor` | General (profile form, language), Members (roles, an invitation from a searched directory), Notifications (switches) and Security (password form, two-step sign-in, signed-in devices). |
| `Layout/` | The sidebar, the team switcher, the user menu, the notifications slideover, the appearance dialog (primary color and color mode) and the command palette. |

## Replace the sample data

- `Data/` holds the sample data, built from a fixed seed so that the server and the browser agree. The pages read it through the services of `Services/AppStores.cs` (`CustomerStore`, `MailStore`, `NotificationStore`, `MemberStore`), registered in `DashboardServices.cs`: change what they do and the pages follow.
- The customer table is `BxDataList` from `Blux.UI.Tools`. `CustomerData.Schema` says which fields search, sort and filter. It is given the rows in memory (`Items`); give it a `Provider` or a `Queryable` instead to read a page at a time from an API or a database (see the package README), and the schema stays as it is.
- `AppearanceState` keeps the chosen primary color in the browser. The other colors, the locales and the size of the components are configured once, in `DashboardServices.cs`.

## Develop

Run the app with `dotnet watch`, and Tailwind in a second terminal, so a new utility class shows up at the next reload:

```bash
dotnet watch --project src/Dashboard.Web
dotnet build src/Dashboard.Shared -t:BluxTailwindWatch
```

In VS Code, the `dev` task (Terminal, Run Task) builds once and starts both. `dotnet watch` does not follow the generated CSS by itself, and it does not reload the page for a CSS-only edit: reload by hand after an edit.

Blux.UI.Tools has no stylesheet of its own: the Tailwind build of `Dashboard.Shared` compiles its classes with yours.

## Native app

The MAUI project needs the MAUI workload of the platform you build for:

```bash
dotnet workload install maui-android      # or maui-ios, maui-maccatalyst, maui-windows
dotnet build src/Dashboard.Maui -f net10.0-android
```

Because `Dashboard.slnx` includes the MAUI project, building the solution needs a workload too. To build only the web app, build `src/Dashboard.Web`, as above. The app targets phones and desktops; Android TV is not set up.

## Serve below a path

Set the `PathBase` setting (an environment variable `PathBase=/dashboard`, or `appsettings.json`) to serve the whole app below a prefix, behind a reverse proxy for example. Links in the pages are relative, so they follow. The host does not redirect to HTTPS, so that a proxy that terminates TLS works as is; if the app is exposed directly, add `app.UseHttpsRedirection()` and `app.UseHsts()` to `Program.cs`.

### Behind a reverse proxy

A proxy that terminates TLS hands the app plain HTTP on its own address, and tells it the real ones in `X-Forwarded-For`, `X-Forwarded-Proto` and `X-Forwarded-Host`. The app ignores those headers unless you turn them on, because a visitor could send them too. Set `ForwardedHeaders:Enabled` to `true` (the environment variable `ForwardedHeaders__Enabled=true`) and list the addresses of your proxies in `ForwardedHeaders:KnownProxies` (`ForwardedHeaders__KnownProxies__0=10.0.0.5`); a proxy on the same machine (loopback) is trusted without being listed. Headers from any other sender are not believed. If the proxy passes the original `Host` header, also set `AllowedHosts` in `appsettings.json` to your domain.

## License

MIT, see `LICENSE`.
