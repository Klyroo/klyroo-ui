using System.Globalization;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Lux.UI.Tests;

public class Form2Tests : LuxTestContext
{
    private static readonly string Sep = "\u001f";
    private static readonly LxSelectItem[] Items = [new("a", "Alpha"), new("b", "Beta"), new("c", "Gamma") { Disabled = true }];

    [Fact]
    public void SelectMenu_renders_trigger_search_listbox_and_values()
    {
        var cut = Render<LxSelectMenu>(p => p.Add(s => s.Items, Items).Add(s => s.Multiple, true).Add(s => s.Name, "tags")
            .Add(s => s.Value, ["a", "b"]));

        var host = cut.Find("lx-select-menu");
        Assert.Equal("a" + Sep + "b", host.GetAttribute("data-values"));
        Assert.Equal("tags", host.GetAttribute("name"));
        Assert.Equal("combobox", cut.Find("[data-slot=trigger]").GetAttribute("role"));
        Assert.Equal("Alpha, Beta", cut.Find("[data-slot=label]").GetAttribute("data-label"));
        Assert.Equal("combobox", cut.Find("[data-slot=search]").GetAttribute("role"));
        Assert.Equal("true", cut.Find("[role=listbox]").GetAttribute("aria-multiselectable"));
        var options = cut.FindAll("[role=option]");
        Assert.Equal(["true", "true", "false"], options.Select(o => o.GetAttribute("aria-selected")));
        Assert.Equal("true", options[2].GetAttribute("aria-disabled"));
    }

    [Fact]
    public void SelectMenu_unbound_has_no_sequence_and_bound_one_hears_the_browser()
    {
        var unbound = Render<LxSelectMenu>(p => p.Add(s => s.Items, Items));
        Assert.False(unbound.Find("lx-select-menu").HasAttribute("data-lx-seq"));

        IReadOnlyList<string>? value = null;
        var cut = Render<LxSelectMenu>(p => p.Add(s => s.Items, Items).Add(s => s.Multiple, true)
            .Add(s => s.Value, []).Add(s => s.ValueChanged, (IReadOnlyList<string>? v) => value = v));

        cut.Find("lx-select-menu").TriggerEvent("onbxchange", new LxChangeEventArgs { Value = "a" + Sep + "b", Sequence = 3 });

        Assert.Equal(["a", "b"], value);
        Assert.Equal("3", cut.Find("lx-select-menu").GetAttribute("data-lx-seq"));
        Assert.Equal("data-lx-seq", cut.Find("lx-select-menu").Attributes[0]!.Name);
    }

    [Fact]
    public void SelectMenu_create_renders_the_hidden_create_option_and_single_posts_empty_value()
    {
        var cut = Render<LxSelectMenu>(p => p.Add(s => s.Items, Items).Add(s => s.Create, true).Add(s => s.Name, "pick"));

        Assert.NotNull(cut.Find("[data-slot=create]").GetAttribute("hidden"));
        Assert.True(cut.Find("lx-select-menu").HasAttribute("data-post-empty"));
        Assert.Null(cut.Find("lx-select-menu").GetAttribute("data-multiple"));
    }

    [Fact]
    public void SelectMenu_in_an_EditForm_reports_the_posted_name_and_invalid_state()
    {
        var model = new Picks();
        var cut = RenderForm(model, b =>
        {
            b.OpenComponent<LxSelectMenu>(1);
            b.AddAttribute(2, "Items", Items);
            b.AddAttribute(3, "Multiple", true);
            b.AddAttribute(4, "Value", model.Values);
            b.AddAttribute(5, "ValueChanged", EventCallback.Factory.Create<IReadOnlyList<string>?>(this, v => model.Values = v ?? []));
            b.AddAttribute(6, "ValueExpression", (Expression<Func<IReadOnlyList<string>?>>)(() => model.Values));
            b.CloseComponent();
        });

        Assert.Equal("model.Values", cut.Find("lx-select-menu").GetAttribute("name"));
        Assert.False(cut.Find("[data-slot=trigger]").HasAttribute("aria-invalid"));

        cut.Find("form").Submit();

        Assert.Equal("true", cut.Find("[data-slot=trigger]").GetAttribute("aria-invalid"));
    }

