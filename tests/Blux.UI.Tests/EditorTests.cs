using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;

namespace Blux.UI.Tests;

public class EditorTests : BluxTestContext
{
    // ---- Editor

    [Fact]
    public void Editor_renders_the_host_the_posted_value_and_the_html_shown_before_the_engine_is_ready()
    {
        var cut = Render<BxEditor>(p => p.Add(e => e.Value, "<p>Hello <strong>you</strong></p>").Add(e => e.Name, "body")
            .Add(e => e.Placeholder, "Write"));

        var host = cut.Find("bx-editor");
        Assert.Equal("<p>Hello <strong>you</strong></p>", host.GetAttribute("data-value"));
        Assert.Equal("Write", host.GetAttribute("data-placeholder"));
        Assert.Equal("Rich text editor", host.GetAttribute("data-label"));
        var input = cut.Find("input[type=hidden]");
        Assert.Equal("body", input.GetAttribute("name"));
        Assert.Equal("<p>Hello <strong>you</strong></p>", input.GetAttribute("value"));
        // No sanitizer configured: the read-only view shows the text, not the markup.
        Assert.Equal("Hello you", cut.Find("[data-slot=fallback]").TextContent.Trim());
        Assert.Empty(cut.FindAll("[data-slot=fallback] strong"));
        var surface = cut.Find("[data-slot=editor]");
        Assert.True(surface.HasAttribute("data-permanent"));
        Assert.Empty(surface.Children);
        Assert.Empty(surface.TextContent);
    }

    [Fact]
    public void Editor_renders_state_attributes_for_read_only_disabled_and_color()
    {
        var cut = Render<BxEditor>(p => p.Add(e => e.ReadOnly, true).Add(e => e.Disabled, true).Add(e => e.Color, BxColor.Success)
            .Add(e => e.Label, "Biography").Add(e => e.Name, "bio"));

        var host = cut.Find("bx-editor");
        Assert.True(host.HasAttribute("data-readonly"));
        Assert.True(host.HasAttribute("disabled"));
        Assert.True(cut.Find("input[type=hidden]").HasAttribute("disabled"));
        Assert.Equal("success", host.GetAttribute("data-color"));
        Assert.Equal("Biography", host.GetAttribute("data-label"));
    }

    [Fact]
    public void Editor_unbound_has_no_sequence_and_bound_one_hears_the_browser()
    {
        var unbound = Render<BxEditor>(p => p.Add(e => e.Value, "<p>a</p>"));
        Assert.False(unbound.Find("bx-editor").HasAttribute("data-bx-seq"));

        string? value = null;
        var cut = Render<BxEditor>(p => p.Add(e => e.Value, "<p>a</p>").Add(e => e.ValueChanged, (string? v) => value = v));
        Assert.Equal("data-bx-seq", cut.Find("bx-editor").Attributes[0]!.Name);
        Assert.Equal("0", cut.Find("bx-editor").GetAttribute("data-bx-seq"));

        cut.Find("bx-editor").TriggerEvent("onbxchange", new BxChangeEventArgs { Value = "<p>b</p>", Sequence = 4 });

        Assert.Equal("<p>b</p>", value);
        Assert.Equal("4", cut.Find("bx-editor").GetAttribute("data-bx-seq"));
        Assert.Equal("<p>b</p>", cut.Find("bx-editor").GetAttribute("data-value"));
        Assert.Equal("<p>b</p>", cut.Find("input[type=hidden]").GetAttribute("value"));
        Assert.Equal("data-bx-seq", cut.Find("bx-editor").Attributes[0]!.Name);
    }

    [Fact]
    public void Editor_ignores_a_report_that_changes_nothing_but_still_remembers_its_sequence()
    {
        var calls = 0;
        var cut = Render<BxEditor>(p => p.Add(e => e.Value, "<p>a</p>").Add(e => e.ValueChanged, (string? _) => calls++));

        cut.Find("bx-editor").TriggerEvent("onbxchange", new BxChangeEventArgs { Value = "<p>a</p>", Sequence = 2 });

        Assert.Equal(0, calls);
        Assert.Equal("2", cut.Find("bx-editor").GetAttribute("data-bx-seq"));
    }

