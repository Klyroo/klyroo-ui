# Changelog

A release notes site on Lux.UI, built from the releases of a GitHub repository: a timeline of versions (date, version, author, notes), a page for each release, an Atom feed, and an "older releases" pagination. The releases are read from the GitHub REST API while the site runs and kept in memory for an hour. The product in the sample releases (Tern, a command line tool) and every name in them are made up.

The pages are rendered by the server and sent as HTML: no circuit, no WebAssembly download. The notes of a release are Markdown written by other people, so they are rendered as untrusted input (see "Release notes are untrusted" below).

<!-- rename:start -->
## Make it yours

This repository is a GitHub template. After you create your repository from it, give the site its own name once, from the root of the repository:

```bash
dotnet run rename.cs MyApp
```

`rename.cs` is a .NET 10 file-based app. It renames the solution, the project and its folder, the namespaces and every other occurrence of the template name (including the `Changelog` section of the settings, which becomes `MyApp`), then deletes itself. The new name is one identifier: letters and digits, starting with a letter. The names of the Lux.UI components that contain the template name (`LxChangelogVersions`) are left alone.

<!-- rename:end -->
## Run

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```bash
dotnet run --project src/Changelog.Web
```

Out of the box the site shows the sample releases of `src/Changelog.Web/data/releases.json`, with a "Sample data" notice, so it runs without a network. As soon as `Changelog:Repository` is set it reads that repository from GitHub instead, here with an environment variable:

```bash
Changelog__Repository=dotnet/aspnetcore dotnet run --project src/Changelog.Web
```

The first build downloads three things and keeps them in caches: the Tailwind CLI (once per machine, checked against a SHA-256 that ships with the Lux.UI package), the fonts of `Changelog.Web.csproj` and the icons your pages name. Later builds, and builds without a network, reuse them.

## Settings

Under `Changelog` in `appsettings.json`, or as environment variables with two underscores (`Changelog__Repository`).

| Setting | Default | Meaning |
| --- | --- | --- |
| `Source` | from `Repository` | Optional. `GitHub` reads the API. `File` reads a JSON file in the format of the API (the sample releases), with no network. Unset, the sample file is used while `Repository` is empty and GitHub once it is set; a value that is set always wins (`Source=GitHub` with no repository answers 503 with a page that says the releases are not available). |
| `Repository` | none | `owner/repo`. Without it (and without `Source`) the sample releases are shown with a notice. With the file source it only decides where the links to GitHub point. |
| `GitHubToken` | none | Optional. Without a token the API allows 60 requests an hour per address, with one 5,000. A fine-grained token with no permission at all is enough for a public repository; a private one needs read access to its contents. Use an environment variable (or `dotnet user-secrets init` and `dotnet user-secrets set Changelog:GitHubToken ...`), never `appsettings.json`. |
| `CacheDuration` | `01:00:00` | How long the releases are used before they are asked for again. |
| `RequestTimeout` | `00:00:15` | How long one request to GitHub may take. |
| `PageSize` | `10` | Releases per page of the list. |
| `MaxReleases` | `100` | How many of the newest releases are fetched and kept. |
| `File` | `data/releases.json` | The file of the file source, relative to the folder of the site. |
| `ApiUrl` | `https://api.github.com/` | Change it for GitHub Enterprise Server (`https://github.example.com/api/v3/`). It must be `https`: the token goes to this host only. |
| `SiteUrl` | none | The public address of the site (`https://example.com/`, with its path base if any), for the links of the feed. Without it they are built from the request. Set it behind a cache or a proxy. |

A setting that cannot work is logged once when the site starts. It does not stop the site: the pages tell the visitor that the releases are not available.

## What is in it

