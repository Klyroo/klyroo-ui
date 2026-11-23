namespace Editor.Shared.Data;

/// <summary>The document the home page offers to a visitor who has none: a short guide that is also a tour of what the editor can do.</summary>
public static class SampleDocument
{
    public const string Title = "Getting started";

    /// <summary>The HTML of the guide. It goes through <see cref="DocumentHtml.Clean"/> like everything else that is stored.</summary>
    public const string Html =
        "<h1>Getting started</h1>" +
        "<p>This is a document. Everything you type is saved in this browser a moment after you stop, and nowhere else.</p>" +
        "<h2>Format as you write</h2>" +
        "<p>Select some words to get a floating toolbar, or use the shortcuts: <strong>bold</strong> (Ctrl+B), <em>italic</em> (Ctrl+I), <u>underline</u> (Ctrl+U) and <s>strikethrough</s>. " +
        "Inline <code>code</code> and <a href=\"https://example.com\">links</a> work the same way.</p>" +
        "<h2>Commands</h2>" +
        "<p>Type a slash on an empty line to insert a heading, a list, a task list, a quote, a code block or a divider. Type a colon and a word for an emoji.</p>" +
        "<ul data-type=\"taskList\">" +
        "<li data-type=\"taskItem\" data-checked=\"true\"><label><input type=\"checkbox\" checked=\"checked\"><span></span></label><div><p>Open this guide</p></div></li>" +
        "<li data-type=\"taskItem\" data-checked=\"false\"><label><input type=\"checkbox\"><span></span></label><div><p>Try the slash menu on a new line</p></div></li>" +
        "<li data-type=\"taskItem\" data-checked=\"false\"><label><input type=\"checkbox\"><span></span></label><div><p>Rename or delete this document from the list</p></div></li>" +
        "</ul>" +
        "<blockquote><p>Alt and the arrow keys move the block under the cursor up or down.</p></blockquote>" +
        "<pre><code>var words = text.Split(' ').Length;</code></pre>";
}
