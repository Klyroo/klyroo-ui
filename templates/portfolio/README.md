# Portfolio

The personal site of a designer or developer, on Blux UI: a home page with an introduction and links, a work section with a page for each project, an about page with an experience timeline, writing with tags and articles, a speaking page, a contact form, and a "uses" page. The owner (Ines Calloway, a design engineer in Lisbon) and every project, employer, client, article and talk in it are made up.

The site is rendered by the server and sent as HTML. There is no circuit and no WebAssembly download, so it is quick to load and cheap to host. Projects, articles and the text pages are Markdown files that you write.

> **The contact form does not send anything.** It checks what is typed and then says that the form is valid and that nothing was sent. No email goes out, no message is stored or logged. See [The contact form is UI only](#the-contact-form-is-ui-only) before you ship the site.

<!-- rename:start -->
## Make it yours

This repository is a GitHub template. After you create your repository from it, give the site its own name once, from the root of the repository:

```bash
dotnet run rename.cs MyApp
```

`rename.cs` is a .NET 10 file-based app. It renames the solution, the project and its folder, the namespaces and every other occurrence of the template name, then deletes itself. The new name is one identifier: letters and digits, starting with a letter.

<!-- rename:end -->
## Run

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```bash
dotnet run --project src/Portfolio.Web
```

The first build downloads three things and keeps them in caches: the Tailwind CLI (once per machine, checked against a SHA-256 that ships with the Blux.UI package), the fonts of `Portfolio.Web.csproj` and the icons your pages name. Later builds, and builds without a network, reuse them.

## What is in it

```text
src/Portfolio.Web/
  content/work/              one .md file per project (year, role, client, stack, links, image in the front matter)
  content/writing/           one .md file per article (title, date, tags, image in the front matter)
  content/about.md           the text of the about page
  content/uses.md            a plain page: any .md file outside work/ and writing/ is served at its own address
  Site/SiteInfo.cs           your name, links and the words of the pages laid out in Razor, as typed C#
  Site/Resume.cs             the experience (the timeline of the about page) and the skills
  Site/Talks.cs              the talks of the speaking page
  Site/ContactMessage.cs     what the contact form posts, with its rules
  Site/Library.cs            reads the projects and articles out of the content folder, and checks them
  Site/SiteDates.cs          writes a date the way the Blux cards do (the culture of the visitor, no day name)
  Content/                   the Markdown code, shared with the other content templates (see below)
  Components/Pages/          Home, WorkIndex, ProjectPage, About, WritingIndex, WritingArticle, Speaking, Contact, PlainPage
  Components/Sections/       the sections of the home page; each reads SiteInfo or the content and has no text of its own
  Components/Layout/         the header with the search, the main area and the footer
  Components/App.razor       the document: theme, fonts, the base address, the stylesheet, the copy script
  wwwroot/img/work/          the cover images of the sample projects
  wwwroot/img/writing/       the cover images of the sample articles
  Styles/app.css             Tailwind entry, and the styles of what Markdown adds (code, callouts, tables)
```

- Start in `Site/SiteInfo.cs`: your name, role, location, the links to your accounts and the words of the hero and the other pages. `Email`, `GitHubUrl`, `LinkedInUrl` and `MastodonUrl` are placeholders on example.com, which is reserved for documentation: point them at your own.
- Replace the people and the places in `Site/Resume.cs` and `Site/Talks.cs`, and rewrite `content/about.md` and `content/uses.md`. The initials in the header and the hero are the avatar of `BxAvatar`: give `BxUser` and `BxAvatar` a `Src` (a file in `wwwroot`) to show a photo instead.
- Reorder, remove or add a section of the home page in `Components/Pages/Home.razor`. A section is a component, so a new one is a new file in `Components/Sections`.
- The text is English only. The language select translates what Blux itself writes (labels, menus, the messages of the forms) and switches the layout to right-to-left where needed; to translate your own text, read `CultureInfo.CurrentUICulture` in `SiteInfo` or move the strings to resource files.
- Tailwind is compiled by the build from `Styles/app.css` (`<BluxTailwind>true</BluxTailwind>` in `Portfolio.Web.csproj`), with no Node and no `package.json`. The result is served as `app.css`.
- Package versions are in `Directory.Packages.props`. The Blux.UI version is pinned there.

## Write content

Everything below `content/` is read when the site starts and is searchable. A page is a `.md` file with a little YAML at the top. Its address is its path without `.md`, and what serves it depends on the folder:

| Where | What it is | Served by |
| --- | --- | --- |
| `content/work/<name>.md` | A project (needs a `year`) | `/work/<name>` and the list at `/work` |
| `content/writing/<name>.md` | An article (needs a `date`) | `/writing/<name>` and the list at `/writing` |
| `content/about.md` | The text of the about page | `/about`, laid out by `About.razor` with the timeline and the skills |
| Anywhere else, such as `content/uses.md` | A plain page | `/uses`, with the layout of the site and nothing else |

The rule is that only the files directly in `work/` and `writing/` are projects and articles: a file in a folder below them (`writing/2026/post.md`) is a plain page at `/writing/2026/post`, not in the list. Link to a plain page from the Razor components (the footer links to `uses`) or from Markdown (`[uses](/uses)`).

### Projects

A project is `content/work/<name>.md`, at the address `/work/<name>`:

```markdown
---
title: Harbor Atlas
description: One or two sentences, shown on the card and under the title.
year: 2025
role: Design and front-end
client: A client
stack: [TypeScript, MapLibre]
featured: true
image: img/work/harbor-atlas.svg
imageAlt: What the cover shows.
links:
  - label: Live site
    url: https://example.com/harbor-atlas
---

Start the body at `##`.
```

`year` is required and has four digits: the site stops at startup and names the file when it is missing or invalid. `featured: true` puts the project on the home page (the three most recent featured ones). `role`, `client`, `stack`, `links`, `image` and `imageAlt` are optional; a link needs a `label` and a `url`, and an address with a scheme has to be `http`, `https` or `mailto`. Projects are listed with the most recent year first. A cover is a path below `wwwroot`, from the root of the site, with or without a leading slash (`img/work/harbor-atlas.svg` and `/img/work/harbor-atlas.svg` are the same: it is rewritten like the images of a body, so it works below a path base), or an `https://` address. A cover that has no file in `wwwroot` is a warning in the log, like an image of a body.

In Development, saving a file with such a mistake keeps the previous content and logs the error with the file name, instead of breaking the pages (`ContentOptions.Validate` in `Program.cs` runs at every read).

### Articles

An article is `content/writing/<name>.md`:

```markdown
---
title: A title
description: One or two sentences, shown on the card and under the title.
date: 2026-11-12
tags: [accessibility, css]
image: img/writing/cover.svg
imageAlt: What the cover shows.
---

Start the body at `##`.
```

`date` is required, written `yyyy-MM-dd`. `tags`, `image` and `imageAlt` are optional. Articles are listed newest first, the first one large; each tag is a link that filters the list (`/writing?tag=css`). The reading time is worked out from the text. The author of every article is the owner of the site (`SiteInfo.Name`).

### What the shared code gives every page

Links start from the root of the site (`/work/quire`) or from the folder of the file (`quire`, `../writing/a.md#usage`). A link to a page that is not a Markdown file (`/contact`) is logged as a warning when the content is read, so link to the contact page from the Razor components, not from the Markdown. Images are served from `wwwroot`. Fenced blocks are highlighted (`bash`, `json`, `yaml`, `toml`, `csharp`, `html`) and get a copy button. Callouts are `::: tip` ... `:::` (also `note`, `important`, `warning`, `caution`). Raw HTML in Markdown is switched off: set `Content:AllowRawHtml` to `true` only if nobody but you writes the content.

The search covers the projects, the articles, the about page and the plain pages, in one index that the browser fetches the first time the search opens (`/search-index.json`).

## The contact form is UI only

`Components/Pages/Contact.razor` uses Blux's `BxForm`, posted to the server with a form name (`FormName="contact"` and a `[SupplyParameterFromForm]` model), so the page needs no circuit. The server checks the rules of `Site/ContactMessage.cs` on every post (name, a valid email, one of the topics, a message of 10 to 2000 characters) and shows the errors beside the fields. When everything is valid it calls `SendAsync`, which only remembers the name so that the page can say "the form is valid" and that nothing was sent. **No email is sent, nothing is stored and nothing is logged.** The page carries no other notice: this README is where it is written down.

To make it real you need somewhere to deliver the message, and these changes:

- Send the message in `SendAsync`: with an SMTP client, an email API, or a form service. Put a message in `_error` when it fails, and keep what the visitor typed so that nothing is lost.
- Stop bots: a rate limit on the page, a hidden field that real visitors leave empty, or a check such as Cloudflare Turnstile. A public form that sends email is found within days.
- Decide what you keep, and say so on the page. A message holds an email address, which is personal data.
- Do not put a visitor's text in an email header: use the API of your mail library for the subject and the reply address, which rejects line breaks.

## Why the site is static

Everything that moves runs in the browser: the menu, the search, the color mode and the language select are Blux elements that need no .NET code, and the copy buttons are a few lines of JavaScript (`Content/CodeCopy.cs`, written into `App.razor`). The contact form is an ordinary form post.

If a page needs a component that runs .NET in the browser, add `AddInteractiveServerComponents()` to `Program.cs` and `@rendermode InteractiveServer` to that component only. The rest stays static.

## Develop

Run the site with `dotnet watch`, and Tailwind in a second terminal, so a new utility class shows up at the next reload:

```bash
dotnet watch --project src/Portfolio.Web
dotnet build src/Portfolio.Web -t:BluxTailwindWatch
```

In Development the site reads the content again a moment after a `.md` or `.yml` file changes (reload the page to see it). A file with a mistake does not take the site down: the previous content stays and the error, with the file name, is in the log. A published site reads its files once, when it starts, and stops with the file name if one cannot be used.

## The shared Markdown code

`src/Portfolio.Web/Content` is a copy of code that the other content templates (Docs, SaaS, Changelog) carry too. Treat it as a library: change it in your project freely, but it is not meant to be edited per site. Every key of the front matter is also available as `MarkdownPage.Data`, which is how the site reads `year`, `stack` and `tags`. Packages: Markdig (BSD-2-Clause) and YamlDotNet (MIT), pinned in `Directory.Packages.props`.

## Serve below a path

Set the `PathBase` setting (an environment variable `PathBase=/site`, or `appsettings.json`) to serve the whole site below a prefix, behind a reverse proxy for example. Links in the pages and in your Markdown are relative, so they follow. The host does not redirect to HTTPS, so that a proxy that terminates TLS works as is; if the site is exposed directly, add `app.UseHttpsRedirection()` and `app.UseHsts()` to `Program.cs`.

### Behind a reverse proxy

A proxy that terminates TLS hands the app plain HTTP on its own address, and tells it the real ones in `X-Forwarded-For`, `X-Forwarded-Proto` and `X-Forwarded-Host`. The app ignores those headers unless you turn them on, because a visitor could send them too. Set `ForwardedHeaders:Enabled` to `true` (the environment variable `ForwardedHeaders__Enabled=true`) and list the addresses of your proxies in `ForwardedHeaders:KnownProxies` (`ForwardedHeaders__KnownProxies__0=10.0.0.5`); a proxy on the same machine (loopback) is trusted without being listed. Headers from any other sender are not believed. If the proxy passes the original `Host` header, also set `AllowedHosts` in `appsettings.json` to your domain.