```text
src/Changelog.Web/
  ReleaseNotes/              the GitHub client, the file source, the cache, the notes renderer, the feed
  Components/Pages/          Home (the timeline and its pages), ReleasePage (/releases/{slug}), NotFound, Error
  Components/Layout/         the header with the feed button, the main area and the footer
  Components/App.razor       the document: theme, fonts, the base address, the stylesheet, the copy script
  Content/                   the Markdown code, shared with the other content templates
  Site/SiteInfo.cs           the name of the site and its words, as typed C#
  data/releases.json         the sample releases, in the format of the GitHub API
  Styles/app.css             Tailwind entry, and the styles of what Markdown adds (code, callouts, tables)
```

## How the releases are read

`GitHubReleaseClient` is a typed `HttpClient` (registered with `AddHttpClient`, so connections are pooled and recycled) for `GET /repos/{owner}/{repo}/releases?per_page=100`.

- **What is sent.** A generic `User-Agent` (`lux-template-changelog`, GitHub requires one) and nothing about you or your site; the `Accept` and `X-GitHub-Api-Version` headers; `Authorization: Bearer` only when a token is set. The token is not logged and is not in any page.
- **Pagination.** The client follows the `Link: rel="next"` header, up to `MaxReleases` and at most ten pages, and only to the host it was configured for and over `https`: a link to another host is ignored, so the token cannot follow it.
- **Cache.** `ReleaseService` keeps the list in memory (`IMemoryCache`) for `CacheDuration`. Visitors who arrive while the first read runs share it. After the hour the next visitor gets the saved copy at once and a refresh runs in the background, so nobody waits for GitHub.
- **Conditional requests.** The refresh sends the `ETag` of the first page as `If-None-Match`. A `304` costs no rate limit and only renews the copy. Releases older than the first page are therefore refreshed when the first page changes, not before.
- **Rate limit and failures.** On a `403` or `429` with `X-RateLimit-Remaining: 0` or `Retry-After`, a `5xx`, a timeout or no network, the saved copy keeps being shown with a notice that it may be out of date, and the service does not ask again until the limit resets (at most an hour) or, after another failure, for a minute. Without a saved copy the page says what happened (503, with `Retry-After`). Nothing in this path throws.
- **Timeouts and cancellation.** Each request has `RequestTimeout`. A visitor who leaves stops waiting but does not stop the read.

Edit a release on GitHub and the site shows it within the hour. To refresh sooner, restart the site or lower `CacheDuration`.

## Pages and the feed

- `/` lists the releases, ten to a page: `/?page=2` is the next ten ("Older releases", "Newer releases"). A page that does not exist is a 404.
- `/releases/{slug}` is one release. The address holds a slug made from the tag (`v1.4.0` stays `v1.4.0`; `release/1.2` becomes `release-1.2`; a tag that leaves nothing becomes `release`; a repeat gets `-2`), never the tag itself. The page finds the release in the list it already holds, so whatever the tag or the address contains nothing is looked up on disk or sent to GitHub with it. An address that matches no release is a 404.
- `/feed.xml` is an Atom feed of the latest twenty releases, with the notes as escaped HTML, valid XML whatever the notes contain, cached by readers for five minutes and answered with `304` when it did not change.

The title of a release is an `h2` on the list and an `h1` on its page, and the headings of its notes start one level below, so the order of headings of a page stays right.

## Release notes are untrusted

A release body can be written by anyone who can publish a release, and generated notes repeat pull request titles written by contributors. `ReleaseNotes/ReleaseBody.cs` renders it with the shared Markdown code (`Content/MarkdownRenderer.cs`) in its mode for content from outside:

- Raw HTML is shown as text, never run. Attributes written in Markdown (`{onclick=...}`), math and media embeds are not supported.
- A link keeps its address only for `http`, `https` and `mailto`. `javascript:`, `data:`, `vbscript:` and the like lose the link and keep their text, whether written in capitals, with tabs or with entities. A relative link would point into your site, so it is dropped too.
- Every link has `rel="noopener noreferrer nofollow"` and opens in the same tab. There is no `target`, so there is no new window to take over, and a reader who wants a new tab asks for one.
- Images must be `https`; they load lazily and send no referrer. Any host is allowed, because release notes show badges and screenshots from several. Privacy: an image from a third-party host makes the visitor's browser contact that host, which learns the visitor's IP address and that they read these notes. If you want only GitHub's, add `img-src 'self' https://github.com https://*.githubusercontent.com` to a Content-Security-Policy.
- `@user` and `#123` in running text (not in code, not inside a link) become links to `github.com/user` and to the issue or pull request of the repository, built from the name or the number and never from the text around it.
- Ids start with the slug of the release, so a heading cannot take the id of an element of the page and two releases on one page never share an id.
- Markdown that the parser refuses (nested too deeply to be sensible) is shown as plain text.

