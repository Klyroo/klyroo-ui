using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Klyroo.UI.Tests;

/// <summary>
/// The inputs that bind to the type of the model property: select, select menu, input menu and listbox to an int, an enum, a Guid or a date, the number field to
/// any numeric type, the date field to DateOnly, DateTime and DateTimeOffset, the slider to any numeric type.
/// </summary>
public class GenericInputsTests : KlyrooTestContext
{
    private enum Status { Draft, InProgress, Done }

    private sealed record Category(int Id, string Name);

    private sealed class Order
    {
        [Range(1, int.MaxValue, ErrorMessage = "Choose a category.")]
        public int CategoryId { get; set; }

        public int? ParentId { get; set; }

        public Status Status { get; set; }

        [Range(1, 100)]
        public int Quantity { get; set; }

        public DateTime Due { get; set; }

        public DateTimeOffset? Shipped { get; set; }
    }

    private static readonly Category[] Categories = [new(1, "Books"), new(2, "Games"), new(3, "Music")];

    // ---- Items

    [Fact]
    public void Items_from_an_enum_are_typed_with_the_enum_and_named_in_words()
    {
        var items = KlySelectItems.FromEnum<Status>();

        Assert.Equal([Status.Draft, Status.InProgress, Status.Done], items.Select(i => i.Value));
        Assert.Equal(["Draft", "In progress", "Done"], items.Select(i => i.Label));
        Assert.Equal(["DRAFT", "IN_PROGRESS", "DONE"], KlySelectItems.FromEnum<Status>(s => s.ToString().ToUpperInvariant().Replace("INPROGRESS", "IN_PROGRESS")).Select(i => i.Label));
    }

    [Fact]
    public void Items_from_rows_take_the_key_as_the_value_and_a_column_as_the_label()
    {
        var items = KlySelectItems.From(Categories, c => c.Id, c => c.Name);

        Assert.Equal([1, 2, 3], items.Select(i => i.Value));
        Assert.Equal(["Books", "Games", "Music"], items.Select(i => i.Label));
    }

    [Fact]
    public void AsNullable_keeps_the_extras_of_each_item()
    {
        var items = new[] { new KlySelectItem<int>(1, "One") { Disabled = true, Count = 3, Icon = "star", Group = "g" } }.AsNullable();

        var item = Assert.Single(items);
        Assert.Equal(1, item.Value);
        Assert.True(item.Disabled);
        Assert.Equal(3, item.Count);
        Assert.Equal("star", item.Icon);
        Assert.Equal("g", item.Group);
    }

    [Fact]
    public void The_item_of_strings_is_still_built_with_two_arguments_and_is_a_typed_item()
    {
        KlySelectItem<string> typed = new KlySelectItem("a", "A") { Disabled = true };

        Assert.Equal("a", typed.Value);
        Assert.True(typed.Disabled);
    }

    // ---- KlySelect

    [Fact]
    public void Select_binds_an_int_foreign_key()
    {
        var id = 2;
        var cut = Render<KlySelect<int>>(p => p
            .Add(s => s.Items, KlySelectItems.From(Categories, c => c.Id, c => c.Name))
            .Add(s => s.Value, id).Add(s => s.ValueChanged, (int v) => id = v).Add(s => s.Name, "category"));

        Assert.Equal("Games", cut.Find("[data-slot=label]").GetAttribute("data-label"));
        Assert.Equal("2", cut.Find("input[type=hidden]").GetAttribute("value"));
        Assert.Equal(["1", "2", "3"], cut.FindAll("[role=option]").Select(o => o.GetAttribute("data-value")!));
        Assert.Equal("true", cut.FindAll("[role=option]")[1].GetAttribute("aria-selected"));

        cut.Find("kly-select").TriggerEvent("onklychange", new KlyChangeEventArgs { Value = "3", Sequence = 1 });

        Assert.Equal(3, id);
    }

