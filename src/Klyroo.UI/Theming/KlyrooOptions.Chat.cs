using Klyroo.UI.Theming;

namespace Klyroo.UI;

public sealed partial class KlyrooOptions
{
    /// <summary>
    /// Theme of <see cref="KlyChatMessage"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme ChatMessage { get; } = Themes.ChatMessage();
    /// <summary>
    /// Theme of <see cref="KlyChatMessages"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme ChatMessages { get; } = Themes.ChatMessages();
    /// <summary>
    /// Theme of <see cref="KlyChatPrompt"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme ChatPrompt { get; } = Themes.ChatPrompt();
    /// <summary>
    /// Theme of <see cref="KlyChatPromptSubmit"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme ChatPromptSubmit { get; } = Themes.ChatPromptSubmit();
    /// <summary>
    /// Theme of <see cref="KlyChatPalette"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme ChatPalette { get; } = Themes.ChatPalette();
    /// <summary>
    /// Theme of <see cref="KlyChatAttachment"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme ChatAttachment { get; } = Themes.ChatAttachment();
    /// <summary>
    /// Theme of <see cref="KlyChatReasoning"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme ChatReasoning { get; } = Themes.ChatReasoning();
    /// <summary>
    /// Theme of <see cref="KlyChatShimmer"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme ChatShimmer { get; } = Themes.ChatShimmer();
    /// <summary>
    /// Theme of <see cref="KlyChatTool"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme ChatTool { get; } = Themes.ChatTool();
}
