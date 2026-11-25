using Klyroo.UI.Theming;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Klyroo.UI;

/// <summary>What a toolbar button or a slash-menu item does to the editor.</summary>
public enum EditorAction
{
    /// <summary>Bold text.</summary>
    Bold,
    /// <summary>Italic text.</summary>
    Italic,
    /// <summary>Struck-through text.</summary>
    Strike,
    /// <summary>Underlined text.</summary>
    Underline,
    /// <summary>Inline code.</summary>
    Code,
    /// <summary>A plain paragraph.</summary>
    Paragraph,

    /// <summary>A heading; the level comes from <c>Level</c> (1 to 6).</summary>
    Heading,
    /// <summary>A list with bullets.</summary>
    BulletList,
    /// <summary>A list with numbers.</summary>
    OrderedList,

    /// <summary>A list of items with a checkbox each.</summary>
    TaskList,
    /// <summary>A quotation block.</summary>
    Blockquote,
    /// <summary>A block of code.</summary>
    CodeBlock,
    /// <summary>A horizontal divider.</summary>
    HorizontalRule,

    /// <summary>Sets or removes the link of the selection; the toolbar asks for the address in a small field.</summary>
    Link,

    /// <summary>Inserts an image from an http or https address; the toolbar asks for the address in a small field.</summary>
    Image,
    /// <summary>Removes the formatting of the selection.</summary>
    ClearFormatting,
    /// <summary>Undoes the last change.</summary>
    Undo,
    /// <summary>Redoes the change that was undone.</summary>
    Redo,

    /// <summary>Moves the block holding the selection up. Alt and the up arrow does the same in the text.</summary>
    MoveUp,

    /// <summary>Moves the block holding the selection down. Alt and the down arrow does the same in the text.</summary>
    MoveDown,
}

/// <summary>Where the toolbar shows.</summary>
public enum EditorToolbarLayout
{
    /// <summary>Always visible, above the text.</summary>
    Fixed,

    /// <summary>Floats above the selection while some text is selected.</summary>
    Bubble,
}

/// <summary>A button of <see cref="KlyEditorToolbar"/>, or a separator between groups of buttons.</summary>
public sealed record EditorToolbarItem
{
    /// <summary>Creates a button for an action.</summary>
    public EditorToolbarItem(EditorAction action) => Action = action;

    /// <summary>What the button does.</summary>
    public EditorAction Action { get; }

    /// <summary>Heading level for <see cref="EditorAction.Heading"/>.</summary>
    public int Level { get; init; }

    /// <summary>Accessible name and tooltip. Defaults to the text of <see cref="KlyMessages"/> for the action.</summary>
    public string? Label { get; init; }

    /// <summary>True for <see cref="Separator"/>, which is a line between groups and not a button.</summary>
    public bool IsSeparator { get; private init; }

    /// <summary>A thin line between two groups of buttons.</summary>
    public static EditorToolbarItem Separator { get; } = new(EditorAction.Bold) { IsSeparator = true };

    /// <summary>Creates a button for a heading of the given level, 1 to 6.</summary>
    public static EditorToolbarItem Heading(int level) => new(EditorAction.Heading) { Level = level };

    /// <summary>Formatting buttons and lists, grouped: what a fixed toolbar shows by default.</summary>
    public static IReadOnlyList<EditorToolbarItem> Defaults { get; } =
    [
        new(EditorAction.Bold), new(EditorAction.Italic), new(EditorAction.Strike), new(EditorAction.Code),
        Separator,
        Heading(1), Heading(2), Heading(3),
        Separator,
        new(EditorAction.BulletList), new(EditorAction.OrderedList), new(EditorAction.Blockquote), new(EditorAction.CodeBlock),
        Separator,
        new(EditorAction.Link),
        Separator,
        new(EditorAction.Undo), new(EditorAction.Redo),
    ];

    /// <summary>Inline formatting: what a bubble toolbar shows by default.</summary>
    public static IReadOnlyList<EditorToolbarItem> BubbleDefaults { get; } =
    [
        new(EditorAction.Bold), new(EditorAction.Italic), new(EditorAction.Strike), new(EditorAction.Code), new(EditorAction.Link),
    ];
}

