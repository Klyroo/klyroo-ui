using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Klyroo.UI.Tests;

/// <summary>KlyInput Debounce and ClearOnEscape: what .NET renders for the browser script, and how it takes the reports.</summary>
public class InputCommitTests : KlyrooTestContext
{
    [Fact]
    public void Without_Debounce_or_ClearOnEscape_the_plain_input_is_rendered_unchanged()
    {
        string? value = null;
        var cut = Render<KlyInput>(p => p.Add(i => i.Value, value).Add(i => i.ValueChanged, (string? v) => value = v));

        var input = cut.Find("input");
        Assert.False(input.HasAttribute("data-kly-debounce"));
        Assert.False(input.HasAttribute("data-kly-clear-on-escape"));
        Assert.False(input.HasAttribute("data-kly-seq"));
        input.Change("typed");
        Assert.Equal("typed", value);
    }

    [Fact]
    public void Debounce_renders_the_delay_and_the_sequence_before_the_value()
    {
        var cut = Render<KlyInput>(p => p.Add(i => i.Debounce, 300).Add(i => i.Value, "abc")
            .Add(i => i.ValueChanged, (string? _) => { }));

        var input = cut.Find("input");
        Assert.Equal("300", input.GetAttribute("data-kly-debounce"));
        Assert.Equal("0", input.GetAttribute("data-kly-seq"));
        Assert.False(input.HasAttribute("data-kly-clear-on-escape"));
        Assert.Equal("abc", input.GetAttribute("value"));
        // Blazor applies attributes in order: the script has to know the sequence of a render before it sees its value.
        var names = input.Attributes.Select(a => a.Name).ToList();
        Assert.True(names.IndexOf("data-kly-seq") < names.IndexOf("value"), string.Join(",", names));
    }

    [Fact]
    public void Debounce_zero_and_negative_values_are_rendered_as_zero_and_Immediate_alone_reports_on_every_input()
    {
        Assert.Equal("0", Render<KlyInput>(p => p.Add(i => i.Debounce, 0)).Find("input").GetAttribute("data-kly-debounce"));
        Assert.Equal("0", Render<KlyInput>(p => p.Add(i => i.Debounce, -5)).Find("input").GetAttribute("data-kly-debounce"));
        var immediate = Render<KlyInput>(p => p.Add(i => i.ClearOnEscape, true).Add(i => i.Immediate, true));
        Assert.Equal("0", immediate.Find("input").GetAttribute("data-kly-debounce"));
        Assert.Equal("500", Render<KlyInput>(p => p.Add(i => i.Debounce, 500).Add(i => i.Immediate, true)).Find("input").GetAttribute("data-kly-debounce"));
    }

    [Fact]
    public void ClearOnEscape_alone_marks_the_input_and_reports_on_commit_only()
    {
        var cut = Render<KlyInput>(p => p.Add(i => i.ClearOnEscape, true));

        var input = cut.Find("input");
        Assert.True(input.HasAttribute("data-kly-clear-on-escape"));
        Assert.False(input.HasAttribute("data-kly-debounce"));
    }

    [Fact]
    public async Task A_report_sets_the_value_once_and_the_render_gives_the_sequence_back()
    {
        var reports = new List<string?>();
        var cut = Render<KlyInput>(p => p.Add(i => i.Debounce, 300).Add(i => i.Value, "")
            .Add(i => i.ValueChanged, (string? v) => reports.Add(v)));

        await cut.Find("input").TriggerEventAsync("onklychange", new KlyChangeEventArgs { Value = "hello", Sequence = 1 });

        Assert.Equal(["hello"], reports);
        Assert.Equal("1", cut.Find("input").GetAttribute("data-kly-seq"));
        Assert.Equal("hello", cut.Find("input").GetAttribute("value"));

        // The same text reported again (after Escape on an empty field, say) raises nothing but still acknowledges.
        await cut.Find("input").TriggerEventAsync("onklychange", new KlyChangeEventArgs { Value = "hello", Sequence = 2 });
        Assert.Equal(["hello"], reports);
        Assert.Equal("2", cut.Find("input").GetAttribute("data-kly-seq"));
    }

    [Fact]
    public async Task Clearing_reports_the_empty_value()
    {
        var reports = new List<string?>();
        var cut = Render<KlyInput>(p => p.Add(i => i.ClearOnEscape, true).Add(i => i.Value, "text")
            .Add(i => i.ValueChanged, (string? v) => reports.Add(v)));

        await cut.Find("input").TriggerEventAsync("onklychange", new KlyChangeEventArgs { Value = "", Sequence = 1 });

        Assert.Equal([""], reports);
    }

