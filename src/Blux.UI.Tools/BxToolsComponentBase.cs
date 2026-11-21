using Blux.UI.Theming;

namespace Blux.UI.Tools;

/// <summary>
/// Base of the components of Blux.UI.Tools: the theme resolution of <see cref="BxComponentBase"/>, plus the options and the texts of this package.
/// </summary>
public abstract class BxToolsComponentBase : BxComponentBase
{
    /// <summary>The options of Blux.UI.Tools: its themes and message overrides.</summary>
    protected BluxToolsOptions ToolsOptions => Options.Extension<BluxToolsOptions>();

    /// <summary>The texts of the locale of this component (the same locale as <c>Messages</c> of Blux.UI), with the changes of <see cref="BluxToolsOptions.Messages"/> applied.</summary>
    protected BxToolsMessages ToolsMessages => ToolsOptions.ResolveMessages(CurrentLocale.Code);
}
