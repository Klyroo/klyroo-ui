using Blux.UI.Theming;

namespace Blux.UI;

public sealed record FormUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Fieldset { get; init; }
    public string? Summary { get; init; }
    public string? SummaryList { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "fieldset" => Fieldset,
        "summary" => Summary,
        "summaryList" => SummaryList,
        _ => null,
    };
}