/// <summary>An entry of <see cref="KlyEditorSuggestionMenu"/>: a block or a format the user can insert.</summary>
public sealed record EditorSuggestionItem
{
    /// <summary>Creates an entry for an action.</summary>
    public EditorSuggestionItem(EditorAction action, string label)
    {
        Action = action;
        Label = label;
    }

    /// <summary>What the entry inserts or applies.</summary>
    public EditorAction Action { get; }
    /// <summary>Text of the entry.</summary>
    public string Label { get; }

    /// <summary>Heading level for <see cref="EditorAction.Heading"/>.</summary>
    public int Level { get; init; }
    /// <summary>Text shown under the label.</summary>
    public string? Description { get; init; }

    /// <summary>Extra words the typed text is matched against.</summary>
    public string? Keywords { get; init; }
    /// <summary>Disables the entry.</summary>
    public bool Disabled { get; init; }

    /// <summary>The default entries in English. The menu itself uses <see cref="For"/> with the messages of the locale in effect.</summary>
    public static IReadOnlyList<EditorSuggestionItem> Defaults { get; } = For(new KlyMessages());

    /// <summary>
    /// Text, headings, lists, quote, code block and divider, named and described by <paramref name="messages"/>: what the slash menu
    /// offers by default. The keywords are markup shorthands (h1, ul, hr) and stay the same in every language.
    /// </summary>
    public static IReadOnlyList<EditorSuggestionItem> For(KlyMessages messages) =>
    [
        new(EditorAction.Paragraph, messages.EditorSuggestionText) { Description = messages.EditorDescriptionParagraph, Keywords = "paragraph p" },
        new(EditorAction.Heading, EditorActions.Label(messages, EditorAction.Heading, 1)) { Level = 1, Description = messages.EditorDescriptionHeading1, Keywords = "h1 title" },
        new(EditorAction.Heading, EditorActions.Label(messages, EditorAction.Heading, 2)) { Level = 2, Description = messages.EditorDescriptionHeading2, Keywords = "h2 subtitle" },
        new(EditorAction.Heading, EditorActions.Label(messages, EditorAction.Heading, 3)) { Level = 3, Description = messages.EditorDescriptionHeading3, Keywords = "h3" },
        new(EditorAction.BulletList, messages.EditorBulletList) { Description = messages.EditorDescriptionBulletList, Keywords = "ul unordered" },
        new(EditorAction.OrderedList, messages.EditorOrderedList) { Description = messages.EditorDescriptionOrderedList, Keywords = "ol ordered" },
        new(EditorAction.Blockquote, messages.EditorBlockquote) { Description = messages.EditorDescriptionBlockquote, Keywords = "blockquote citation" },
        new(EditorAction.CodeBlock, messages.EditorCodeBlock) { Description = messages.EditorDescriptionCodeBlock, Keywords = "pre snippet" },
        new(EditorAction.HorizontalRule, messages.EditorHorizontalRule) { Description = messages.EditorDescriptionHorizontalRule, Keywords = "hr rule separator" },
    ];
}

/// <summary>An entry of <see cref="KlyEditorMentionMenu"/>: someone or something that can be mentioned.</summary>
public sealed record EditorMentionItem
{
    /// <summary>Creates an entry for something that can be mentioned.</summary>
    public EditorMentionItem(string id, string label)
    {
        Id = id;
        Label = label;
    }

    /// <summary>Stored in the mention as <c>data-id</c>.</summary>
    public string Id { get; }

    /// <summary>Shown in the menu and in the text after the @.</summary>
    public string Label { get; }
    /// <summary>Text shown under the label.</summary>
    public string? Description { get; init; }
    /// <summary>Disables the entry.</summary>
    public bool Disabled { get; init; }
}

/// <summary>An entry of <see cref="KlyEditorEmojiMenu"/>.</summary>
public sealed record EditorEmojiItem
{
    /// <summary>Creates an emoji entry.</summary>
    public EditorEmojiItem(string emoji, string name)
    {
        Emoji = emoji;
        Name = name;
    }

    /// <summary>The emoji inserted into the text.</summary>
    public string Emoji { get; }

    /// <summary>What the user types after the colon to find it: <c>:smile</c>.</summary>
    public string Name { get; }