    [Fact]
    public void Editor_in_an_EditForm_posts_under_the_field_name_and_reports_invalid()
    {
        var model = new Note();
        var cut = RenderForm(model, b =>
        {
            b.OpenComponent<BxEditor>(1);
            b.AddAttribute(2, "Value", model.Body);
            b.AddAttribute(3, "ValueChanged", EventCallback.Factory.Create<string?>(this, v => model.Body = v));
            b.AddAttribute(4, "ValueExpression", (Expression<Func<string?>>)(() => model.Body));
            b.CloseComponent();
        });

        Assert.Equal("model.Body", cut.Find("input[type=hidden]").GetAttribute("name"));
        Assert.False(cut.Find("bx-editor").HasAttribute("data-invalid"));

        cut.Find("form").Submit();

        Assert.Equal("true", cut.Find("bx-editor").GetAttribute("data-invalid"));

        cut.Find("bx-editor").TriggerEvent("onbxchange", new BxChangeEventArgs { Value = "<p>x</p>", Sequence = 1 });

        Assert.Equal("<p>x</p>", model.Body);
        Assert.False(cut.Find("bx-editor").HasAttribute("data-invalid"));
    }

    [Fact]
    public void FormField_labels_and_describes_the_editor_and_marks_it_invalid()
    {
        var cut = Render<BxFormField>(p => p.Add(f => f.Label, "Body").Add(f => f.Help, "Markup").Add(f => f.Error, "Required").Add(f => f.Required, true)
            .Add(f => f.ChildContent, (RenderFragment)(b =>
            {
                b.OpenComponent<BxEditor>(0);
                b.CloseComponent();
            })));

        var host = cut.Find("bx-editor");
        Assert.Equal(cut.Find("label").Id, host.GetAttribute("data-labelledby"));
        Assert.Equal(cut.Find("label").GetAttribute("for"), host.GetAttribute("data-editor-id"));
        Assert.Equal("true", host.GetAttribute("data-invalid"));
        var described = host.GetAttribute("data-describedby")!.Split(' ');
        Assert.Contains(cut.Find("[data-slot=error]").Id, described);
        Assert.Equal("true", host.GetAttribute("data-required"));
    }

    [Fact]
    public void Editor_forwards_class_ui_and_additional_attributes()
    {
        var cut = Render<BxEditor>(p => p.Add(e => e.Class, "my-root").Add(e => e.Ui, new EditorUi { Content = "my-content", Fallback = "my-fallback" })
            .AddUnmatched("data-testid", "ed"));

        Assert.Contains("my-root", cut.Find("bx-editor").ClassList);
        Assert.Equal("ed", cut.Find("bx-editor").GetAttribute("data-testid"));
        Assert.Contains("my-content", cut.Find("[data-slot=content]").ClassList);
        Assert.Contains("my-fallback", cut.Find("[data-slot=fallback]").ClassList);
    }

    [Fact]
    public void Editor_places_its_children_before_the_text_area()
    {
        var cut = Render<BxEditor>(p => p.Add(e => e.ChildContent, (RenderFragment)(b =>
        {
            b.OpenComponent<BxEditorToolbar>(0);
            b.CloseComponent();
        })));

        var children = cut.Find("bx-editor").Children.Select(c => c.LocalName).ToList();
        Assert.Equal(["input", "bx-editor-toolbar", "div"], children);
    }

    // ---- Toolbar

