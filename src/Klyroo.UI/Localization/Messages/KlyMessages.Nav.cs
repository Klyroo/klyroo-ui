namespace Klyroo.UI;

public sealed partial record KlyMessages
{
    public string Breadcrumb { get; init; } = "Breadcrumb";
    public string Pagination { get; init; } = "Pagination";
    public string Page { get; init; } = "Page";
    public string FirstPage { get; init; } = "First page";
    public string PreviousPage { get; init; } = "Previous page";
    public string NextPage { get; init; } = "Next page";
    public string LastPage { get; init; } = "Last page";
    public string StepCompleted { get; init; } = "completed";
    public string ResizePanels { get; init; } = "Resize panels";
}