    [Fact]
    public void FormField_labels_and_describes_a_SelectMenu()
    {
        var cut = Render<LxFormField>(p => p.Add(f => f.Label, "Stack").Add(f => f.Error, "Required")
            .Add(f => f.ChildContent, (RenderFragment)(b =>
            {
                b.OpenComponent<LxSelectMenu>(0);
                b.AddAttribute(1, "Items", Items);
                b.CloseComponent();
            })));

        var trigger = cut.Find("[data-slot=trigger]");
        Assert.Equal(trigger.Id, cut.Find("label").GetAttribute("for"));
        Assert.Equal("true", trigger.GetAttribute("aria-invalid"));
        Assert.Contains(cut.Find("[data-slot=error]").Id, trigger.GetAttribute("aria-describedby")!.Split(' '));
    }

    [Fact]
    public void InputMenu_free_text_shows_value_and_strict_shows_the_label()
    {
        var free = Render<LxInputMenu>(p => p.Add(m => m.Items, Items).Add(m => m.Value, "typed").Add(m => m.Name, "city"));
        Assert.Equal("typed", free.Find("[data-slot=input]").GetAttribute("value"));
        Assert.Equal("combobox", free.Find("[data-slot=input]").GetAttribute("role"));
        Assert.Equal("list", free.Find("[data-slot=input]").GetAttribute("aria-autocomplete"));
        Assert.Equal("city", free.Find("input[type=hidden]").GetAttribute("name"));

        var strict = Render<LxInputMenu>(p => p.Add(m => m.Items, Items).Add(m => m.Strict, true).Add(m => m.Value, "b"));
        Assert.Equal("Beta", strict.Find("[data-slot=input]").GetAttribute("value"));
        Assert.Equal("b", strict.Find("input[type=hidden]").GetAttribute("value"));
        Assert.NotNull(strict.Find("lx-input-menu").GetAttribute("data-strict"));
    }

    [Fact]
    public void InputMenu_updates_the_bound_value_from_the_browser()
    {
        string? value = null;
        var cut = Render<LxInputMenu>(p => p.Add(m => m.Items, Items).Add(m => m.Value, value).Add(m => m.ValueChanged, (string? v) => value = v));

        cut.Find("lx-input-menu").TriggerEvent("onbxchange", new LxChangeEventArgs { Value = "b", Sequence = 2 });

        Assert.Equal("b", value);
    }

    [Fact]
    public void InputNumber_renders_spinbutton_buttons_and_invariant_value()
    {
        var cut = Render<LxInputNumber>(p => p.Add(n => n.Value, 1234.5m).Add(n => n.Min, 0m).Add(n => n.Max, 2000m)
            .Add(n => n.Step, 0.5m).Add(n => n.Culture, new CultureInfo("fr-FR")).Add(n => n.Name, "price"));

        var host = cut.Find("lx-input-number");
        Assert.Equal("1234.5", host.GetAttribute("value"));
        Assert.Equal("fr-FR", host.GetAttribute("locale"));
        Assert.Equal("0.5", host.GetAttribute("step"));
        Assert.Equal("spinbutton", cut.Find("[data-slot=input]").GetAttribute("role"));
        Assert.Equal("price", cut.Find("input[type=hidden]").GetAttribute("name"));
        Assert.Equal("1234.5", cut.Find("input[type=hidden]").GetAttribute("value"));
        Assert.Equal("Increase", cut.Find("[data-slot=increment]").GetAttribute("aria-label"));
        Assert.Equal("Decrease", cut.Find("[data-slot=decrement]").GetAttribute("aria-label"));
    }