    [Fact]
    public void Toolbar_renders_a_roving_toolbar_with_one_tab_stop_and_toggle_buttons()
    {
        var cut = Render<BxEditorToolbar>();

        var toolbar = cut.Find("bx-editor-toolbar");
        Assert.Equal("toolbar", toolbar.GetAttribute("role"));
        Assert.Equal("Formatting", toolbar.GetAttribute("aria-label"));
        Assert.Equal("fixed", toolbar.GetAttribute("data-layout"));
        Assert.False(toolbar.HasAttribute("popover"));
        var buttons = cut.FindAll("button[data-action]");
        Assert.Equal(EditorToolbarItem.Defaults.Count(i => !i.IsSeparator), buttons.Count);
        Assert.Single(buttons, b => b.GetAttribute("tabindex") == "0");
        Assert.All(buttons, b => Assert.True(b.HasAttribute("disabled")));
        Assert.All(buttons, b => Assert.False(string.IsNullOrEmpty(b.GetAttribute("aria-label"))));
        var bold = buttons.First(b => b.GetAttribute("data-action") == "bold");
        Assert.Equal("false", bold.GetAttribute("aria-pressed"));
        Assert.Equal("Bold", bold.GetAttribute("aria-label"));
        Assert.True(bold.HasAttribute("data-toggle"));
        var undo = buttons.First(b => b.GetAttribute("data-action") == "undo");
        Assert.False(undo.HasAttribute("aria-pressed"));
        Assert.False(undo.HasAttribute("data-toggle"));
        Assert.Equal("Heading 2", buttons.First(b => b.GetAttribute("data-level") == "2").GetAttribute("aria-label"));
        Assert.Equal(4, cut.FindAll("[role=separator]").Count);
    }

    [Fact]
    public void Toolbar_bubble_layout_is_a_manual_popover_with_inline_defaults()
    {
        var cut = Render<BxEditorToolbar>(p => p.Add(t => t.Layout, EditorToolbarLayout.Bubble));

        var toolbar = cut.Find("bx-editor-toolbar");
        Assert.Equal("manual", toolbar.GetAttribute("popover"));
        Assert.Equal("bubble", toolbar.GetAttribute("data-layout"));
        Assert.Equal(["bold", "italic", "strike", "code", "link"], cut.FindAll("button[data-action]").Select(b => b.GetAttribute("data-action")));
    }

    [Fact]
    public void Toolbar_custom_items_labels_and_the_link_form_only_with_a_link_button()
    {
        var without = Render<BxEditorToolbar>(p => p.Add(t => t.Items, [new EditorToolbarItem(EditorAction.Bold) { Label = "Strong" }, EditorToolbarItem.Separator, new EditorToolbarItem(EditorAction.MoveUp)]));
        Assert.Equal("Strong", without.Find("button[data-action=bold]").GetAttribute("aria-label"));
        Assert.Equal("Move block up", without.Find("button[data-action=moveUp]").GetAttribute("aria-label"));
        Assert.Empty(without.FindAll("[data-slot=link-form]"));

        var with = Render<BxEditorToolbar>();
        var form = with.Find("[data-slot=link-form]");
        Assert.True(form.HasAttribute("hidden"));
        Assert.Equal("Link address", with.Find("[data-slot=link-input]").GetAttribute("aria-label"));
        Assert.Equal("text", with.Find("[data-slot=link-input]").GetAttribute("type"));
        Assert.All(form.QuerySelectorAll("button"), b => Assert.Equal("button", b.GetAttribute("type")));
    }

    [Fact]
    public void Toolbar_uses_size_color_and_label()
    {
        var cut = Render<BxEditorToolbar>(p => p.Add(t => t.Size, BxSize.Lg).Add(t => t.Color, BxColor.Info).Add(t => t.Label, "Format"));

        Assert.Equal("Format", cut.Find("bx-editor-toolbar").GetAttribute("aria-label"));
        Assert.Equal("info", cut.Find("bx-editor-toolbar").GetAttribute("data-color"));
        Assert.Contains("size-9", cut.Find("button[data-action=bold]").ClassList);
    }

    // ---- Drag handle

