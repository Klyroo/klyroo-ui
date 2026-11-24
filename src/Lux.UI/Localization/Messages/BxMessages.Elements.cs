namespace Lux.UI;

public sealed partial record BxMessages
{
    /// <summary>Accessible name of a progress bar that has no <c>AriaLabel</c>.</summary>
    public string Progress { get; init; } = "Progress";

    /// <summary>Name of the "+N" avatar of an avatar group; <c>{0}</c> is the number of hidden avatars.</summary>
    public string MoreAvatars { get; init; } = "{0} more";

    /// <summary>Text of the button of <c>BxInfiniteScroll</c> that loads the next items by hand.</summary>
    public string LoadMore { get; init; } = "Load more";

    /// <summary>Announced politely while <c>BxInfiniteScroll</c> loads the next items.</summary>
    public string LoadingMore { get; init; } = "Loading more...";

    /// <summary>Announced politely when <c>BxInfiniteScroll</c> has no more items to load, and shown by <c>ShowEnd</c>.</summary>
    public string NoMoreItems { get; init; } = "No more items";
}
