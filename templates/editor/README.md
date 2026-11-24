# Editor

A document app on Lux.UI: a sidebar with your documents (new, search, rename, delete, grouped by the day they were last changed), a rich text editor with a toolbar for headings, bold, italic, underline, strikethrough, lists, task lists, links, code, quotes and images by address, a floating menu over a selection, slash commands, an autosave that tells you when the text is in the browser's storage, a word count, and light and dark mode. The same pages run on the web (server first, then WebAssembly) and in a native app (MAUI Blazor Hybrid).

There is no server side to it: the documents are kept in the browser (or the web view of the native app) of the person who writes them, and the web host only serves the pages.

<!-- rename:start -->
## Make it yours

This repository is a GitHub template. After you create your repository from it, give the app its own name once, from the root of the repository:

```bash
dotnet run rename.cs MyApp
```

`rename.cs` is a .NET 10 file-based app. It renames the solution, the projects and their folders, the namespaces, the MAUI application title and id, and every other occurrence of the template name, then deletes itself. The new name is one identifier: letters and digits, starting with a letter. What Lux.UI names with "Editor" (`BxEditor`, `EditorAction`, `bx-editor`) is not the template name and is left alone.

<!-- rename:end -->
## Run

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```bash
dotnet run --project src/Editor.Web
```

The first build downloads three things and keeps them in caches: the Tailwind CLI (once per machine, checked against a SHA-256 that ships with the Lux.UI package), the fonts of `Editor.Web.csproj` and the icons your pages name. Later builds, and builds without a network, reuse them.

## How a document is kept

`IDocumentStore` (`src/Editor.Shared/Services/DocumentStore.cs`) is the one seam: read the list, read one document, search the text, save one, delete one, and (optionally) tell when someone else changed the storage. The default, `LocalStorageDocumentStore`, writes one key per document to the local storage of the browser, under a prefix that carries the path base of the app, so two apps on one site never read each other's documents. The server never sees a document it was not handed, which is also what keeps the visitors of a public demo apart. To keep documents in a database behind your own sign-in, register your own `IDocumentStore` before `AddEditorUi()`.

Everything a store returns is untrusted and is checked by `DocumentState` before the pages see it: a value that is not a document, has the wrong id, is longer than the app ever writes or weighs more than a message may carry is dropped and removed from the storage. The storage of a browser is a few megabytes for a whole site, so the app keeps to bounds (`DocumentLimits`): at most 100 documents, 150,000 characters of HTML for one, and 1,000,000 for all of them. A change that would pass the first is refused (the editor itself stops sending it, and the status bar says so); past the others the least recently used documents go, and the app says so with a notice that counts them. The sidebar warns when the browser is at nine tenths of a bound. If the browser refuses a write (storage full or blocked) the status bar says that the text lasts for this visit only.

The text is saved when the editor reports a change, 400 ms after you stop typing, and when the focus leaves the text. A change that arrives while another is being written waits, and only the last one waiting is written: the status bar says "Saved" once the last one is, and a failure of a later write is never hidden by an earlier success. A new document is titled "Untitled" and its title follows the first line of the text until you rename it.

### One document at a time

In the Server render mode every answer of the browser to the server is a message of the circuit, and Blazor Server accepts 32 KB by default, then closes the connection. So the app never sends the whole library: the list is read **light** (the id, title, dates and length of each document, a few dozen bytes each, built in the browser by `loadIndex` in `wwwroot/host.js`), the **text of a document is read when it is opened**, and a **search runs in the browser** and returns ids. The sidebar shows titles only.

The hub is raised to `DocumentLimits.MaxReceiveMessageBytes` = **512 KB** (`Program.cs`). It is worked out from the limits, in the worst case that is not made up on purpose:

| What travels | Size at the limit |
| --- | --- |
| One change of the editor: 150,000 characters of HTML, at most 3 bytes each in UTF-8 (all CJK text), a quote or a backslash counted twice | about 450,000 bytes at the worst, under 300,000 for text and code |
| One stored document handed to the server, as JSON inside an interop result (quotes and backslashes written twice again) | at most `DocumentLimits.MaxTransferBytes` = 512 KB - 16 KB of envelope |

`DocumentJson.TransferBytes` counts the second one exactly as the browser writes it, and a save that would pass it is refused like one that is too long, so no stored document is one the app could not read back. A document of 150,000 characters that is all quotes or backslashes is the one case this refuses before the character limit; control characters, which the allow-list drops, are the one input that can still make the editor's own message bigger than the bound (the connection then closes and the circuit starts again). The WebAssembly and MAUI hosts have no such limit; this one is for the Server renderer.

The editor never sends a text past the limit: `BxEditor MaxLength="@DocumentLimits.MaxHtmlCharacters"` keeps it on the page, marks it invalid and the status bar says "Not saved: this document is too long (N of 150,000 characters)". The stored text is the last one that fitted.

### Other tabs

Two tabs of one browser share the storage. The app listens to the `storage` event (`watchStorage` in `host.js`): documents another tab adds, changes or deletes reach the list; the open document is read again when it is not being typed in (and a line in the status bar says so), and when it is, the screen is left alone and the status bar offers to load the other version (saving replaces it: the last write wins). A document deleted in another tab is left with a notice. Before a save removes the least recently used documents the app reads the stored list again, so a document that another tab edited since is not the one removed. Two tabs that write at the very same moment can still overwrite each other.

## Content safety

The editor writes HTML, and HTML from a storage, a paste or a hand edit is untrusted. `DocumentHtml.Clean` (`src/Editor.Shared/Data/DocumentHtml.cs`) rebuilds it from an allow-list, tag by tag, before it is stored, before it goes back to the editor and before the server draws it as markup (`SanitizeEditorHtml`, set in `EditorServices.cs`, for the read-only view that precedes the editor):

- Elements that are not listed are dropped; `script`, `style`, `iframe`, `svg` and the like are dropped with everything inside them. Every attribute is dropped except the few each element needs.
- A link keeps its address only when it is an absolute `http`, `https` or `mailto` address (an address written as `java&#x09;script:` is read the way a browser reads it: leading and trailing control characters and spaces are trimmed, tabs and line breaks are dropped wherever they are, and any other space inside means it is not an address and the link is dropped, never closed up into another host), and gets `rel="noopener noreferrer nofollow"`.
- An image keeps its address only when it is `https`, and is loaded lazily and without a referrer. Other images are dropped.
- The editor and the allow-list agree: the page tells the editor what is kept (`ImageSchemes="https"`, `LinkSchemes="http https mailto"` in `DocumentPage.razor`), so an `http` image or a relative link is refused when it is entered, with a message that says which addresses are accepted, instead of appearing, saying "Saved" and vanishing after a reload.
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
| `Services/` | `DocumentState` (the list, the bounds, the search, other tabs), `DocumentSaver` (the order of the saves) and the stores. |

## Develop

Run the app with `dotnet watch`, and Tailwind in a second terminal, so a new utility class shows up at the next reload:

```bash
dotnet watch --project src/Editor.Web
dotnet build src/Editor.Shared -t:LuxTailwindWatch
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
