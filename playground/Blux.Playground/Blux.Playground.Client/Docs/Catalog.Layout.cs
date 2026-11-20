using Blux.UI;

namespace Blux.Playground.Client.Docs;

public static partial class Catalog
{
    private static readonly ComponentDoc[] Layout =
    [
        new("app", "App", "Layout", "The root wrapper of an app: renders the toaster, resolves the locale and sets language and direction, and cascades global class overrides.",
            [new("Global overrides", typeof(Examples.AppExample), "Place it once in the layout, around the body, in place of a hand-placed toaster. The overrides do not cross a render mode boundary: with per-page interactivity use AddBluxUi for app-wide ones.")], [typeof(BxApp)]),
        new("header", "Header", "Layout", "A site header with a title, links, actions and a mobile menu that opens in a slideover.",
            [new("Header", typeof(Examples.HeaderExample), "The mobile menu is a native dialog handled in the browser: it works in static rendering and closes when a link is followed."),
             new("Custom slots", typeof(Examples.HeaderCustomExample), "Left, center, right and the mobile body are all replaceable.")],
            [typeof(BxHeader)]),
        new("footer", "Footer", "Layout", "A site footer with top and bottom areas and left, center and right slots.",
            [new("Footer", typeof(Examples.FooterExample))], [typeof(BxFooter)]),
        new("footer-columns", "FooterColumns", "Layout", "Columns of links under headings, for the top of a footer.",
            [new("Columns in a footer", typeof(Examples.FooterColumnsExample))], [typeof(BxFooterColumns), typeof(BxFooterColumn)]),
        new("main", "Main", "Layout", "The main landmark, tall enough to keep the footer at the bottom of the screen.",
            [new("Main between a header and a footer", typeof(Examples.MainExample), "A page has one main landmark: use As=\"div\" when the layout already has one.")], [typeof(BxMain)]),
        new("sidebar", "Sidebar", "Layout", "An app sidebar that collapses to icons, can be resized, and becomes an off-canvas panel on small screens.",
            [new("Collapsible, remembered", typeof(Examples.SidebarExample), "The collapsed state lives in the browser and is stored in a cookie named after the Id. The content is rendered twice, in the column and in the mobile panel: avoid ids and local state inside it."),
             new("Bound state", typeof(Examples.SidebarBoundExample), "@bind-Collapsed reports the state to .NET; your state wins over the stored one."),
             new("Resizable", typeof(Examples.SidebarResizableExample))],
            [typeof(BxSidebar)]),
        new("error", "Error", "Layout", "The content of an error page: status code, message and a way back.",
            [new("Not found", typeof(Examples.ErrorExample), "Use it as the body of the NotFound page."),
             new("Error boundary", typeof(Examples.ErrorBoundaryExample), "Pass the exception from ErrorContent; the details are shown only on request.")],
            [typeof(BxError)]),
        new("locale-select", "LocaleSelect", "Layout", "A select of locales that writes the ASP.NET Core culture cookie and reloads the page.",
            [new("Locale select", typeof(Examples.LocaleSelectExample), "Server setup: app.UseRequestLocalization(...) with the same cultures; the default cookie provider reads the cookie. Components follow the culture automatically. In WebAssembly, call ApplyCultureCookieAsync in the client Program.cs so the client picks the same culture."),
             new("Locales per subtree", typeof(Examples.LocalePanelsExample), "BxApp Locale sets the texts, the direction and the date and number culture of everything inside it. Arabic is right to left."),
             new("Switching at runtime", typeof(Examples.LocaleSwitchExample), "Changing the Locale parameter re-renders the components below in the new language. The buttons need interactivity."),
             new("Extending a locale", typeof(Examples.LocaleExtendExample), "BxLocales.Extend keeps every message you do not override.")],
            [typeof(BxLocaleSelect), typeof(BxLocale), typeof(BxMessages)]),
        new("theme", "Theme", "Layout", "Cascades Ui overrides to every component of a type below it, without touching each instance.",
            [new("One component type", typeof(Examples.ThemeExample), "Instance Ui and Class still win over the theme. A theme does not cross a render mode boundary: repeat it inside an interactive island, or use AddBluxUi."),
             new("Several types, nested", typeof(Examples.ThemeNestedExample))],
            [typeof(BxTheme)]),
    ];
}
