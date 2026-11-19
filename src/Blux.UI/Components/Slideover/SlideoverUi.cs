using Blux.UI.Theming;

namespace Blux.UI;

public enum SlideoverSide
{
    Right,
    Left,
    Top,
    Bottom,
}

/// <summary>Per-instance class overrides of <see cref="BxSlideover"/> slots.</summary>
public sealed record SlideoverUi : ISlotOverrides
{
    public string? Content { get; init; }
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