    /// <summary>Extra words the typed text is matched against.</summary>
    public string? Keywords { get; init; }

    /// <summary>A few common emoji: what the emoji menu offers by default.</summary>
    public static IReadOnlyList<EditorEmojiItem> Defaults { get; } =
    [
        new("😀", "grinning") { Keywords = "smile happy" }, new("😄", "smile") { Keywords = "happy joy" }, new("😂", "joy") { Keywords = "laugh lol" },
        new("🙂", "slight_smile"), new("😉", "wink"), new("😍", "heart_eyes") { Keywords = "love" }, new("🤔", "thinking") { Keywords = "hmm" },
        new("😎", "sunglasses") { Keywords = "cool" }, new("😢", "cry") { Keywords = "sad tear" }, new("😡", "angry") { Keywords = "mad" },
        new("👍", "thumbsup") { Keywords = "+1 yes ok" }, new("👎", "thumbsdown") { Keywords = "-1 no" }, new("👏", "clap") { Keywords = "applause bravo" },
        new("🙏", "pray") { Keywords = "thanks please" }, new("👋", "wave") { Keywords = "hello bye" }, new("💪", "muscle") { Keywords = "strong" },
        new("❤️", "heart") { Keywords = "love" }, new("🔥", "fire") { Keywords = "hot lit" }, new("⭐", "star"), new("✨", "sparkles") { Keywords = "magic new" },
        new("🎉", "tada") { Keywords = "party celebrate" }, new("🚀", "rocket") { Keywords = "launch ship" }, new("💡", "bulb") { Keywords = "idea" },
        new("✅", "white_check_mark") { Keywords = "done check" }, new("❌", "x") { Keywords = "cross no" }, new("⚠️", "warning") { Keywords = "careful" },
        new("🐛", "bug"), new("📌", "pushpin") { Keywords = "pin" }, new("📎", "paperclip") { Keywords = "attach" }, new("☕", "coffee"),
    ];
}

/// <summary>Per-instance class overrides of <see cref="KlyEditor"/> slots.</summary>
public sealed record EditorUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyEditor"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }

    /// <summary>The area around the text.</summary>
    public string? Content { get; init; }

    /// <summary>The element the editable text is drawn in.</summary>
    public string? Base { get; init; }

    /// <summary>The HTML shown before the editor is ready.</summary>
    public string? Fallback { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "content" => Content,
        "base" => Base,
        "fallback" => Fallback,
        _ => null,
    };
}

/// <summary>Per-instance class overrides of <see cref="KlyEditorToolbar"/> slots.</summary>
public sealed record EditorToolbarUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyEditorToolbar"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>button</c> slot of <see cref="KlyEditorToolbar"/>, after the theme and the app configuration.</summary>
    public string? Button { get; init; }
    /// <summary>Classes merged into the <c>icon</c> slot of <see cref="KlyEditorToolbar"/>, after the theme and the app configuration.</summary>
    public string? Icon { get; init; }
    /// <summary>Classes merged into the <c>separator</c> slot of <see cref="KlyEditorToolbar"/>, after the theme and the app configuration.</summary>
    public string? Separator { get; init; }
    /// <summary>Classes merged into the <c>linkForm</c> slot of <see cref="KlyEditorToolbar"/>, after the theme and the app configuration.</summary>
    public string? LinkForm { get; init; }
    /// <summary>Classes merged into the <c>linkInput</c> slot of <see cref="KlyEditorToolbar"/>, after the theme and the app configuration.</summary>
    public string? LinkInput { get; init; }
    /// <summary>Classes merged into the <c>linkButton</c> slot of <see cref="KlyEditorToolbar"/>, after the theme and the app configuration.</summary>
    public string? LinkButton { get; init; }

    /// <summary>The explanation shown under the address field when the editor does not accept the address.</summary>
    public string? LinkMessage { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "button" => Button,
        "icon" => Icon,
        "separator" => Separator,
        "linkForm" => LinkForm,
        "linkInput" => LinkInput,
        "linkButton" => LinkButton,
        "linkMessage" => LinkMessage,
        _ => null,
    };
}

