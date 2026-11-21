# Docs

A documentation site written in Markdown, on Blux UI: a header with a search button and a color mode button, a navigation tree built from your folders, the page with its table of contents, previous and next links, a home page and a 404 page. The product in the sample pages (Tern, a command line tool) and every name in them are made up.

Pages are `.md` files with a little YAML at the top. The site reads them when it starts, renders them with [Markdig](https://github.com/xoofx/markdig) and builds the navigation, the table of contents and the search from what it finds. It is rendered by the server and sent as HTML: no circuit, no WebAssembly download.

<!-- rename:start -->
## Make it yours

This repository is a GitHub template. After you create your repository from it, give the site its own name once, from the root of the repository:

```bash
dotnet run rename.cs MyApp
```

`rename.cs` is a .NET 10 file-based app. It renames the solution, the project and its folder, the namespaces and every other occurrence of the template name, then deletes itself. The new name is one identifier: letters, digits and underscores, starting with a letter.

<!-- rename:end -->
## Run

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```bash
dotnet run --project src/Docs.Web
```

The first build downloads three things and keeps them in caches: the Tailwind CLI (once per machine, checked against a SHA-256 that ships with the Blux.UI package), the fonts of `Docs.Web.csproj` and the icons your pages name. Later builds, and builds without a network, reuse them.

## What is in it

```text
src/Docs.Web/
  content/                   your pages: folders of .md files, and a _dir.yml in a folder to name it
  Site/SiteInfo.cs           the name of the site and the words of the home page, as typed C#
  Content/                   the Markdown code, shared with the other content templates (see below)
  Components/Pages/          Home, Article (every page of content/), NotFound, Error
  Components/Layout/         the header with the search, the main area and the footer
  Components/App.razor       the document: theme, fonts, the base address, the stylesheet, the copy script
  Styles/app.css             Tailwind entry, and the styles of what Markdown adds (code, callouts, tables)
```

## Write pages

A page is a file below `content/`. Its address is its path without `.md`: `content/guides/setup.md` is `/guides/setup`, and an `index.md` is the address of its folder.

```markdown
---
title: Installation
description: Install the tool on Linux, macOS or Windows.
order: 2
navigationTitle: Install
icon: ph:download
draft: false
---

Start the body at `##`: the title above is the only `h1`.
```

| Key | What it does |
| --- | --- |
| `title` | The title of the page and its `h1`. Without it, the file name is used (`first-run.md` is "First run"). |
| `description` | The line under the title, the meta description and the line under the title in search results. |
| `order` | The position among the pages and folders next to it, smallest first. Without it a page comes after the ones that have one, by title. |
| `navigationTitle` | A shorter title for the link in the navigation. |
| `icon` | An icon name. The build cannot see names that are in Markdown: add them to the `BluxIcon` item in `Docs.Web.csproj`. |
| `draft` | `true` keeps the page out of the site, the navigation and the search. Set `Content:IncludeDrafts` to `true` to see drafts. |

A folder becomes a group in the navigation. Put a `_dir.yml` in it to set `title`, `order` and `icon`; without one the folder name is used ("getting-started" is "Getting started"). The top level folders also become the links of the header. Keys that are not listed are ignored, so check the spelling when a setting has no effect.

### Links, images and code

- Links start from the root of the site (`/guides/setup`) or from the folder of the file (`setup`, `./setup.md`, `../guides/setup.md#usage`). Both work below a path base, and `.md` and a final `index` are removed for you. A link to a page that does not exist is logged as a warning when the content is read.
- A link or an image may use `http`, `https` or `mailto`, or be relative. Any other scheme (`javascript:`, `data:`, `file:`) is dropped and the text stays. Put images in `wwwroot` and write `![A chart](/img/chart.png)`.
- A fenced block with a language gets colors and a copy button: `bash`, `json`, `yaml`, `toml`, `csharp` and `html` are known, and any other language is shown without color. The colors are CSS classes (`tok-keyword`, `tok-string`...) in `Styles/app.css`, so they follow light and dark mode.
- Callouts: `::: tip` ... `:::` (also `note`, `important`, `warning`, `caution`; text after the word becomes the title) or a GitHub alert (`> [!NOTE]`).
- Tables, task lists, footnotes, definition lists and heading ids (`## Install the CLI` is `#install-the-cli`) work as in GitHub Markdown.

## Search, navigation and table of contents

The search is a Blux `BxContentSearch` filled from the pages when the content is read: titles, `h2` and `h3` headings and the first 2,000 characters of each page. It filters in the browser, so it needs no server and no index file. Raise or lower the limit with `SearchTextLimit` in `Content/ContentOptions.cs` (through the options in `Program.cs`).

The navigation tree, the table of contents (`h2` and `h3` of the page) and the previous and next links are Blux components fed by the same data, in the order of the navigation.

## Why the site is static

Everything that moves runs in the browser: the menu, the search, the navigation groups, the table of contents, the color mode (Blux elements that need no .NET code) and the copy buttons (a few lines of JavaScript, `Content/CodeCopy.cs`, written into `App.razor`). A site with a Content-Security-Policy gives that script a nonce, or serves it as a file.

If a page needs a component that runs .NET in the browser, add `AddInteractiveServerComponents()` to `Program.cs` and `@rendermode InteractiveServer` to that component only. The rest stays static.

## Content and security

Markdown is rendered with raw HTML switched off: `<script>`, `<iframe>`, `onclick="..."` and the like are shown as text, never run. Links and images follow the rule above. The extensions are listed one by one in `MarkdownRenderer`: Markdig's "advanced" set is not used because it includes generic attributes (`{onclick=...}`), media embeds and math.

If you write all the content yourself and want raw HTML in it (an `<iframe>` of a video, a `<kbd>`), set `Content:AllowRawHtml` to `true` in `appsettings.json` or with the environment variable `Content__AllowRawHtml=true`. Then do not accept pages from anyone you do not trust: raw HTML in a page runs for every visitor.

## Develop

Run the site with `dotnet watch`, and Tailwind in a second terminal, so a new utility class shows up at the next reload:

```bash
dotnet watch --project src/Docs.Web
dotnet build src/Docs.Web -t:BluxTailwindWatch
```

In Development the site reads the content again a moment after a `.md` or `.yml` file changes (reload the page to see it). A file with a mistake does not take the site down: the previous content stays and the error, with the file name, is in the log. A published site reads its files once, when it starts, and stops with the file name if one cannot be used.

## The shared Markdown code

`src/Docs.Web/Content` is a copy of code that the other content templates (SaaS, Portfolio, Changelog) carry too. Treat it as a library: change it in your project freely, but it is not meant to be edited per site. Packages: Markdig (BSD-2-Clause) and YamlDotNet (MIT), pinned in `Directory.Packages.props`.

## Serve below a path

Set the `PathBase` setting (an environment variable `PathBase=/site`, or `appsettings.json`) to serve the whole site below a prefix, behind a reverse proxy for example. Links in the pages and in your Markdown are relative, so they follow. The host does not redirect to HTTPS, so that a proxy that terminates TLS works as is; if the site is exposed directly, add `app.UseHttpsRedirection()` and `app.UseHsts()` to `Program.cs`.

## License

MIT, see `LICENSE`.