    [Fact]
    public void InputNumber_parses_the_invariant_value_reported_by_the_browser()
    {
        decimal? value = null;
        var cut = Render<LxInputNumber>(p => p.Add(n => n.Value, value).Add(n => n.ValueChanged, (decimal? v) => value = v));

        cut.Find("lx-input-number").TriggerEvent("onbxchange", new LxChangeEventArgs { Value = "12.5", Sequence = 1 });
        Assert.Equal(12.5m, value);

        cut.Find("lx-input-number").TriggerEvent("onbxchange", new LxChangeEventArgs { Value = "", Sequence = 2 });
        Assert.Null(value);
    }

    [Fact]
    public void InputTags_renders_the_values_and_texts_for_the_browser()
    {
        IReadOnlyList<string> tags = ["x", "y"];
        var cut = Render<LxInputTags>(p => p.Add(t => t.Value, tags).Add(t => t.Max, 3).Add(t => t.Name, "tags"));

        var host = cut.Find("lx-input-tags");
        Assert.Equal("x" + Sep + "y", host.GetAttribute("data-values"));
        Assert.Equal("3", host.GetAttribute("data-max"));
        Assert.Equal("tags", host.GetAttribute("name"));
        Assert.Equal("Remove {0}", host.GetAttribute("data-remove-label"));
        Assert.False(string.IsNullOrEmpty(host.GetAttribute("data-tag-class")));
        Assert.Empty(cut.Find("[data-slot=tags]").Children);
    }

    [Fact]
    public void InputTags_reports_a_list_to_the_bound_value()
    {
        IReadOnlyList<string> tags = [];
        var cut = Render<LxInputTags>(p => p.Add(t => t.Value, tags).Add(t => t.ValueChanged, (IReadOnlyList<string>? v) => tags = v!));

        cut.Find("lx-input-tags").TriggerEvent("onbxchange", new LxChangeEventArgs { Value = "one" + Sep + "two", Sequence = 4 });

        Assert.Equal(["one", "two"], tags);
    }

    [Fact]
    public void PinInput_renders_one_labelled_box_per_character()
    {
        var cut = Render<LxPinInput>(p => p.Add(i => i.Length, 4).Add(i => i.Value, "12").Add(i => i.Mask, true).Add(i => i.Name, "pin").Add(i => i.AriaLabel, "Code"));

        var cells = cut.FindAll("[data-slot=cell]");
        Assert.Equal(4, cells.Count);
        Assert.Equal(["1", "2", null, null], cells.Select(c => c.GetAttribute("value")));
        Assert.All(cells, c => Assert.Equal("password", c.GetAttribute("type")));
        Assert.Equal("Character 3 of 4", cells[2].GetAttribute("aria-label"));
        Assert.Equal("one-time-code", cells[0].GetAttribute("autocomplete"));
        Assert.Equal("group", cut.Find("lx-pin-input").GetAttribute("role"));
        Assert.Equal("pin", cut.Find("input[type=hidden]").GetAttribute("name"));
        Assert.Equal("12", cut.Find("input[type=hidden]").GetAttribute("value"));
    }

    [Fact]
    public void PinInput_numeric_uses_a_numeric_keyboard()
    {
        var cut = Render<LxPinInput>(p => p.Add(i => i.Length, 3));

        Assert.Equal("numeric", cut.Find("lx-pin-input").GetAttribute("data-type"));
        Assert.All(cut.FindAll("[data-slot=cell]"), c => Assert.Equal("numeric", c.GetAttribute("inputmode")));
    }

    [Fact]
    public void Slider_renders_a_native_range_input()
    {
        var cut = Render<LxSlider>(p => p.Add(s => s.Min, 10).Add(s => s.Max, 50).Add(s => s.Step, 5).Add(s => s.Value, 25).Add(s => s.Name, "volume"));

        var input = cut.Find("input[type=range]");
        Assert.Equal("10", input.GetAttribute("min"));
        Assert.Equal("50", input.GetAttribute("max"));
        Assert.Equal("5", input.GetAttribute("step"));
        Assert.Equal("25", input.GetAttribute("value"));
        Assert.Equal("volume", input.GetAttribute("name"));
    }

