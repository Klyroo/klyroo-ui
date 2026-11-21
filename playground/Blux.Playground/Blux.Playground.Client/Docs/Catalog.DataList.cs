using Blux.UI.Tools;

namespace Blux.Playground.Client.Docs;

public static partial class Catalog
{
    /// <summary>Components of the Blux.UI.Tools package: the Package of their bundle entry is Blux.UI.Tools.</summary>
    private static readonly ComponentDoc[] DataList =
    [
        new("data-list", "DataList", "Data", "A table of rows loaded in memory, from a provider or from a database query (a Queryable, run asynchronously by the Blux.UI.Tools.EntityFrameworkCore adapter), with sortable columns, pagination, a result count, loading and error states, and its state in the URL.",
            [new("In-memory list with a schema", typeof(Examples.DataListExample), "The schema says which fields sort. Click a header to sort, use the pagination or the page size, then Back: the state is in the URL (mem.s, mem.p, mem.ps). Statically rendered, the headers and the pages are links and the page size is a GET form."),
             new("Provider", typeof(Examples.DataListProviderExample), "A provider loads the rows after a delay: placeholders on the first load, then the previous rows stay on screen while the next page loads. A newer request cancels the one it replaces."),
             new("State in the URL", typeof(Examples.DataListUrlExample), "No UrlKey, so the keys are s, p and ps. The footer counts the provider calls of this render: a prerendered list hands its result to the interactive render, which loads nothing a second time. The time zone is the one of the browser, null in static rendering."),
             new("Error and retry", typeof(Examples.DataListErrorExample), "The provider fails while the address says ?broken=1. The error state offers a retry and keeps the rows that were on screen.")],
            [typeof(BxDataList<object>), typeof(BxDataListTable<object>), typeof(BxDataListColumn<object>), typeof(BxDataListPagination<object>), typeof(BxDataListResultCount<object>),
             typeof(BxDataListRefresh<object>), typeof(BxDataListErrorState<object>)]),
        new("data-list-empty", "DataListEmpty", "Data", "The empty state of a data list: a localized title and hint, an icon and room for actions.",
            [new("Default texts", typeof(Examples.DataListEmptyExample), "The title and the hint are localized in the 12 languages of Blux UI and follow the culture of the user."),
             new("With actions", typeof(Examples.DataListEmptyActionsExample), "Actions sit under the text; an empty Description or Icon hides that part.")],
            [typeof(BxDataListEmpty)]),
    ];
}
