using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>Per-instance class overrides of the slots of <see cref="KlyForm"/>, passed to its <c>Ui</c> parameter. Each property holds the classes merged into one slot.</summary>
public sealed record FormUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyForm"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>fieldset</c> slot of <see cref="KlyForm"/>, after the theme and the app configuration.</summary>
    public string? Fieldset { get; init; }
    /// <summary>Classes merged into the <c>summary</c> slot of <see cref="KlyForm"/>, after the theme and the app configuration.</summary>
    public string? Summary { get; init; }
    /// <summary>Classes merged into the <c>summaryList</c> slot of <see cref="KlyForm"/>, after the theme and the app configuration.</summary>
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
