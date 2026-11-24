namespace Lux.UI;

public sealed partial record BxMessages
{
    /// <summary>Accessible name of the site navigation of a header.</summary>
    public string HeaderNavigation { get; init; } = "Main menu";

    /// <summary>Title of the mobile menu panel of a header, and name of its navigation.</summary>
    public string HeaderMenu { get; init; } = "Menu";

    /// <summary>Name of the button that opens the mobile menu of a header.</summary>
    public string OpenHeaderMenu { get; init; } = "Open menu";

    /// <summary>Text of the skip link (<c>BxSkipLink</c>): the first link of a page, to the main content.</summary>
    public string SkipToContent { get; init; } = "Skip to content";

    /// <summary>Accessible name of a sidebar, and title of its mobile panel.</summary>
    public string SidebarLabel { get; init; } = "Sidebar";

    /// <summary>Name of the button that opens the mobile panel of a sidebar.</summary>
    public string OpenSidebar { get; init; } = "Open sidebar";

    /// <summary>Name of the button that collapses a sidebar to icons; it is pressed while the sidebar is collapsed.</summary>
    public string CollapseSidebar { get; init; } = "Collapse sidebar";

    /// <summary>Name of the handle that resizes a sidebar.</summary>
    public string ResizeSidebar { get; init; } = "Resize sidebar";

    /// <summary>Accessible name of the link columns of a footer.</summary>
    public string FooterLinks { get; init; } = "Footer";

    /// <summary>Accessible name of a language select.</summary>
    public string LocaleSelectLabel { get; init; } = "Language";

    public string ErrorBackHome { get; init; } = "Back to home";
    public string ErrorNotFound { get; init; } = "Page not found";
    public string ErrorNotFoundMessage { get; init; } = "The page you are looking for does not exist or has moved.";
    public string ErrorUnauthorized { get; init; } = "Sign in required";
    public string ErrorUnauthorizedMessage { get; init; } = "You need to sign in to view this page.";
    public string ErrorForbidden { get; init; } = "Access denied";
    public string ErrorForbiddenMessage { get; init; } = "You do not have permission to view this page.";
    public string ErrorServer { get; init; } = "Something went wrong";
    public string ErrorServerMessage { get; init; } = "An unexpected error occurred. Please try again later.";
}
