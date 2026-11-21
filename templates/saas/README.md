# SaaS

The marketing site of a subscription product, on Blux UI: a landing page, a pricing page with a monthly and yearly switch, a plan comparison and a FAQ, a blog, a documentation section with search, a changelog, and sign-in and sign-up pages. The product (Tideline, heartbeat monitoring for scheduled jobs) and every name, price and quote in it are made up.

The site is rendered by the server and sent as HTML. There is no circuit and no WebAssembly download, so it is quick to load and cheap to host. The documentation, the blog posts and the changelog are Markdown files that you write.

> **The sign-in and sign-up pages are UI only.** They validate what is typed and then do nothing: no account is created, nobody is signed in, no cookie is set and nothing is stored or sent. See [Authentication](#authentication-is-ui-only) before you ship anything.

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
dotnet run --project src/SaaS.Web
```

The first build downloads three things and keeps them in caches: the Tailwind CLI (once per machine, checked against a SHA-256 that ships with the Blux.UI package), the fonts of `SaaS.Web.csproj` and the icons your pages name. Later builds, and builds without a network, reuse them.

## What is in it

```text
src/SaaS.Web/
  content/docs/              the documentation: folders of .md files, and a _dir.yml in a folder to name it
  content/blog/              one .md file per post (title, date, author, tags, image in the front matter)
  content/changelog/         one .md file per version
  Site/SiteInfo.cs           the name, the links and the words of the home page, as typed C#
  Site/PricingInfo.cs        the plans, the comparison table and the questions of the pricing page
  Site/Library.cs            reads the posts, the versions and the documentation out of the content folder
  Content/                   the Markdown code, shared with the other content templates (see below)
  Components/Pages/          Home, Pricing, BlogIndex, BlogArticle, DocsHome, DocsArticle, ChangelogPage, ReleasePage, Login, Signup
  Components/Sections/       the sections of the home page; each reads SiteInfo and has no text of its own
  Components/Layout/         the header with the search, the main area and the footer
  Components/App.razor       the document: theme, fonts, the base address, the stylesheet, the copy script
  wwwroot/img/blog/          the cover images of the sample posts
  Styles/app.css             Tailwind entry, and the styles of what Markdown adds (code, callouts, tables)
```

- Change the name, links and words in `Site/SiteInfo.cs`, and the plans, prices and questions in `Site/PricingInfo.cs`. The label of the yearly option ("Yearly, 2 months free") is worked out from the prices. `ContactUrl`, `StatusUrl` and the two sign-in addresses are placeholders on example.com: point them at your own.
- Reorder, remove or add a section of the home page in `Components/Pages/Home.razor`. A section is a component, so a new one is a new file in `Components/Sections`.
- The text is English only. The language select translates what Blux itself writes (labels, menus, the messages of the forms) and switches the layout to right-to-left where needed; to translate your own text, read `CultureInfo.CurrentUICulture` in `SiteInfo` or move the strings to resource files.
- Tailwind is compiled by the build from `Styles/app.css` (`<BluxTailwind>true</BluxTailwind>` in `SaaS.Web.csproj`), with no Node and no `package.json`. The result is served as `app.css`.
- Package versions are in `Directory.Packages.props`. The Blux.UI version is pinned there.

## Write content

Everything below `content/` is read when the site starts. A page is a `.md` file with a little YAML at the top. Its address is its path without `.md`: `content/docs/guides/setup.md` is `/docs/guides/setup`.

### Documentation

`content/docs/`, with the same rules as the Docs template: folders are groups of the navigation (a `_dir.yml` sets `title`, `order` and `icon`), pages have `title`, `description`, `order`, `navigationTitle`, `icon` and `draft`, and the table of contents, the previous and next links and the search come from the files. Start the body at `##`: the title is the only `h1`.

### Blog posts

A post is `content/blog/<name>.md`, at the address `/blog/<name>`:

```markdown
---
title: A title
description: One or two sentences, shown on the card and under the title.
date: 2026-11-12
author: Noor Haddad
authorRole: Co-founder
tags: [product, announcements]
image: img/blog/waves.svg
imageAlt: What the cover shows.
---

Start the body at `##`.
```

`date` is required, written `yyyy-MM-dd`: the site stops at startup and names the file when it is missing or invalid. `author`, `authorRole`, `tags`, `image` (a path below `wwwroot`, written without a leading slash so that it works below a path base) and `imageAlt` are optional. Posts are listed newest first, the first one large; each tag is a link that filters the list (`/blog?tag=product`). The reading time is worked out from the text.

### Changelog

A version is `content/changelog/<name>.md`: `title`, `version` (the heading shown in the list), `description`, `date` (required) and an optional `badge`. Each version also has a page of its own, `/changelog/<name>`, which is where the search leads. Do not use headings inside a version: the list shows all of them on one page, and a repeated heading would repeat its `id`.

### What the shared code gives every page

Links start from the root of the site (`/docs/guides/setup`) or from the folder of the file (`setup`, `../guides/setup.md#usage`). A link to a page that is not a Markdown file (`/pricing`) is logged as a warning when the content is read, so link to the sign-up and pricing pages from the Razor components, not from the Markdown. Images are served from `wwwroot`. Fenced blocks are highlighted (`bash`, `json`, `yaml`, `toml`, `csharp`, `html`) and get a copy button. Callouts are `::: tip` ... `:::` (also `note`, `important`, `warning`, `caution`). Raw HTML in Markdown is switched off: set `Content:AllowRawHtml` to `true` only if nobody but you writes the content.

The search covers the documentation, the blog and the changelog, in one index that the browser fetches the first time the search opens (`/search-index.json`).

## Authentication is UI only

`Components/Pages/Login.razor` and `Signup.razor` use Blux's `BxAuthForm`, posted to the server with a form name, so the pages need no circuit. The form checks the required fields, the format of the email and, on the sign-up page, a password of at least 8 characters. When everything is valid it calls `SignInAsync` or `SignUpAsync`, which only remember the email so that the page can say "the form is valid". **Nobody is signed in and nothing is stored.** The two "Continue with" buttons lead to placeholder addresses in `SiteInfo.cs`.

To make it real, you need an identity system (ASP.NET Core Identity, an OpenID Connect provider such as Entra ID, Auth0 or Keycloak, or your own API) and these changes:

- Check the credentials in `SignInAsync` and put a message in `_error` when they are wrong; create the account in `SignUpAsync` and answer an email that is taken with the `FieldErrors` parameter of `BxAuthForm`.
- Sign the user in (a cookie) and redirect with `NavigationManager.NavigateTo`. Add `UseAuthentication()` and `UseAuthorization()` to `Program.cs`.
- Add rate limiting to the two pages, and a way to reset a password: the template has no page for it.
- Replace the two example.com addresses of the "Continue with" buttons with the sign-in endpoints of your providers.

The app that sits behind the sign-in (a dashboard, say) is not part of this template.

## Why the site is static

Everything that moves runs in the browser: the menu, the search, the navigation groups, the table of contents, the color mode, the language select and the accordion are Blux elements that need no .NET code, the copy buttons are a few lines of JavaScript (`Content/CodeCopy.cs`, written into `App.razor`), and the monthly and yearly switch of the pricing page is two radio buttons and CSS (`:has()`), so it works with the keyboard and does not wait for a connection. The cost is that the plans are written twice in the HTML, one copy hidden. The forms are ordinary form posts.

If a page needs a component that runs .NET in the browser, add `AddInteractiveServerComponents()` to `Program.cs` and `@rendermode InteractiveServer` to that component only. The rest stays static.

## Develop

Run the site with `dotnet watch`, and Tailwind in a second terminal, so a new utility class shows up at the next reload:

```bash
dotnet watch --project src/SaaS.Web
dotnet build src/SaaS.Web -t:BluxTailwindWatch
```

In Development the site reads the content again a moment after a `.md` or `.yml` file changes (reload the page to see it). A file with a mistake does not take the site down: the previous content stays and the error, with the file name, is in the log. A published site reads its files once, when it starts, and stops with the file name if one cannot be used.

## The shared Markdown code

`src/SaaS.Web/Content` is a copy of code that the other content templates (Docs, Changelog, Portfolio) carry too. Treat it as a library: change it in your project freely, but it is not meant to be edited per site. Every key of the front matter is also available as `MarkdownPage.Data`, which is how the blog reads `date` and `tags`. Packages: Markdig (BSD-2-Clause) and YamlDotNet (MIT), pinned in `Directory.Packages.props`.

## Serve below a path

Set the `PathBase` setting (an environment variable `PathBase=/site`, or `appsettings.json`) to serve the whole site below a prefix, behind a reverse proxy for example. Links in the pages and in your Markdown are relative, so they follow. The host does not redirect to HTTPS, so that a proxy that terminates TLS works as is; if the site is exposed directly, add `app.UseHttpsRedirection()` and `app.UseHsts()` to `Program.cs`.

### Behind a reverse proxy

A proxy that terminates TLS hands the app plain HTTP on its own address, and tells it the real ones in `X-Forwarded-For`, `X-Forwarded-Proto` and `X-Forwarded-Host`. The app ignores those headers unless you turn them on, because a visitor could send them too. Set `ForwardedHeaders:Enabled` to `true` (the environment variable `ForwardedHeaders__Enabled=true`) and list the addresses of your proxies in `ForwardedHeaders:KnownProxies` (`ForwardedHeaders__KnownProxies__0=10.0.0.5`); a proxy on the same machine (loopback) is trusted without being listed. Headers from any other sender are not believed. If the proxy passes the original `Host` header, also set `AllowedHosts` in `appsettings.json` to your domain.

## License

MIT, see `LICENSE`.