    [Fact]
    public void RangeSlider_renders_two_named_thumbs_in_a_group_and_parses_both_values()
    {
        IReadOnlyList<double> value = [10, 20];
        var cut = Render<LxRangeSlider>(p => p.Add(s => s.Name, "price").Add(s => s.Value, value)
            .Add(s => s.ValueChanged, (IReadOnlyList<double>? v) => value = v!).Add(s => s.AriaLabel, "Price"));

        var inputs = cut.FindAll("input[type=range]");
        Assert.Equal(2, inputs.Count);
        Assert.Equal(["10", "20"], inputs.Select(i => i.GetAttribute("value")));
        Assert.Equal(["Minimum", "Maximum"], inputs.Select(i => i.GetAttribute("aria-label")));
        Assert.All(inputs, i => Assert.Equal("price", i.GetAttribute("name")));
        Assert.Equal("group", cut.Find("lx-slider").GetAttribute("role"));

        cut.Find("lx-slider").TriggerEvent("onbxchange", new LxChangeEventArgs { Value = "30" + Sep + "40.5", Sequence = 1 });

        Assert.Equal([30d, 40.5d], value);
    }

    [Fact]
    public void CheckboxGroup_checks_the_bound_values_and_toggles_in_item_order()
    {
        IReadOnlyList<int> value = [3];
        LxCheckboxItem<int>[] items = [new(1, "One"), new(2, "Two") { Description = "Second" }, new(3, "Three") { Disabled = true }];
        var cut = Render<LxCheckboxGroup<int>>(p => p.Add(g => g.Items, items).Add(g => g.Legend, "Numbers").Add(g => g.Name, "n")
            .Add(g => g.Value, value).Add(g => g.ValueChanged, (IReadOnlyList<int>? v) => value = v!));

        var boxes = cut.FindAll("input[type=checkbox]");
        Assert.Equal([false, false, true], boxes.Select(b => b.HasAttribute("checked")));
        Assert.All(boxes, b => Assert.Equal("n", b.GetAttribute("name")));
        Assert.True(boxes[2].HasAttribute("disabled"));
        Assert.Equal("Numbers", cut.Find("legend").TextContent);
        Assert.Equal(cut.Find("#" + boxes[1].Id + "-description").Id, boxes[1].GetAttribute("aria-describedby"));

        boxes[1].Change(true);
        Assert.Equal([2, 3], value);
        cut.FindAll("input[type=checkbox]")[0].Change(true);
        Assert.Equal([1, 2, 3], value);
    }

    [Fact]
    public void CheckboxGroup_in_an_EditForm_marks_each_box_invalid()
    {
        var model = new Picks();
        LxCheckboxItem<string>[] items = [new("a", "Alpha"), new("b", "Beta")];
        var cut = RenderForm(model, b =>
        {
            b.OpenComponent<LxCheckboxGroup<string>>(1);
            b.AddAttribute(2, "Items", items);
            b.AddAttribute(3, "Value", model.Values);
            b.AddAttribute(4, "ValueChanged", EventCallback.Factory.Create<IReadOnlyList<string>?>(this, v => model.Values = v ?? []));
            b.AddAttribute(5, "ValueExpression", (Expression<Func<IReadOnlyList<string>?>>)(() => model.Values));
            b.CloseComponent();
        });
        Assert.All(cut.FindAll("input[type=checkbox]"), c => Assert.Equal("model.Values", c.GetAttribute("name")));

        cut.Find("form").Submit();

        Assert.All(cut.FindAll("input[type=checkbox]"), c => Assert.Equal("true", c.GetAttribute("aria-invalid")));
    }

