using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>Per-instance class overrides of <see cref="KlyError"/> slots.</summary>
public sealed record ErrorUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyError"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>statusCode</c> slot of <see cref="KlyError"/>, after the theme and the app configuration.</summary>
    public string? StatusCode { get; init; }
    /// <summary>Classes merged into the <c>statusMessage</c> slot of <see cref="KlyError"/>, after the theme and the app configuration.</summary>
    public string? StatusMessage { get; init; }
    /// <summary>Classes merged into the <c>message</c> slot of <see cref="KlyError"/>, after the theme and the app configuration.</summary>
    public string? Message { get; init; }
    /// <summary>Classes merged into the <c>actions</c> slot of <see cref="KlyError"/>, after the theme and the app configuration.</summary>
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
