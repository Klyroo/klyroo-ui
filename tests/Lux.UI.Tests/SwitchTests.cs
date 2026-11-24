using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Lux.UI.Tests;

public class SwitchTests : BluxTestContext
{
    [Fact]
    public void Standalone_renders_a_checkbox_with_switch_role()
    {
        var cut = Render<BxSwitch>(p => p.Add(s => s.Value, true).Add(s => s.Name, "notify").Add(s => s.Label, "Notify me"));

        var input = cut.Find("input");
        Assert.Equal("checkbox", input.GetAttribute("type"));
        Assert.Equal("switch", input.GetAttribute("role"));
        Assert.Equal("notify", input.GetAttribute("name"));
        Assert.Equal("true", input.GetAttribute("value"));
        Assert.True(input.HasAttribute("checked"));
        Assert.Equal(input.Id, cut.Find("label[for]").GetAttribute("for"));
    }

    [Fact]
    public void Change_updates_the_bound_value()
    {
        var value = false;
        var cut = Render<BxSwitch>(p => p
            .Add(s => s.Value, value)
            .Add(s => s.ValueChanged, (bool v) => value = v));

        cut.Find("input").Change(true);

        Assert.True(value);
    }

    [Fact]
    public void Unbound_switch_attaches_no_change_handler()
    {
        var cut = Render<BxSwitch>(p => p.Add(s => s.Value, true));

        Assert.Throws<MissingEventHandlerException>(() => cut.Find("input").Change(false));
    }

    [Fact]
    public void Description_is_linked_to_the_input()
    {
        var cut = Render<BxSwitch>(p => p.Add(s => s.Description, "Sent weekly"));

        var describedBy = cut.Find("input").GetAttribute("aria-describedby");
        Assert.Equal("Sent weekly", cut.Find($"#{describedBy}").TextContent);
    }

    [Fact]
    public void Disabled_marks_input_and_root()
    {
        var cut = Render<BxSwitch>(p => p.Add(s => s.Disabled, true));

        Assert.True(cut.Find("input").HasAttribute("disabled"));
        Assert.True(cut.Find("div").HasAttribute("data-disabled"));
    }

    [Fact]
    public void Inside_an_EditForm_posts_under_the_bound_field_name_and_reports_validation()
    {
        var model = new Settings();
        var cut = Render<EditForm>(p => p
            .Add(f => f.Model, model)
            .Add(f => f.ChildContent, (EditContext _) => (RenderFragment)(b =>
            {
                b.OpenComponent<DataAnnotationsValidator>(0);
                b.CloseComponent();
                b.OpenComponent<BxSwitch>(1);
                b.AddAttribute(2, nameof(BxSwitch.Value), model.AcceptTerms);
                b.AddAttribute(3, nameof(BxSwitch.ValueChanged), EventCallback.Factory.Create<bool>(this, v => model.AcceptTerms = v));
                b.AddAttribute(4, nameof(BxSwitch.ValueExpression), (System.Linq.Expressions.Expression<Func<bool>>)(() => model.AcceptTerms));
                b.CloseComponent();
            })));

        Assert.Equal("model.AcceptTerms", cut.Find("input").GetAttribute("name"));

        cut.Find("form").Submit();

        Assert.Equal("true", cut.Find("input").GetAttribute("aria-invalid"));
    }

    private sealed class Settings
    {
        [Range(typeof(bool), "true", "true")]
        public bool AcceptTerms { get; set; }
    }
}