    [Fact]
    public void DragHandle_renders_a_hidden_labeled_button_that_is_not_a_tab_stop()
    {
        var cut = Render<BxEditorDragHandle>();

        var host = cut.Find("bx-editor-drag-handle");
        Assert.True(host.HasAttribute("hidden"));
        var button = cut.Find("button");
        Assert.Equal("Drag to move block", button.GetAttribute("aria-label"));
        Assert.Equal("-1", button.GetAttribute("tabindex"));
        Assert.Equal("button", button.GetAttribute("type"));
        Assert.Equal("true", cut.Find("svg").GetAttribute("aria-hidden"));
    }

    // ---- Menus

    [Fact]
    public void SuggestionMenu_renders_a_listbox_of_options_with_actions_and_a_hidden_empty_message()
    {
        var cut = Render<BxEditorSuggestionMenu>();

        var menu = cut.Find("bx-editor-menu");
        Assert.Equal("/", menu.GetAttribute("data-char"));
        Assert.Equal("suggestion", menu.GetAttribute("data-kind"));
        Assert.Equal("manual", cut.Find("[data-slot=content]").GetAttribute("popover"));
        var list = cut.Find("[role=listbox]");
        Assert.Equal("Commands", list.GetAttribute("aria-label"));
        Assert.False(string.IsNullOrEmpty(list.Id));
        var options = cut.FindAll("[role=option]");
        Assert.Equal(EditorSuggestionItem.Defaults.Count, options.Count);
        Assert.All(options, o => Assert.Equal("false", o.GetAttribute("aria-selected")));
        var h2 = options.First(o => o.GetAttribute("data-label") == "Heading 2");
        Assert.Equal("heading", h2.GetAttribute("data-action"));
        Assert.Equal("2", h2.GetAttribute("data-level"));
        Assert.Contains("Medium section heading", h2.TextContent);
        Assert.True(cut.Find("[data-slot=empty]").HasAttribute("hidden"));
        Assert.Equal("No results", cut.Find("[data-slot=empty]").TextContent);
    }

    [Fact]
    public void SuggestionMenu_takes_custom_items_character_limit_and_disabled_state()
    {
        var cut = Render<BxEditorSuggestionMenu>(p => p
            .Add(m => m.Items, [new EditorSuggestionItem(EditorAction.CodeBlock, "Snippet") { Keywords = "pre", Disabled = true }])
            .Add(m => m.Char, "!").Add(m => m.Limit, 3).Add(m => m.Label, "Blocks"));

        var menu = cut.Find("bx-editor-menu");
        Assert.Equal("!", menu.GetAttribute("data-char"));
        Assert.Equal("3", menu.GetAttribute("data-limit"));
        Assert.Equal("Blocks", cut.Find("[role=listbox]").GetAttribute("aria-label"));
        var option = cut.Find("[role=option]");
        Assert.Equal("codeBlock", option.GetAttribute("data-action"));
        Assert.Equal("pre", option.GetAttribute("data-keywords"));
        Assert.Equal("true", option.GetAttribute("aria-disabled"));
    }

    [Fact]
    public void MentionMenu_renders_items_with_their_id_and_the_at_character()
    {
        var cut = Render<BxEditorMentionMenu>(p => p.Add(m => m.Items, [new EditorMentionItem("ada", "Ada Lovelace") { Description = "Mathematician" }, new EditorMentionItem("alan", "Alan Turing")]));

        var menu = cut.Find("bx-editor-menu");
        Assert.Equal("@", menu.GetAttribute("data-char"));
        Assert.Equal("mention", menu.GetAttribute("data-kind"));
        Assert.Equal("Mentions", cut.Find("[role=listbox]").GetAttribute("aria-label"));
        var options = cut.FindAll("[role=option]");
        Assert.Equal(["ada", "alan"], options.Select(o => o.GetAttribute("data-value")));
        Assert.Equal(["Ada Lovelace", "Alan Turing"], options.Select(o => o.GetAttribute("data-label")));
        Assert.Contains("Mathematician", options[0].TextContent);
        Assert.Null(options[0].GetAttribute("data-action"));
    }

