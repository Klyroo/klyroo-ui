using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using AngleSharp.Dom;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Klyroo.UI.Tests;

/// <summary>
/// Controls with custom markup take part in an EditForm through a bridge; they must carry the classes of the
/// context's FieldCssClassProvider (modified, valid, invalid, or the app's own) as InputText does, next to aria-invalid.
/// </summary>
public class FieldCssClassTests : KlyrooTestContext
{
    private sealed class Model
    {
        [MaxLength(1)] public string? Text { get; set; } = "too long";
        [Range(1, 5)] public decimal? Number { get; set; } = 99;
        [Range(1, 5)] public double Slider { get; set; } = 99;
        [Range(typeof(bool), "true", "true")] public bool Flag { get; set; }
        [MinLength(3)] public IReadOnlyList<string> Tags { get; set; } = ["a"];
        [MaxLength(2)] public string? Pin { get; set; } = "12345";
    }

    private sealed class Provider : FieldCssClassProvider
    {
        public override string GetFieldCssClass(EditContext editContext, in FieldIdentifier fieldIdentifier) =>
            editContext.GetValidationMessages(fieldIdentifier).Any() ? "app-bad" : "app-fine";
    }

    private static RenderFragment Control<TValue>(Type component, Model model, Expression<Func<TValue>> expression, Func<TValue> get, Action<TValue> set, object? extra = null) =>
        b =>
        {
            b.OpenComponent(0, component);
            b.AddAttribute(1, "Value", get());
            b.AddAttribute(2, "ValueChanged", EventCallback.Factory.Create<TValue>(model, set));
            b.AddAttribute(3, "ValueExpression", expression);
            if (extra is not null) b.AddAttribute(4, "Debounce", extra);
            b.CloseComponent();
        };

    private (IRenderedComponent<EditForm> Cut, EditContext Context) Render(Model model, RenderFragment content, FieldCssClassProvider? provider = null)
    {
        EditContext? context = null;
        var cut = Render<EditForm>(p => p.Add(f => f.Model, model).Add(f => f.ChildContent, (EditContext c) =>
        {
            context = c;
            if (provider is not null) c.SetFieldCssClassProvider(provider);
            return content;
        }));
        return (cut, context!);
    }

    private static void Validate(IRenderedComponent<EditForm> cut, EditContext context, params Expression<Func<object?>>[] fields)
    {
        cut.InvokeAsync(() =>
        {
            foreach (var field in fields) context.NotifyFieldChanged(FieldIdentifier.Create(field));
            // DataAnnotations of the model, field by field.
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(context.Model, new ValidationContext(context.Model), results, true);
            var store = new ValidationMessageStore(context);
            foreach (var result in results)
            {
                foreach (var name in result.MemberNames) store.Add(context.Field(name), result.ErrorMessage!);
            }
            context.NotifyValidationStateChanged();
        });
    }

    public static TheoryData<string> Controls => new("input", "input-number", "slider", "toggle", "input-tags", "pin-input");

    private (RenderFragment Content, Func<IRenderedComponent<EditForm>, IElement> Target, Expression<Func<object?>> Field) For(string control, Model model) => control switch
    {
        "input" => (Control<string?>(typeof(KlyInput), model, () => model.Text, () => model.Text, v => model.Text = v, 250),
            cut => cut.Find("input"), () => model.Text),
        "input-number" => (Control<decimal?>(typeof(KlyInputNumber), model, () => model.Number, () => model.Number, v => model.Number = v),
            cut => cut.Find("input[data-slot=input]"), () => model.Number),
        "slider" => (Control<double>(typeof(KlySlider), model, () => model.Slider, () => model.Slider, v => model.Slider = v),
            cut => cut.Find("input[type=range]"), () => model.Slider),
        "toggle" => (Control<bool>(typeof(KlyToggle), model, () => model.Flag, () => model.Flag, v => model.Flag = v),
            cut => cut.Find("kly-toggle"), () => model.Flag),
        "input-tags" => (Control<IReadOnlyList<string>>(typeof(KlyInputTags), model, () => model.Tags, () => model.Tags, v => model.Tags = v),
            cut => cut.Find("input[data-slot=input]"), () => model.Tags),
        "pin-input" => (Control<string?>(typeof(KlyPinInput), model, () => model.Pin, () => model.Pin, v => model.Pin = v),
            cut => cut.Find("input[data-slot=cell]"), () => model.Pin),
        _ => throw new ArgumentOutOfRangeException(nameof(control)),
    };

    [Theory]
    [MemberData(nameof(Controls))]
    public void The_default_provider_classes_follow_the_validation_state(string control)
    {
        var model = new Model();
        var (content, target, field) = For(control, model);
        var (cut, context) = Render(model, content);
        Assert.DoesNotContain("modified", target(cut).ClassName ?? "");

        Validate(cut, context, field);

        var classes = (target(cut).ClassName ?? "").Split(' ');
        Assert.Contains("modified", classes);
        Assert.Contains("invalid", classes);
        Assert.DoesNotContain("valid", classes);
    }

    [Theory]
    [MemberData(nameof(Controls))]
    public void A_custom_provider_decides_the_classes(string control)
    {
        var model = new Model();
        var (content, target, field) = For(control, model);
        var (cut, context) = Render(model, content, new Provider());
        Assert.Contains("app-fine", (target(cut).ClassName ?? "").Split(' '));

        Validate(cut, context, field);

        var classes = (target(cut).ClassName ?? "").Split(' ');
        Assert.Contains("app-bad", classes);
        Assert.DoesNotContain("app-fine", classes);
        Assert.DoesNotContain("invalid", classes);
    }

    [Fact]
    public void The_debounced_input_keeps_aria_invalid_next_to_the_classes()
    {
        var model = new Model();
        var (content, target, field) = For("input", model);
        var (cut, context) = Render(model, content);

        Validate(cut, context, field);

        Assert.Equal("true", target(cut).GetAttribute("aria-invalid"));
    }
}
