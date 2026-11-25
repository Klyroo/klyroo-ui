using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>Per-instance class overrides of <see cref="KlyError"/> slots.</summary>
public sealed record ErrorUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? StatusCode { get; init; }
    public string? StatusMessage { get; init; }
    public string? Message { get; init; }
    public string? Actions { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "statusCode" => StatusCode,
        "statusMessage" => StatusMessage,
        "message" => Message,
        "actions" => Actions,
        _ => null,
    };
}
