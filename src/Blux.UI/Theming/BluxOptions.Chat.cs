using Blux.UI.Theming;

namespace Blux.UI;

public sealed partial class BluxOptions
{
    public ComponentTheme ChatMessage { get; } = Themes.ChatMessage();
    public ComponentTheme ChatMessages { get; } = Themes.ChatMessages();
    public ComponentTheme ChatPrompt { get; } = Themes.ChatPrompt();
    public ComponentTheme ChatPromptSubmit { get; } = Themes.ChatPromptSubmit();
    public ComponentTheme ChatPalette { get; } = Themes.ChatPalette();
    public ComponentTheme ChatAttachment { get; } = Themes.ChatAttachment();
    public ComponentTheme ChatReasoning { get; } = Themes.ChatReasoning();
    public ComponentTheme ChatShimmer { get; } = Themes.ChatShimmer();
    public ComponentTheme ChatTool { get; } = Themes.ChatTool();
}