    [Fact]
    public void EmojiMenu_renders_emoji_values_and_needs_one_character_after_the_colon()
    {
        var cut = Render<BxEditorEmojiMenu>();

        var menu = cut.Find("bx-editor-menu");
        Assert.Equal(":", menu.GetAttribute("data-char"));
        Assert.Equal("emoji", menu.GetAttribute("data-kind"));
        Assert.Equal("1", menu.GetAttribute("data-min-query"));
        var smile = cut.FindAll("[role=option]").First(o => o.GetAttribute("data-label") == "smile");
        Assert.Equal("😄", smile.GetAttribute("data-value"));
        Assert.Equal("Emoji", cut.Find("[role=listbox]").GetAttribute("aria-label"));
    }

    [Fact]
    public void Menu_options_are_not_selected_and_use_unique_list_ids()
    {
        var first = Render<BxEditorEmojiMenu>();
        var second = Render<BxEditorEmojiMenu>();

        Assert.NotEqual(first.Find("[role=listbox]").Id, second.Find("[role=listbox]").Id);
    }

    private IRenderedComponent<EditForm> RenderForm(object model, Action<RenderTreeBuilder> field) =>
        Render<EditForm>(p => p
            .Add(f => f.Model, model)
            .Add(f => f.ChildContent, (EditContext _) => (RenderFragment)(b =>
            {
                b.OpenComponent<DataAnnotationsValidator>(0);
                b.CloseComponent();
                field(b);
            })));

    private sealed class Note
    {
        [Required]
        public string? Body { get; set; }
    }
}

public class EditorLocalizedTests : BluxTestContext
{
    public EditorLocalizedTests() : base(o =>
    {
        o.Strings.EditorBold = "Gras";
        o.Strings.EditorToolbar = "Mise en forme";
        o.Strings.EditorNoResults = "Aucun resultat";
        o.Strings.EditorDragHandle = "Deplacer";
    })
    {
    }

    [Fact]
    public void Texts_come_from_the_configured_strings()
    {
        var toolbar = Render<BxEditorToolbar>();
        Assert.Equal("Gras", toolbar.Find("button[data-action=bold]").GetAttribute("aria-label"));
        Assert.Equal("Mise en forme", toolbar.Find("bx-editor-toolbar").GetAttribute("aria-label"));

        Assert.Equal("Aucun resultat", Render<BxEditorEmojiMenu>().Find("[data-slot=empty]").TextContent);
        Assert.Equal("Deplacer", Render<BxEditorDragHandle>().Find("button").GetAttribute("aria-label"));
    }

    [Fact]
    public void Read_only_view_never_renders_posted_markup_without_a_sanitizer()
    {
        var cut = Render<BxEditor>(p => p.Add(e => e.Value, "<p>Hello <img src=x onerror=alert(1)></p><script>alert(2)</script><p>World</p>"));

        var fallback = cut.Find("[data-slot=fallback]");
        Assert.Empty(fallback.QuerySelectorAll("img, script"));
        Assert.Contains("Hello", fallback.TextContent);
        Assert.Contains("World", fallback.TextContent);
        Assert.DoesNotContain("alert(2)", fallback.TextContent);
    }

}

public class EditorSanitizedTests() : BluxTestContext(o => o.SanitizeEditorHtml = html => html.Replace("<script>", "").Replace("</script>", ""))
{
    [Fact]
    public void Read_only_view_renders_markup_through_the_configured_sanitizer()
    {
        var cut = Render<BxEditor>(p => p.Add(e => e.Value, "<p>Hello <strong>you</strong></p>"));

        Assert.Equal("you", cut.Find("[data-slot=fallback] strong").TextContent);
    }


    [Fact]
    public void SuggestionMenu_listbox_id_stays_the_same_across_renders()
    {
        var cut = Render<BxEditorSuggestionMenu>();
        var id = cut.Find("[role=listbox]").Id;

        cut.Render();
        cut.Render(p => p.Add(m => m.Limit, 10));

        Assert.Equal(id, cut.Find("[role=listbox]").Id);
    }
}
