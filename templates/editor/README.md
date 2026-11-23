# Editor

A document app on Blux UI: a sidebar with your documents (new, search, rename, delete, grouped by the day they were last changed), a rich text editor with a toolbar for headings, bold, italic, underline, strikethrough, lists, task lists, links, code, quotes and images by address, a floating menu over a selection, slash commands, an autosave that tells you when the text is in the browser's storage, a word count, and light and dark mode. The same pages run on the web (server first, then WebAssembly) and in a native app (MAUI Blazor Hybrid).

There is no server side to it: the documents are kept in the browser (or the web view of the native app) of the person who writes them, and the web host only serves the pages.

<!-- rename:start -->
## Make it yours

This repository is a GitHub template. After you create your repository from it, give the app its own name once, from the root of the repository:

```bash
dotnet run rename.cs MyApp
```

`rename.cs` is a .NET 10 file-based app. It renames the solution, the projects and their folders, the namespaces, the MAUI application title and id, and every other occurrence of the template name, then deletes itself. The new name is one identifier: letters and digits, starting with a letter. What Blux UI names with "Editor" (`BxEditor`, `EditorAction`, `bx-editor`) is not the template name and is left alone.

<!-- rename:end -->
## Run

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```bash
dotnet run --project src/Editor.Web
```

The first build downloads three things and keeps them in caches: the Tailwind CLI (once per machine, checked against a SHA-256 that ships with the Blux.UI package), the fonts of `Editor.Web.csproj` and the icons your pages name. Later builds, and builds without a network, reuse them.

## How a document is kept

`IDocumentStore` (`src/Editor.Shared/Services/DocumentStore.cs`) is the one seam: load everything, save one document, delete one. The default, `LocalStorageDocumentStore`, writes one key per document to the local storage of the browser, under a prefix that carries the path base of the app, so two apps on one site never read each other's documents. The server never sees a document, which is also what keeps the visitors of a public demo apart. To keep documents in a database behind your own sign-in, register your own `IDocumentStore` before `AddEditorUi()`.

Everything a store returns is untrusted and is checked by `DocumentState` before the pages see it: a value that is not a document, has the wrong id or is longer than the app ever writes is dropped and removed from the storage. The storage of a browser is a few megabytes for a whole site, so the app keeps to bounds (`DocumentLimits`): at most 100 documents, 150,000 characters of HTML for one, and 1,000,000 for all of them. A change that would pass the first is refused and the status bar says so; past the others the least recently used documents go. If the browser refuses a write (storage full or blocked) the status bar says that the text lasts for this visit only.

The text is saved when the editor reports a change, 400 ms after you stop typing, and when the focus leaves the text. A new document is titled "Untitled" and its title follows the first line of the text until you rename it.

## Content safety

The editor writes HTML, and HTML from a storage, a paste or a hand edit is untrusted. `DocumentHtml.Clean` (`src/Editor.Shared/Data/DocumentHtml.cs`) rebuilds it from an allow-list, tag by tag, before it is stored, before it goes back to the editor and before the server draws it as markup (`SanitizeEditorHtml`, set in `EditorServices.cs`, for the read-only view that precedes the editor):

- Elements that are not listed are dropped; `script`, `style`, `iframe`, `svg` and the like are dropped with everything inside them. Every attribute is dropped except the few each element needs.
- A link keeps its address only when it is `http`, `https` or `mailto` (an address written as `java&#x09;script:` is read the way a browser reads it), and gets `rel="noopener noreferrer nofollow"`.
- An image keeps its address only when it is `https`, and is loaded lazily and without a referrer. Other images are dropped.
- Text is written again, escaped.

The editor itself only builds nodes it has a schema for, so pasted HTML is filtered twice. Images are third-party requests made by the browser of the reader: add a `Content-Security-Policy` with `img-src` if you want to limit where they can come from.

## What is in it

| Where | What |
| --- | --- |
| `src/Editor.Web/` | The web host: `Program.cs` (server and WebAssembly render modes, path base, forwarded headers), `App.razor` (head, fonts, scripts). |
| `src/Editor.Web.Client/` | The WebAssembly entry point. |
| `src/Editor.Maui/` | The native app: a web view that runs the same pages. |
| `src/Editor.Shared/` | Everything the three share. |
| `Layout/` | The shell: sidebar with the list and the dialogs, the main layout. |
| `Pages/` | The home page, the document page (toolbar, editor, status bar), error pages. |
| `Data/` | The document, its JSON, the allow-list, the guide. |
| `Services/` | `DocumentState` (the list, the bounds, the search) and the stores. |

## Develop

Run the app with `dotnet watch`, and Tailwind in a second terminal, so a new utility class shows up at the next reload:

```bash
dotnet watch --project src/Editor.Web
dotnet build src/Editor.Shared -t:BluxTailwindWatch
```

In VS Code, the `dev` task (Terminal, Run Task) builds once and starts both. `dotnet watch` does not follow the generated CSS by itself, and it does not reload the page for a CSS-only edit: reload by hand after an edit.

## Native app

The MAUI project needs the MAUI workload of the platform you build for:

```bash
dotnet workload install maui-android      # or maui-ios, maui-maccatalyst, maui-windows
dotnet build src/Editor.Maui -f net10.0-android
```

Because `Editor.slnx` includes the MAUI project, building the solution needs a workload too. To build only the web app, build `src/Editor.Web`, as above. The app targets phones and desktops; Android TV is not set up. The documents are kept in the local storage of the web view, on the device.

## Serve below a path

Set the `PathBase` setting (an environment variable `PathBase=/notes`, or `appsettings.json`) to serve the whole app below a prefix, behind a reverse proxy for example. Links in the pages are relative, so they follow. The host does not redirect to HTTPS, so that a proxy that terminates TLS works as is; if the app is exposed directly, add `app.UseHttpsRedirection()` and `app.UseHsts()` to `Program.cs`.

### Behind a reverse proxy

A proxy that terminates TLS hands the app plain HTTP on its own address, and tells it the real ones in `X-Forwarded-For`, `X-Forwarded-Proto` and `X-Forwarded-Host`. The app ignores those headers unless you turn them on, because a visitor could send them too. Set `ForwardedHeaders:Enabled` to `true` (the environment variable `ForwardedHeaders__Enabled=true`) and list the addresses of your proxies in `ForwardedHeaders:KnownProxies` (`ForwardedHeaders__KnownProxies__0=10.0.0.5`) and/or whole ranges in `ForwardedHeaders:KnownNetworks`, in CIDR notation (`ForwardedHeaders__KnownNetworks__0=10.0.0.0/8`); a proxy on the same machine (loopback) is trusted without being listed. Headers from any other sender are not believed. If the proxy passes the original `Host` header, also set `AllowedHosts` in `appsettings.json` to your domain.

## License

MIT, see `LICENSE`.
