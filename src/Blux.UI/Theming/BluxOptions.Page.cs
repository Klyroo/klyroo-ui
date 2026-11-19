using Blux.UI.Theming;

namespace Blux.UI;

public sealed partial class BluxOptions
{
    public ComponentTheme Page { get; } = Themes.Page();
    public ComponentTheme PageHeader { get; } = Themes.PageHeader();
    public ComponentTheme PageHero { get; } = Themes.PageHero();
    public ComponentTheme PageSection { get; } = Themes.PageSection();
    public ComponentTheme PageFeature { get; } = Themes.PageFeature();
    public ComponentTheme PageGrid { get; } = Themes.PageGrid();
    public ComponentTheme PageColumns { get; } = Themes.PageColumns();
    public ComponentTheme PageCard { get; } = Themes.PageCard();
    public ComponentTheme PageCta { get; } = Themes.PageCta();
    public ComponentTheme PageLinks { get; } = Themes.PageLinks();
    public ComponentTheme PageList { get; } = Themes.PageList();
    public ComponentTheme PageLogos { get; } = Themes.PageLogos();
    public ComponentTheme PageAnchors { get; } = Themes.PageAnchors();
    public ComponentTheme PageAside { get; } = Themes.PageAside();
    public ComponentTheme PageBody { get; } = Themes.PageBody();
}

public sealed partial class BluxStrings
{
    /// <summary>Accessible name of a <c>BxPageLinks</c> list that has neither a title nor an <c>AriaLabel</c>.</summary>
    public string PageLinks { get; set; } = "Links";

    /// <summary>Accessible name of a <c>BxPageAnchors</c> list that has no <c>AriaLabel</c>.</summary>
    public string PageAnchors { get; set; } = "On this page";

    /// <summary>Hidden text added to the name of a page link that opens in a new tab.</summary>
    public string OpensInNewTab { get; set; } = "(opens in a new tab)";
}
