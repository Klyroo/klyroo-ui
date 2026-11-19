using Blux.UI;

namespace Blux.Playground.Client.Docs;

public static partial class Catalog
{
    private static readonly ComponentDoc[] Editor =
    [
        new("editor", "Editor", "Editor", "A rich text editor built on TipTap. The value is an HTML string; it loads from a separate file only on pages that have an editor, and static pages show the HTML read-only until it is ready.",
            [new("Bound with menus", typeof(Examples.EditorExample), "The value reaches .NET on blur and after 400 ms without typing, not on every key. The text below shows the bound HTML."),
             new("In an EditForm", typeof(Examples.EditorFormExample), "Posts the HTML under the field name, with validation and aria-invalid, also in static rendering. The posted HTML is user input: sanitize it on the server."),
             new("Floating toolbar and read only", typeof(Examples.EditorBubbleExample), "Unbound: the editor keeps its own state in the browser and posts through a hidden input.")],
            [typeof(BxEditor)]),
        new("editor-toolbar", "EditorToolbar", "Editor", "Formatting buttons for the editor, fixed above the text or floating over a selection, with pressed states that follow the cursor.",
            [new("Custom buttons", typeof(Examples.EditorToolbarExample), "A WAI-ARIA toolbar: one tab stop, arrow keys, Home and End move between buttons; Escape returns to the text."),
             new("Floating", typeof(Examples.EditorBubbleExample))],
            [typeof(BxEditorToolbar), typeof(EditorToolbarItem), typeof(EditorAction), typeof(EditorToolbarLayout)]),
        new("editor-drag-handle", "EditorDragHandle", "Editor", "A handle beside the block under the pointer that drags it to a new place. Alt and the arrow keys move a block from the keyboard.",
            [new("Drag a block", typeof(Examples.EditorDragHandleExample), "The handle is hidden below the sm breakpoint; the keyboard shortcuts and the MoveUp and MoveDown toolbar buttons still work.")],
            [typeof(BxEditorDragHandle)]),
        new("editor-suggestion-menu", "EditorSuggestionMenu", "Editor", "A slash menu that inserts blocks and formats. Items come from .NET; filtering and keys run in the browser.",
            [new("Custom commands", typeof(Examples.EditorSuggestionMenuExample), "Up and down move, Enter or Tab insert, Escape closes. The text field announces the list as a combobox while it is open.")],
            [typeof(BxEditorSuggestionMenu), typeof(EditorSuggestionItem)]),
        new("editor-mention-menu", "EditorMentionMenu", "Editor", "An @ menu that inserts a mention from a list of items.",
            [new("Mentions", typeof(Examples.EditorMentionMenuExample), "The mention is a span with data-type, data-id and data-label in the HTML.")],
            [typeof(BxEditorMentionMenu), typeof(EditorMentionItem)]),
        new("editor-emoji-menu", "EditorEmojiMenu", "Editor", "A colon menu that inserts an emoji.",
            [new("Emoji", typeof(Examples.EditorEmojiMenuExample), "The menu opens after the colon and one more character, so times like 12:30 are left alone.")],
            [typeof(BxEditorEmojiMenu), typeof(EditorEmojiItem)]),
    ];
}