    [Fact]
    public void Toggle_renders_a_pressed_button_and_posts_only_while_pressed()
    {
        var cut = Render<LxToggle>(p => p.Add(t => t.Label, "Bold").Add(t => t.Value, true).Add(t => t.Name, "bold"));

        Assert.Equal("true", cut.Find("button").GetAttribute("aria-pressed"));
        Assert.True(cut.Find("lx-toggle").HasAttribute("pressed"));
        Assert.False(cut.Find("input[type=hidden]").HasAttribute("disabled"));

        var off = Render<LxToggle>(p => p.Add(t => t.Label, "Bold").Add(t => t.Name, "bold"));
        Assert.Equal("false", off.Find("button").GetAttribute("aria-pressed"));
        Assert.True(off.Find("input[type=hidden]").HasAttribute("disabled"));
    }

    [Fact]
    public void Toggle_reports_the_browser_state()
    {
        var value = false;
        var cut = Render<LxToggle>(p => p.Add(t => t.Label, "Bold").Add(t => t.Value, value).Add(t => t.ValueChanged, (bool v) => value = v));

        cut.Find("lx-toggle").TriggerEvent("onbxchange", new LxChangeEventArgs { Value = "true", Sequence = 1 });

        Assert.True(value);
    }

    [Fact]
    public void ToggleGroup_is_a_group_of_pressable_buttons_with_one_tab_stop()
    {
        LxToggleItem[] items = [new("l", "Left"), new("c", "Center"), new("r", "Right") { Disabled = true }];
        var cut = Render<LxToggleGroup>(p => p.Add(g => g.Items, items).Add(g => g.AriaLabel, "Alignment").Add(g => g.Value, ["c"]).Add(g => g.Name, "align"));

        var host = cut.Find("lx-toggle-group");
        Assert.Equal("group", host.GetAttribute("role"));
        Assert.Equal("Alignment", host.GetAttribute("aria-label"));
        var buttons = cut.FindAll("button");
        Assert.Equal(["false", "true", "false"], buttons.Select(b => b.GetAttribute("aria-pressed")));
        Assert.Equal(["-1", "0", "-1"], buttons.Select(b => b.GetAttribute("tabindex")));
        Assert.True(buttons[2].HasAttribute("disabled"));
    }

    [Fact]
    public void Listbox_renders_options_and_a_focusable_listbox()
    {
        var cut = Render<LxListbox>(p => p.Add(l => l.Items, Items).Add(l => l.Multiple, true).Add(l => l.AriaLabel, "Letters")
            .Add(l => l.Value, ["b"]).Add(l => l.Name, "letters"));

        var listbox = cut.Find("[role=listbox]");
        Assert.Equal("0", listbox.GetAttribute("tabindex"));
        Assert.Equal("true", listbox.GetAttribute("aria-multiselectable"));
        Assert.Equal("Letters", listbox.GetAttribute("aria-label"));
        Assert.Equal(["false", "true", "false"], cut.FindAll("[role=option]").Select(o => o.GetAttribute("aria-selected")));
        Assert.Equal("letters", cut.Find("lx-listbox").GetAttribute("name"));
    }

    [Fact]
    public void InputRating_is_a_radiogroup_of_radios_in_reading_order()
    {
        var cut = Render<LxInputRating>(p => p.Add(r => r.Max, 3).Add(r => r.Value, 2).Add(r => r.Name, "stars").Add(r => r.AriaLabel, "Rating"));

        Assert.Equal("radiogroup", cut.Find("[role=radiogroup]").GetAttribute("role"));
        var radios = cut.FindAll("input[type=radio]");
        Assert.Equal(["1", "2", "3"], radios.Select(r => r.GetAttribute("value")));
        Assert.Equal([false, true, false], radios.Select(r => r.HasAttribute("checked")));
        Assert.Equal("2 out of 3", radios[1].GetAttribute("aria-label")); // the middle one either way
        Assert.All(radios, r => Assert.Equal("stars", r.GetAttribute("name")));
    }

    [Fact]
    public void InputRating_with_halves_has_a_radio_per_half_and_reports_the_choice()
    {
        double value = 0;
        var cut = Render<LxInputRating>(p => p.Add(r => r.Max, 2).Add(r => r.AllowHalf, true)
            .Add(r => r.Value, value).Add(r => r.ValueChanged, (double v) => value = v));

        var radios = cut.FindAll("input[type=radio]");
        Assert.Equal(["0.5", "1", "1.5", "2"], radios.Select(r => r.GetAttribute("value")));

        radios[2].Change("1.5");

        Assert.Equal(1.5, value);
    }