/// <summary>Per-instance class overrides of <see cref="KlyEditorDragHandle"/> slots.</summary>
public sealed record EditorDragHandleUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyEditorDragHandle"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>handle</c> slot of <see cref="KlyEditorDragHandle"/>, after the theme and the app configuration.</summary>
    public string? Handle { get; init; }
    /// <summary>Classes merged into the <c>icon</c> slot of <see cref="KlyEditorDragHandle"/>, after the theme and the app configuration.</summary>
    public string? Icon { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "handle" => Handle,
        "icon" => Icon,
        _ => null,
    };
}

/// <summary>Per-instance class overrides of the editor suggestion menus.</summary>
public sealed record EditorMenuUi : ISlotOverrides
{
    /// <summary>
    /// Classes merged into the <c>root</c> slot of <see cref="KlyEditorSuggestionMenu"/>, <see cref="KlyEditorMentionMenu"/> and <see cref="KlyEditorEmojiMenu"/>, after the theme and the app configuration.
    /// </summary>
    public string? Root { get; init; }
    /// <summary>
    /// Classes merged into the <c>content</c> slot of <see cref="KlyEditorSuggestionMenu"/>, <see cref="KlyEditorMentionMenu"/> and <see cref="KlyEditorEmojiMenu"/>, after the theme and the app configuration.
    /// </summary>
    public string? Content { get; init; }
    /// <summary>
    /// Classes merged into the <c>listbox</c> slot of <see cref="KlyEditorSuggestionMenu"/>, <see cref="KlyEditorMentionMenu"/> and <see cref="KlyEditorEmojiMenu"/>, after the theme and the app configuration.
    /// </summary>
    public string? Listbox { get; init; }
    /// <summary>
    /// Classes merged into the <c>item</c> slot of <see cref="KlyEditorSuggestionMenu"/>, <see cref="KlyEditorMentionMenu"/> and <see cref="KlyEditorEmojiMenu"/>, after the theme and the app configuration.
    /// </summary>
    public string? Item { get; init; }
    /// <summary>
    /// Classes merged into the <c>icon</c> slot of <see cref="KlyEditorSuggestionMenu"/>, <see cref="KlyEditorMentionMenu"/> and <see cref="KlyEditorEmojiMenu"/>, after the theme and the app configuration.
    /// </summary>
    public string? Icon { get; init; }
    /// <summary>
    /// Classes merged into the <c>body</c> slot of <see cref="KlyEditorSuggestionMenu"/>, <see cref="KlyEditorMentionMenu"/> and <see cref="KlyEditorEmojiMenu"/>, after the theme and the app configuration.
    /// </summary>
    public string? Body { get; init; }
    /// <summary>
    /// Classes merged into the <c>itemLabel</c> slot of <see cref="KlyEditorSuggestionMenu"/>, <see cref="KlyEditorMentionMenu"/> and <see cref="KlyEditorEmojiMenu"/>, after the theme and the app configuration.
    /// </summary>
    public string? ItemLabel { get; init; }
    /// <summary>
    /// Classes merged into the <c>itemDescription</c> slot of <see cref="KlyEditorSuggestionMenu"/>, <see cref="KlyEditorMentionMenu"/> and <see cref="KlyEditorEmojiMenu"/>, after the theme and the app configuration.
    /// </summary>
    public string? ItemDescription { get; init; }
    /// <summary>
    /// Classes merged into the <c>empty</c> slot of <see cref="KlyEditorSuggestionMenu"/>, <see cref="KlyEditorMentionMenu"/> and <see cref="KlyEditorEmojiMenu"/>, after the theme and the app configuration.
    /// </summary>
    public string? Empty { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "content" => Content,
        "listbox" => Listbox,
        "item" => Item,
        "icon" => Icon,
        "body" => Body,
        "itemLabel" => ItemLabel,
        "itemDescription" => ItemDescription,
        "empty" => Empty,
        _ => null,
    };
}

