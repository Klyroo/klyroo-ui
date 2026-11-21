namespace Blux.UI.Tools;

/// <summary>
/// Every text the components of Blux.UI.Tools render: accessible names, labels, hints. One set per locale of Blux.UI
/// (<see cref="BxToolsLocales"/>); build a variant with a <c>with</c> expression and register it with <see cref="BluxToolsOptions.Messages"/>.
/// </summary>
/// <remarks>
/// Partial: each family of components declares its messages in its own file. A message with placeholders ({0}, {1}) is formatted by the
/// component with <see cref="string.Format(string, object?[])"/>; keep the same placeholders when you translate it.
/// </remarks>
public sealed partial record BxToolsMessages;