    [Fact]
    public void An_unbound_debounced_input_renders_no_sequence_and_keeps_its_name()
    {
        var cut = Render<KlyInput>(p => p.Add(i => i.Debounce, 200).Add(i => i.Name, "q"));

        var input = cut.Find("input");
        Assert.Equal("q", input.GetAttribute("name"));
        Assert.False(input.HasAttribute("data-kly-seq"));
    }

    [Fact]
    public async Task In_an_EditForm_a_debounced_report_sets_the_model_and_validates()
    {
        var model = new Account();
        var cut = Render<EditForm>(p => p
            .Add(f => f.Model, model)
            .Add(f => f.ChildContent, (EditContext _) => (RenderFragment)(b =>
            {
                b.OpenComponent<DataAnnotationsValidator>(0);
                b.CloseComponent();
                b.OpenComponent<KlyInput>(1);
                b.AddAttribute(2, nameof(KlyInput.Debounce), 250);
                b.AddAttribute(3, nameof(KlyInput.Value), model.Email);
                b.AddAttribute(4, nameof(KlyInput.ValueChanged), EventCallback.Factory.Create<string?>(this, v => model.Email = v));
                b.AddAttribute(5, nameof(KlyInput.ValueExpression), (Expression<Func<string?>>)(() => model.Email));
                b.CloseComponent();
            })));

        Assert.Equal("model.Email", cut.Find("input").GetAttribute("name"));
        Assert.Equal("250", cut.Find("input").GetAttribute("data-kly-debounce"));

        await cut.Find("input").TriggerEventAsync("onklychange", new KlyChangeEventArgs { Value = "a@b.c", Sequence = 1 });

        Assert.Equal("a@b.c", model.Email);
        cut.Find("form").Submit();
        Assert.Empty(cut.FindAll("[aria-invalid=true]"));
    }

    [Fact]
    public async Task SetValueAsync_renders_a_force_marker_with_the_value_reports_it_once_and_raises_the_sequence_floor()
    {
        var reports = new List<string?>();
        var cut = Render<KlyInput>(p => p.Add(i => i.Debounce, 300).Add(i => i.Value, "old")
            .Add(i => i.ValueChanged, (string? v) => reports.Add(v)));
        Assert.False(cut.Find("input").HasAttribute("data-kly-force"));

        await cut.InvokeAsync(() => cut.Instance.SetValueAsync("new").AsTask());

        var input = cut.Find("input");
        Assert.Equal(["new"], reports);
        Assert.Equal("1", input.GetAttribute("data-kly-force"));
        Assert.Equal("new", input.GetAttribute("data-kly-set"));
        var floor = int.Parse(input.GetAttribute("data-kly-seq")!);
        Assert.True(floor >= 65536);
        var names = input.Attributes.Select(a => a.Name).ToList();
        Assert.True(names.IndexOf("data-kly-force") < names.IndexOf("value"), string.Join(",", names));

        // A report the browser sent before the call and .NET handles after it is dropped; one from the new sequence counts.
        await input.TriggerEventAsync("onklychange", new KlyChangeEventArgs { Value = "typed before", Sequence = 7 });
        Assert.Equal(["new"], reports);
        await cut.Find("input").TriggerEventAsync("onklychange", new KlyChangeEventArgs { Value = "typed after", Sequence = floor + 1 });
        Assert.Equal(["new", "typed after"], reports);

        // Setting the same text again still cancels what is pending in the browser: the marker changes.
        await cut.InvokeAsync(() => cut.Instance.SetValueAsync("typed after").AsTask());
        Assert.Equal("2", cut.Find("input").GetAttribute("data-kly-force"));
        Assert.Equal(2, reports.Count);
    }

    [Fact]
    public async Task SetValueAsync_on_a_plain_input_only_sets_the_value()
    {
        string? value = "a";
        var cut = Render<KlyInput>(p => p.Add(i => i.Value, value).Add(i => i.ValueChanged, (string? v) => value = v));

        await cut.InvokeAsync(() => cut.Instance.SetValueAsync("b").AsTask());

        Assert.Equal("b", value);
        Assert.Equal("b", cut.Find("input").GetAttribute("value"));
        Assert.False(cut.Find("input").HasAttribute("data-kly-force"));
    }

    private sealed class Account
    {
        [Required]
        public string? Email { get; set; }
    }
}
