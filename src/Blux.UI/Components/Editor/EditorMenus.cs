using Blux.UI.Theming;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Blux.UI;

/// <summary>An option of a suggestion menu, as rendered.</summary>
internal sealed record EditorMenuOption(string Value, string Label, string? Description = null, string? Keywords = null, string? Action = null, int Level = 0, bool Disabled = false, string? IconPath = null, string? Prefix = null);

/// <summary>
/// Base of the menus that open when a trigger character is typed in the editor. The options are rendered as a listbox;
/// the browser filters them as the user types and takes the keys (up, down, Enter, Tab, Escape) while the menu is open,
/// so typing does not make a round trip to .NET.
/// </summary>
public abstract class BxEditorMenuBase : BxComponentBase
{
    private SlotSet _slots = default!;
    private string _listId = default!;

    /// <summary>Character that opens the menu.</summary>
    [Parameter] public string? Char { get; set; }

    /// <summary>Most options shown at a time.</summary>
    [Parameter] public int Limit { get; set; } = 42;

    /// <summary>Accessible name of the list.</summary>
    [Parameter] public string? Label { get; set; }
    [Parameter] public BxSize Size { get; set; } = BxSize.Md;
    [Parameter] public EditorMenuUi? Ui { get; set; }

    private protected abstract string DefaultChar { get; }
    private protected abstract string Kind { get; }
    private protected abstract string DefaultLabel(BxMessages strings);
    private protected abstract IEnumerable<EditorMenuOption> BuildOptions();

    /// <summary>Characters typed after the trigger before the menu opens.</summary>
    private protected virtual int MinQuery => 0;

    protected override void OnParametersSet()
    {
        _listId ??= NewId("editor-list");
        _slots = ResolveSlots(Options.EditorMenu, new Dictionary<string, string?> { ["size"] = VariantValue.Of(Size) }, Ui, "root");
    }

    protected override void BuildRenderTree(RenderTreeBuilder b)
    {
        b.OpenElement(0, "bx-editor-menu");
        b.AddAttribute(1, "class", _slots["root"]);
        b.AddAttribute(2, "data-char", Char ?? DefaultChar);
        b.AddAttribute(3, "data-kind", Kind);
        b.AddAttribute(4, "data-limit", Limit.ToString(System.Globalization.CultureInfo.InvariantCulture));
        b.AddAttribute(5, "data-min-query", MinQuery.ToString(System.Globalization.CultureInfo.InvariantCulture));
        b.AddMultipleAttributes(6, AdditionalAttributes);

        b.OpenElement(7, "div");
        b.AddAttribute(8, "popover", "manual");
        b.AddAttribute(9, "class", _slots["content"]);
        b.AddAttribute(10, "data-slot", "content");

        b.OpenElement(11, "div");
        b.AddAttribute(12, "role", "listbox");
        b.AddAttribute(13, "id", _listId);
        b.AddAttribute(14, "aria-label", Label ?? DefaultLabel(Messages));
        b.AddAttribute(15, "class", _slots["listbox"]);
        foreach (var option in BuildOptions())
        {
            b.OpenElement(16, "div");
            b.AddAttribute(17, "role", "option");
            b.AddAttribute(18, "class", _slots["item"]);
            b.AddAttribute(19, "data-value", option.Value);
            b.AddAttribute(20, "data-label", option.Label);
            b.AddAttribute(21, "data-description", option.Description);
            b.AddAttribute(22, "data-keywords", option.Keywords);
            b.AddAttribute(23, "data-action", option.Action);
            b.AddAttribute(24, "data-level", option.Level > 0 ? option.Level.ToString(System.Globalization.CultureInfo.InvariantCulture) : null);
            b.AddAttribute(25, "aria-selected", "false");
            b.AddAttribute(26, "aria-disabled", option.Disabled ? "true" : null);
            if (option.IconPath is not null)
            {
                b.AddContent(27, EditorActions.Icon(option.IconPath, _slots["icon"]));
            }
            else if (option.Prefix is not null)
            {
                b.OpenElement(28, "span");
                b.AddAttribute(29, "class", _slots["icon"] + " text-center");
                b.AddAttribute(30, "aria-hidden", "true");
                b.AddContent(31, option.Prefix);
                b.CloseElement();
            }
            b.OpenElement(32, "span");
            b.AddAttribute(33, "class", _slots["body"]);
            b.OpenElement(34, "span");
            b.AddAttribute(35, "class", _slots["itemLabel"]);
            b.AddContent(36, option.Label);
            b.CloseElement();
            if (option.Description is not null)
            {
                b.OpenElement(37, "span");
                b.AddAttribute(38, "class", _slots["itemDescription"]);
                b.AddContent(39, option.Description);
                b.CloseElement();
            }
            b.CloseElement();
            b.CloseElement();
        }
        b.CloseElement();

        b.OpenElement(40, "p");
        b.AddAttribute(41, "class", _slots["empty"]);
        b.AddAttribute(42, "data-slot", "empty");
        b.AddAttribute(43, "hidden", true);
        b.AddContent(44, Messages.EditorNoResults);
        b.CloseElement();

        b.CloseElement();
        b.CloseElement();
    }
}

