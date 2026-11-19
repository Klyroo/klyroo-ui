using Blux.UI.Theming;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Blux.UI;

/// <summary>What a toolbar button or a slash-menu item does to the editor.</summary>
public enum EditorAction
{
    Bold,
    Italic,
    Strike,
    Underline,
    Code,
    Paragraph,

    /// <summary>A heading; the level comes from <c>Level</c> (1 to 6).</summary>
    Heading,
    BulletList,
    OrderedList,
    Blockquote,
    CodeBlock,
    HorizontalRule,

    /// <summary>Sets or removes the link of the selection; the toolbar asks for the address in a small field.</summary>
    Link,
    ClearFormatting,
    Undo,
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

/// <summary>A button of <see cref="BxEditorToolbar"/>, or a separator between groups of buttons.</summary>
public sealed record EditorToolbarItem
{
    public EditorToolbarItem(EditorAction action) => Action = action;

    public EditorAction Action { get; }

    /// <summary>Heading level for <see cref="EditorAction.Heading"/>.</summary>
    public int Level { get; init; }

    /// <summary>Accessible name and tooltip. Defaults to the text of <see cref="BluxStrings"/> for the action.</summary>
    public string? Label { get; init; }

    public bool IsSeparator { get; private init; }

    /// <summary>A thin line between two groups of buttons.</summary>
    public static EditorToolbarItem Separator { get; } = new(EditorAction.Bold) { IsSeparator = true };

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

/// <summary>An entry of <see cref="BxEditorSuggestionMenu"/>: a block or a format the user can insert.</summary>
public sealed record EditorSuggestionItem
{
    public EditorSuggestionItem(EditorAction action, string label)
    {
        Action = action;
        Label = label;
    }

    public EditorAction Action { get; }
    public string Label { get; }

    /// <summary>Heading level for <see cref="EditorAction.Heading"/>.</summary>
    public int Level { get; init; }
    public string? Description { get; init; }

    /// <summary>Extra words the typed text is matched against.</summary>
    public string? Keywords { get; init; }
    public bool Disabled { get; init; }

    /// <summary>Text, headings, lists, quote, code block and divider: what the slash menu offers by default.</summary>
    public static IReadOnlyList<EditorSuggestionItem> Defaults { get; } =
    [
        new(EditorAction.Paragraph, "Text") { Description = "Plain paragraph", Keywords = "paragraph p" },
        new(EditorAction.Heading, "Heading 1") { Level = 1, Description = "Big section heading", Keywords = "h1 title" },
        new(EditorAction.Heading, "Heading 2") { Level = 2, Description = "Medium section heading", Keywords = "h2 subtitle" },
        new(EditorAction.Heading, "Heading 3") { Level = 3, Description = "Small section heading", Keywords = "h3" },
        new(EditorAction.BulletList, "Bulleted list") { Description = "A simple list of points", Keywords = "ul unordered" },
        new(EditorAction.OrderedList, "Numbered list") { Description = "A list with numbers", Keywords = "ol ordered" },
        new(EditorAction.Blockquote, "Quote") { Description = "Quote a passage", Keywords = "blockquote citation" },
        new(EditorAction.CodeBlock, "Code block") { Description = "Monospaced code", Keywords = "pre snippet" },
        new(EditorAction.HorizontalRule, "Divider") { Description = "A horizontal line", Keywords = "hr rule separator" },
    ];
}

/// <summary>An entry of <see cref="BxEditorMentionMenu"/>: someone or something that can be mentioned.</summary>
public sealed record EditorMentionItem
{
    public EditorMentionItem(string id, string label)
    {
        Id = id;
        Label = label;
    }

    /// <summary>Stored in the mention as <c>data-id</c>.</summary>
    public string Id { get; }

    /// <summary>Shown in the menu and in the text after the @.</summary>
    public string Label { get; }
    public string? Description { get; init; }
    public bool Disabled { get; init; }
}

/// <summary>An entry of <see cref="BxEditorEmojiMenu"/>.</summary>
public sealed record EditorEmojiItem
{
    public EditorEmojiItem(string emoji, string name)
    {
        Emoji = emoji;
        Name = name;
    }

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

/// <summary>Per-instance class overrides of <see cref="BxEditor"/> slots.</summary>
public sealed record EditorUi : ISlotOverrides
{
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

/// <summary>Per-instance class overrides of <see cref="BxEditorToolbar"/> slots.</summary>
public sealed record EditorToolbarUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Button { get; init; }
    public string? Icon { get; init; }
    public string? Separator { get; init; }
    public string? LinkForm { get; init; }
    public string? LinkInput { get; init; }
    public string? LinkButton { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "button" => Button,
        "icon" => Icon,
        "separator" => Separator,
        "linkForm" => LinkForm,
        "linkInput" => LinkInput,
        "linkButton" => LinkButton,
        _ => null,
    };
}

/// <summary>Per-instance class overrides of <see cref="BxEditorDragHandle"/> slots.</summary>
public sealed record EditorDragHandleUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Handle { get; init; }
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
    public string? Root { get; init; }
    public string? Content { get; init; }
    public string? Listbox { get; init; }
    public string? Item { get; init; }
    public string? Icon { get; init; }
    public string? Body { get; init; }
    public string? ItemLabel { get; init; }
    public string? ItemDescription { get; init; }
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
        EditorAction.Blockquote => "blockquote",
        EditorAction.CodeBlock => "codeBlock",
        EditorAction.HorizontalRule => "horizontalRule",
        EditorAction.Link => "link",
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
        EditorAction.BulletList or EditorAction.OrderedList or EditorAction.Blockquote or EditorAction.CodeBlock or EditorAction.Link;

