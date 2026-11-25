namespace Klyroo.UI;

public sealed partial record KlyMessages
{
    /// <summary>Accessible name of the navigation landmark of <see cref="KlyBreadcrumb"/>.</summary>
    public string Breadcrumb { get; init; } = "Breadcrumb";
    /// <summary>Accessible name of the navigation landmark of <see cref="KlyPagination"/> when no <c>AriaLabel</c> is given.</summary>
    public string Pagination { get; init; } = "Pagination";
    /// <summary>Prefix of the accessible name of a page button of <see cref="KlyPagination"/> ("Page 3").</summary>
    public string Page { get; init; } = "Page";
    /// <summary>Accessible name of the first page button of <see cref="KlyPagination"/>.</summary>
    public string FirstPage { get; init; } = "First page";
    /// <summary>Accessible name of the previous page button of <see cref="KlyPagination"/>.</summary>
    public string PreviousPage { get; init; } = "Previous page";
    /// <summary>Accessible name of the next page button of <see cref="KlyPagination"/>.</summary>
    public string NextPage { get; init; } = "Next page";
    /// <summary>Accessible name of the last page button of <see cref="KlyPagination"/>.</summary>
    public string LastPage { get; init; } = "Last page";
    /// <summary>Word read by assistive technology after the title of a finished step of <see cref="KlyStepper"/>.</summary>
    public string StepCompleted { get; init; } = "completed";
    /// <summary>Accessible name of the resize handle of <see cref="KlySplitter"/> and <see cref="KlyDashboardResizeHandle"/>.</summary>
    public string ResizePanels { get; init; } = "Resize panels";
}