/// <summary>
/// The slash menu: opens when "/" is typed (after a space or at the start of a block) and inserts a block or a format.
/// Items run in the browser: pass them as a parameter, no round trip is made while the user types.
/// </summary>
public sealed class BxEditorSuggestionMenu : BxEditorMenuBase
{
    /// <summary>Entries of the menu. Defaults to <see cref="EditorSuggestionItem.For"/> of the messages of the locale in effect.</summary>
    [Parameter] public IReadOnlyList<EditorSuggestionItem>? Items { get; set; }

    private protected override string DefaultChar => "/";
    private protected override string Kind => "suggestion";
    private protected override string DefaultLabel(BxMessages strings) => strings.EditorSuggestionMenu;

    private protected override IEnumerable<EditorMenuOption> BuildOptions() =>
        (Items ?? EditorSuggestionItem.For(Messages)).Select(i => new EditorMenuOption(
            EditorActions.Name(i.Action), i.Label, i.Description, i.Keywords, EditorActions.Name(i.Action), i.Level, i.Disabled,
            IconPath: EditorActions.IconPath(i.Action, i.Level)));
}

/// <summary>
/// The mention menu: opens when "@" is typed and inserts a mention, a span with <c>data-type="mention"</c>, <c>data-id</c>
/// and <c>data-label</c>. Items are supplied from .NET and filtered in the browser.
/// </summary>
public sealed class BxEditorMentionMenu : BxEditorMenuBase
{
    /// <summary>Who or what can be mentioned.</summary>
    [Parameter, EditorRequired] public IReadOnlyList<EditorMentionItem> Items { get; set; } = [];

    private protected override string DefaultChar => "@";
    private protected override string Kind => "mention";
    private protected override string DefaultLabel(BxMessages strings) => strings.EditorMentionMenu;

    private protected override IEnumerable<EditorMenuOption> BuildOptions() =>
        Items.Select(i => new EditorMenuOption(i.Id, i.Label, i.Description, Disabled: i.Disabled, Prefix: "@"));
}

/// <summary>
/// The emoji menu: opens when ":" is typed followed by at least one character, and inserts the emoji as text.
/// </summary>
public sealed class BxEditorEmojiMenu : BxEditorMenuBase
{
    /// <summary>Emoji of the menu. Defaults to <see cref="EditorEmojiItem.Defaults"/>.</summary>
    [Parameter] public IReadOnlyList<EditorEmojiItem>? Items { get; set; }

    private protected override string DefaultChar => ":";
    private protected override string Kind => "emoji";
    private protected override int MinQuery => 1;
    private protected override string DefaultLabel(BxMessages strings) => strings.EditorEmojiMenu;

    private protected override IEnumerable<EditorMenuOption> BuildOptions() =>
        (Items ?? EditorEmojiItem.Defaults).Select(i => new EditorMenuOption(i.Emoji, i.Name, null, i.Keywords, Prefix: i.Emoji));
}
