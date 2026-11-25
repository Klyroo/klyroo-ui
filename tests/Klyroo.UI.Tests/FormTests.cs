using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Klyroo.UI.Tests;

public class FormTests : KlyrooTestContext
{
    [Fact]
    public void Input_binds_on_change_and_renders_type_and_placeholder()
    {
        string? value = null;
        var cut = Render<KlyInput>(p => p
            .Add(i => i.Type, "email").Add(i => i.Placeholder, "you@example.com")
            .Add(i => i.Value, value).Add(i => i.ValueChanged, (string? v) => value = v));

        var input = cut.Find("input");
        Assert.Equal("email", input.GetAttribute("type"));
        Assert.Equal("you@example.com", input.GetAttribute("placeholder"));

        input.Change("a@b.c");

        Assert.Equal("a@b.c", value);
    }

    [Fact]
    public void Input_with_leading_icon_gets_the_matching_padding()
    {
        var cut = Render<KlyInput>(p => p.Add(i => i.LeadingIcon, "<svg></svg>"));

        Assert.Contains("ps-9", cut.Find("input").ClassList);
    }

    [Fact]
    public void FormField_labels_and_describes_its_control()
    {
        var cut = Render<KlyFormField>(p => p
            .Add(f => f.Label, "Email").Add(f => f.Description, "We never share it.").Add(f => f.Error, "Required")
            .Add(f => f.ChildContent, (RenderFragment)(b =>
            {
                b.OpenComponent<KlyInput>(0);
                b.CloseComponent();
            })));

        var input = cut.Find("input");
        Assert.Equal(input.Id, cut.Find("label").GetAttribute("for"));
        Assert.Equal("true", input.GetAttribute("aria-invalid"));
        var describedBy = input.GetAttribute("aria-describedby")!.Split(' ');
        Assert.Contains(cut.Find("[data-slot=error]").Id, describedBy);
        Assert.Equal(2, describedBy.Length);
    }

    [Fact]
    public void FormField_shows_validation_messages_of_its_field()
    {
        var model = new Account();
        var cut = Render<EditForm>(p => p
            .Add(f => f.Model, model)
            .Add(f => f.ChildContent, (EditContext _) => (RenderFragment)(b =>
            {
                b.OpenComponent<DataAnnotationsValidator>(0);
                b.CloseComponent();
                b.OpenComponent<KlyFormField>(1);
                b.AddAttribute(2, nameof(KlyFormField.Label), "Email");
                b.AddAttribute(3, nameof(KlyFormField.For), (Expression<Func<object?>>)(() => model.Email));
                b.AddAttribute(4, nameof(KlyFormField.ChildContent), (RenderFragment)(c =>
                {
                    c.OpenComponent<KlyInput>(0);
                    c.AddAttribute(1, nameof(KlyInput.Value), model.Email);
                    c.AddAttribute(2, nameof(KlyInput.ValueChanged), EventCallback.Factory.Create<string?>(this, v => model.Email = v));
                    c.AddAttribute(3, nameof(KlyInput.ValueExpression), (Expression<Func<string?>>)(() => model.Email));
                    c.CloseComponent();
                }));
                b.CloseComponent();
            })));

        cut.Find("form").Submit();

        Assert.Equal("The Email field is required.", cut.Find("[data-slot=error]").TextContent);
        Assert.Equal("model.Email", cut.Find("input").GetAttribute("name"));
        Assert.Equal("true", cut.Find("input").GetAttribute("aria-invalid"));
    }

    [Fact]
    public void Checkbox_standalone_posts_true_and_binds()
    {
        var value = false;
        var cut = Render<KlyCheckbox>(p => p.Add(c => c.Name, "terms").Add(c => c.Label, "Accept")
            .Add(c => c.Value, value).Add(c => c.ValueChanged, (bool v) => value = v));

        var input = cut.Find("input[type=checkbox]");
        Assert.Equal("terms", input.GetAttribute("name"));
        input.Change(true);
        Assert.True(value);
    }

    [Fact]
    public void Checkbox_renders_data_indeterminate_and_a_minus_only_while_indeterminate()
    {
        var plain = Render<KlyCheckbox>(p => p.Add(c => c.Label, "A"));
        Assert.False(plain.Find("input").HasAttribute("data-indeterminate"));
        Assert.Single(plain.FindAll("svg"));

        var mixed = Render<KlyCheckbox>(p => p.Add(c => c.Label, "A").Add(c => c.Indeterminate, true));
        Assert.True(mixed.Find("input").HasAttribute("data-indeterminate"));
        Assert.Equal(2, mixed.FindAll("svg").Count);
        Assert.Contains("peer-indeterminate:opacity-100", mixed.FindAll("svg")[1].GetAttribute("class"));
        Assert.Contains("peer-indeterminate:opacity-0", mixed.FindAll("svg")[0].GetAttribute("class"));
        Assert.Contains("indeterminate:bg-(--kly-color)", mixed.Find("input").GetAttribute("class"));
    }