/// <summary>Browser-side names and icons of the editor actions.</summary>
internal static class EditorActions
{
    public static string Name(EditorAction action) => action switch
    {
        EditorAction.Bold => "bold",
        EditorAction.Italic => "italic",
        EditorAction.Strike => "strike",
        EditorAction.Underline => "underline",
        EditorAction.Code => "code",
        EditorAction.Paragraph => "paragraph",
        EditorAction.Heading => "heading",
        EditorAction.BulletList => "bulletList",
        EditorAction.OrderedList => "orderedList",
        EditorAction.TaskList => "taskList",
        EditorAction.Blockquote => "blockquote",
        EditorAction.CodeBlock => "codeBlock",
        EditorAction.HorizontalRule => "horizontalRule",
        EditorAction.Link => "link",
        EditorAction.Image => "image",
        EditorAction.ClearFormatting => "clearFormatting",
        EditorAction.Undo => "undo",
        EditorAction.Redo => "redo",
        EditorAction.MoveUp => "moveUp",
        EditorAction.MoveDown => "moveDown",
        _ => throw new ArgumentOutOfRangeException(nameof(action), action, null),
    };

    /// <summary>Buttons that stay pressed while the selection has the format.</summary>
    public static bool IsToggle(EditorAction action) => action is
        EditorAction.Bold or EditorAction.Italic or EditorAction.Strike or EditorAction.Underline or EditorAction.Code or EditorAction.Heading or
        EditorAction.BulletList or EditorAction.OrderedList or EditorAction.TaskList or EditorAction.Blockquote or EditorAction.CodeBlock or EditorAction.Link;

    public static string Label(KlyMessages strings, EditorAction action, int level) => action switch
    {
        EditorAction.Bold => strings.EditorBold,
        EditorAction.Italic => strings.EditorItalic,
        EditorAction.Strike => strings.EditorStrike,
        EditorAction.Underline => strings.EditorUnderline,
        EditorAction.Code => strings.EditorCode,
        EditorAction.Paragraph => strings.EditorParagraph,
        EditorAction.Heading => string.Format(strings.EditorHeading, level),
        EditorAction.BulletList => strings.EditorBulletList,
        EditorAction.OrderedList => strings.EditorOrderedList,
        EditorAction.TaskList => strings.EditorTaskList,
        EditorAction.Blockquote => strings.EditorBlockquote,
        EditorAction.CodeBlock => strings.EditorCodeBlock,
        EditorAction.HorizontalRule => strings.EditorHorizontalRule,
        EditorAction.Link => strings.EditorLink,
        EditorAction.Image => strings.EditorImage,
        EditorAction.ClearFormatting => strings.EditorClearFormatting,
        EditorAction.Undo => strings.EditorUndo,
        EditorAction.Redo => strings.EditorRedo,
        EditorAction.MoveUp => strings.EditorMoveUp,
        EditorAction.MoveDown => strings.EditorMoveDown,
        _ => throw new ArgumentOutOfRangeException(nameof(action), action, null),
    };

    /// <summary>The icon of an action: a Lucide name, or the app's arrow for the actions that move a block.</summary>
    public static string IconName(EditorAction action, int level, KlyrooIconOptions icons) => action switch
    {
        EditorAction.Bold => "lucide:bold",
        EditorAction.Italic => "lucide:italic",
        EditorAction.Strike => "lucide:strikethrough",
        EditorAction.Underline => "lucide:underline",
        EditorAction.Code => "lucide:code",
        EditorAction.Paragraph => "lucide:pilcrow",
        EditorAction.Heading => level switch { 1 => "lucide:heading-1", 2 => "lucide:heading-2", 3 => "lucide:heading-3", _ => "lucide:heading-4" },
        EditorAction.BulletList => "lucide:list",
        EditorAction.OrderedList => "lucide:list-ordered",
        EditorAction.TaskList => "lucide:list-todo",
        EditorAction.Blockquote => "lucide:quote",
        EditorAction.CodeBlock => "lucide:square-code",
        EditorAction.HorizontalRule => icons.Minus,
        EditorAction.Link => "lucide:link",
        EditorAction.Image => "lucide:image",
        EditorAction.ClearFormatting => "lucide:remove-formatting",
        EditorAction.Undo => "lucide:undo-2",
        EditorAction.Redo => "lucide:redo-2",
        EditorAction.MoveUp => icons.ArrowUp,
        EditorAction.MoveDown => icons.ArrowDown,
        _ => "",
    };
}
