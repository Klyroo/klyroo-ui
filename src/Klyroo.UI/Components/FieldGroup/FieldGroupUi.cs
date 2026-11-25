using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>Per-instance class overrides of <see cref="KlyFieldGroup"/> slots.</summary>
public sealed record FieldGroupUi : ISlotOverrides
{
    public string? Base { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "base" => Base,
        _ => null,
    };
}
