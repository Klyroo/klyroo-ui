using Lux.UI.Theming;

namespace Lux.UI;

public enum DrawerDirection
{
    Bottom,
    Top,
    Left,
    Right,
}

/// <summary>Per-instance class overrides of <see cref="BxDrawer"/> slots.</summary>
public sealed record DrawerUi : ISlotOverrides
{
    public string? Content { get; init; }
    public string? Handle { get; init; }
    public string? Header { get; init; }
    public string? Wrapper { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? Close { get; init; }
    public string? Body { get; init; }
    public string? Footer { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "content" => Content,
        "handle" => Handle,
        "header" => Header,
        "wrapper" => Wrapper,
        "title" => Title,
        "description" => Description,
        "close" => Close,
        "body" => Body,
        "footer" => Footer,
        _ => null,
    };
}