    [Fact]
    public void InputRating_readonly_is_an_image_with_a_text_alternative()
    {
        var cut = Render<LxInputRating>(p => p.Add(r => r.Max, 5).Add(r => r.Value, 3.5).Add(r => r.ReadOnly, true).Add(r => r.AllowHalf, true).Add(r => r.Name, "r"));

        Assert.Equal("img", cut.Find("[role=img]").GetAttribute("role"));
        Assert.Equal("3.5 out of 5", cut.Find("[role=img]").GetAttribute("aria-label"));
        Assert.Empty(cut.FindAll("input[type=radio]"));
        Assert.Equal("3.5", cut.Find("input[type=hidden]").GetAttribute("value"));
        Assert.Equal(["full", "full", "full", "half"], cut.FindAll("[data-fill]").Select(f => f.GetAttribute("data-fill")));
    }

    [Fact]
    public void Form_with_a_model_error_lists_it_in_the_summary()
    {
        var model = new NameModel();
        var cut = Render<LxForm>(p => p.Add(f => f.Model, model).Add(f => f.ShowSummary, true)
            .Add(f => f.ChildContent, (EditContext _) => (RenderFragment)(b => { })));

        cut.Find("form").Submit();

        cut.WaitForAssertion(() => Assert.True(cut.FindAll("li").Count > 0, cut.Markup));
        Assert.Equal("Name is required.", cut.Find("li").TextContent);
        Assert.Equal("alert", cut.Find("[data-slot=summary]").GetAttribute("role"));
    }

    [Fact]
    public async Task Form_is_busy_and_disabled_while_the_submit_handler_runs()
    {
        var model = new NameModel { Name = "Ada" };
        var gate = new TaskCompletionSource();
        var cut = Render<LxForm>(p => p.Add(f => f.Model, model)
            .Add(f => f.OnValidSubmit, EventCallback.Factory.Create<EditContext>(this, _ => gate.Task))
            .Add(f => f.ChildContent, (EditContext _) => (RenderFragment)(b => { })));

        Assert.False(cut.Find("fieldset").HasAttribute("disabled"));

        var submit = cut.Find("form").SubmitAsync();
        cut.WaitForAssertion(() => Assert.True(cut.Find("fieldset").HasAttribute("disabled")));
        Assert.Equal("true", cut.Find("form").GetAttribute("aria-busy"));

        gate.SetResult();
        await submit;
        cut.WaitForAssertion(() => Assert.False(cut.Find("fieldset").HasAttribute("disabled")));
    }

    [Fact]
    public void Ui_overrides_and_class_reach_the_slots()
    {
        var cut = Render<LxPinInput>(p => p.Add(i => i.Length, 2).Add(i => i.Class, "my-root").Add(i => i.Ui, new PinInputUi { Cell = "my-cell" }));

        Assert.Contains("my-root", cut.Find("lx-pin-input").ClassList);
        Assert.Contains("my-cell", cut.Find("[data-slot=cell]").ClassList);
    }

    private IRenderedComponent<EditForm> RenderForm(object model, Action<Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder> field) =>
        Render<EditForm>(p => p
            .Add(f => f.Model, model)
            .Add(f => f.ChildContent, (EditContext _) => (RenderFragment)(b =>
            {
                b.OpenComponent<DataAnnotationsValidator>(0);
                b.CloseComponent();
                field(b);
            })));

    private sealed class Picks
    {
        [NotEmpty]
        public IReadOnlyList<string> Values { get; set; } = [];
    }

    private sealed class NotEmptyAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value) => value is System.Collections.IEnumerable items && items.Cast<object>().Any();
    }

    private sealed class NameModel
    {
        [Required(ErrorMessage = "Name is required.")]
        public string? Name { get; set; }
    }
}
