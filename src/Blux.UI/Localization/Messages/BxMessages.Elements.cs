namespace Blux.UI;

public sealed partial record BxMessages
{
    /// <summary>Accessible name of a progress bar that has no <c>AriaLabel</c>.</summary>
    public string Progress { get; init; } = "Progress";

    /// <summary>Name of the "+N" avatar of an avatar group; <c>{0}</c> is the number of hidden avatars.</summary>
    public string MoreAvatars { get; init; } = "{0} more";
}
