using Microsoft.AspNetCore.Components;

namespace Blux.UI.Tools;

/// <summary>
/// Starts the first load of a list that has no schema yet. The table places it after its columns: they register while they render, so by the time it
/// initializes they can describe the schema. For a list with a schema it only waits for the load the list already started.
/// </summary>
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public sealed class DataListLoadTrigger<TItem> : ComponentBase
{
    [CascadingParameter] private DataListContext<TItem>? Context { get; set; } = default!;

    protected override Task OnInitializedAsync() => Context?.EnsureLoadedAsync() ?? Task.CompletedTask;

    protected override bool ShouldRender() => false;
}
