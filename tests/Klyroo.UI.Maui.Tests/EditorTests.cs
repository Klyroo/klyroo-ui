using System.ComponentModel;

namespace Klyroo.UI.Maui.Tests;

public class EditorTests
{
    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class InlineDispatcherProvider : IDispatcherProvider
    {
        private readonly InlineDispatcher _dispatcher = new();

        public IDispatcher? GetForCurrentThread() => _dispatcher;
    }

    private sealed class InlineDispatcher : IDispatcher
    {
        public bool IsDispatchRequired => false;

        public bool Dispatch(Action action)
        {
            action();
            return true;
        }

        public bool DispatchDelayed(TimeSpan delay, Action action) => Dispatch(action);

        public IDispatcherTimer CreateTimer() => throw new NotSupportedException();
    }

    private sealed class Model : INotifyPropertyChanged
    {
        private string? _markdown;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string? Markdown
        {
            get => _markdown;
            set
            {
                _markdown = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Markdown)));
            }
        }
    }

    private static (string Text, int Start, int Length) Do(EditorAction action, string text, int start = 0, int length = 0, int level = 0, string? argument = null)
    {
        var edit = EditorMarkdown.Apply(action, level, argument, text, start, length) ?? throw new InvalidOperationException("The action made no edit.");
        return (edit.Text, edit.Start, edit.Length);
    }

    /// <summary>Types into the native editor the way the platform does: the text, then the cursor and the selection.</summary>
    private static void Type(KlyEditor editor, string text, int start, int length = 0)
    {
        var native = editor.NativeEditor;
        native.Text = text;
        native.CursorPosition = start;
        native.SelectionLength = length;
    }

    private static KlyEditor EditorWith(string text, int start, int length = 0, params View[] children)
    {
        var editor = new KlyEditor();
        foreach (var child in children) editor.Children.Add(child);
        Type(editor, text, start, length);
        return editor;
    }

    private static EditorToolbarButton ButtonOf(KlyEditorToolbar toolbar, EditorAction action, int level = 0) =>
        toolbar.Buttons.Single(b => b.Item.Action == action && b.Item.Level == level);

    // ---- Inline marks

    [Fact]
    public void Bold_wraps_the_selection_and_keeps_it_selected()
    {
        Assert.Equal(("hello **world**", 8, 5), Do(EditorAction.Bold, "hello world", 6, 5));
    }

    [Fact]
    public void Bold_with_no_selection_opens_a_pair_around_the_cursor_and_a_second_press_takes_it_back()
    {
        var pair = Do(EditorAction.Bold, "ab", 1);
        Assert.Equal(("a****b", 3, 0), pair);
        Assert.Equal(("ab", 1, 0), Do(EditorAction.Bold, pair.Text, pair.Start));
    }

    [Fact]
    public void Bold_takes_the_marks_off_the_word_under_the_cursor()
    {
        Assert.Equal(("a bold b", 4, 0), Do(EditorAction.Bold, "a **bold** b", 6));
    }

    [Fact]
    public void Italic_on_bold_text_makes_it_both_and_each_mark_comes_off_alone()
    {
        var both = Do(EditorAction.Italic, "**bold**", 2, 4);
        Assert.Equal(("***bold***", 3, 4), both);

        var formats = EditorMarkdown.Inspect(both.Text, both.Start, both.Length);
        Assert.True(formats.Bold);
        Assert.True(formats.Italic);

        Assert.Equal(("*bold*", 1, 4), Do(EditorAction.Bold, both.Text, both.Start, both.Length));
        Assert.Equal(("**bold**", 2, 4), Do(EditorAction.Italic, both.Text, both.Start, both.Length));
    }

    [Theory]
    [InlineData(EditorAction.Strike, "~~", "~~")]
    [InlineData(EditorAction.Code, "`", "`")]
    [InlineData(EditorAction.Underline, "<u>", "</u>")]
    public void Strike_code_and_underline_wrap_the_selection_and_toggle_off(EditorAction action, string open, string close)
    {
        var wrapped = Do(action, "one two", 4, 3);
        Assert.Equal(($"one {open}two{close}", 4 + open.Length, 3), wrapped);
        Assert.Equal(("one two", 4, 3), Do(action, wrapped.Text, wrapped.Start, wrapped.Length));
    }

    [Fact]
    public void An_asterisk_that_is_a_list_marker_or_stands_between_spaces_is_not_italic()
    {
        Assert.False(EditorMarkdown.Inspect("* item", 3, 0).Italic);
        Assert.False(EditorMarkdown.Inspect("2 * 3 * 4", 4, 0).Italic);
        Assert.True(EditorMarkdown.Inspect("an *odd* word", 6, 0).Italic);
    }

    // ---- Block formats

    [Fact]
    public void Heading_prefixes_the_line_toggles_off_and_changes_level()
    {
        var heading = Do(EditorAction.Heading, "Title", 0, 0, level: 2);
        Assert.Equal(("## Title", 3, 0), heading);
        Assert.Equal(("Title", 0, 0), Do(EditorAction.Heading, heading.Text, heading.Start, 0, level: 2));
        Assert.Equal(("# Title", 2, 0), Do(EditorAction.Heading, heading.Text, heading.Start, 0, level: 1));
    }

    [Fact]
    public void Heading_reads_the_level_and_ignores_a_hash_without_a_space()
    {
        Assert.Equal(3, EditorMarkdown.Inspect("### x", 4, 0).Heading);
        Assert.Equal(0, EditorMarkdown.Inspect("#tag", 2, 0).Heading);
        Assert.Equal(0, EditorMarkdown.Inspect("# a\nplain", 0, 9).Heading);
    }

    [Fact]
    public void Bullet_list_marks_every_selected_line_and_leaves_the_selection_on_the_text()
    {
        var list = Do(EditorAction.BulletList, "a\nb\nc", 0, 5);
        Assert.Equal(("- a\n- b\n- c", 2, 9), list);
        Assert.Equal(("a\nb\nc", 0, 5), Do(EditorAction.BulletList, list.Text, list.Start, list.Length));
    }

    [Fact]
    public void Ordered_list_numbers_the_lines_and_a_task_list_replaces_a_bullet()
    {
        Assert.Equal(("1. a\n2. b", 3, 6), Do(EditorAction.OrderedList, "a\nb", 0, 3));
        Assert.Equal(("- [ ] a", 6, 0), Do(EditorAction.TaskList, "- a", 2));
        Assert.True(EditorMarkdown.Inspect("- [x] done", 8, 0).TaskList);
        Assert.False(EditorMarkdown.Inspect("- [x] done", 8, 0).BulletList);
    }

    [Fact]
    public void List_keeps_the_indent_of_a_nested_item()
    {
        Assert.Equal(("  - nested", 4, 0), Do(EditorAction.BulletList, "  nested", 2));
    }

    [Fact]
    public void Blockquote_prefixes_lines_keeps_the_quote_through_empty_ones_and_wraps_a_list()
    {
        var quote = Do(EditorAction.Blockquote, "a\n\nb", 0, 4);
        Assert.Equal("> a\n>\n> b", quote.Text);
        Assert.Equal("a\n\nb", Do(EditorAction.Blockquote, quote.Text, quote.Start, quote.Length).Text);

        var quoted = Do(EditorAction.Blockquote, "- item", 3);
        Assert.Equal("> - item", quoted.Text);
        var formats = EditorMarkdown.Inspect(quoted.Text, quoted.Start, 0);
        Assert.True(formats.Blockquote);
        Assert.True(formats.BulletList);
    }

    [Fact]
    public void Paragraph_removes_the_heading_marker()
    {
        Assert.Equal(("Title", 0, 0), Do(EditorAction.Paragraph, "## Title", 3));
    }

    [Fact]
    public void Code_block_fences_the_lines_and_a_second_press_removes_the_fences()
    {
        var block = Do(EditorAction.CodeBlock, "code", 2);
        Assert.Equal(("```\ncode\n```", 6, 0), block);
        Assert.True(EditorMarkdown.Inspect(block.Text, block.Start, 0).CodeBlock);
        Assert.Equal(("code", 2, 0), Do(EditorAction.CodeBlock, block.Text, block.Start));
    }

    [Fact]
    public void Code_block_empty_fences_go_in_one_piece()
    {
        Assert.Equal(("a\nb", 2, 0), Do(EditorAction.CodeBlock, "a\n```\n```\nb", 6));
    }

    [Fact]
    public void Horizontal_rule_goes_after_a_blank_line_so_it_is_not_a_heading()
    {
        Assert.Equal(("para\n\n---\n", 10, 0), Do(EditorAction.HorizontalRule, "para", 4));
        Assert.Equal(("a\n\n---\n", 7, 0), Do(EditorAction.HorizontalRule, "a\n", 2));
        Assert.Equal(("a\n\n---\n\nb", 7, 0), Do(EditorAction.HorizontalRule, "a\n\nb", 2));
    }

    // ---- Links, images, clear

    [Fact]
    public void Link_wraps_the_selection_updates_the_address_and_removes_the_link()
    {
        var link = Do(EditorAction.Link, "see docs", 4, 4, argument: "https://a.b");
        Assert.Equal(("see [docs](https://a.b)", 5, 4), link);
        Assert.Equal("https://a.b", EditorMarkdown.LinkAddress(link.Text, 6, 0));

        var updated = Do(EditorAction.Link, link.Text, 6, 0, argument: "https://c.d");
        Assert.Equal("see [docs](https://c.d)", updated.Text);

        Assert.Equal(("see docs", 4, 4), Do(EditorAction.Link, link.Text, 6, 0, argument: ""));
    }

    [Fact]
    public void Link_with_no_selection_uses_the_address_as_text_and_escapes_parentheses()
    {
        Assert.Equal(("[https://a.b](https://a.b)", 1, 11), Do(EditorAction.Link, "", 0, 0, argument: "https://a.b"));
        Assert.Equal("x [t](https://a.b/%28x%29)", Do(EditorAction.Link, "x t", 2, 1, argument: "https://a.b/(x)").Text);
        Assert.Equal("https://a.b/(x)", EditorMarkdown.LinkAddress("x [t](https://a.b/%28x%29)", 4, 0));
    }

    [Fact]
    public void Link_does_nothing_without_a_link_to_remove_or_across_lines()
    {
        Assert.Null(EditorMarkdown.Apply(EditorAction.Link, 0, "", "plain", 0, 0));
        Assert.Null(EditorMarkdown.Apply(EditorAction.Link, 0, "https://a.b", "a\nb", 0, 3));
    }

    [Fact]
    public void Image_takes_the_alt_text_from_the_selection_or_the_file_name()
    {
        Assert.Equal(("![team photo](https://x.y/team-photo.jpg)", 41, 0), Do(EditorAction.Image, "", 0, 0, argument: "https://x.y/team-photo.jpg"));
        Assert.Equal("![Our team](https://x.y/a.png)", Do(EditorAction.Image, "Our team", 0, 8, argument: "https://x.y/a.png").Text);
    }

    [Fact]
    public void Clear_formatting_removes_the_marks_of_the_selection_and_the_markers_of_its_lines()
    {
        Assert.Equal(("bold and code", 0, 13), Do(EditorAction.ClearFormatting, "## **bold** and `code`", 0, 22));
        Assert.Equal(("see docs", 0, 8), Do(EditorAction.ClearFormatting, "see [docs](https://a.b)", 0, 23));
        Assert.Equal(("quoted", 0, 0), Do(EditorAction.ClearFormatting, "> - quoted", 4));
    }

    // ---- Blocks

    [Fact]
    public void Move_swaps_the_block_with_its_neighbour_and_the_cursor_goes_with_it()
    {
        const string text = "one\n\ntwo\n\nthree";
        Assert.Equal(("two\n\none\n\nthree", 1, 0), Do(EditorAction.MoveUp, text, 6));
        Assert.Equal(("one\n\nthree\n\ntwo", 13, 0), Do(EditorAction.MoveDown, text, 6));
        Assert.Null(EditorMarkdown.Apply(EditorAction.MoveUp, 0, null, text, 1, 0));
        Assert.Null(EditorMarkdown.Apply(EditorAction.MoveDown, 0, null, text, 12, 0));
    }

    [Fact]
    public void Move_treats_a_list_and_a_fenced_block_with_blank_lines_as_one_block()
    {
        var moved = Do(EditorAction.MoveDown, "- a\n- b\n\n```\nx\n\ny\n```", 1);
        Assert.Equal("```\nx\n\ny\n```\n\n- a\n- b", moved.Text);
        var formats = EditorMarkdown.Inspect("a\n\nb", 1, 0);
        Assert.True(formats.CanMoveDown);
        Assert.False(formats.CanMoveUp);
        Assert.False(EditorMarkdown.Inspect("a\n\nb", 2, 0).CanMoveUp);
    }

    // ---- Triggers

    [Theory]
    [InlineData("hi @ad", 6, "@", 3, 3, "ad")]
    [InlineData("a /", 3, "/", 2, 1, "")]
    [InlineData("@ab cd", 3, "@", 0, 3, "ab")]
    [InlineData("x\n/h1", 5, "/", 2, 3, "h1")]
    public void A_trigger_is_found_when_the_word_before_the_cursor_starts_with_it(string text, int cursor, string trigger, int start, int length, string query) =>
        Assert.Equal(new EditorTrigger(start, length, query), EditorMarkdown.FindTrigger(text, cursor, trigger));

    [Theory]
    [InlineData("mail@x", 6, "@")]
    [InlineData("http://x", 8, "/")]
    [InlineData("@a@b", 4, "@")]
    [InlineData("@a b", 4, "@")]
    [InlineData("", 0, "@")]
    public void No_trigger_inside_a_word_after_a_space_or_with_the_trigger_again(string text, int cursor, string trigger) =>
        Assert.Null(EditorMarkdown.FindTrigger(text, cursor, trigger));

    // ---- Addresses

    [Theory]
    [InlineData("https://a.b/x.png", true)]
    [InlineData("http://a.b", true)]
    [InlineData("data:image/png;base64,AAAA", false)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("https://user:pw@a.b/x", false)]
    [InlineData("https:a.b", false)]
    [InlineData("ftp://a.b/x.png", false)]
    [InlineData("/relative.png", false)]
    [InlineData("https://a.b/x y.png", false)]
    [InlineData("", false)]
    public void An_image_address_is_an_absolute_http_or_https_one(string address, bool accepted) =>
        Assert.Equal(accepted, EditorAddress.IsImage(address, null));

    [Fact]
    public void An_image_address_follows_the_schemes_the_app_lists()
    {
        Assert.True(EditorAddress.IsImage("https://a.b/x.png", ["https"]));
        Assert.False(EditorAddress.IsImage("http://a.b/x.png", ["https:"]));
        Assert.False(EditorAddress.IsImage("https://a.b/" + new string('a', 2100), null));
    }

    [Theory]
    [InlineData("https://a.b", true)]
    [InlineData("/docs", true)]
    [InlineData("docs/x", true)]
    [InlineData("mailto:a@b.c", true)]
    [InlineData("tel:123", true)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("vbscript:x", false)]
    [InlineData("two words", false)]
    public void Without_a_list_a_link_takes_the_usual_schemes_and_relative_addresses(string address, bool accepted) =>
        Assert.Equal(accepted, EditorAddress.IsLink(address, null));

    [Fact]
    public void With_a_list_a_link_takes_only_absolute_addresses_of_those_schemes()
    {
        string[] schemes = ["https", "MAILTO:"];
        Assert.True(EditorAddress.IsLink("https://a.b", schemes));
        Assert.True(EditorAddress.IsLink("mailto:a@b.c", schemes));
        Assert.False(EditorAddress.IsLink("http://a.b", schemes));
        Assert.False(EditorAddress.IsLink("/docs", schemes));
        Assert.False(EditorAddress.IsLink("mailto:", schemes));
        Assert.Equal("Only https, mailto addresses are accepted.", EditorAddress.Refusal(schemes));
        Assert.Equal("This address is not accepted.", EditorAddress.Refusal(null));
    }

    // ---- History

    [Fact]
    public void Typing_in_a_run_is_one_undo_entry_and_a_pause_or_an_action_starts_another()
    {
        var clock = new Clock();
        var history = new EditorHistory();
        history.Record(new EditorEdit("", 0, 0), clock.Now, typing: true);
        clock.Now += TimeSpan.FromMilliseconds(400);
        history.Record(new EditorEdit("a", 1, 0), clock.Now, typing: true);
        clock.Now += TimeSpan.FromSeconds(2);
        history.Record(new EditorEdit("ab", 2, 0), clock.Now, typing: true);
        history.Record(new EditorEdit("abc", 3, 0), clock.Now, typing: false);

        var current = new EditorEdit("abcd", 4, 0);
        Assert.Equal(new EditorEdit("abc", 3, 0), history.Undo(current));
        Assert.Equal(new EditorEdit("ab", 2, 0), history.Undo(new EditorEdit("abc", 3, 0)));
        Assert.Equal(new EditorEdit("", 0, 0), history.Undo(new EditorEdit("ab", 2, 0)));
        Assert.Null(history.Undo(new EditorEdit("", 0, 0)));
        Assert.True(history.CanRedo);
    }

    [Fact]
    public void A_new_change_clears_what_could_be_redone()
    {
        var clock = new Clock();
        var history = new EditorHistory();
        history.Record(new EditorEdit("", 0, 0), clock.Now, typing: true);
        history.Undo(new EditorEdit("a", 1, 0));
        Assert.True(history.CanRedo);

        history.Record(new EditorEdit("", 0, 0), clock.Now, typing: true);
        Assert.False(history.CanRedo);
    }

    // ---- KlyEditor

    [Fact]
    public void Editor_has_the_documented_defaults_and_forwards_its_options_to_the_native_editor()
    {
        var editor = new KlyEditor();
        var native = editor.NativeEditor;

        Assert.Null(editor.Value);
        Assert.Equal(FieldVariant.Outline, editor.Variant);
        Assert.Equal(KlySize.Md, editor.Size);
        Assert.Equal(KlyColor.Primary, editor.Color);
        Assert.False(editor.Invalid);
        Assert.False(editor.IsReadOnly);
        Assert.Null(editor.MaxLength);
        Assert.Equal(14, native.FontSize);
        Assert.Equal(128, native.MinimumHeightRequest);
        Assert.Equal(EditorAutoSizeOption.TextChanges, native.AutoSize);
        Assert.Equal(KlyTheme.Radius, ((Microsoft.Maui.Controls.Shapes.RoundRectangle)editor.StrokeShape!).CornerRadius.TopLeft);

        editor.Placeholder = "Write";
        editor.IsReadOnly = true;
        editor.MaxLength = 20;
        Assert.Equal("Write", native.Placeholder);
        Assert.True(native.IsReadOnly);
        Assert.Equal(20, native.MaxLength);

        editor.MaxLength = null;
        Assert.Equal(int.MaxValue, native.MaxLength);
    }

    [Theory]
    [InlineData(KlySize.Xs, 12)]
    [InlineData(KlySize.Sm, 12)]
    [InlineData(KlySize.Md, 14)]
    [InlineData(KlySize.Lg, 14)]
    [InlineData(KlySize.Xl, 16)]
    public void Editor_size_sets_the_font_of_the_text(KlySize size, double fontSize) =>
        Assert.Equal(fontSize, new KlyEditor { Size = size }.NativeEditor.FontSize);

    [Theory]
    [InlineData(FieldVariant.Outline, 1)]
    [InlineData(FieldVariant.Soft, 0)]
    [InlineData(FieldVariant.Subtle, 1)]
    [InlineData(FieldVariant.Ghost, 0)]
    [InlineData(FieldVariant.None, 0)]
    public void Editor_variant_sets_the_border_and_the_error_ring_wins(FieldVariant variant, double thickness)
    {
        var editor = new KlyEditor { Variant = variant };
        Assert.Equal(thickness, editor.StrokeThickness);

        editor.Invalid = true;
        Assert.Equal(2, editor.StrokeThickness);
    }

    [Fact]
    public void Editor_dims_while_disabled_and_disables_the_native_editor_and_the_actions()
    {
        var editor = EditorWith("text", 0, 4);
        Assert.True(editor.CanRun(EditorAction.Bold));

        editor.IsEnabled = false;
        Assert.Equal(0.6, editor.Opacity);
        Assert.False(editor.NativeEditor.IsEnabled);
        Assert.False(editor.CanRun(EditorAction.Bold));
        Assert.False(editor.Run(EditorAction.Bold));

        editor.IsEnabled = true;
        Assert.Equal(1, editor.Opacity);
        Assert.True(editor.NativeEditor.IsEnabled);
    }

    [Fact]
    public void Editor_has_an_accessible_name_and_a_hint_about_markdown()
    {
        var editor = new KlyEditor();
        Assert.Equal("Markdown editor", SemanticProperties.GetDescription(editor.NativeEditor));
        Assert.Equal("The text is written in Markdown", SemanticProperties.GetHint(editor.NativeEditor));

        editor.Label = "Comment";
        Assert.Equal("Comment", SemanticProperties.GetDescription(editor.NativeEditor));
    }

    [Fact]
    public void Editor_value_goes_both_ways_and_raises_one_event_per_change()
    {
        var editor = new KlyEditor();
        var changes = new List<(string?, string?)>();
        editor.ValueChanged += (_, e) => changes.Add((e.OldTextValue, e.NewTextValue));

        editor.Value = "ada";
        Assert.Equal("ada", editor.NativeEditor.Text);

        Type(editor, "ada l", 5);
        Assert.Equal("ada l", editor.Value);
        Assert.Equal([(null, "ada"), ("ada", "ada l")], changes);
    }

    [Fact]
    public void Editor_value_binds_two_way()
    {
        var previous = DispatcherProvider.Current;
        DispatcherProvider.SetCurrent(new InlineDispatcherProvider());
        try
        {
            var model = new Model { Markdown = "# one" };
            var editor = new KlyEditor { BindingContext = model };
            editor.SetBinding(KlyEditor.ValueProperty, nameof(Model.Markdown));
            Assert.Equal("# one", editor.NativeEditor.Text);

            Type(editor, "# two", 5);
            Assert.Equal("# two", model.Markdown);

            model.Markdown = "# three";
            Assert.Equal("# three", editor.NativeEditor.Text);
        }
        finally
        {
            DispatcherProvider.SetCurrent(previous);
        }
    }

    [Fact]
    public void An_action_runs_on_the_selection_of_the_native_editor_and_raises_the_event_once()
    {
        var editor = EditorWith("hello world", 6, 5);
        var changes = new List<string?>();
        editor.ValueChanged += (_, e) => changes.Add(e.NewTextValue);

        Assert.True(editor.Run(EditorAction.Bold));

        Assert.Equal("hello **world**", editor.Value);
        Assert.Equal("hello **world**", editor.NativeEditor.Text);
        Assert.Equal(8, editor.NativeEditor.CursorPosition);
        Assert.Equal(5, editor.NativeEditor.SelectionLength);
        Assert.Equal(["hello **world**"], changes);
    }

    [Fact]
    public void An_action_that_changes_nothing_does_not_raise_the_event()
    {
        var editor = EditorWith("plain", 0);
        var raised = 0;
        editor.ValueChanged += (_, _) => raised++;

        Assert.False(editor.Run(EditorAction.Link, argument: ""));
        Assert.False(editor.Run(EditorAction.MoveUp));
        Assert.Equal(0, raised);
    }

    [Fact]
    public void Undo_and_redo_restore_the_text_and_the_selection()
    {
        var editor = EditorWith("hello", 0, 5);
        Assert.False(editor.CanRun(EditorAction.Redo));

        editor.Run(EditorAction.Bold);
        Assert.Equal("**hello**", editor.Value);

        Assert.True(editor.Run(EditorAction.Undo));
        Assert.Equal("hello", editor.Value);
        Assert.Equal((0, 5), editor.Selection);
        Assert.True(editor.CanRun(EditorAction.Redo));

        Assert.True(editor.Run(EditorAction.Redo));
        Assert.Equal("**hello**", editor.Value);
    }

    [Fact]
    public void Typing_in_a_run_is_undone_in_one_step_and_a_pause_splits_it()
    {
        var clock = new Clock();
        var editor = new KlyEditor { Clock = clock };
        Type(editor, "a", 1);
        clock.Now += TimeSpan.FromMilliseconds(200);
        Type(editor, "ab", 2);
        clock.Now += TimeSpan.FromSeconds(3);
        Type(editor, "abc", 3);

        editor.Run(EditorAction.Undo);
        Assert.Equal("ab", editor.Value);
        editor.Run(EditorAction.Undo);
        Assert.Equal("", editor.Value);
        Assert.False(editor.CanRun(EditorAction.Undo));
    }

    [Fact]
    public void A_value_set_by_the_app_can_be_undone()
    {
        var editor = EditorWith("typed", 5);
        editor.Value = "loaded";
        Assert.Equal("loaded", editor.NativeEditor.Text);

        editor.Run(EditorAction.Undo);
        Assert.Equal("typed", editor.Value);
    }

    [Fact]
    public void Read_only_editor_runs_no_action()
    {
        var editor = EditorWith("text", 0, 4);
        editor.IsReadOnly = true;

        Assert.False(editor.CanRun(EditorAction.Bold));
        Assert.False(editor.Run(EditorAction.Bold));
        Assert.Equal("text", editor.Value);
    }

    [Fact]
    public void An_action_that_would_pass_the_limit_does_nothing()
    {
        var editor = EditorWith("abcd", 0, 4);
        editor.MaxLength = 5;

        Assert.False(editor.Run(EditorAction.Bold));
        Assert.Equal("abcd", editor.Value);

        editor.MaxLength = 8;
        Assert.True(editor.Run(EditorAction.Bold));
        Assert.Equal("**abcd**", editor.Value);
    }

    [Fact]
    public void A_link_address_the_schemes_refuse_is_not_applied()
    {
        var editor = EditorWith("see docs", 4, 4);
        editor.LinkSchemes = ["https"];

        Assert.False(editor.Run(EditorAction.Link, argument: "http://a.b"));
        Assert.Equal("see docs", editor.Value);
        Assert.True(editor.Run(EditorAction.Link, argument: "https://a.b"));
        Assert.Equal("see [docs](https://a.b)", editor.Value);
    }

    [Fact]
    public void Editor_children_are_toolbars_a_drag_handle_and_menus_only()
    {
        var editor = new KlyEditor();
        editor.Children.Add(new KlyEditorToolbar());
        editor.Children.Add(new KlyEditorDragHandle());
        editor.Children.Add(new KlyEditorEmojiMenu());

        Assert.Throws<ArgumentException>(() => editor.Children.Add(new Label()));
        Assert.Equal(3, editor.Children.Count);
    }

    // ---- KlyEditorToolbar

    [Fact]
    public void Toolbar_has_the_defaults_of_the_web_toolbar()
    {
        var toolbar = new KlyEditorToolbar();

        Assert.Equal(EditorToolbarLayout.Fixed, toolbar.Layout);
        Assert.Equal(KlyColor.Primary, toolbar.Color);
        Assert.Equal(KlySize.Sm, toolbar.Size);
        Assert.Equal(EditorToolbarItem.Defaults.Count(i => !i.IsSeparator), toolbar.Buttons.Count);
        Assert.Equal([EditorAction.Bold, EditorAction.Italic, EditorAction.Strike, EditorAction.Code], toolbar.Buttons.Take(4).Select(b => b.Item.Action));
        Assert.Equal("Formatting", SemanticProperties.GetDescription(toolbar));

        toolbar.Label = "Text tools";
        Assert.Equal("Text tools", SemanticProperties.GetDescription(toolbar));
    }

    [Fact]
    public void Toolbar_buttons_have_an_accessible_name_from_the_item_or_the_action()
    {
        var toolbar = new KlyEditorToolbar
        {
            Items = [new(EditorAction.Bold), EditorToolbarItem.Heading(2), new(EditorAction.Italic) { Label = "Emphasis" }, EditorToolbarItem.Separator, new(EditorAction.Undo)],
        };

        Assert.Equal(["Bold", "Heading 2", "Emphasis", "Undo"], toolbar.Buttons.Select(b => SemanticProperties.GetDescription(b.Hit)));
        Assert.Equal("heading-2", ((KlyIcon)((Border)toolbar.Buttons[1].Children[0]).Content!).Name);
    }

    [Theory]
    [InlineData(KlySize.Xs, 24)]
    [InlineData(KlySize.Sm, 28)]
    [InlineData(KlySize.Md, 32)]
    [InlineData(KlySize.Lg, 36)]
    [InlineData(KlySize.Xl, 40)]
    public void Toolbar_size_sets_the_face_of_the_buttons_and_the_cell_stays_a_touch_target(KlySize size, double face)
    {
        var toolbar = new KlyEditorToolbar { Size = size };
        var button = toolbar.Buttons[0];

        Assert.Equal(face, EditorToolbarButton.FaceOf(size));
        Assert.Equal(Math.Max(40, face), button.WidthRequest);
        Assert.Equal(Math.Max(40, face), button.HeightRequest);
        Assert.Equal(face, ((Border)button.Children[0]).WidthRequest);
    }

    [Fact]
    public void Toolbar_buttons_stay_pressed_while_the_selection_has_the_format()
    {
        var toolbar = new KlyEditorToolbar();
        var editor = EditorWith("**a** and\n## head", 2, 0, toolbar);

        Assert.True(ButtonOf(toolbar, EditorAction.Bold).Pressed);
        Assert.False(ButtonOf(toolbar, EditorAction.Italic).Pressed);
        Assert.False(ButtonOf(toolbar, EditorAction.Heading, 2).Pressed);

        editor.NativeEditor.CursorPosition = 14;
        Assert.False(ButtonOf(toolbar, EditorAction.Bold).Pressed);
        Assert.True(ButtonOf(toolbar, EditorAction.Heading, 2).Pressed);
        Assert.False(ButtonOf(toolbar, EditorAction.Heading, 1).Pressed);
    }

    [Fact]
    public void Toolbar_undo_and_redo_are_off_until_there_is_something_to_undo_or_redo()
    {
        var toolbar = new KlyEditorToolbar();
        var editor = new KlyEditor { Children = { toolbar } };
        Assert.False(ButtonOf(toolbar, EditorAction.Undo).Enabled);
        Assert.False(ButtonOf(toolbar, EditorAction.Redo).Enabled);
        Assert.True(ButtonOf(toolbar, EditorAction.Bold).Enabled);

        Type(editor, "text", 0, 4);
        Assert.True(ButtonOf(toolbar, EditorAction.Undo).Enabled);

        ButtonOf(toolbar, EditorAction.Undo).Hit.SendClicked();
        Assert.True(ButtonOf(toolbar, EditorAction.Redo).Enabled);
        Assert.Equal("", editor.Value);
    }

    [Fact]
    public void Toolbar_buttons_dim_while_off_and_all_go_off_in_a_read_only_editor()
    {
        var toolbar = new KlyEditorToolbar();
        var editor = EditorWith("text", 0, 4, toolbar);
        var redo = ButtonOf(toolbar, EditorAction.Redo);
        Assert.Equal(0.4, redo.Opacity);
        Assert.Equal(1, ButtonOf(toolbar, EditorAction.Bold).Opacity);

        editor.IsReadOnly = true;
        Assert.All(toolbar.Buttons, b => Assert.False(b.Enabled));
    }

    [Fact]
    public void Toolbar_without_an_editor_has_every_button_off()
    {
        var toolbar = new KlyEditorToolbar();
        Assert.All(toolbar.Buttons, b => Assert.False(b.Enabled));
        ButtonOf(toolbar, EditorAction.Bold).Hit.SendClicked();
    }

    [Fact]
    public void A_button_applies_its_action_to_the_selection()
    {
        var toolbar = new KlyEditorToolbar();
        var editor = EditorWith("title", 0, 5, toolbar);

        ButtonOf(toolbar, EditorAction.Heading, 2).Hit.SendClicked();
        Assert.Equal("## title", editor.Value);
        Assert.True(ButtonOf(toolbar, EditorAction.Heading, 2).Pressed);

        ButtonOf(toolbar, EditorAction.Heading, 2).Hit.SendClicked();
        Assert.Equal("title", editor.Value);
    }

    [Fact]
    public void Bubble_toolbar_shows_only_while_text_is_selected_and_has_the_inline_buttons()
    {
        var toolbar = new KlyEditorToolbar { Layout = EditorToolbarLayout.Bubble };
        var editor = EditorWith("text", 0, 0, toolbar);

        Assert.Equal(EditorToolbarItem.BubbleDefaults.Count, toolbar.Buttons.Count);
        Assert.False(toolbar.IsVisible);

        editor.NativeEditor.SelectionLength = 4;
        Assert.True(toolbar.IsVisible);

        editor.NativeEditor.SelectionLength = 0;
        Assert.False(toolbar.IsVisible);

        Assert.True(new KlyEditorToolbar().IsVisible);
    }

    [Fact]
    public void Fixed_toolbar_has_a_rule_under_it_and_the_bubble_a_border_instead()
    {
        var toolbar = new KlyEditorToolbar();
        Assert.Equal(0, toolbar.StrokeThickness);
        Assert.True(Rule(toolbar).IsVisible);

        toolbar.Layout = EditorToolbarLayout.Bubble;
        Assert.Equal(1, toolbar.StrokeThickness);
        Assert.False(Rule(toolbar).IsVisible);

        static BoxView Rule(KlyEditorToolbar t) => ((VerticalStackLayout)t.Content!).Children.OfType<BoxView>().Single();
    }

    [Fact]
    public void The_link_form_applies_the_address_to_the_selection_kept_when_it_opened()
    {
        var toolbar = new KlyEditorToolbar();
        var editor = EditorWith("see docs", 4, 4, toolbar);

        ButtonOf(toolbar, EditorAction.Link).Hit.SendClicked();
        Assert.True(toolbar.LinkForm.IsVisible);
        Assert.Equal("", toolbar.LinkForm.Input.Text);
        Assert.False(toolbar.LinkForm.RemoveButton.IsVisible);

        // The field takes the focus and the editor forgets its selection.
        editor.NativeEditor.CursorPosition = 0;
        editor.NativeEditor.SelectionLength = 0;
        toolbar.LinkForm.Input.Text = " https://a.b ";
        toolbar.LinkForm.ApplyButton.SendClicked();

        Assert.Equal("see [docs](https://a.b)", editor.Value);
        Assert.False(toolbar.LinkForm.IsVisible);
    }

    [Fact]
    public void The_link_form_keeps_a_refused_address_and_says_why()
    {
        var toolbar = new KlyEditorToolbar();
        var editor = EditorWith("see docs", 4, 4, toolbar);
        ButtonOf(toolbar, EditorAction.Link).Hit.SendClicked();

        toolbar.LinkForm.Input.Text = "javascript:alert(1)";
        toolbar.LinkForm.ApplyButton.SendClicked();
        Assert.True(toolbar.LinkForm.IsVisible);
        Assert.Equal("This address is not accepted.", toolbar.LinkForm.Problem);
        Assert.Equal("see docs", editor.Value);

        editor.LinkSchemes = ["https", "mailto"];
        toolbar.LinkForm.Input.Text = "http://a.b";
        toolbar.LinkForm.ApplyButton.SendClicked();
        Assert.Equal("Only https, mailto addresses are accepted.", toolbar.LinkForm.Problem);

        // Typing a new address takes back the explanation.
        toolbar.LinkForm.Input.Text = "https://a.b";
        Assert.Null(toolbar.LinkForm.Problem);
    }

    [Fact]
    public void The_link_form_shows_the_address_of_the_link_and_removes_it()
    {
        var toolbar = new KlyEditorToolbar();
        var editor = EditorWith("see [docs](https://a.b)", 6, 0, toolbar);
        Assert.True(ButtonOf(toolbar, EditorAction.Link).Pressed);

        ButtonOf(toolbar, EditorAction.Link).Hit.SendClicked();
        Assert.Equal("https://a.b", toolbar.LinkForm.Input.Text);
        Assert.True(toolbar.LinkForm.RemoveButton.IsVisible);

        toolbar.LinkForm.RemoveButton.SendClicked();
        Assert.Equal("see docs", editor.Value);
        Assert.False(toolbar.LinkForm.IsVisible);
    }

    [Fact]
    public void Pressing_the_link_button_again_closes_the_form_and_the_image_form_inserts_an_image()
    {
        var toolbar = new KlyEditorToolbar { Items = [new(EditorAction.Link), new(EditorAction.Image)] };
        var editor = EditorWith("", 0, 0, toolbar);

        ButtonOf(toolbar, EditorAction.Link).Hit.SendClicked();
        ButtonOf(toolbar, EditorAction.Link).Hit.SendClicked();
        Assert.False(toolbar.LinkForm.IsVisible);

        ButtonOf(toolbar, EditorAction.Image).Hit.SendClicked();
        Assert.True(toolbar.ImageForm.IsVisible);
        Assert.False(toolbar.LinkForm.IsVisible);

        toolbar.ImageForm.Input.Text = "data:image/png;base64,AAAA";
        toolbar.ImageForm.ApplyButton.SendClicked();
        Assert.Equal("This address is not accepted.", toolbar.ImageForm.Problem);

        toolbar.ImageForm.Input.Text = "https://x.y/team-photo.jpg";
        toolbar.ImageForm.ApplyButton.SendClicked();
        Assert.Equal("![team photo](https://x.y/team-photo.jpg)", editor.Value);
        Assert.False(toolbar.ImageForm.IsVisible);
    }

    // ---- Menus

    [Fact]
    public void Mention_menu_opens_on_the_trigger_filters_and_inserts_the_label()
    {
        var mentions = new KlyEditorMentionMenu { Items = [new("1", "Ada Lovelace") { Description = "Engineer" }, new("2", "Grace Hopper")] };
        var editor = EditorWith("hi @", 4, 0, mentions);
        var chosen = new List<EditorMentionItem>();
        mentions.Mentioned += (_, item) => chosen.Add(item);

        Assert.True(mentions.IsOpen);
        Assert.Equal(["Ada Lovelace", "Grace Hopper"], mentions.Shown.Select(o => o.Label));

        Type(editor, "hi @ad", 6);
        Assert.Equal(["Ada Lovelace"], mentions.Shown.Select(o => o.Label));

        mentions.Choose(mentions.Shown[0]);
        Assert.Equal("hi @Ada Lovelace ", editor.Value);
        Assert.Equal(17, editor.NativeEditor.CursorPosition);
        Assert.False(mentions.IsOpen);
        Assert.Equal("1", Assert.Single(chosen).Id);
    }

    [Fact]
    public void Menu_closes_when_the_word_changes_the_selection_grows_or_the_editor_is_read_only()
    {
        var mentions = new KlyEditorMentionMenu { Items = [new("1", "Ada")] };
        var editor = EditorWith("hi @a", 5, 0, mentions);
        Assert.True(mentions.IsOpen);

        Type(editor, "hi @a b", 7);
        Assert.False(mentions.IsOpen);

        Type(editor, "hi @a", 5);
        Assert.True(mentions.IsOpen);
        editor.NativeEditor.SelectionLength = 1;
        editor.NativeEditor.CursorPosition = 4;
        Assert.False(mentions.IsOpen);

        editor.NativeEditor.SelectionLength = 0;
        editor.NativeEditor.CursorPosition = 5;
        Assert.True(mentions.IsOpen);
        editor.IsReadOnly = true;
        Assert.False(mentions.IsOpen);
    }

    [Fact]
    public void Menu_says_there_are_no_results_and_a_custom_character_opens_it()
    {
        var mentions = new KlyEditorMentionMenu { Char = "#", Items = [new("1", "Ada")] };
        var editor = EditorWith("#zz", 3, 0, mentions);

        Assert.True(mentions.IsOpen);
        Assert.Empty(mentions.Shown);
        Assert.True(mentions.ShowsEmpty);

        Type(editor, "#a", 2);
        Assert.False(mentions.ShowsEmpty);
        Type(editor, "@a", 2);
        Assert.False(mentions.IsOpen);

        Type(editor, "#a", 2);
        mentions.Choose(mentions.Shown[0]);
        Assert.Equal("#Ada ", editor.Value);
    }

    [Fact]
    public void Emoji_menu_needs_a_character_ranks_matches_and_inserts_the_emoji_in_place_of_the_word()
    {
        var emoji = new KlyEditorEmojiMenu();
        var editor = EditorWith(":", 1, 0, emoji);
        Assert.False(emoji.IsOpen);

        Type(editor, "a :sm", 5);
        Assert.True(emoji.IsOpen);
        Assert.Equal(["smile", "slight_smile", "grinning"], emoji.Shown.Select(o => o.Label));

        emoji.Choose(emoji.Shown[0]);
        Assert.Equal("a 😄", editor.Value);
        Assert.False(emoji.IsOpen);
    }

    [Fact]
    public void Slash_menu_removes_the_typed_word_and_applies_the_action_to_the_line()
    {
        var slash = new KlyEditorSuggestionMenu();
        var editor = EditorWith("/", 1, 0, slash);
        Assert.True(slash.IsOpen);
        Assert.Equal(EditorSuggestionItem.Defaults.Count, slash.Shown.Count);

        Type(editor, "/h1", 3);
        Assert.Equal(["Heading 1"], slash.Shown.Select(o => o.Label));
        slash.Choose(slash.Shown[0]);
        Assert.Equal("# ", editor.Value);
        Assert.Equal(2, editor.NativeEditor.CursorPosition);

        Type(editor, "a\n/ul", 5);
        slash.Choose(slash.Shown[0]);
        Assert.Equal("a\n- ", editor.Value);

        Type(editor, "x\n/hr", 5);
        slash.Choose(slash.Shown[0]);
        Assert.Equal("x\n\n---\n", editor.Value);
    }

    [Fact]
    public void A_menu_choice_is_one_undo_step()
    {
        var slash = new KlyEditorSuggestionMenu();
        var editor = EditorWith("/q", 2, 0, slash);
        slash.Choose(slash.Shown.Single(o => o.Label == "Quote"));
        Assert.Equal("> ", editor.Value);

        editor.Run(EditorAction.Undo);
        Assert.Equal("/q", editor.Value);
    }

    [Fact]
    public void Menu_ranks_by_label_start_then_label_then_keywords_and_stops_at_the_limit()
    {
        var items = new[]
        {
            new EditorMentionItem("1", "Zed Ada"),
            new EditorMentionItem("2", "Ada Zed"),
            new EditorMentionItem("3", "Other"),
        };
        var mentions = new KlyEditorMentionMenu { Items = items };
        mentions.Show("ada");
        Assert.Equal(["Ada Zed", "Zed Ada"], mentions.Shown.Select(o => o.Label));

        mentions.Show("");
        Assert.Equal(3, mentions.Shown.Count);

        mentions.Limit = 2;
        Assert.Equal(2, mentions.Shown.Count);

        var emoji = new KlyEditorEmojiMenu { Limit = 1 };
        emoji.Show("sm");
        Assert.Equal("smile", Assert.Single(emoji.Shown).Label);
    }

    [Fact]
    public void Menu_matches_keywords_and_a_new_item_list_refreshes_an_open_menu()
    {
        var slash = new KlyEditorSuggestionMenu();
        slash.Show("h2");
        Assert.Equal(["Heading 2"], slash.Shown.Select(o => o.Label));

        slash.Items = [new(EditorAction.Blockquote, "Quote"), new(EditorAction.Heading, "H2 custom") { Level = 2 }];
        Assert.Equal(["H2 custom"], slash.Shown.Select(o => o.Label));
    }

    [Fact]
    public void A_disabled_option_or_a_disabled_menu_chooses_nothing()
    {
        var mentions = new KlyEditorMentionMenu { Items = [new("1", "Ada") { Disabled = true }, new("2", "Grace")] };
        var editor = EditorWith("@", 1, 0, mentions);
        var chosen = 0;
        mentions.Mentioned += (_, _) => chosen++;

        Assert.Equal(2, mentions.Shown.Count);
        mentions.Choose(mentions.Shown[0]);
        Assert.Equal("@", editor.Value);

        mentions.IsEnabled = false;
        Assert.Equal(0.6, mentions.Opacity);
        mentions.Choose(mentions.Shown[1]);
        Assert.Equal("@", editor.Value);
        Assert.Equal(0, chosen);

        mentions.IsEnabled = true;
        mentions.Choose(mentions.Shown[1]);
        Assert.Equal("@Grace ", editor.Value);
        Assert.Equal(1, chosen);
    }

    [Fact]
    public void Menus_have_the_labels_triggers_and_defaults_of_the_web_menus()
    {
        var slash = new KlyEditorSuggestionMenu();
        var mentions = new KlyEditorMentionMenu();
        var emoji = new KlyEditorEmojiMenu();

        Assert.Equal(["/", "@", ":"], new KlyEditorMenuBase[] { slash, mentions, emoji }.Select(m => m.Trigger));
        Assert.Equal([0, 0, 1], new KlyEditorMenuBase[] { slash, mentions, emoji }.Select(m => m.MinQuery));
        Assert.All(new KlyEditorMenuBase[] { slash, mentions, emoji }, m =>
        {
            Assert.False(m.IsVisible);
            Assert.Equal(42, m.Limit);
            Assert.Equal(KlySize.Md, m.Size);
        });
        Assert.Equal(["Commands", "Mentions", "Emoji"], new KlyEditorMenuBase[] { slash, mentions, emoji }.Select(ListLabel));

        slash.Label = "Blocks";
        Assert.Equal("Blocks", ListLabel(slash));

        static string? ListLabel(KlyEditorMenuBase menu) =>
            SemanticProperties.GetDescription(((VerticalStackLayout)((ScrollView)menu.Content!).Content!).Children[0] as BindableObject);
    }

    [Fact]
    public void Menu_rows_follow_the_size_and_describe_their_option()
    {
        var mentions = new KlyEditorMentionMenu { Size = KlySize.Xl, Items = [new("1", "Ada") { Description = "Engineer" }] };
        mentions.Show("");

        var row = (Border)((VerticalStackLayout)((VerticalStackLayout)((ScrollView)mentions.Content!).Content!).Children[0]).Children[0];
        Assert.Equal("Ada, Engineer", SemanticProperties.GetDescription(row));
        Assert.Equal(new Thickness(12, 8), ((Grid)row.Content!).Padding);
        Assert.True(row.MinimumHeightRequest >= 40);
        Assert.Single(row.GestureRecognizers.OfType<TapGestureRecognizer>());
    }

    [Fact]
    public void Suggestion_items_default_to_the_blocks_of_the_web_slash_menu()
    {
        Assert.Equal(
            [EditorAction.Paragraph, EditorAction.Heading, EditorAction.Heading, EditorAction.Heading, EditorAction.BulletList, EditorAction.OrderedList, EditorAction.Blockquote, EditorAction.CodeBlock, EditorAction.HorizontalRule],
            EditorSuggestionItem.Defaults.Select(i => i.Action));
        Assert.Equal([1, 2, 3], EditorSuggestionItem.Defaults.Where(i => i.Action == EditorAction.Heading).Select(i => i.Level));
        Assert.Equal(30, EditorEmojiItem.Defaults.Count);
    }

    // ---- KlyEditorDragHandle

    [Fact]
    public void Drag_handle_has_a_touch_target_a_name_and_a_hint()
    {
        var handle = new KlyEditorDragHandle();

        Assert.Equal(32, handle.Step);
        Assert.Equal(KlyColor.Primary, handle.Color);
        Assert.True(handle.WidthRequest >= 40);
        Assert.True(handle.HeightRequest >= 40);
        Assert.Equal("Drag to move block", SemanticProperties.GetDescription(handle));
        Assert.NotEmpty(SemanticProperties.GetHint(handle));
        Assert.Single(handle.GestureRecognizers.OfType<PanGestureRecognizer>());

        handle.Label = "Move";
        Assert.Equal("Move", SemanticProperties.GetDescription(handle));

        handle.IsEnabled = false;
        Assert.Equal(0.6, handle.Opacity);
    }

    [Fact]
    public void Drag_handle_raises_one_event_per_step_in_either_direction()
    {
        var handle = new KlyEditorDragHandle();
        var steps = new List<int>();
        handle.Dragged += (_, e) => steps.Add(e.Direction);

        handle.Pan(31);
        Assert.Empty(steps);
        handle.Pan(32);
        Assert.Equal([1], steps);
        handle.Pan(97);
        Assert.Equal([1, 1, 1], steps);
        handle.Pan(-33);
        Assert.Equal([1, 1, 1, -1, -1, -1, -1], steps);

        handle.EndPan();
        steps.Clear();
        handle.Pan(10);
        Assert.Empty(steps);

        handle.Step = 10;
        handle.Pan(10);
        Assert.Equal([1], steps);

        handle.IsEnabled = false;
        handle.Pan(50);
        Assert.Single(steps);
    }

    [Fact]
    public void Drag_handle_moves_the_block_that_holds_the_cursor_and_steps_stop_at_the_ends()
    {
        var handle = new KlyEditorDragHandle();
        var editor = EditorWith("one\n\ntwo", 1, 0, handle);

        handle.Pan(32);
        Assert.Equal("two\n\none", editor.Value);

        handle.Pan(64);
        Assert.Equal("two\n\none", editor.Value);

        handle.Pan(0);
        Assert.Equal("one\n\ntwo", editor.Value);
    }

    [Fact]
    public void Drag_event_args_carry_the_direction() =>
        Assert.Equal(-1, new EditorDragEventArgs(-1).Direction);

    // ---- Items

    [Fact]
    public void Toolbar_items_name_their_heading_and_separators_are_not_buttons()
    {
        var heading = EditorToolbarItem.Heading(3);
        Assert.Equal(EditorAction.Heading, heading.Action);
        Assert.Equal(3, heading.Level);
        Assert.False(heading.IsSeparator);
        Assert.True(EditorToolbarItem.Separator.IsSeparator);
        Assert.Equal(4, EditorToolbarItem.Defaults.Count(i => i.IsSeparator));
    }

    [Theory]
    [InlineData(EditorAction.Bold, 0, "Bold", "bold")]
    [InlineData(EditorAction.Heading, 4, "Heading 4", "heading-4")]
    [InlineData(EditorAction.Heading, 6, "Heading 6", "heading-4")]
    [InlineData(EditorAction.Strike, 0, "Strikethrough", "strikethrough")]
    [InlineData(EditorAction.CodeBlock, 0, "Code block", "square-code")]
    [InlineData(EditorAction.MoveUp, 0, "Move block up", "arrow-up")]
    [InlineData(EditorAction.Undo, 0, "Undo", "undo-2")]
    public void Actions_have_the_labels_and_the_icons_of_the_web_toolbar(EditorAction action, int level, string label, string icon)
    {
        Assert.Equal(label, EditorActionInfo.Label(action, level));
        Assert.Equal(icon, EditorActionInfo.IconName(action, level));
        Assert.True(KlyIcons.TryGet(icon, out _));
    }

    [Fact]
    public void Every_action_has_a_label_and_a_built_in_icon()
    {
        foreach (var action in Enum.GetValues<EditorAction>())
        {
            Assert.NotEmpty(EditorActionInfo.Label(action, 1));
            Assert.True(KlyIcons.TryGet(EditorActionInfo.IconName(action, 1), out _), action.ToString());
        }
    }
}
