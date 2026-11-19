using Blux.UI.Theming;

namespace Blux.UI;

public sealed partial class BluxOptions
{
    public ComponentTheme App { get; } = Themes.App();
    public ComponentTheme Header { get; } = Themes.Header();
    public ComponentTheme Footer { get; } = Themes.Footer();
    public ComponentTheme FooterColumns { get; } = Themes.FooterColumns();
    public ComponentTheme Main { get; } = Themes.Main();
    public ComponentTheme Sidebar { get; } = Themes.Sidebar();
    public ComponentTheme Error { get; } = Themes.Error();
    public ComponentTheme LocaleSelect { get; } = Themes.LocaleSelect();
}

public sealed partial class BluxStrings
{
    /// <summary>Accessible name of the site navigation of a header.</summary>
    public string HeaderNavigation { get; set; } = "Main menu";

    /// <summary>Title of the mobile menu panel of a header, and name of its navigation.</summary>
    public string HeaderMenu { get; set; } = "Menu";

    /// <summary>Name of the button that opens the mobile menu of a header.</summary>
    public string OpenHeaderMenu { get; set; } = "Open menu";

    /// <summary>Accessible name of a sidebar, and title of its mobile panel.</summary>
    public string SidebarLabel { get; set; } = "Sidebar";

    /// <summary>Name of the button that opens the mobile panel of a sidebar.</summary>
    public string OpenSidebar { get; set; } = "Open sidebar";

    /// <summary>Name of the button that collapses a sidebar to icons; it is pressed while the sidebar is collapsed.</summary>
    public string CollapseSidebar { get; set; } = "Collapse sidebar";

    /// <summary>Name of the handle that resizes a sidebar.</summary>
    public string ResizeSidebar { get; set; } = "Resize sidebar";

    /// <summary>Accessible name of the link columns of a footer.</summary>
    public string FooterLinks { get; set; } = "Footer";

    /// <summary>Accessible name of a language select.</summary>
    public string LocaleSelectLabel { get; set; } = "Language";

    public string ErrorBackHome { get; set; } = "Back to home";
    public string ErrorNotFound { get; set; } = "Page not found";
    public string ErrorNotFoundMessage { get; set; } = "The page you are looking for does not exist or has moved.";
    public string ErrorUnauthorized { get; set; } = "Sign in required";
    public string ErrorUnauthorizedMessage { get; set; } = "You need to sign in to view this page.";
    public string ErrorForbidden { get; set; } = "Access denied";
    public string ErrorForbiddenMessage { get; set; } = "You do not have permission to view this page.";
    public string ErrorServer { get; set; } = "Something went wrong";
    public string ErrorServerMessage { get; set; } = "An unexpected error occurred. Please try again later.";
}
