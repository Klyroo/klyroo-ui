namespace Lux.UI;

/// <summary>
/// What <see cref="BxOptionsProvider"/> is asked for: one page of options for a search term. Both kinds of paging are
/// carried: a provider that pages by position uses <see cref="Skip"/> and <see cref="Take"/>, one that pages with an opaque
/// token uses <see cref="Cursor"/>. The component fills all of them, whichever the provider reads.
/// </summary>
/// <param name="Search">The text typed in the filter field, trimmed. Empty when nothing is typed: list the options unfiltered.</param>
/// <param name="Skip">How many options the earlier pages of this search returned (0 for the first page).</param>
/// <param name="Take">The number of options wanted (<c>PageSize</c> of the component).</param>
/// <param name="Cursor">The <see cref="BxOptionsResult.NextCursor"/> of the previous page; null for the first page and for a provider that does not use cursors.</param>
public sealed record BxOptionsRequest(string Search, int Skip, int Take, string? Cursor);

/// <summary>One page of options returned by a <see cref="BxOptionsProvider"/>.</summary>
/// <param name="Items">The options of this page, in the order to list them. A value that an earlier page of the same search already listed is skipped.</param>
/// <param name="HasMore">Whether another page exists. A non-null <see cref="NextCursor"/> also means there is one.</param>
/// <param name="NextCursor">The cursor that asks for the page after this one; null when the provider pages by <c>Skip</c> and <c>Take</c> or this is the last page.</param>
public sealed record BxOptionsResult(IReadOnlyList<BxSelectItem> Items, bool HasMore = false, string? NextCursor = null);

/// <summary>
/// Loads the options of a <see cref="BxSelectMenu"/> or <see cref="BxInputMenu"/> from a remote source, one page at a time.
/// The token is cancelled when a newer search replaces the request (the component also ignores a late answer that the provider
/// did not stop). An exception shows the translated error with a retry button; it is not logged by the component.
/// </summary>
public delegate ValueTask<BxOptionsResult> BxOptionsProvider(BxOptionsRequest request, CancellationToken cancellationToken);

/// <summary>
/// Returns the options of values the component has not listed (the value was restored from the address or a saved
/// form, and its page was never loaded), so the field can show their labels. Values it cannot find may be left out:
/// the component then shows their raw value.
/// </summary>
public delegate ValueTask<IReadOnlyList<BxSelectItem>> BxSelectedOptionsResolver(IReadOnlyList<string> values, CancellationToken cancellationToken);