    [Fact]
    public void Checkbox_changes_data_indeterminate_on_every_render_so_the_script_sets_the_property_again()
    {
        var value = false;
        var cut = Render<KlyCheckbox>(p => p.Add(c => c.Indeterminate, true)
            .Add(c => c.Value, value).Add(c => c.ValueChanged, (bool v) => value = v));
        var first = cut.Find("input").GetAttribute("data-indeterminate");

        cut.Find("input").Change(true);
        var afterClick = cut.Find("input").GetAttribute("data-indeterminate");
        cut.Render(p => p.Add(c => c.Indeterminate, true).Add(c => c.Value, true));
        var afterParent = cut.Find("input").GetAttribute("data-indeterminate");

        Assert.True(value);
        Assert.NotEqual(first, afterClick);
        Assert.NotEqual(afterClick, afterParent);
    }

    [Fact]
    public void Checkbox_Ui_overrides_the_indeterminate_icon_slot()
    {
        var cut = Render<KlyCheckbox>(p => p.Add(c => c.Indeterminate, true)
            .Add(c => c.Ui, new CheckboxUi { IndeterminateIcon = "text-red-500" }));

        Assert.Contains("text-red-500", cut.FindAll("svg")[1].GetAttribute("class"));
    }

    [Fact]
    public void RadioGroup_selects_the_bound_item_and_reports_changes()
    {
        var value = "b";
        KlyRadioItem<string>[] items = [new("a", "Alpha"), new("b", "Beta") { Description = "Second" }];
        var cut = Render<KlyRadioGroup<string>>(p => p
            .Add(r => r.Items, items).Add(r => r.Legend, "Pick").Add(r => r.Name, "letter")
            .Add(r => r.Value, value).Add(r => r.ValueChanged, (string? v) => value = v!));

        var radios = cut.FindAll("input[type=radio]");
        Assert.False(radios[0].HasAttribute("checked"));
        Assert.True(radios[1].HasAttribute("checked"));
        Assert.All(radios, r => Assert.Equal("letter", r.GetAttribute("name")));
        Assert.Equal("Pick", cut.Find("legend").TextContent);

        radios[0].Change("a");

        Assert.Equal("a", value);
    }

    [Fact]
    public void Select_renders_combobox_hidden_value_and_options()
    {
        KlySelectItem[] items = [new("fr", "France"), new("de", "Germany") { Disabled = true }];
        var cut = Render<KlySelect>(p => p.Add(s => s.Items, items).Add(s => s.Value, "fr").Add(s => s.Name, "country"));

        var trigger = cut.Find("[role=combobox]");
        Assert.Equal("button", trigger.TagName.ToLowerInvariant());
        Assert.Equal("France", cut.Find("[data-slot=label]").GetAttribute("data-label"));
        var hidden = cut.Find("input[type=hidden]");
        Assert.Equal("country", hidden.GetAttribute("name"));
        Assert.Equal("fr", hidden.GetAttribute("value"));
        var options = cut.FindAll("[role=option]");
        Assert.Equal("true", options[0].GetAttribute("aria-selected"));
        Assert.Equal("true", options[1].GetAttribute("aria-disabled"));
    }

    [Fact]
    public void Select_shows_placeholder_and_updates_bound_value_from_browser()
    {
        string? value = null;
        KlySelectItem[] items = [new("fr", "France"), new("de", "Germany")];
        var cut = Render<KlySelect>(p => p.Add(s => s.Items, items).Add(s => s.Placeholder, "Choose")
            .Add(s => s.Value, value).Add(s => s.ValueChanged, (string? v) => value = v));

        var label = cut.Find("[data-slot=label]");
        Assert.Equal("Choose", label.GetAttribute("data-label"));
        Assert.True(label.HasAttribute("data-placeholder"));

        cut.Find("kly-select").TriggerEvent("onklychange", new KlyChangeEventArgs { Value = "de", Sequence = 1 });

        Assert.Equal("de", value);
        Assert.Equal("1", cut.Find("kly-select").GetAttribute("data-kly-seq"));
    }

    [Fact]
    public void RadioGroup_in_an_EditForm_reports_validation_on_the_group()
    {
        var model = new Plan();
        KlyRadioItem<string>[] items = [new("free", "Free"), new("pro", "Pro")];
        var cut = Render<EditForm>(p => p
            .Add(f => f.Model, model)
            .Add(f => f.ChildContent, (EditContext _) => (RenderFragment)(b =>
            {
                b.OpenComponent<DataAnnotationsValidator>(0);
                b.CloseComponent();
                b.OpenComponent<KlyRadioGroup<string>>(1);
                b.AddAttribute(2, "Items", items);
                b.AddAttribute(3, "Legend", "Plan");
                b.AddAttribute(4, "Value", model.Choice);
                b.AddAttribute(5, "ValueChanged", EventCallback.Factory.Create<string?>(this, v => model.Choice = v));
                b.AddAttribute(6, "ValueExpression", (Expression<Func<string?>>)(() => model.Choice));
                b.CloseComponent();
            })));
        var group = cut.Find("fieldset");
        Assert.Equal("radiogroup", group.GetAttribute("role"));
        Assert.False(group.HasAttribute("aria-invalid"));

        cut.Find("form").Submit();

        Assert.Equal("true", cut.Find("fieldset").GetAttribute("aria-invalid"));
        Assert.All(cut.FindAll("input[type=radio]"), r => Assert.Equal("model.Choice", r.GetAttribute("name")));
    }

    private sealed class Plan
    {
        [Required]
        public string? Choice { get; set; }
    }

    private sealed class Account
    {
        [Required]
        public string? Email { get; set; }
    }
}