Authors are checked too: a login must be one GitHub could have, and an avatar must be an `https` address on `avatars.githubusercontent.com`, otherwise it is left out. The address of the author and of the release on GitHub are built from the login and the tag, not copied from the data.

## The sample releases

`data/releases.json` is the array the API returns, trimmed to what the site reads, with twelve made up releases (a pre-release, a release with no name and no notes, tables, a task list, code, callouts, mentions and issue numbers). Replace it with your own, or keep it as the fixture of your tests: with `Changelog:Source=File` nothing leaves the machine.

## Serve below a path

Set the `PathBase` setting (an environment variable `PathBase=/site`, or `appsettings.json`) to serve the whole site below a prefix, behind a reverse proxy for example. Links in the pages are relative, so they follow, and the feed uses the prefix in its addresses. The host does not redirect to HTTPS, so that a proxy that terminates TLS works as is; if the site is exposed directly, add `app.UseHttpsRedirection()` and `app.UseHsts()` to `Program.cs`.

### Behind a reverse proxy

A proxy that terminates TLS hands the app plain HTTP on its own address, and tells it the real ones in `X-Forwarded-For`, `X-Forwarded-Proto` and `X-Forwarded-Host`. The app ignores those headers unless you turn them on, because a visitor could send them too. Set `ForwardedHeaders:Enabled` to `true` (the environment variable `ForwardedHeaders__Enabled=true`) and list the addresses of your proxies in `ForwardedHeaders:KnownProxies` (`ForwardedHeaders__KnownProxies__0=10.0.0.5`); a proxy on the same machine (loopback) is trusted without being listed. Headers from any other sender are not believed.

### Production: set `AllowedHosts` and `Changelog:SiteUrl`

When `Changelog:SiteUrl` is not set, the links of the feed are built from the `Host` header of the request. A client can send any host, so in production set both: `AllowedHosts` in `appsettings.json` to your domain (ASP.NET Core host filtering then answers 400 to any other host before the feed or a page runs), and `Changelog:SiteUrl` so the feed does not depend on the header at all. The rendering of the notes never contains the host, so nothing a request can choose is kept in the cache; the `Host` header only reaches the feed's `xml:base` and its links, and a shared cache that keys on the URL will only ever see hosts that `AllowedHosts` lets through.

## Why the site is static

Everything that moves runs in the browser: the color mode (a Lux element that needs no .NET code) and the copy buttons of the code blocks (a few lines of JavaScript, `Content/CodeCopy.cs`, written into `App.razor`; a site with a Content-Security-Policy gives that script a nonce, or serves it as a file). If a page needs a component that runs .NET in the browser, add `AddInteractiveServerComponents()` to `Program.cs` and `@rendermode InteractiveServer` to that component only.

## Develop

Run the site with `dotnet watch`, and Tailwind in a second terminal, so a new utility class shows up at the next reload:

```bash
dotnet watch --project src/Changelog.Web
dotnet build src/Changelog.Web -t:LuxTailwindWatch
```

## The shared Markdown code

`src/Changelog.Web/Content` is a copy of code that the other content templates (Docs, SaaS, Portfolio) carry too. It holds more than this site uses (the files for pages and search of the Docs template); only the renderer is used here. Treat it as a library. Packages: Markdig (BSD-2-Clause) and YamlDotNet (MIT), pinned in `Directory.Packages.props`.

## License

MIT, see `LICENSE`.