    public static string Label(BluxStrings strings, EditorAction action, int level) => action switch
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
        EditorAction.Blockquote => strings.EditorBlockquote,
        EditorAction.CodeBlock => strings.EditorCodeBlock,
        EditorAction.HorizontalRule => strings.EditorHorizontalRule,
        EditorAction.Link => strings.EditorLink,
        EditorAction.ClearFormatting => strings.EditorClearFormatting,
        EditorAction.Undo => strings.EditorUndo,
        EditorAction.Redo => strings.EditorRedo,
        EditorAction.MoveUp => strings.EditorMoveUp,
        EditorAction.MoveDown => strings.EditorMoveDown,
        _ => throw new ArgumentOutOfRangeException(nameof(action), action, null),
    };

    /// <summary>SVG path data (24x24, stroked) of the icon of an action.</summary>
    public static string IconPath(EditorAction action, int level) => action switch
    {
        EditorAction.Bold => "M6 12h9a4 4 0 0 1 0 8H7a1 1 0 0 1-1-1V5a1 1 0 0 1 1-1h7a4 4 0 0 1 0 8",
        EditorAction.Italic => "M19 4h-9M14 20H5M15 4 9 20",
        EditorAction.Strike => "M16 4H9a3 3 0 0 0-2.83 4M14 12a4 4 0 0 1 0 8H6M4 12h16",
        EditorAction.Underline => "M6 4v6a6 6 0 0 0 12 0V4M4 20h16",
        EditorAction.Code => "m16 18 6-6-6-6M8 6l-6 6 6 6",
        EditorAction.Paragraph => "M13 4v16M17 4v16M19 4H9.5a4.5 4.5 0 0 0 0 9H13",
        EditorAction.Heading => level switch
        {
            1 => "M4 12h8M4 18V6M12 18V6m5 6 3-2v8",
            2 => "M4 12h8M4 18V6M12 18V6M21 18h-4c0-4 4-3 4-6 0-1.5-2-2.5-4-1",
            3 => "M4 12h8M4 18V6M12 18V6M17.5 10.5c1.7-1 3.5 0 3.5 1.5a2 2 0 0 1-2 2M17 17.5c2 1.5 4 .3 4-1.5a2 2 0 0 0-2-2",
            _ => "M4 12h8M4 18V6M12 18V6M17 10v4h4M21 10v8",
        },
        EditorAction.BulletList => "M3 12h.01M3 18h.01M3 6h.01M8 12h13M8 18h13M8 6h13",
        EditorAction.OrderedList => "M11 5h10M11 12h10M11 19h10M4 4h1v5M4 9h2M6.5 20H3.4c0-1 2.6-1.925 2.6-3.5a1.5 1.5 0 0 0-2.6-1.02",
        EditorAction.Blockquote => "M3 21c3 0 7-1 7-8V5c0-1.25-.756-2.017-2-2H4c-1.25 0-2 .75-2 1.972V11c0 1.25.75 2 2 2 1 0 1 0 1 1v1c0 1-1 2-2 2s-1 .008-1 1.031V20c0 1 0 1 1 1zM15 21c3 0 7-1 7-8V5c0-1.25-.757-2.017-2-2h-4c-1.25 0-2 .75-2 1.972V11c0 1.25.75 2 2 2h.75c0 2.25.25 4-2.75 4v3c0 1 0 1 1 1z",
        EditorAction.CodeBlock => "m10 9-3 3 3 3M14 15l3-3-3-3M5 3h14a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2z",
        EditorAction.HorizontalRule => "M5 12h14",
        EditorAction.Link => "M10 13a5 5 0 0 0 7.54.54l3-3a5 5 0 0 0-7.07-7.07l-1.72 1.71M14 11a5 5 0 0 0-7.54-.54l-3 3a5 5 0 0 0 7.07 7.07l1.71-1.71",
        EditorAction.ClearFormatting => "M4 7V4h16v3M5 20h6M13 4 8 20M15 15l5 5M15 20l5-5",
        EditorAction.Undo => "M9 14 4 9l5-5M4 9h10.5a5.5 5.5 0 0 1 5.5 5.5a5.5 5.5 0 0 1-5.5 5.5H11",
        EditorAction.Redo => "m15 14 5-5-5-5M20 9H9.5A5.5 5.5 0 0 0 4 14.5A5.5 5.5 0 0 0 9.5 20H13",
        EditorAction.MoveUp => "m5 12 7-7 7 7M12 19V5",
        EditorAction.MoveDown => "M12 5v14m7-7-7 7-7-7",
        _ => "",
    };

    public static RenderFragment Icon(string path, string? @class) => builder =>
    {
        builder.OpenElement(0, "svg");
        builder.AddAttribute(1, "viewBox", "0 0 24 24");
        builder.AddAttribute(2, "fill", "none");
        builder.AddAttribute(3, "stroke", "currentColor");
        builder.AddAttribute(4, "stroke-width", "2");
        builder.AddAttribute(5, "stroke-linecap", "round");
        builder.AddAttribute(6, "stroke-linejoin", "round");
        builder.AddAttribute(7, "aria-hidden", "true");
        builder.AddAttribute(8, "class", @class);
        builder.OpenElement(9, "path");
        builder.AddAttribute(10, "d", path);
        builder.CloseElement();
        builder.CloseElement();
    };

    public const string GripPath = "M9 5h.01M9 12h.01M9 19h.01M15 5h.01M15 12h.01M15 19h.01";
}
