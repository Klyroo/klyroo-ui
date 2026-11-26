namespace Klyroo.UI;

/// <summary>
/// The HTML element a layout component renders its box as (the <c>As</c> parameter). Layout components render a
/// generic box by default; pick a landmark element when the box is one, keeping to one <c>main</c> and one top-level <c>header</c> per page.
/// </summary>
public enum KlyElement
{
    /// <summary>A generic box with no meaning of its own.</summary>
    Div,
    /// <summary>A thematic section of a page.</summary>
    Section,
    /// <summary>A self-contained piece of content.</summary>
    Article,
    /// <summary>A complementary landmark, such as a sidebar.</summary>
    Aside,
    /// <summary>A header: the banner landmark of the page when it sits outside any sectioning element.</summary>
    Header,
    /// <summary>A footer: the content-info landmark of the page when it sits outside any sectioning element.</summary>
    Footer,
    /// <summary>The main landmark. A page has only one.</summary>
    Main,
    /// <summary>A navigation landmark.</summary>
    Nav,
}
