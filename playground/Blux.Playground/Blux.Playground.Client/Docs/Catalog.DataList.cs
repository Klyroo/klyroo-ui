using Blux.UI.Tools;

namespace Blux.Playground.Client.Docs;

public static partial class Catalog
{
    /// <summary>Components of the Blux.UI.Tools package: the Package of their bundle entry is Blux.UI.Tools.</summary>
    private static readonly ComponentDoc[] DataList =
    [
        new("data-list-empty", "DataListEmpty", "Data", "The empty state of a data list: a localized title and hint, an icon and room for actions.",
            [new("Default texts", typeof(Examples.DataListEmptyExample), "The title and the hint are localized in the 12 languages of Blux UI and follow the culture of the user."),
             new("With actions", typeof(Examples.DataListEmptyActionsExample), "Actions sit under the text; an empty Description or Icon hides that part.")],
            [typeof(BxDataListEmpty)]),
    ];
}