    [Fact]
    public void Select_binds_an_enum_by_name()
    {
        var status = Status.Draft;
        var cut = Render<KlySelect<Status>>(p => p
            .Add(s => s.Items, KlySelectItems.FromEnum<Status>())
            .Add(s => s.Value, status).Add(s => s.ValueChanged, (Status v) => status = v));

        Assert.Equal(["Draft", "InProgress", "Done"], cut.FindAll("[role=option]").Select(o => o.GetAttribute("data-value")!));

        cut.Find("kly-select").TriggerEvent("onklychange", new KlyChangeEventArgs { Value = "InProgress", Sequence = 1 });

        Assert.Equal(Status.InProgress, status);
    }

    [Fact]
    public void Select_binds_a_guid()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        Guid value = second;
        var cut = Render<KlySelect<Guid>>(p => p
            .Add(s => s.Items, [new KlySelectItem<Guid>(first, "First"), new KlySelectItem<Guid>(second, "Second")])
            .Add(s => s.Value, value).Add(s => s.ValueChanged, (Guid v) => value = v));

        Assert.Equal("Second", cut.Find("[data-slot=label]").GetAttribute("data-label"));
        Assert.Equal(second.ToString(), cut.Find("input[type=hidden]").GetAttribute("value"));

        cut.Find("kly-select").TriggerEvent("onklychange", new KlyChangeEventArgs { Value = first.ToString(), Sequence = 1 });

