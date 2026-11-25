namespace Klyroo.UI;

public sealed partial record KlyMessages
{
    /// <summary>Accessible name of the site navigation of a header.</summary>
    public string HeaderNavigation { get; init; } = "Main menu";

    /// <summary>Title of the mobile menu panel of a header, and name of its navigation.</summary>
    public string HeaderMenu { get; init; } = "Menu";

    /// <summary>Name of the button that opens the mobile menu of a header.</summary>
    public string OpenHeaderMenu { get; init; } = "Open menu";

    /// <summary>Text of the skip link (<c>KlySkipLink</c>): the first link of a page, to the main content.</summary>
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

    /// <summary>Label of the link of <see cref="KlyError"/> that goes back to the home page.</summary>
    public string ErrorBackHome { get; init; } = "Back to home";
    /// <summary>Title of <see cref="KlyError"/> for status 404.</summary>
    public string ErrorNotFound { get; init; } = "Page not found";
    /// <summary>Message of <see cref="KlyError"/> for status 404.</summary>
    public string ErrorNotFoundMessage { get; init; } = "The page you are looking for does not exist or has moved.";
    /// <summary>Title of <see cref="KlyError"/> for status 401.</summary>
    public string ErrorUnauthorized { get; init; } = "Sign in required";
    /// <summary>Message of <see cref="KlyError"/> for status 401.</summary>
    public string ErrorUnauthorizedMessage { get; init; } = "You need to sign in to view this page.";
    /// <summary>Title of <see cref="KlyError"/> for status 403.</summary>
    public string ErrorForbidden { get; init; } = "Access denied";
    /// <summary>Message of <see cref="KlyError"/> for status 403.</summary>
    public string ErrorForbiddenMessage { get; init; } = "You do not have permission to view this page.";
    /// <summary>Title of <see cref="KlyError"/> for status 500 and any other status without its own text.</summary>
    public string ErrorServer { get; init; } = "Something went wrong";
    /// <summary>Message of <see cref="KlyError"/> for status 500 and any other status without its own text.</summary>
    public string ErrorServerMessage { get; init; } = "An unexpected error occurred. Please try again later.";
}
