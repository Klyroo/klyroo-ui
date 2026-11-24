using Lux.UI.Theming;

namespace Lux.UI;

public sealed record LxCheckboxItem<TValue>(TValue Value, string Label)
{
    public string? Description { get; init; }
    public bool Disabled { get; init; }
}

public sealed record CheckboxGroupUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Legend { get; init; }
    public string? Fieldset { get; init; }
    public string? Item { get; init; }
    public string? Container { get; init; }
    public string? Base { get; init; }
    public string? Icon { get; init; }
    public string? Wrapper { get; init; }
    public string? Label { get; init; }
    public string? Description { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "legend" => Legend,
        "fieldset" => Fieldset,
        "item" => Item,
        "container" => Container,
        "base" => Base,
        "icon" => Icon,
        "wrapper" => Wrapper,
        "label" => Label,
        "description" => Description,
        _ => null,
    };
}