        Assert.Equal(first, value);
    }

    [Fact]
    public void Select_binds_a_date_time_and_keeps_the_exact_instant()
    {
        var january = new DateTime(2026, 1, 15, 8, 30, 0, DateTimeKind.Utc);
        var june = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime? value = january;
        var cut = Render<KlySelect<DateTime?>>(p => p
            .Add(s => s.Items, [new KlySelectItem<DateTime?>(january, "January"), new KlySelectItem<DateTime?>(june, "June")])
            .Add(s => s.Value, value).Add(s => s.ValueChanged, (DateTime? v) => value = v));

        Assert.Equal("2026-01-15T08:30:00.0000000Z", cut.Find("input[type=hidden]").GetAttribute("value"));

        cut.Find("kly-select").TriggerEvent("onklychange", new KlyChangeEventArgs { Value = "2026-06-01T00:00:00.0000000Z", Sequence = 1 });

        Assert.Equal(june, value);
    }

    [Fact]
    public void Select_of_a_nullable_int_clears_to_null_and_shows_the_placeholder()
    {
        int? parent = 1;
        var cut = Render<KlySelect<int?>>(p => p
            .Add(s => s.Items, KlySelectItems.From(Categories, c => c.Id, c => c.Name).AsNullable())
            .Add(s => s.Placeholder, "None").Add(s => s.Value, parent).Add(s => s.ValueChanged, (int? v) => parent = v));

        cut.Find("kly-select").TriggerEvent("onklychange", new KlyChangeEventArgs { Value = "", Sequence = 1 });

        Assert.Null(parent);
    }

    [Fact]
    public void Select_of_a_string_property_that_may_be_null_takes_the_items_of_strings()
    {
        string? country = null;
        KlySelectItem[] items = [new("fr", "France"), new("de", "Germany")];
        var cut = Render<KlySelect<string?>>(p => p.Add(s => s.Items, items).Add(s => s.Value, country).Add(s => s.ValueChanged, (string? v) => country = v));

        cut.Find("kly-select").TriggerEvent("onklychange", new KlyChangeEventArgs { Value = "de", Sequence = 1 });

        Assert.Equal("de", country);
    }

    [Fact]
    public void Select_in_an_EditForm_validates_the_real_property_and_posts_under_its_name()
    {
        var model = new Order();
        var cut = Render<EditForm>(p => p
            .Add(f => f.Model, model)
            .Add(f => f.ChildContent, (EditContext _) => (RenderFragment)(b =>
            {
                b.OpenComponent<DataAnnotationsValidator>(0);
                b.CloseComponent();
                b.OpenComponent<KlySelect<int>>(1);
                b.AddAttribute(2, "Items", KlySelectItems.From(Categories, c => c.Id, c => c.Name));
                b.AddAttribute(3, "Value", model.CategoryId);
                b.AddAttribute(4, "ValueChanged", EventCallback.Factory.Create<int>(this, v => model.CategoryId = v));
                b.AddAttribute(5, "ValueExpression", (Expression<Func<int>>)(() => model.CategoryId));
                b.CloseComponent();
            })));

        Assert.Equal("model.CategoryId", cut.Find("input[type=hidden]").GetAttribute("name"));
        Assert.False(cut.Find("[role=combobox]").HasAttribute("aria-invalid"));

        cut.Find("form").Submit();

        Assert.Equal("true", cut.Find("[role=combobox]").GetAttribute("aria-invalid"));

        cut.Find("kly-select").TriggerEvent("onklychange", new KlyChangeEventArgs { Value = "2", Sequence = 1 });

        Assert.Equal(2, model.CategoryId);
        Assert.False(cut.Find("[role=combobox]").HasAttribute("aria-invalid"));
    }

    [Fact]
    public void Select_in_an_EditForm_binds_an_enum_property()
    {
        var model = new Order();
        var cut = Render<EditForm>(p => p
            .Add(f => f.Model, model)
            .Add(f => f.ChildContent, (EditContext _) => (RenderFragment)(b =>
            {
                b.OpenComponent<KlySelect<Status>>(0);
                b.AddAttribute(1, "Items", KlySelectItems.FromEnum<Status>());
                b.AddAttribute(2, "Value", model.Status);
                b.AddAttribute(3, "ValueChanged", EventCallback.Factory.Create<Status>(this, v => model.Status = v));
                b.AddAttribute(4, "ValueExpression", (Expression<Func<Status>>)(() => model.Status));
                b.CloseComponent();
            })));

        cut.Find("kly-select").TriggerEvent("onklychange", new KlyChangeEventArgs { Value = "Done", Sequence = 1 });

        Assert.Equal(Status.Done, model.Status);
        Assert.Equal("model.Status", cut.Find("input[type=hidden]").GetAttribute("name"));
    }

    // ---- KlySelectMenu

    [Fact]
    public void SelectMenu_binds_a_list_of_ints()
    {
        IReadOnlyList<int> ids = [1, 2];
        var cut = Render<KlySelectMenu<int>>(p => p
            .Add(s => s.Items, KlySelectItems.From(Categories, c => c.Id, c => c.Name))
            .Add(s => s.Multiple, true).Add(s => s.Value, ids).Add(s => s.ValueChanged, (IReadOnlyList<int>? v) => ids = v!));

        Assert.Equal("1\u001f2", cut.Find("kly-select-menu").GetAttribute("data-values"));
        Assert.Equal("Books, Games", cut.Find("[data-slot=label]").GetAttribute("data-label"));
        Assert.Equal(["true", "true", "false"], cut.FindAll("[role=option]").Select(o => o.GetAttribute("aria-selected")!));

        cut.Find("kly-select-menu").TriggerEvent("onklychange", new KlyChangeEventArgs { Value = "2\u001f3", Sequence = 1 });

        Assert.Equal([2, 3], ids);
    }

    [Fact]
    public void SelectMenu_Create_needs_string_values()
    {
        var error = Assert.ThrowsAny<InvalidOperationException>(() => Render<KlySelectMenu<int>>(p => p
            .Add(s => s.Items, KlySelectItems.From(Categories, c => c.Id, c => c.Name)).Add(s => s.Create, true)));

        Assert.Contains("string", error.Message);
    }

    [Fact]
    public async Task SelectMenu_loads_int_options_from_a_provider_and_reports_the_chosen_int()
    {
        IReadOnlyList<int> ids = [];
        var cut = Render<KlySelectMenu<int>>(p => p
            .Add(s => s.OptionsProvider, (KlyOptionsRequest _, CancellationToken _) =>
                new ValueTask<KlyOptionsResult>(new KlyOptionsResult([.. Categories.Select(c => new KlySelectItem<int>(c.Id, c.Name))])))
            .Add(s => s.Value, ids).Add(s => s.ValueChanged, (IReadOnlyList<int>? v) => ids = v!));

        await cut.Find("kly-select-menu").TriggerEventAsync("onklysearch", new KlySearchEventArgs { Value = "", Sequence = 1 });

        Assert.Equal(["1", "2", "3"], cut.FindAll("[role=option]").Select(o => o.GetAttribute("data-value")!));

        await cut.Find("kly-select-menu").TriggerEventAsync("onklychange", new KlyChangeEventArgs { Value = "3", Sequence = 1 });

        Assert.Equal([3], ids);
    }

    [Fact]
    public async Task SelectMenu_resolves_the_label_of_an_int_that_was_never_listed()
    {
        var cut = Render<KlySelectMenu<int>>(p => p
            .Add(s => s.OptionsProvider, (KlyOptionsRequest _, CancellationToken _) => new ValueTask<KlyOptionsResult>(new KlyOptionsResult([])))
            .Add(s => s.ResolveSelected, (IReadOnlyList<string> keys, CancellationToken _) =>
                new ValueTask<IReadOnlyList<KlySelectItem>>(keys.Select(k => new KlySelectItem(k, "Category " + k)).ToList()))
            .Add(s => s.Value, [42]).Add(s => s.ValueChanged, (IReadOnlyList<int>? _) => { }));

        await cut.WaitForAssertionAsync(() => Assert.Equal("Category 42", cut.Find("[data-slot=label]").GetAttribute("data-label")));
    }

    [Fact]
    public async Task A_remote_option_of_the_wrong_type_is_said_plainly()
    {
        var cut = Render<KlySelectMenu<int>>(p => p
            .Add(s => s.OptionsProvider, (KlyOptionsRequest _, CancellationToken _) => new ValueTask<KlyOptionsResult>(new KlyOptionsResult([new KlySelectItem("a", "A")])))
            .Add(s => s.Value, []).Add(s => s.ValueChanged, (IReadOnlyList<int>? _) => { }));
        await cut.Find("kly-select-menu").TriggerEventAsync("onklysearch", new KlySearchEventArgs { Value = "", Sequence = 1 });

        var error = await Assert.ThrowsAnyAsync<Exception>(() => cut.Find("kly-select-menu").TriggerEventAsync("onklychange", new KlyChangeEventArgs { Value = "a", Sequence = 1 }));

        Assert.Contains("KlySelectItem<Int32>", error.ToString());
    }

    // ---- KlyListbox

    [Fact]
    public void Listbox_binds_a_list_of_guids()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        IReadOnlyList<Guid> value = [second];
        var cut = Render<KlyListbox<Guid>>(p => p
            .Add(l => l.Items, [new KlySelectItem<Guid>(first, "First"), new KlySelectItem<Guid>(second, "Second")])
            .Add(l => l.Multiple, true).Add(l => l.Value, value).Add(l => l.ValueChanged, (IReadOnlyList<Guid>? v) => value = v!));

        Assert.Equal(["false", "true"], cut.FindAll("[role=option]").Select(o => o.GetAttribute("aria-selected")!));

        cut.Find("kly-listbox").TriggerEvent("onklychange", new KlyChangeEventArgs { Value = $"{first}\u001f{second}", Sequence = 1 });

        Assert.Equal([first, second], value);
    }

    // ---- KlyInputMenu

    [Fact]
    public void InputMenu_of_an_int_is_strict_and_shows_the_label_of_the_value()
    {
        int? id = 3;
        var cut = Render<KlyInputMenu<int?>>(p => p
            .Add(s => s.Items, KlySelectItems.From(Categories, c => c.Id, c => c.Name).AsNullable())
            .Add(s => s.Value, id).Add(s => s.ValueChanged, (int? v) => id = v));

        Assert.True(cut.Find("kly-input-menu").HasAttribute("data-strict"));
        Assert.Equal("Music", cut.Find("input[role=combobox]").GetAttribute("value"));
        Assert.Equal("3", cut.Find("input[type=hidden]").GetAttribute("value"));

        cut.Find("kly-input-menu").TriggerEvent("onklychange", new KlyChangeEventArgs { Value = "1", Sequence = 1 });
        Assert.Equal(1, id);

        cut.Find("kly-input-menu").TriggerEvent("onklychange", new KlyChangeEventArgs { Value = "", Sequence = 2 });
        Assert.Null(id);
    }

    [Fact]
    public void InputMenu_of_strings_still_takes_the_typed_text()
    {
        string? text = null;
        KlySelectItem[] items = [new("fr", "France")];
        var cut = Render<KlyInputMenu<string>>(p => p.Add(s => s.Items, items).Add(s => s.Value, text).Add(s => s.ValueChanged, (string? v) => text = v));

        Assert.False(cut.Find("kly-input-menu").HasAttribute("data-strict"));

        cut.Find("kly-input-menu").TriggerEvent("onklychange", new KlyChangeEventArgs { Value = "Franc", Sequence = 1 });

        Assert.Equal("Franc", text);
    }

    // ---- KlyInputNumber

    [Fact]
    public void InputNumber_binds_an_int()
    {
        var quantity = 5;
        var cut = Render<KlyInputNumber<int>>(p => p.Add(n => n.Value, quantity).Add(n => n.ValueChanged, (int v) => quantity = v).Add(n => n.Min, 0));

        Assert.Equal("5", cut.Find("input[type=hidden]").GetAttribute("value"));
        Assert.Equal("0", cut.Find("kly-input-number").GetAttribute("min"));
        Assert.Equal("numeric", cut.Find("input[role=spinbutton]").GetAttribute("inputmode"));

        cut.Find("kly-input-number").TriggerEvent("onklychange", new KlyChangeEventArgs { Value = "7", Sequence = 1 });
        Assert.Equal(7, quantity);

        cut.Find("kly-input-number").TriggerEvent("onklychange", new KlyChangeEventArgs { Value = "7.6", Sequence = 2 });
        Assert.Equal(8, quantity);

        cut.Find("kly-input-number").TriggerEvent("onklychange", new KlyChangeEventArgs { Value = "", Sequence = 3 });
        Assert.Equal(0, quantity);
    }

    [Fact]
    public void InputNumber_of_a_nullable_int_can_be_empty()
    {
        int? value = 4;
        var cut = Render<KlyInputNumber<int?>>(p => p.Add(n => n.Value, value).Add(n => n.ValueChanged, (int? v) => value = v));

        cut.Find("kly-input-number").TriggerEvent("onklychange", new KlyChangeEventArgs { Value = "", Sequence = 1 });

        Assert.Null(value);
        Assert.Equal("", cut.Find("input[type=hidden]").GetAttribute("value") ?? "");
    }

    [Theory]
    [InlineData("300", 255)]
    [InlineData("-3", 0)]
    [InlineData("12.4", 12)]
    public void InputNumber_of_a_byte_clamps_to_the_type(string text, int expected)
    {
        byte value = 1;
        var cut = Render<KlyInputNumber<byte>>(p => p.Add(n => n.Value, value).Add(n => n.ValueChanged, (byte v) => value = v));

        cut.Find("kly-input-number").TriggerEvent("onklychange", new KlyChangeEventArgs { Value = text, Sequence = 1 });

        Assert.Equal(expected, value);
    }

    [Fact]
    public void InputNumber_keeps_decimal_and_double_values_exact_in_every_culture()
    {
        decimal? price = 12.5m;
        double ratio = 0.25;
        var original = System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("fr-FR");
        try
        {
            var money = Render<KlyInputNumber<decimal?>>(p => p.Add(n => n.Value, price).Add(n => n.ValueChanged, (decimal? v) => price = v).Add(n => n.Step, 0.5m));
            var rate = Render<KlyInputNumber<double>>(p => p.Add(n => n.Value, ratio).Add(n => n.ValueChanged, (double v) => ratio = v));

            Assert.Equal("12.5", money.Find("input[type=hidden]").GetAttribute("value"));
            Assert.Equal("0.5", money.Find("kly-input-number").GetAttribute("step"));
            Assert.Equal("decimal", money.Find("input[role=spinbutton]").GetAttribute("inputmode"));
            money.Find("kly-input-number").TriggerEvent("onklychange", new KlyChangeEventArgs { Value = "13.25", Sequence = 1 });
            rate.Find("kly-input-number").TriggerEvent("onklychange", new KlyChangeEventArgs { Value = "0.75", Sequence = 1 });

            Assert.Equal(13.25m, price);
            Assert.Equal(0.75, ratio);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void InputNumber_of_a_type_that_is_not_a_number_says_which_types_work()
    {
        var error = Assert.ThrowsAny<InvalidOperationException>(() => Render<KlyInputNumber<string>>());

        Assert.Contains("not a number type", error.Message);
        Assert.Contains("decimal", error.Message);
    }

    [Fact]
    public void InputNumber_in_an_EditForm_validates_the_real_property()
    {
        var model = new Order { Quantity = 500 };
        var cut = Render<EditForm>(p => p
            .Add(f => f.Model, model)
            .Add(f => f.ChildContent, (EditContext _) => (RenderFragment)(b =>
            {
                b.OpenComponent<DataAnnotationsValidator>(0);
                b.CloseComponent();
                b.OpenComponent<KlyInputNumber<int>>(1);
                b.AddAttribute(2, "Value", model.Quantity);
                b.AddAttribute(3, "ValueChanged", EventCallback.Factory.Create<int>(this, v => model.Quantity = v));
                b.AddAttribute(4, "ValueExpression", (Expression<Func<int>>)(() => model.Quantity));
                b.CloseComponent();
            })));

        Assert.Equal("model.Quantity", cut.Find("input[type=hidden]").GetAttribute("name"));
        cut.Find("form").Submit();
        Assert.Equal("true", cut.Find("input[role=spinbutton]").GetAttribute("aria-invalid"));

        cut.Find("kly-input-number").TriggerEvent("onklychange", new KlyChangeEventArgs { Value = "50", Sequence = 1 });

        Assert.Equal(50, model.Quantity);
        Assert.False(cut.Find("input[role=spinbutton]").HasAttribute("aria-invalid"));
    }

    // ---- KlyInputDate

    [Fact]
    public void InputDate_binds_a_date_time_at_midnight()
    {
        var due = new DateTime(2026, 11, 26, 14, 30, 0);
        var cut = Render<KlyInputDate<DateTime>>(p => p.Add(d => d.Value, due).Add(d => d.ValueChanged, (DateTime v) => due = v));

        Assert.Equal("2026-11-26", cut.Find("input[type=date]").GetAttribute("value"));

        cut.Find("input[type=date]").Change("2026-12-01");

        Assert.Equal(new DateTime(2026, 12, 1), due);
    }

    [Fact]
    public void InputDate_binds_a_nullable_date_time_offset_and_can_be_cleared()
    {
        DateTimeOffset? shipped = new DateTimeOffset(2026, 3, 4, 0, 0, 0, TimeSpan.FromHours(2));
        var cut = Render<KlyInputDate<DateTimeOffset?>>(p => p.Add(d => d.Value, shipped).Add(d => d.ValueChanged, (DateTimeOffset? v) => shipped = v));

        Assert.Equal("2026-03-04", cut.Find("input[type=date]").GetAttribute("value"));

        cut.Find("input[type=date]").Change("2026-03-09");
        Assert.Equal(new DateOnly(2026, 3, 9), DateOnly.FromDateTime(shipped!.Value.DateTime));

        cut.Find("input[type=date]").Change("");
        Assert.Null(shipped);
    }

    [Fact]
    public void InputDate_of_a_date_only_still_binds_and_limits_the_calendar()
    {
        DateOnly? day = new DateOnly(2026, 1, 2);
        var cut = Render<KlyInputDate<DateOnly?>>(p => p
            .Add(d => d.Value, day).Add(d => d.ValueChanged, (DateOnly? v) => day = v)
            .Add(d => d.Min, new DateOnly(2026, 1, 1)).Add(d => d.Max, new DateOnly(2026, 12, 31)));

        Assert.Equal("2026-01-01", cut.Find("input[type=date]").GetAttribute("min"));
        Assert.Equal("2026-12-31", cut.Find("input[type=date]").GetAttribute("max"));

        cut.Find("input[type=date]").Change("2026-02-03");

        Assert.Equal(new DateOnly(2026, 2, 3), day);
    }

    [Fact]
    public void InputDate_in_an_EditForm_binds_the_DateTime_property_and_posts_under_its_name()
    {
        var model = new Order { Due = new DateTime(2026, 5, 6) };
        var cut = Render<EditForm>(p => p
            .Add(f => f.Model, model)
            .Add(f => f.ChildContent, (EditContext _) => (RenderFragment)(b =>
            {
                b.OpenComponent<KlyInputDate<DateTime>>(0);
                b.AddAttribute(1, "Value", model.Due);
                b.AddAttribute(2, "ValueChanged", EventCallback.Factory.Create<DateTime>(this, v => model.Due = v));
                b.AddAttribute(3, "ValueExpression", (Expression<Func<DateTime>>)(() => model.Due));
                b.CloseComponent();
            })));

        Assert.Equal("model.Due", cut.Find("input[type=date]").GetAttribute("name"));

        cut.Find("input[type=date]").Change("2026-07-08");

        Assert.Equal(new DateTime(2026, 7, 8), model.Due);
    }

    [Fact]
    public void InputDate_of_a_type_that_is_not_a_date_says_which_types_work()
    {
        var error = Assert.ThrowsAny<InvalidOperationException>(() => Render<KlyInputDate<int>>());

        Assert.Contains("not a date type", error.Message);
    }

    // ---- KlySlider

    [Fact]
    public void Slider_binds_an_int()
    {
        var volume = 30;
        var cut = Render<KlySlider<int>>(p => p.Add(s => s.Value, volume).Add(s => s.ValueChanged, (int v) => volume = v));

        Assert.Equal("30", cut.Find("input[type=range]").GetAttribute("value"));

        cut.Find("kly-slider").TriggerEvent("onklychange", new KlyChangeEventArgs { Value = "42", Sequence = 1 });

        Assert.Equal(42, volume);
    }

    [Fact]
    public void Slider_binds_a_float_and_a_double()
    {
        var gain = 0.5f;
        var level = 1.5;
        var floatSlider = Render<KlySlider<float>>(p => p.Add(s => s.Value, gain).Add(s => s.ValueChanged, (float v) => gain = v).Add(s => s.Max, 1).Add(s => s.Step, 0.1));
        var doubleSlider = Render<KlySlider<double>>(p => p.Add(s => s.Value, level).Add(s => s.ValueChanged, (double v) => level = v));

        floatSlider.Find("kly-slider").TriggerEvent("onklychange", new KlyChangeEventArgs { Value = "0.7", Sequence = 1 });
        doubleSlider.Find("kly-slider").TriggerEvent("onklychange", new KlyChangeEventArgs { Value = "2.25", Sequence = 1 });

        Assert.Equal(0.7f, gain);
        Assert.Equal(2.25, level);
    }

    [Fact]
    public void Slider_of_a_nullable_number_rests_on_its_minimum_until_it_moves()
    {
        decimal? amount = null;
        var cut = Render<KlySlider<decimal?>>(p => p.Add(s => s.Value, amount).Add(s => s.ValueChanged, (decimal? v) => amount = v).Add(s => s.Min, 10));

        Assert.Equal("10", cut.Find("input[type=range]").GetAttribute("value"));

        cut.Find("kly-slider").TriggerEvent("onklychange", new KlyChangeEventArgs { Value = "25", Sequence = 1 });

        Assert.Equal(25m, amount);
    }
}
