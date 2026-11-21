# Landing

A marketing site for a product, on Blux UI: a header with navigation, a language select and a color mode button, a hero, a logo strip, features, a "how it works" section, testimonials, pricing with a monthly and yearly switch, a FAQ, a call to action and a footer. The product in the copy (Paperlane, an invoicing tool) and every name in it are made up.

The site is rendered by the server and sent as HTML. There is no circuit and no WebAssembly download, so it is quick to load and cheap to host.

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
dotnet run --project src/Landing.Web
```

The first build downloads three things and keeps them in caches: the Tailwind CLI (once per machine, checked against a SHA-256 that ships with the Blux.UI package), the fonts of `Landing.Web.csproj` and the icons your pages name. Later builds, and builds without a network, reuse them.

## What is in it

```text
src/Landing.Web/
  Content/SiteContent.cs     every sentence, price, link and icon name of the page, as typed C#
  Components/Pages/          Home (the sections, in order), NotFound, Error
  Components/Sections/       one component per section; each reads SiteContent and has no text of its own
  Components/Layout/         the header, the main area and the footer
  Components/App.razor       the document: theme, fonts, the base address, the stylesheet
  Assets/marks/              the SVG marks of the logo strip, bundled as the icons mark:<file name>
  Styles/app.css             Tailwind entry: your own CSS goes under the two imports
```

- Change the words, prices, plans, questions and links in `Content/SiteContent.cs`. `SignUpUrl` and `ContactUrl` are placeholders on example.com: point them at your product.
- Reorder, remove or add a section in `Components/Pages/Home.razor`. A section is a component, so a new one is a new file in `Components/Sections`.
- Replace the marks in `Assets/marks` with the logos of your customers (SVG that uses `currentColor` follows light and dark mode), and keep the names in `SiteContent.Logos` in step.
- The copy is English only. The language select translates what Blux itself writes (labels, the menu, the accordion) and switches the layout to right-to-left where needed; to translate your own text, read `CultureInfo.CurrentUICulture` in `SiteContent` or move the strings to resource files.
- Tailwind is compiled by the build from `Styles/app.css` (`<BluxTailwind>true</BluxTailwind>` in `Landing.Web.csproj`), with no Node and no `package.json`. The result is served as `app.css`.
- Package versions are in `Directory.Packages.props`. The Blux.UI version is pinned there.

## Why the page is static

Everything that moves runs in the browser: the mobile menu, the color mode, the language select and the accordion are Blux elements that need no .NET code. The monthly and yearly switch of the pricing section is two radio buttons and CSS (`:has()`), so it works with the keyboard, is remembered by the browser and does not wait for a connection. The cost is that the plans are written twice in the HTML, one copy hidden.

If a page needs a component that runs .NET in the browser (a form with live validation, a chart that you feed from the server), add `AddInteractiveServerComponents()` to `Program.cs` and `@rendermode InteractiveServer` to that component only. The rest stays static.

## Develop

Run the site with `dotnet watch`, and Tailwind in a second terminal, so a new utility class shows up at the next reload:

```bash
dotnet watch --project src/Landing.Web
dotnet build src/Landing.Web -t:BluxTailwindWatch
```

In VS Code, the `dev` task (Terminal, Run Task) builds once and starts both. `dotnet watch` does not follow the generated CSS by itself, and it does not reload the page for a CSS-only edit: reload by hand after an edit.

## Serve below a path

Set the `PathBase` setting (an environment variable `PathBase=/site`, or `appsettings.json`) to serve the whole site below a prefix, behind a reverse proxy for example. Links in the pages are relative, so they follow. The host does not redirect to HTTPS, so that a proxy that terminates TLS works as is; if the site is exposed directly, add `app.UseHttpsRedirection()` and `app.UseHsts()` to `Program.cs`.

## License

MIT, see `LICENSE`.
