using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Controls.Shapes;

namespace Klyroo.UI.Maui.Tests;

public class InputsTests
{
    /// <summary>A culture with the separators of a comma culture and English month names, which does not depend on the globalization data of the machine.</summary>
    private static CultureInfo CommaCulture()
    {
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        culture.NumberFormat.NumberDecimalSeparator = ",";
        culture.NumberFormat.NumberGroupSeparator = ".";
        culture.DateTimeFormat.YearMonthPattern = "MMMM yyyy";
        culture.DateTimeFormat.FirstDayOfWeek = DayOfWeek.Monday;
        return culture;
    }

    // ---- KlyInputNumber

    [Fact]
    public void InputNumber_has_a_numeric_keyboard_and_is_empty_by_default()
    {
        var input = new KlyInputNumber();

        Assert.Null(input.Value);
        Assert.Equal(1m, input.Step);
        Assert.Equal(Keyboard.Numeric, input.Entry.Keyboard);
        Assert.Equal(string.Empty, input.Entry.Text ?? string.Empty);
        Assert.Equal(StackOrientation.Horizontal, input.Orientation);
    }

    [Fact]
    public void InputNumber_shows_the_value_in_its_culture_and_reads_typed_text_back()
    {
        var culture = CommaCulture();
        var input = new KlyInputNumber { Culture = culture, Value = 1234.5m };
        Assert.Equal("1.234,5", input.Entry.Text);

        input.Entry.Text = "12,25";
        input.CommitText();
        Assert.Equal(12.25m, input.Value);
        Assert.Equal("12,25", input.Entry.Text);
    }

    [Theory]
    [InlineData("abc", 7)]
    [InlineData("1,5x", 7)]
    public void InputNumber_puts_the_value_back_when_the_text_is_not_a_number(string typed, int current)
    {
        var input = new KlyInputNumber { Culture = CommaCulture(), Value = current };

        input.Entry.Text = typed;
        input.CommitText();

        Assert.Equal(current, input.Value);
        Assert.Equal("7", input.Entry.Text);
    }

    [Fact]
    public void InputNumber_limits_typed_text_to_min_and_max_and_empty_text_clears_the_value()
    {
        var input = new KlyInputNumber { Min = 0, Max = 10, Value = 5 };

        input.Entry.Text = "99";
        input.CommitText();
        Assert.Equal(10m, input.Value);

        input.Entry.Text = "-4";
        input.CommitText();
        Assert.Equal(0m, input.Value);

        input.Entry.Text = "  ";
        input.CommitText();
        Assert.Null(input.Value);
    }

    [Fact]
    public void InputNumber_buttons_step_the_value_and_stop_at_the_limits()
    {
        var input = new KlyInputNumber { Min = 0, Max = 2, Step = 0.5m, Value = 1.5m };

        input.IncrementButton.Press();
        Assert.Equal(2m, input.Value);
        Assert.False(input.IncrementButton.IsEnabled);
        Assert.True(input.DecrementButton.IsEnabled);

        input.IncrementButton.Press();
        Assert.Equal(2m, input.Value);

        input.Value = 0;
        Assert.False(input.DecrementButton.IsEnabled);
        input.DecrementButton.Press();
        Assert.Equal(0m, input.Value);
    }

    [Fact]
    public void InputNumber_with_no_value_steps_from_zero_limited_to_its_range()
    {
        var plain = new KlyInputNumber();
        plain.IncrementButton.Press();
        Assert.Equal(1m, plain.Value);

        var limited = new KlyInputNumber { Min = 5, Max = 10 };
        limited.IncrementButton.Press();
        Assert.Equal(6m, limited.Value);
    }

    [Fact]
    public void InputNumber_ignores_a_step_of_zero_or_less()
    {
        var input = new KlyInputNumber { Step = 0.5m };

        input.Step = 0;
        input.Step = -1;

        Assert.Equal(0.5m, input.Step);
    }

    [Fact]
    public void InputNumber_raises_one_event_per_change_and_the_value_binds_both_ways()
    {
        var input = new KlyInputNumber();
        var events = new List<(decimal?, decimal?)>();
        input.ValueChanged += (_, e) => events.Add((e.OldValue, e.NewValue));

        input.Value = 3;
        input.IncrementButton.Press();
        input.Value = 4;

        Assert.Equal([(null, 3m), (3m, 4m)], events);
        Assert.Equal("4", input.Entry.Text);
    }

    [Fact]
    public void InputNumber_stacks_the_buttons_at_the_end_when_vertical()
    {
        var input = new KlyInputNumber { Orientation = StackOrientation.Vertical };
        var layout = Assert.IsType<Grid>(input.Content);

        Assert.Equal(2, layout.ColumnDefinitions.Count);
        Assert.Equal(2, layout.RowDefinitions.Count);
        Assert.Equal(1, Grid.GetColumn(input.IncrementButton));
        Assert.Equal(0, Grid.GetRow(input.IncrementButton));
        Assert.Equal(1, Grid.GetRow(input.DecrementButton));
        Assert.Equal(TextAlignment.Start, input.Entry.HorizontalTextAlignment);
    }

    [Fact]
    public void InputNumber_gives_the_frame_padding_to_the_entry_so_the_buttons_reach_the_edges()
    {
        var input = new KlyInputNumber { Size = KlySize.Lg };

        Assert.Equal(new Thickness(0), input.Padding);
        Assert.Equal(new Thickness(0, 8), input.Entry.Margin);
        Assert.Equal(14, input.Entry.FontSize);
    }

    [Fact]
    public void InputNumber_disables_its_entry_and_buttons_and_dims()
    {
        var input = new KlyInputNumber { IsEnabled = false };

        Assert.False(input.Entry.IsEnabled);
        Assert.False(input.IncrementButton.IsEnabled);
        Assert.False(input.DecrementButton.IsEnabled);
        Assert.Equal(0.6, input.Opacity);

        input.IsEnabled = true;
        Assert.True(input.IncrementButton.IsEnabled);
    }

    [Fact]
    public void InputNumber_names_the_field_and_its_buttons_for_a_screen_reader()
    {
        var input = new KlyInputNumber { AccessibleName = "Quantity", Invalid = true };

        Assert.Equal("Quantity", SemanticProperties.GetDescription(input.Entry));
        Assert.Equal("Increase", SemanticProperties.GetDescription(input.IncrementButton));
        Assert.Equal("Decrease", SemanticProperties.GetDescription(input.DecrementButton));
        Assert.Equal(2, input.StrokeThickness);
    }

    // ---- KlyInputDate and KlyInputTime

    [Fact]
    public void InputDate_shows_the_day_in_the_native_picker_and_keeps_the_time_of_the_value_when_a_day_is_picked()
    {
        var input = new KlyInputDate { Value = new DateTime(2026, 3, 4, 14, 30, 0) };
        Assert.Equal(new DateTime(2026, 3, 4), input.Picker.Date);

        input.Picker.Date = new DateTime(2026, 3, 10);

        Assert.Equal(new DateTime(2026, 3, 10, 14, 30, 0), input.Value);
    }

    [Fact]
    public void InputDate_starts_a_new_value_at_midnight_and_clearing_the_picker_clears_the_value()
    {
        var input = new KlyInputDate();
        Assert.Null(input.Value);

        input.Picker.Date = new DateTime(2026, 1, 2);
        Assert.Equal(new DateTime(2026, 1, 2), input.Value);

        input.Picker.Date = null;
        Assert.Null(input.Value);
    }

    [Fact]
    public void InputDate_raises_one_event_per_change_and_passes_its_limits_on()
    {
        var input = new KlyInputDate { Min = new DateTime(2026, 1, 1), Max = new DateTime(2026, 12, 31) };
        var events = new List<(DateTime?, DateTime?)>();
        input.ValueChanged += (_, e) => events.Add((e.OldValue, e.NewValue));

        input.Value = new DateTime(2026, 5, 5);
        input.Picker.Date = new DateTime(2026, 5, 6);

        Assert.Equal([(null, new DateTime(2026, 5, 5)), (new DateTime(2026, 5, 5), new DateTime(2026, 5, 6))], events);
        Assert.Equal(new DateTime(2026, 1, 1), input.Picker.MinimumDate);
        Assert.Equal(new DateTime(2026, 12, 31), input.Picker.MaximumDate);
    }

    [Fact]
    public void InputDate_opens_its_picker_from_the_calendar_button_and_disables_the_button_with_the_field()
    {
        var input = new KlyInputDate();

        input.OpenButton.Press();
        Assert.True(input.Picker.IsOpen);
        Assert.Equal("Choose date", SemanticProperties.GetDescription(input.OpenButton));

        input.IsEnabled = false;
        Assert.False(input.OpenButton.IsEnabled);
        Assert.False(input.Picker.IsEnabled);
        Assert.Equal(0.6, input.Opacity);
    }

    [Fact]
    public void InputTime_binds_a_time_only_both_ways_with_one_event_per_change()
    {
        var input = new KlyInputTime();
        var events = new List<TimeOnly?>();
        input.ValueChanged += (_, e) => events.Add(e.NewValue);

        input.Value = new TimeOnly(9, 30);
        Assert.Equal(new TimeSpan(9, 30, 0), input.Picker.Time);

        input.Picker.Time = new TimeSpan(18, 5, 0);
        Assert.Equal(new TimeOnly(18, 5), input.Value);

        input.Picker.Time = null;
        Assert.Null(input.Value);
        Assert.Equal([new TimeOnly(9, 30), new TimeOnly(18, 5), null], events);
    }

    [Theory]
    [InlineData(true, "h:mm tt")]
    [InlineData(false, "HH:mm")]
    [InlineData(null, "t")]
    public void InputTime_sets_the_clock_format_of_the_picker(bool? hour12, string format)
    {
        var input = new KlyInputTime { Hour12 = hour12 };

        Assert.Equal(format, input.Picker.Format);
    }

    [Fact]
    public void InputTime_gives_the_size_to_the_font_and_the_padding()
    {
        var input = new KlyInputTime { Size = KlySize.Xl, AccessibleName = "Start time", IsEnabled = false };

        Assert.Equal(16, input.Picker.FontSize);
        Assert.Equal(new Thickness(12, 8), input.Padding);
        Assert.Equal("Start time", SemanticProperties.GetDescription(input.Picker));
        Assert.False(input.Picker.IsEnabled);
    }

    // ---- KlyInputRating

    [Theory]
    [InlineData(false, 3.7, 4)]
    [InlineData(true, 3.7, 3.5)]
    [InlineData(true, 3.2, 3)]
    [InlineData(false, 9, 5)]
    [InlineData(false, -2, 0)]
    public void InputRating_moves_the_value_to_a_step_inside_its_range(bool half, double value, double expected)
    {
        var rating = new KlyInputRating { AllowHalf = half, Value = value };

        Assert.Equal(expected, rating.Value);
    }

    [Fact]
    public void InputRating_fills_whole_and_half_stars_from_the_start()
    {
        var rating = new KlyInputRating { AllowHalf = true, Value = 2.5 };

        Assert.Equal([1d, 1d, 0.5, 0d, 0d], rating.Stars.Select(s => s.Filled));

        rating.Value = 0;
        Assert.All(rating.Stars, s => Assert.Equal(0, s.Filled));
    }

    [Fact]
    public void InputRating_has_as_many_stars_as_max_and_a_smaller_max_lowers_the_value()
    {
        var rating = new KlyInputRating { Max = 10, Value = 8 };
        Assert.Equal(10, rating.Stars.Count);

        rating.Max = 6;
        Assert.Equal(6, rating.Stars.Count);

        rating.Max = 3;
        Assert.Equal(3, rating.Value);
    }

    [Theory]
    [InlineData(KlySize.Xs, 16)]
    [InlineData(KlySize.Sm, 20)]
    [InlineData(KlySize.Md, 24)]
    [InlineData(KlySize.Lg, 28)]
    [InlineData(KlySize.Xl, 32)]
    public void InputRating_size_is_the_size_of_a_star(KlySize size, double dimension)
    {
        var rating = new KlyInputRating { Size = size };

        Assert.All(rating.Stars, s => Assert.Equal(dimension, s.WidthRequest));
    }

    [Fact]
    public void InputRating_taps_choose_a_star_or_half_a_star_less_on_its_left_half()
    {
        var rating = new KlyInputRating { AllowHalf = true };

        rating.Tap(3, left: false);
        Assert.Equal(3, rating.Value);

        rating.Tap(3, left: true);
        Assert.Equal(2.5, rating.Value);

        var whole = new KlyInputRating();
        whole.Tap(2, left: true);
        Assert.Equal(2, whole.Value);
    }

    [Fact]
    public void InputRating_read_only_and_disabled_ignore_taps_and_the_value_is_read_aloud()
    {
        var rating = new KlyInputRating { Value = 2, ReadOnly = true, AccessibleName = "Rating" };
        rating.Tap(4, left: false);
        Assert.Equal(2, rating.Value);
        Assert.Equal(string.Create(CultureInfo.CurrentCulture, $"Rating, 2 out of 5"), SemanticProperties.GetDescription(rating));

        rating.ReadOnly = false;
        rating.IsEnabled = false;
        rating.Tap(4, left: false);
        Assert.Equal(2, rating.Value);
        Assert.Equal(0.6, rating.Opacity);
    }

    [Fact]
    public void InputRating_raises_one_event_per_change_and_draws_the_error_outline_while_invalid()
    {
        var rating = new KlyInputRating();
        var events = new List<double>();
        rating.ValueChanged += (_, e) => events.Add(e.NewValue);

        rating.Value = 3;
        rating.Tap(3, left: false);
        rating.Tap(5, left: false);
        rating.Invalid = true;

        Assert.Equal([3d, 5d], events);
        Assert.Equal(2, rating.StrokeThickness);
    }

    // ---- KlyPinInput

    [Fact]
    public void PinInput_has_five_empty_numeric_boxes_by_default()
    {
        var pin = new KlyPinInput();

        Assert.Equal(5, pin.Cells.Count);
        Assert.All(pin.Cells, c => Assert.Equal(Keyboard.Numeric, c.Entry.Keyboard));
        Assert.Equal(PinInputType.Numeric, pin.Type);
        Assert.Null(pin.Value);
    }

    [Fact]
    public void PinInput_reads_the_boxes_as_the_value_and_fills_the_boxes_from_the_value()
    {
        var pin = new KlyPinInput();
        var events = new List<string?>();
        pin.ValueChanged += (_, e) => events.Add(e.NewValue);

        pin.Cells[0].Entry.Text = "4";
        pin.Cells[1].Entry.Text = "2";
        Assert.Equal("42", pin.Value);

        pin.Value = "98765";
        Assert.Equal(["9", "8", "7", "6", "5"], pin.Cells.Select(c => c.Entry.Text));
        Assert.Equal(["4", "42", "98765"], events);
    }

    [Fact]
    public void PinInput_spreads_a_pasted_code_over_the_boxes_and_drops_what_does_not_fit()
    {
        var pin = new KlyPinInput { Length = 4 };

        pin.Cells[1].Entry.Text = "12345";

        Assert.Equal(["", "1", "2", "3"], pin.Cells.Select(c => c.Entry.Text ?? string.Empty));
        Assert.Equal("123", pin.Value);
    }

    [Fact]
    public void PinInput_drops_characters_the_type_does_not_accept()
    {
        var numeric = new KlyPinInput();
        numeric.Cells[0].Entry.Text = "a";
        Assert.Equal(string.Empty, numeric.Cells[0].Entry.Text);
        numeric.Cells[0].Entry.Text = "1-2x3";
        Assert.Equal("123", numeric.Value);

        var mixed = new KlyPinInput { Type = PinInputType.Alphanumeric };
        mixed.Cells[0].Entry.Text = "a1";
        Assert.Equal("a1", mixed.Value);
        Assert.Equal(Keyboard.Default, mixed.Cells[0].Entry.Keyboard);
    }

    [Fact]
    public void PinInput_typing_over_a_filled_box_keeps_the_new_character()
    {
        var pin = new KlyPinInput();
        pin.Cells[0].Entry.Text = "1";

        pin.Cells[0].Entry.Text = "12";

        Assert.Equal("2", pin.Cells[0].Entry.Text);
        Assert.True(string.IsNullOrEmpty(pin.Cells[1].Entry.Text));
        Assert.Equal("2", pin.Value);
    }

    [Fact]
    public void PinInput_emptying_a_box_removes_its_character_from_the_value()
    {
        var pin = new KlyPinInput { Value = "123" };

        pin.Cells[1].Entry.Text = string.Empty;

        Assert.Equal("13", pin.Value);
    }

    [Fact]
    public void PinInput_masks_the_characters_and_changing_the_length_rebuilds_the_boxes()
    {
        var pin = new KlyPinInput { Mask = true, Length = 6, Value = "12" };

        Assert.Equal(6, pin.Cells.Count);
        Assert.All(pin.Cells, c => Assert.True(c.Entry.IsPassword));
        Assert.Equal("1", pin.Cells[0].Entry.Text);
        Assert.Equal("Character 6 of 6", pin.Cells[5].AccessibleName);
        Assert.Equal("Character 1 of 6", SemanticProperties.GetDescription(pin.Cells[0].Entry));
    }

    [Theory]
    [InlineData(KlySize.Xs, 28, 4)]
    [InlineData(KlySize.Sm, 32, 6)]
    [InlineData(KlySize.Md, 36, 6)]
    [InlineData(KlySize.Lg, 40, 8)]
    [InlineData(KlySize.Xl, 44, 8)]
    public void PinInput_size_gives_the_box_its_square_and_the_gap_between_boxes(KlySize size, double dimension, double gap)
    {
        var pin = new KlyPinInput { Size = size };

        Assert.All(pin.Cells, c =>
        {
            Assert.Equal(dimension, c.WidthRequest);
            Assert.Equal(dimension, c.HeightRequest);
            Assert.Equal(new Thickness(0), c.Padding);
        });
        Assert.Equal(gap, pin.Spacing);
    }

    [Fact]
    public void PinInput_passes_variant_error_and_enabled_state_to_every_box()
    {
        var pin = new KlyPinInput { Variant = FieldVariant.Soft, Invalid = true, IsEnabled = false, AccessibleName = "Code" };

        Assert.All(pin.Cells, c =>
        {
            Assert.Equal(FieldVariant.Soft, c.Variant);
            Assert.True(c.Invalid);
            Assert.False(c.IsEnabled);
        });
        Assert.Equal(0.6, pin.Opacity);
        Assert.Equal("Code", SemanticProperties.GetDescription(pin));
    }

    // ---- KlyInputTags

    [Fact]
    public void InputTags_adds_the_typed_text_on_return_and_ignores_empty_and_duplicate_text()
    {
        var tags = new KlyInputTags();

        tags.Entry.Text = "  red ";
        tags.AddTyped();
        tags.Entry.Text = "red";
        tags.AddTyped();
        tags.Entry.Text = "   ";
        tags.AddTyped();

        Assert.Equal(["red"], tags.Value);
        Assert.Equal(string.Empty, tags.Entry.Text);
    }

    [Fact]
    public void InputTags_a_comma_ends_a_tag_and_the_text_after_it_stays_in_the_field()
    {
        var tags = new KlyInputTags();

        tags.Entry.Text = "a, b,c";

        Assert.Equal(["a", "b"], tags.Value);
        Assert.Equal("c", tags.Entry.Text);
    }

    [Fact]
    public void InputTags_stops_at_max_tags()
    {
        var tags = new KlyInputTags { Max = 2 };

        tags.Entry.Text = "a,b,c,d";

        Assert.Equal(["a", "b"], tags.Value);
    }

    [Fact]
    public void InputTags_draws_a_chip_for_each_tag_and_its_remove_button_removes_it()
    {
        var tags = new KlyInputTags { Value = ["one", "two", "three"] };
        var events = new List<string>();
        tags.ValueChanged += (_, e) => events.Add(string.Join("|", e.NewValue ?? []));

        Assert.Equal(["one", "two", "three"], tags.Chips.Select(c => c.Tag));
        Assert.Equal("Remove two", SemanticProperties.GetDescription(tags.Chips[1].Remove));

        tags.Chips[1].Remove.Press();

        Assert.Equal(["one", "three"], tags.Value);
        Assert.Equal(["one", "three"], tags.Chips.Select(c => c.Tag));
        Assert.Equal(["one|three"], events);
    }

    [Fact]
    public void InputTags_takes_a_placeholder_and_disables_the_remove_buttons_with_the_field()
    {
        var tags = new KlyInputTags { Placeholder = "Add a tag", Value = ["a"], Size = KlySize.Lg };

        Assert.Equal("Add a tag", tags.Entry.Placeholder);
        Assert.Equal(14, tags.Entry.FontSize);

        tags.IsEnabled = false;
        Assert.False(tags.Chips[0].Remove.IsEnabled);
        Assert.Equal(0.6, tags.Opacity);
    }

    // ---- shared option helpers, KlyListbox

    [Fact]
    public void Options_come_from_select_items_and_from_plain_objects()
    {
        var options = InputsOptions.From(new object[]
        {
            new KlySelectItem("fr", "France") { Count = 12, Icon = KlyIconNames.Star, Disabled = true },
            42,
            null!,
        });

        Assert.Equal(2, options.Count);
        Assert.Equal(new InputsOption("fr", "France", true, 12, KlyIconNames.Star, null), options[0]);
        Assert.Equal(new InputsOption(42, "42", false, null, null, null), options[1]);
    }

    [Fact]
    public void Options_are_grouped_with_the_ungrouped_first_and_each_group_in_the_order_of_its_first_option()
    {
        var options = InputsOptions.From(new[]
        {
            new KlySelectItem(1, "a") { Group = "G2" },
            new KlySelectItem(2, "b"),
            new KlySelectItem(3, "c") { Group = "G1" },
            new KlySelectItem(4, "d") { Group = "G2" },
        });

        var sections = InputsOptions.Sections(options);

        Assert.Equal([null, "G2", "G1"], sections.Select(s => s.Heading));
        Assert.Equal([["b"], ["a", "d"], ["c"]], sections.Select(s => s.Options.Select(o => o.Label).ToArray()));
    }

    [Fact]
    public void Options_filter_by_a_part_of_the_label_ignoring_case()
    {
        var options = InputsOptions.From(new[] { "France", "Franche", "Spain" });

        Assert.Equal(["France", "Franche"], InputsOptions.Filter(options, " FRAN").Select(o => o.Label));
        Assert.Equal(3, InputsOptions.Filter(options, null).Count);
    }

    [Fact]
    public void Listbox_lists_its_options_with_a_check_on_the_selected_one()
    {
        var list = new KlyListbox { Items = new[] { "One", "Two", "Three" }, Value = new object[] { "Two" } };

        Assert.Equal(["One", "Two", "Three"], list.List.Rows.Select(r => r.LabelText));
        Assert.Equal([false, true, false], list.List.Rows.Select(r => r.CheckVisible));
        Assert.Equal("Two, selected", SemanticProperties.GetDescription(list.List.Rows[1]));
    }

    [Fact]
    public void Listbox_single_selection_replaces_the_value_and_choosing_the_chosen_option_keeps_it()
    {
        var list = new KlyListbox { Items = new[] { "One", "Two" } };
        var events = new List<int>();
        list.ValueChanged += (_, e) => events.Add(e.NewValue!.Count);

        list.List.Rows[0].Choose();
        list.List.Rows[1].Choose();
        list.List.Rows[1].Choose();

        Assert.Equal(["Two"], list.Value);
        Assert.Equal([1, 1], events);
    }

    [Fact]
    public void Listbox_multiple_selection_toggles_options()
    {
        var list = new KlyListbox { Items = new[] { 1, 2, 3 }, Multiple = true };

        list.List.Rows[0].Choose();
        list.List.Rows[2].Choose();
        list.List.Rows[0].Choose();

        Assert.Equal([3], list.Value!.Cast<int>());
    }

    [Fact]
    public void Listbox_ignores_disabled_options_and_a_disabled_list_and_dims()
    {
        var list = new KlyListbox { Items = new[] { new KlySelectItem("a", "A") { Disabled = true }, new KlySelectItem("b", "B") } };

        list.List.Rows[0].Choose();
        Assert.Null(list.Value);

        list.IsEnabled = false;
        list.List.Rows[1].Choose();
        Assert.Null(list.Value);
        Assert.Equal(0.6, list.Opacity);
    }

    [Fact]
    public void Listbox_shows_group_headings_counts_and_the_error_border()
    {
        var list = new KlyListbox
        {
            Items = new[] { new KlySelectItem(1, "A") { Group = "Letters", Count = 1200 }, new KlySelectItem(2, "B") },
            Invalid = true,
            AccessibleName = "Letters",
        };

        var stack = list.List;
        Assert.Equal(2, stack.Rows.Count);
        Assert.Equal("B", stack.Rows[0].LabelText);
        Assert.Equal(3, stack.Children.Count);
        Assert.Equal("Letters", ((Label)stack.Children[1]).Text);
        Assert.Equal("A, " + 1200.ToString("N0", CultureInfo.CurrentCulture), SemanticProperties.GetDescription(stack.Rows[1]));
        Assert.Equal(2, list.StrokeThickness);
        Assert.Equal("Letters", SemanticProperties.GetDescription(list));
    }

    [Theory]
    [InlineData(KlySize.Xs, 12)]
    [InlineData(KlySize.Md, 14)]
    [InlineData(KlySize.Xl, 16)]
    public void Listbox_size_is_the_font_of_the_options(KlySize size, double font)
    {
        var list = new KlyListbox { Items = new[] { "A" }, Size = size };

        Assert.Equal(font, ((Label)((Grid)list.List.Rows[0].Content!).Children.OfType<Label>().First()).FontSize);
    }

    // ---- KlySelectMenu

    private static KlySelectMenu Countries(Action<KlySelectMenu>? configure = null)
    {
        var menu = new KlySelectMenu
        {
            Items = new[]
            {
                new KlySelectItem("fr", "France") { Icon = KlyIconNames.Star },
                new KlySelectItem("de", "Germany"),
                new KlySelectItem("es", "Spain"),
            },
            Placeholder = "Pick a country",
        };
        configure?.Invoke(menu);
        return menu;
    }

    [Fact]
    public void SelectMenu_shows_the_placeholder_then_the_label_of_the_value_and_the_icon_of_the_item()
    {
        var menu = Countries();
        Assert.Equal("Pick a country", menu.Field.Label.Text);
        Assert.False(menu.Field.Leading.IsVisible);

        menu.Value = new object[] { "fr" };

        Assert.Equal("France", menu.Field.Label.Text);
        Assert.True(menu.Field.Leading.IsVisible);
        Assert.Equal(KlyIconNames.Star, menu.Field.Leading.Name);
    }

    [Fact]
    public void SelectMenu_opens_inline_and_choosing_an_option_sets_the_value_and_closes()
    {
        var menu = Countries();
        Assert.False(menu.Panel.IsVisible);

        menu.IsOpen = true;
        Assert.True(menu.Panel.IsVisible);

        menu.Panel.List.Rows[2].Choose();

        Assert.Equal(["es"], menu.Value);
        Assert.False(menu.IsOpen);
        Assert.False(menu.Panel.IsVisible);
        Assert.Equal("Spain", menu.Field.Label.Text);
    }

    [Fact]
    public void SelectMenu_multiple_toggles_values_and_stays_open()
    {
        var menu = Countries(m => m.Multiple = true);
        var events = new List<string>();
        menu.ValueChanged += (_, e) => events.Add(string.Join(",", e.NewValue!));
        menu.IsOpen = true;

        menu.Panel.List.Rows[0].Choose();
        menu.Panel.List.Rows[1].Choose();
        menu.Panel.List.Rows[0].Choose();

        Assert.Equal(["de"], menu.Value);
        Assert.True(menu.IsOpen);
        Assert.Equal(["fr", "fr,de", "de"], events);
        Assert.Equal("Germany", menu.Field.Label.Text);
    }

    [Fact]
    public void SelectMenu_multiple_shows_the_labels_joined()
    {
        var menu = Countries(m => m.Multiple = true);

        menu.Value = new object[] { "fr", "es" };

        Assert.Equal("France, Spain", menu.Field.Label.Text);
    }

    [Fact]
    public void SelectMenu_filters_the_options_by_the_text_of_the_search_field_and_says_when_none_match()
    {
        var menu = Countries();
        var searches = new List<string?>();
        menu.SearchChanged += (_, e) => searches.Add(e.NewTextValue);

        menu.Panel.Search!.Text = "sp";
        Assert.Equal(["Spain"], menu.Panel.List.Rows.Select(r => r.LabelText));
        Assert.False(menu.Panel.Empty.IsVisible);

        menu.Panel.Search.Text = "zzz";
        Assert.Empty(menu.Panel.List.Rows);
        Assert.True(menu.Panel.Empty.IsVisible);
        Assert.Equal(["sp", "zzz"], searches);
    }

    [Fact]
    public void SelectMenu_closing_clears_the_search()
    {
        var menu = Countries();
        menu.IsOpen = true;
        menu.Panel.Search!.Text = "fr";

        menu.IsOpen = false;

        Assert.Equal(string.Empty, menu.Panel.Search.Text);
        Assert.Equal(3, menu.Panel.List.Rows.Count);
    }

    [Fact]
    public void SelectMenu_offers_the_typed_text_as_a_new_value_when_create_is_on()
    {
        var menu = Countries(m => m.Create = true);

        menu.Panel.Search!.Text = "Italy";
        Assert.Equal(["Create \"Italy\""], menu.Panel.List.Rows.Select(r => r.LabelText));

        menu.Panel.List.Rows[0].Choose();

        Assert.Equal(["Italy"], menu.Value);
        Assert.Equal("Italy", menu.Field.Label.Text);
    }

    [Fact]
    public void SelectMenu_does_not_offer_to_create_text_that_is_an_option_label_or_already_a_value()
    {
        var menu = Countries(m => m.Create = true);
        menu.Panel.Search!.Text = "france";
        Assert.DoesNotContain(menu.Panel.List.Rows, r => r.LabelText.StartsWith("Create", StringComparison.Ordinal));

        menu.Multiple = true;
        menu.Value = new object[] { "Italy" };
        menu.Panel.Search.Text = "Italy";
        Assert.DoesNotContain(menu.Panel.List.Rows, r => r.LabelText.StartsWith("Create", StringComparison.Ordinal));
    }

    [Fact]
    public void SelectMenu_a_value_that_is_not_an_option_shows_as_itself()
    {
        var menu = Countries(m => m.Multiple = true);

        menu.Value = new object[] { "fr", "Atlantis" };

        Assert.Equal("France, Atlantis", menu.Field.Label.Text);
    }

    [Fact]
    public void SelectMenu_passes_its_look_to_the_field_and_dims_when_disabled()
    {
        var menu = Countries(m =>
        {
            m.Variant = FieldVariant.Soft;
            m.Size = KlySize.Lg;
            m.Invalid = true;
            m.Color = KlyColor.Success;
            m.AccessibleName = "Country";
        });

        Assert.Equal(FieldVariant.Soft, menu.Field.Variant);
        Assert.Equal(KlySize.Lg, menu.Field.Size);
        Assert.True(menu.Field.Invalid);
        Assert.Equal(KlyColor.Success, menu.Field.Color);
        Assert.Equal("Country, Pick a country", SemanticProperties.GetDescription(menu.Field.Label));

        menu.IsEnabled = false;
        Assert.Equal(0.6, menu.Opacity);
        menu.Panel.List.Rows[0].Choose();
        Assert.Null(menu.Value);
    }

    [Fact]
    public void SelectMenu_labels_its_search_field()
    {
        var menu = Countries(m => m.SearchLabel = "Find");

        Assert.Equal("Find", menu.Panel.Search!.Placeholder);
        Assert.Equal("Find", SemanticProperties.GetDescription(menu.Panel.Search));
    }

    // ---- KlyInputMenu

    private static KlyInputMenu Fruits(Action<KlyInputMenu>? configure = null)
    {
        var menu = new KlyInputMenu { Items = new[] { "Apple", "Apricot", "Banana" }, Placeholder = "Fruit" };
        configure?.Invoke(menu);
        return menu;
    }

    [Fact]
    public void InputMenu_filters_the_suggestions_as_the_user_types_and_opens_the_list()
    {
        var menu = Fruits();
        var searches = new List<string?>();
        menu.SearchChanged += (_, e) => searches.Add(e.NewTextValue);

        menu.Field.Entry.Text = "ap";

        Assert.True(menu.IsOpen);
        Assert.Equal(["Apple", "Apricot"], menu.Panel.List.Rows.Select(r => r.LabelText));
        Assert.Equal(["ap"], searches);
    }

    [Fact]
    public void InputMenu_free_text_is_the_value_once_the_field_is_left()
    {
        var menu = Fruits();
        var events = new List<object?>();
        menu.ValueChanged += (_, e) => events.Add(e.NewValue);

        menu.Field.Entry.Text = "Cherry";
        Assert.Null(menu.Value);

        menu.Commit();
        Assert.Equal("Cherry", menu.Value);

        menu.Field.Entry.Text = string.Empty;
        menu.Commit();
        Assert.Null(menu.Value);
        Assert.Equal(["Cherry", null], events);
    }

    [Fact]
    public void InputMenu_choosing_a_suggestion_completes_the_text_and_closes_the_list()
    {
        var menu = Fruits();
        menu.Field.Entry.Text = "ban";

        menu.Panel.List.Rows[0].Choose();

        Assert.Equal("Banana", menu.Value);
        Assert.Equal("Banana", menu.Field.Entry.Text);
        Assert.False(menu.IsOpen);
        Assert.Equal(3, menu.Panel.List.Rows.Count);
    }

    [Fact]
    public void InputMenu_setting_the_value_shows_it_in_the_field_without_filtering()
    {
        var menu = Fruits();

        menu.Value = "Apricot";

        Assert.Equal("Apricot", menu.Field.Entry.Text);
        Assert.Equal(3, menu.Panel.List.Rows.Count);
    }

    [Fact]
    public void InputMenu_strict_takes_the_value_of_the_chosen_item_and_shows_its_label()
    {
        var menu = new KlyInputMenu
        {
            Strict = true,
            Items = new[] { new KlySelectItem("fr", "France"), new KlySelectItem("de", "Germany") },
        };
        menu.Field.Entry.Text = "ger";

        menu.Panel.List.Rows[0].Choose();

        Assert.Equal("de", menu.Value);
        Assert.Equal("Germany", menu.Field.Entry.Text);
        Assert.Equal([false, true], menu.Panel.List.Rows.Select(r => r.CheckVisible));
    }

    [Fact]
    public void InputMenu_strict_puts_the_label_back_when_the_field_is_left_and_emptying_it_clears_the_value()
    {
        var menu = new KlyInputMenu { Strict = true, Items = new[] { new KlySelectItem("fr", "France") }, Value = "fr" };
        Assert.Equal("France", menu.Field.Entry.Text);

        menu.Field.Entry.Text = "Fra";
        menu.Commit();
        Assert.Equal("France", menu.Field.Entry.Text);
        Assert.Equal("fr", menu.Value);

        menu.Field.Entry.Text = string.Empty;
        Assert.Null(menu.Value);
    }

    [Fact]
    public void InputMenu_is_strict_when_an_item_has_a_value_that_is_not_text()
    {
        var menu = new KlyInputMenu { Items = new[] { new KlySelectItem(1, "One"), new KlySelectItem(2, "Two") } };

        menu.Panel.List.Rows[1].Choose();

        Assert.Equal(2, menu.Value);
        Assert.Equal("Two", menu.Field.Entry.Text);
    }

    [Fact]
    public void InputMenu_passes_its_look_to_the_field_and_dims_when_disabled()
    {
        var menu = Fruits(m =>
        {
            m.Variant = FieldVariant.Subtle;
            m.Size = KlySize.Sm;
            m.Invalid = true;
            m.AccessibleName = "Fruit";
        });

        Assert.Equal("Fruit", menu.Field.Entry.Placeholder);
        Assert.Equal(FieldVariant.Subtle, menu.Field.Variant);
        Assert.Equal(KlySize.Sm, menu.Field.Size);
        Assert.True(menu.Field.Invalid);
        Assert.Equal("Fruit", SemanticProperties.GetDescription(menu.Field.Entry));

        menu.IsEnabled = false;
        Assert.Equal(0.6, menu.Opacity);
        menu.Panel.List.Rows[0].Choose();
        Assert.Null(menu.Value);
    }

    [Fact]
    public void InputMenu_the_chevron_toggles_the_list_with_every_suggestion()
    {
        var menu = Fruits();
        menu.Value = "Apple";

        menu.Field.Chevron.Press();
        Assert.True(menu.IsOpen);

        menu.Field.Chevron.Press();
        Assert.False(menu.IsOpen);
    }

    // ---- KlySlider

    [Theory]
    [InlineData(37, 0, 100, 5, 35)]
    [InlineData(38, 0, 100, 5, 40)]
    [InlineData(250, 0, 100, 5, 100)]
    [InlineData(-5, 0, 100, 5, 0)]
    [InlineData(7, 3, 20, 5, 8)]
    [InlineData(100, 0, 99, 10, 90)]
    [InlineData(0.3, 0, 1, 0.1, 0.3)]
    public void Slider_snaps_the_value_to_the_step_from_min_inside_the_track(double value, double min, double max, double step, double expected)
    {
        Assert.Equal(expected, KlySlider.Snap(value, min, max, step));
    }

    [Fact]
    public void Slider_defaults_to_the_range_of_a_web_slider()
    {
        var slider = new KlySlider();

        Assert.Equal(0, slider.Min);
        Assert.Equal(100, slider.Max);
        Assert.Equal(1, slider.Step);
        Assert.Equal(0, slider.Value);
        Assert.Equal(0, slider.Native.Minimum);
        Assert.Equal(100, slider.Native.Maximum);
    }

    [Fact]
    public void Slider_value_goes_both_ways_snapped_and_raises_one_event_per_change()
    {
        var slider = new KlySlider { Step = 5 };
        var events = new List<(double, double)>();
        slider.ValueChanged += (_, e) => events.Add((e.OldValue, e.NewValue));

        slider.Value = 40;
        Assert.Equal(40, slider.Native.Value);

        slider.Native.Value = 37;
        Assert.Equal(35, slider.Value);
        Assert.Equal(35, slider.Native.Value);
        Assert.Equal([(0d, 40d), (40d, 35d)], events);
    }

    [Fact]
    public void Slider_moving_the_limits_keeps_the_native_slider_valid_and_the_value_inside()
    {
        var slider = new KlySlider { Value = 80 };

        slider.Max = 50;
        Assert.Equal(50, slider.Native.Maximum);
        Assert.Equal(50, slider.Value);

        slider.Min = 200;
        slider.Max = 300;
        Assert.Equal(200, slider.Native.Minimum);
        Assert.Equal(300, slider.Native.Maximum);
        Assert.Equal(200, slider.Value);

        slider.Max = 150;
        slider.Min = 100;
        Assert.Equal(100, slider.Native.Minimum);
        Assert.Equal(150, slider.Native.Maximum);
    }

    [Fact]
    public void Slider_ignores_a_step_of_zero_and_disables_the_native_slider_and_dims()
    {
        var slider = new KlySlider { AccessibleName = "Volume", Step = 5 };

        slider.Step = 0;
        Assert.Equal(5, slider.Step);

        slider.IsEnabled = false;
        Assert.False(slider.Native.IsEnabled);
        Assert.Equal(0.6, slider.Opacity);
        Assert.Equal("Volume", SemanticProperties.GetDescription(slider.Native));
        Assert.True(slider.Native.MinimumHeightRequest >= 40);
    }

    // ---- KlyRangeSlider

    [Fact]
    public void RangeSlider_rests_on_the_limits_until_it_has_a_value()
    {
        var slider = new KlyRangeSlider { Min = 10, Max = 20 };

        Assert.Equal(10, slider.Low);
        Assert.Equal(20, slider.High);
        Assert.Null(slider.Value);
    }

    [Theory]
    [InlineData(100, 200, 16, 0, 100, 1, 50)]
    [InlineData(8, 200, 16, 0, 100, 1, 0)]
    [InlineData(192, 200, 16, 0, 100, 1, 100)]
    [InlineData(-30, 200, 16, 0, 100, 1, 0)]
    [InlineData(95, 216, 16, 0, 100, 5, 45)]
    public void RangeSlider_reads_the_value_under_a_touch(double position, double length, double thumb, double min, double max, double step, double expected)
    {
        Assert.Equal(expected, KlyRangeSlider.ValueAt(position, length, thumb, min, max, step));
    }

    [Theory]
    [InlineData(10, 20, 80, 0)]
    [InlineData(70, 20, 80, 1)]
    [InlineData(50, 20, 80, 0)]
    [InlineData(55, 20, 80, 1)]
    [InlineData(30, 40, 40, 0)]
    [InlineData(60, 40, 40, 1)]
    public void RangeSlider_a_touch_takes_the_nearest_thumb_and_on_a_tie_the_one_that_can_move_toward_it(double value, double low, double high, int expected)
    {
        Assert.Equal(expected, KlyRangeSlider.ThumbNear(value, low, high));
    }

    [Fact]
    public void RangeSlider_moving_a_thumb_keeps_the_lower_value_below_the_upper_one()
    {
        var slider = new KlyRangeSlider { Value = new[] { 20d, 60d } };

        slider.MoveThumb(0, 80);
        Assert.Equal([60d, 60d], slider.Value);

        slider.MoveThumb(1, 10);
        Assert.Equal([60d, 60d], slider.Value);

        slider.MoveThumb(0, 30);
        Assert.Equal([30d, 60d], slider.Value);
    }

    [Fact]
    public void RangeSlider_the_first_move_gives_it_a_value_and_raises_one_event_per_change()
    {
        var slider = new KlyRangeSlider();
        var events = new List<string>();
        slider.ValueChanged += (_, e) => events.Add(string.Join("-", e.NewValue!));

        slider.MoveThumb(0, 25);
        slider.MoveThumb(0, 25);
        slider.MoveThumb(1, 75);

        Assert.Equal(["25-100", "25-75"], events);
    }

    [Fact]
    public void RangeSlider_a_touch_on_the_view_moves_the_nearest_thumb()
    {
        var slider = new KlyRangeSlider { Value = new[] { 20d, 80d } };
        ((IView)slider).Arrange(new Rect(0, 0, 216, 40));
        var graphics = (IGraphicsView)slider;

        graphics.StartInteraction([new PointF(30, 20)]);
        graphics.DragInteraction([new PointF(108, 20)]);
        graphics.EndInteraction([new PointF(108, 20)], true);
        graphics.DragInteraction([new PointF(8, 20)]);

        Assert.Equal([50d, 80d], slider.Value);
    }

    [Fact]
    public void RangeSlider_orientation_gives_it_a_vertical_track_and_a_touch_target_of_forty()
    {
        var slider = new KlyRangeSlider();
        Assert.Equal(40, slider.HeightRequest);
        Assert.Equal(LayoutOptions.Fill, slider.HorizontalOptions);

        slider.Orientation = StackOrientation.Vertical;

        Assert.Equal(40, slider.WidthRequest);
        Assert.Equal(192, slider.HeightRequest);
    }

    [Theory]
    [InlineData(KlySize.Xs, 12)]
    [InlineData(KlySize.Sm, 14)]
    [InlineData(KlySize.Md, 16)]
    [InlineData(KlySize.Lg, 20)]
    [InlineData(KlySize.Xl, 24)]
    public void RangeSlider_size_is_the_size_of_the_thumbs(KlySize size, double thumb)
    {
        Assert.Equal(thumb, KlyRangeSlider.ThumbSize(size));
    }

    [Fact]
    public void RangeSlider_is_read_aloud_with_its_range_and_dims_when_disabled()
    {
        var slider = new KlyRangeSlider { Value = new[] { 10d, 30d }, AccessibleName = "Price" };

        Assert.Equal(string.Create(CultureInfo.CurrentCulture, $"Price, 10 - 30"), SemanticProperties.GetDescription(slider));

        slider.IsEnabled = false;
        Assert.Equal(0.6, slider.Opacity);
    }

    // ---- KlyToggle and KlyToggleGroup

    [Fact]
    public void Toggle_a_tap_flips_the_value_with_one_event_and_the_value_binds_both_ways()
    {
        var toggle = new KlyToggle { Label = "Bold" };
        var events = new List<bool>();
        toggle.ValueChanged += (_, e) => events.Add(e.NewValue);

        toggle.Press();
        toggle.Press();
        toggle.Value = true;

        Assert.Equal([true, false, true], events);
        Assert.Equal("Bold", toggle.Text.Text);
    }

    [Fact]
    public void Toggle_says_pressed_to_a_screen_reader_and_hides_an_empty_label()
    {
        var toggle = new KlyToggle();
        Assert.False(toggle.Text.IsVisible);

        toggle.Label = "Italic";
        Assert.True(toggle.Text.IsVisible);
        Assert.Equal("Italic", SemanticProperties.GetDescription(toggle));

        toggle.Value = true;
        Assert.Equal("Italic, pressed", SemanticProperties.GetDescription(toggle));
    }

    [Theory]
    [InlineData(ToggleVariant.Outline, false, 1)]
    [InlineData(ToggleVariant.Outline, true, 1)]
    [InlineData(ToggleVariant.Soft, false, 0)]
    [InlineData(ToggleVariant.Soft, true, 1)]
    [InlineData(ToggleVariant.Ghost, false, 0)]
    [InlineData(ToggleVariant.Ghost, true, 1)]
    public void Toggle_variant_and_state_decide_the_border(ToggleVariant variant, bool pressed, double border)
    {
        var toggle = new KlyToggle { Variant = variant, Value = pressed };

        Assert.Equal(border, toggle.StrokeThickness);
    }

    [Theory]
    [InlineData(KlySize.Xs, 12, 28)]
    [InlineData(KlySize.Sm, 12, 32)]
    [InlineData(KlySize.Md, 14, 40)]
    [InlineData(KlySize.Lg, 14, 44)]
    [InlineData(KlySize.Xl, 16, 48)]
    public void Toggle_size_sets_the_font_and_a_touch_target(KlySize size, double font, double minHeight)
    {
        var toggle = new KlyToggle { Label = "A", Size = size };

        Assert.Equal(font, toggle.Text.FontSize);
        Assert.Equal(minHeight, toggle.MinimumHeightRequest);
    }

    [Fact]
    public void Toggle_draws_an_icon_before_the_label_and_dims_when_disabled()
    {
        var icon = new KlyIcon { Name = KlyIconNames.Star };
        var toggle = new KlyToggle { Label = "Star", Icon = icon };

        var row = Assert.IsType<HorizontalStackLayout>(toggle.Content);
        Assert.Same(icon, row.Children[0]);

        toggle.IsEnabled = false;
        toggle.Press();
        Assert.False(toggle.Value);
        Assert.Equal(0.6, toggle.Opacity);
    }

    private static KlyToggleGroup Alignments(Action<KlyToggleGroup>? configure = null)
    {
        var group = new KlyToggleGroup
        {
            Items = new[] { new KlyToggleItem("left", "Left"), new KlyToggleItem("center", "Center"), new KlyToggleItem("right", "Right") { Disabled = true } },
        };
        configure?.Invoke(group);
        return group;
    }

    [Fact]
    public void ToggleGroup_has_one_toggle_per_item_laid_out_horizontally()
    {
        var group = Alignments();

        Assert.Equal(StackOrientation.Horizontal, group.Orientation);
        Assert.Equal(["Left", "Center", "Right"], group.Toggles.Select(t => t.Toggle.Label));
        Assert.Equal([true, true, false], group.Toggles.Select(t => t.Toggle.IsEnabled));
    }

    [Fact]
    public void ToggleGroup_single_pressing_releases_the_others_and_pressing_the_pressed_one_releases_it()
    {
        var group = Alignments();
        var events = new List<string>();
        group.ValueChanged += (_, e) => events.Add(string.Join(",", e.NewValue!));

        group.Toggles[0].Toggle.Press();
        group.Toggles[1].Toggle.Press();
        Assert.Equal(["center"], group.Value);
        Assert.Equal([false, true, false], group.Toggles.Select(t => t.Toggle.Value));

        group.Toggles[1].Toggle.Press();
        Assert.Empty(group.Value!);
        Assert.Equal(["left", "center", ""], events);
    }

    [Fact]
    public void ToggleGroup_multiple_presses_any_number_of_buttons()
    {
        var group = Alignments(g => g.Multiple = true);

        group.Toggles[0].Toggle.Press();
        group.Toggles[1].Toggle.Press();
        group.Toggles[0].Toggle.Press();

        Assert.Equal(["center"], group.Value);

        group.Value = ["left", "center"];
        Assert.Equal([true, true, false], group.Toggles.Select(t => t.Toggle.Value));
    }

    [Fact]
    public void ToggleGroup_gives_its_look_to_every_button_and_a_vertical_group_stretches_them()
    {
        var group = Alignments(g =>
        {
            g.Variant = ToggleVariant.Ghost;
            g.Size = KlySize.Lg;
            g.Color = KlyColor.Info;
        });
        Assert.All(group.Toggles, t =>
        {
            Assert.Equal(ToggleVariant.Ghost, t.Toggle.Variant);
            Assert.Equal(KlySize.Lg, t.Toggle.Size);
            Assert.Equal(KlyColor.Info, t.Toggle.Color);
            Assert.Equal(LayoutOptions.Start, t.Toggle.HorizontalOptions);
        });

        group.Orientation = StackOrientation.Vertical;
        Assert.All(group.Toggles, t => Assert.Equal(LayoutOptions.Fill, t.Toggle.HorizontalOptions));
    }

    [Fact]
    public void ToggleGroup_a_disabled_group_disables_every_button_and_names_the_group()
    {
        var group = Alignments(g => g.AccessibleName = "Alignment");

        group.IsEnabled = false;
        Assert.All(group.Toggles, t => Assert.False(t.Toggle.IsEnabled));
        Assert.Equal("Alignment", SemanticProperties.GetDescription(group));

        group.IsEnabled = true;
        Assert.Equal([true, true, false], group.Toggles.Select(t => t.Toggle.IsEnabled));
    }

    // ---- KlyRadioGroup

    private static KlyRadioGroup Plans(Action<KlyRadioGroup>? configure = null)
    {
        var group = new KlyRadioGroup
        {
            Legend = "Plan",
            Items = new[]
            {
                new KlyRadioItem("free", "Free") { Description = "For hobbies" },
                new KlyRadioItem("pro", "Pro"),
                new KlyRadioItem("team", "Team") { Disabled = true },
            },
        };
        configure?.Invoke(group);
        return group;
    }

    [Fact]
    public void RadioGroup_has_a_radio_button_per_item_in_one_native_group_with_a_legend()
    {
        var group = Plans();

        Assert.Equal(3, group.Radios.Count);
        Assert.Single(group.Radios.Select(r => r.Radio.GroupName).Distinct());
        Assert.Equal("Plan", group.ChoiceLayout.Legend.Text);
        Assert.True(group.ChoiceLayout.Legend.IsVisible);
        Assert.Equal([true, true, false], group.Radios.Select(r => r.Radio.IsEnabled));
        Assert.Equal("Free, For hobbies", SemanticProperties.GetDescription(group.Radios[0].Radio));
    }

    [Fact]
    public void RadioGroup_two_groups_do_not_share_their_radio_buttons()
    {
        Assert.NotEqual(Plans().Radios[0].Radio.GroupName, Plans().Radios[0].Radio.GroupName);
    }

    [Fact]
    public void RadioGroup_checking_a_radio_sets_the_value_and_setting_the_value_checks_a_radio()
    {
        var group = Plans();
        var events = new List<object?>();
        group.ValueChanged += (_, e) => events.Add(e.NewValue);

        group.Radios[1].Radio.IsChecked = true;
        Assert.Equal("pro", group.Value);

        group.Value = "free";
        Assert.Equal([true, false, false], group.Radios.Select(r => r.Radio.IsChecked));
        Assert.Equal(["pro", "free"], events);

        group.Value = null;
        Assert.Equal([false, false, false], group.Radios.Select(r => r.Radio.IsChecked));
        Assert.Equal(["pro", "free", null], events);
    }

    [Fact]
    public void RadioGroup_a_value_set_before_the_items_checks_its_radio_once_they_arrive()
    {
        var group = new KlyRadioGroup { Value = "b" };

        group.Items = new[] { new KlyRadioItem("a", "A"), new KlyRadioItem("b", "B") };

        Assert.Equal([false, true], group.Radios.Select(r => r.Radio.IsChecked));
    }

    [Fact]
    public void RadioGroup_orientation_stacks_or_wraps_the_choices_and_the_size_is_their_font()
    {
        var group = Plans(g => g.Size = KlySize.Xl);
        Assert.Equal(Microsoft.Maui.Layouts.FlexDirection.Column, group.ChoiceLayout.Items.Direction);
        Assert.Equal(16, group.ChoiceLayout.Legend.FontSize);

        group.Orientation = StackOrientation.Horizontal;
        Assert.Equal(Microsoft.Maui.Layouts.FlexDirection.Row, group.ChoiceLayout.Items.Direction);
        Assert.Equal(Microsoft.Maui.Layouts.FlexWrap.Wrap, group.ChoiceLayout.Items.Wrap);
        Assert.All(group.Radios, r => Assert.Equal(new Thickness(0, 0, 16, 6), r.Radio.Margin));
    }

    [Fact]
    public void RadioGroup_draws_an_error_frame_while_invalid_and_disables_every_radio_with_the_group()
    {
        var group = Plans(g => g.Invalid = true);
        Assert.Equal(2, group.StrokeThickness);

        group.Invalid = false;
        Assert.Equal(0, group.StrokeThickness);

        group.IsEnabled = false;
        Assert.All(group.Radios, r => Assert.False(r.Radio.IsEnabled));
    }

    // ---- KlyCheckboxGroup

    private static KlyCheckboxGroup Toppings(Action<KlyCheckboxGroup>? configure = null)
    {
        var group = new KlyCheckboxGroup
        {
            Legend = "Toppings",
            Items = new[]
            {
                new KlyCheckboxItem("ham", "Ham"),
                new KlyCheckboxItem("cheese", "Cheese") { Description = "Extra" },
                new KlyCheckboxItem("olives", "Olives") { Disabled = true },
            },
        };
        configure?.Invoke(group);
        return group;
    }

    [Fact]
    public void CheckboxGroup_has_a_checkbox_per_item_with_its_text_and_description()
    {
        var group = Toppings();

        Assert.Equal(["Ham", "Cheese", "Olives"], group.Boxes.Select(b => b.Box.Text));
        Assert.Equal("Extra", group.Boxes[1].Box.Description);
        Assert.Equal([true, true, false], group.Boxes.Select(b => b.Box.IsEnabled));
        Assert.Equal("Toppings", group.ChoiceLayout.Legend.Text);
    }

    [Fact]
    public void CheckboxGroup_keeps_the_order_of_the_items_whatever_the_order_the_boxes_were_checked_in()
    {
        var group = Toppings();
        var events = new List<string>();
        group.ValueChanged += (_, e) => events.Add(string.Join(",", e.NewValue!));

        group.Boxes[1].Box.IsChecked = true;
        group.Boxes[0].Box.IsChecked = true;
        group.Boxes[1].Box.IsChecked = false;

        Assert.Equal(["ham"], group.Value);
        Assert.Equal(["cheese", "ham,cheese", "ham"], events);
    }

    [Fact]
    public void CheckboxGroup_setting_the_value_checks_the_boxes_without_raising_the_group_twice()
    {
        var group = Toppings();
        var events = 0;
        group.ValueChanged += (_, _) => events++;

        group.Value = new object[] { "cheese", "ham" };

        Assert.Equal([true, true, false], group.Boxes.Select(b => b.Box.IsChecked));
        Assert.Equal(1, events);
        Assert.Equal(["cheese", "ham"], group.Value);
    }

    [Fact]
    public void CheckboxGroup_orientation_color_error_frame_and_disabled_state_follow_the_group()
    {
        var group = Toppings(g =>
        {
            g.Orientation = StackOrientation.Horizontal;
            g.Color = KlyColor.Success;
            g.Invalid = true;
            g.AccessibleName = "Toppings";
        });

        Assert.Equal(Microsoft.Maui.Layouts.FlexDirection.Row, group.ChoiceLayout.Items.Direction);
        Assert.All(group.Boxes, b => Assert.Equal(KlyColor.Success, b.Box.Color));
        Assert.Equal(2, group.StrokeThickness);
        Assert.Equal("Toppings", SemanticProperties.GetDescription(group));

        group.IsEnabled = false;
        Assert.All(group.Boxes, b => Assert.False(b.Box.IsEnabled));
    }

    // ---- KlyCalendar

    private static KlyCalendar March(Action<KlyCalendar>? configure = null)
    {
        var calendar = new KlyCalendar { Culture = CommaCulture(), DisplayedMonth = new DateOnly(2026, 3, 17) };
        configure?.Invoke(calendar);
        return calendar;
    }

    [Fact]
    public void Calendar_shows_one_month_with_a_day_for_each_date_and_a_heading_in_its_culture()
    {
        var calendar = March();

        Assert.Equal(new DateOnly(2026, 3, 1), calendar.DisplayedMonth);
        Assert.Equal("March 2026", calendar.Heading!.Text);
        Assert.Equal(31, calendar.Days.Count);
        Assert.Equal(["1", "2"], calendar.Days.Take(2).Select(d => d.Label.Text));
    }

    [Fact]
    public void Calendar_starts_its_weeks_on_the_first_day_of_the_culture_or_on_week_start()
    {
        var calendar = March();
        var grid = (Grid)calendar.Children[1];

        Assert.Equal("Mo", ((Label)grid.Children[0]).Text);
        // 1 March 2026 is a Sunday: the last of the seven columns when weeks start on Monday.
        Assert.Equal(6, Grid.GetColumn(calendar.Days[0]));

        calendar.WeekStart = DayOfWeek.Sunday;
        grid = (Grid)calendar.Children[1];
        Assert.Equal("Su", ((Label)grid.Children[0]).Text);
        Assert.Equal(0, Grid.GetColumn(calendar.Days[0]));
    }

    [Fact]
    public void Calendar_single_mode_selects_a_date_with_one_event_and_a_second_tap_on_it_changes_nothing()
    {
        var calendar = March();
        var events = new List<DateOnly?>();
        calendar.ValueChanged += (_, e) => events.Add(e.NewValue);

        calendar.Days[9].Press();
        calendar.Days[9].Press();

        Assert.Equal(new DateOnly(2026, 3, 10), calendar.Value);
        Assert.Equal([new DateOnly(2026, 3, 10)], events);
        Assert.True(calendar.Days[9].Selected);
        Assert.EndsWith(", selected", SemanticProperties.GetDescription(calendar.Days[9]));
    }

    [Fact]
    public void Calendar_multiple_mode_toggles_dates_and_keeps_them_in_order()
    {
        var calendar = March(c => c.Mode = KlyCalendarMode.Multiple);

        calendar.Days[19].Press();
        calendar.Days[4].Press();
        calendar.Days[11].Press();
        calendar.Days[19].Press();

        Assert.Equal([new DateOnly(2026, 3, 5), new DateOnly(2026, 3, 12)], calendar.Values);
        Assert.Equal([true, true], new[] { calendar.Days[4].Selected, calendar.Days[11].Selected });
    }

    [Fact]
    public void Calendar_range_mode_the_first_date_starts_the_second_ends_and_the_third_starts_another()
    {
        var calendar = March(c => c.Mode = KlyCalendarMode.Range);
        var events = new List<KlyDateRange?>();
        calendar.RangeChanged += (_, e) => events.Add(e.NewValue);

        calendar.Days[9].Press();
        Assert.Equal(new KlyDateRange(new DateOnly(2026, 3, 10), null), calendar.Range);

        calendar.Days[4].Press();
        Assert.Equal(new KlyDateRange(new DateOnly(2026, 3, 5), new DateOnly(2026, 3, 10)), calendar.Range);

        Assert.Equal([false, false, false, false, true, false, false, false, false, true], calendar.Days.Take(10).Select(d => d.Selected));
        Assert.Equal([6, 7, 8, 9], calendar.Days.Where(d => d.InRange).Select(d => d.Date.Day));

        calendar.Days[14].Press();
        Assert.Equal(new KlyDateRange(new DateOnly(2026, 3, 15), null), calendar.Range);
        Assert.Equal(3, events.Count);
    }

    [Fact]
    public void Calendar_range_mode_marks_the_dates_between_the_ends()
    {
        var calendar = March(c =>
        {
            c.Mode = KlyCalendarMode.Range;
            c.Range = new KlyDateRange(new DateOnly(2026, 3, 5), new DateOnly(2026, 3, 9));
        });

        Assert.Equal([6, 7, 8], calendar.Days.Where(d => d.InRange).Select(d => d.Date.Day));
        Assert.Equal([5, 9], calendar.Days.Where(d => d.Selected).Select(d => d.Date.Day));
    }

    [Fact]
    public void Calendar_dates_outside_min_and_max_and_in_disabled_dates_cannot_be_picked()
    {
        var calendar = March(c =>
        {
            c.Min = new DateOnly(2026, 3, 5);
            c.Max = new DateOnly(2026, 3, 20);
            c.DisabledDates = [new DateOnly(2026, 3, 10)];
        });

        calendar.Days[0].Press();
        calendar.Days[9].Press();
        calendar.Days[24].Press();
        Assert.Null(calendar.Value);
        Assert.True(calendar.Days[0].Disabled);
        Assert.True(calendar.Days[9].Disabled);
        Assert.True(calendar.Days[24].Disabled);
        Assert.Equal(TextDecorations.Strikethrough, calendar.Days[9].Label.TextDecorations);

        calendar.Days[10].Press();
        Assert.Equal(new DateOnly(2026, 3, 11), calendar.Value);
    }

    [Fact]
    public void Calendar_buttons_change_the_month_and_stop_at_min_and_max()
    {
        var calendar = March();

        calendar.NextButton!.Press();
        Assert.Equal(new DateOnly(2026, 4, 1), calendar.DisplayedMonth);
        Assert.Equal("April 2026", calendar.Heading!.Text);
        Assert.Equal(30, calendar.Days.Count);

        calendar.PreviousButton!.Press();
        calendar.PreviousButton!.Press();
        Assert.Equal(new DateOnly(2026, 2, 1), calendar.DisplayedMonth);
        Assert.Equal(28, calendar.Days.Count);

        calendar.Min = new DateOnly(2026, 2, 1);
        calendar.Max = new DateOnly(2026, 2, 28);
        Assert.False(calendar.PreviousButton!.IsEnabled);
        Assert.False(calendar.NextButton!.IsEnabled);
        Assert.Equal("Previous month", SemanticProperties.GetDescription(calendar.PreviousButton));
        Assert.Equal("Next month", SemanticProperties.GetDescription(calendar.NextButton));
    }

    [Fact]
    public void Calendar_a_value_set_by_code_in_another_month_shows_that_month()
    {
        var calendar = March();

        calendar.Value = new DateOnly(2027, 8, 20);

        Assert.Equal(new DateOnly(2027, 8, 1), calendar.DisplayedMonth);
        Assert.Equal("August 2027", calendar.Heading!.Text);
        Assert.True(calendar.Days[19].Selected);
    }

    [Fact]
    public void Calendar_marks_today_and_gives_the_selected_day_the_semantic_color()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var calendar = new KlyCalendar { Culture = CommaCulture(), Value = today };

        var day = calendar.Days[today.Day - 1];
        Assert.True(day.Today);
        Assert.True(day.Selected);
        Assert.Equal(1, day.StrokeThickness);
    }

    [Theory]
    [InlineData(KlySize.Xs, 28, 12, 12)]
    [InlineData(KlySize.Sm, 32, 12, 12)]
    [InlineData(KlySize.Md, 36, 14, 12)]
    [InlineData(KlySize.Lg, 40, 14, 14)]
    [InlineData(KlySize.Xl, 44, 16, 14)]
    public void Calendar_size_is_the_size_of_a_day_and_the_fonts(KlySize size, double dimension, double dayFont, double weekdayFont)
    {
        var calendar = March(c => c.Size = size);

        Assert.Equal(dimension, calendar.Days[0].WidthRequest);
        Assert.Equal(dimension, calendar.Days[0].HeightRequest);
        Assert.Equal(dayFont, calendar.Days[0].Label.FontSize);
        Assert.Equal(weekdayFont, ((Label)((Grid)calendar.Children[1]).Children[0]).FontSize);
    }

    [Fact]
    public void Calendar_a_disabled_calendar_ignores_taps_and_dims()
    {
        var calendar = March();

        calendar.IsEnabled = false;
        calendar.Days[3].Press();

        Assert.Null(calendar.Value);
        Assert.Equal(0.6, calendar.Opacity);
        Assert.False(calendar.NextButton!.IsEnabled);
    }

    [Fact]
    public void Calendar_range_days_have_square_corners_and_the_others_the_radius()
    {
        var calendar = March(c =>
        {
            c.Mode = KlyCalendarMode.Range;
            c.Range = new KlyDateRange(new DateOnly(2026, 3, 5), new DateOnly(2026, 3, 9));
        });

        Assert.Equal(0, ((RoundRectangle)calendar.Days[6].StrokeShape!).CornerRadius.TopLeft);
        Assert.Equal(KlyTheme.Radius, ((RoundRectangle)calendar.Days[0].StrokeShape!).CornerRadius.TopLeft);
    }

    // ---- KlyColorPicker

    [Theory]
    [InlineData("#abc", "#aabbcc")]
    [InlineData("abc", "#aabbcc")]
    [InlineData("#AABBCC", "#aabbcc")]
    [InlineData("  3b5900 ", "#3b5900")]
    [InlineData("#abcd", null)]
    [InlineData("#ggg", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void ColorPicker_reads_hex_text(string? text, string? expected)
    {
        Assert.Equal(expected, InputsColor.Normalize(text));
    }

    [Theory]
    [InlineData("#ff0000", 0, 1, 1)]
    [InlineData("#00ff00", 120, 1, 1)]
    [InlineData("#0000ff", 240, 1, 1)]
    [InlineData("#ffffff", 0, 0, 1)]
    [InlineData("#000000", 0, 0, 0)]
    [InlineData("#808080", 0, 0, 0.5019607843137255)]
    public void ColorPicker_converts_between_hex_and_hsv(string hex, double hue, double saturation, double brightness)
    {
        var (h, s, v) = InputsColor.ToHsv(hex);

        Assert.Equal(hue, h);
        Assert.Equal(saturation, s, 6);
        Assert.Equal(brightness, v, 6);
        Assert.Equal(hex, InputsColor.ToHex(h, s, v));
    }

    [Fact]
    public void ColorPicker_converts_every_primary_and_secondary_hue_back_to_its_hex()
    {
        string[] hexes = ["#ff0000", "#ffff00", "#00ff00", "#00ffff", "#0000ff", "#ff00ff"];

        for (var i = 0; i < hexes.Length; i++) Assert.Equal(hexes[i], InputsColor.ToHex(i * 60, 1, 1));
        Assert.Equal(hexes[0], InputsColor.ToHex(360, 1, 1));
    }

    [Fact]
    public void ColorPicker_shows_the_value_in_the_hex_field_the_swatch_and_the_controls()
    {
        var picker = new KlyColorPicker();
        var events = new List<string?>();
        picker.ValueChanged += (_, e) => events.Add(e.NewValue);

        picker.Value = "#3b5900";

        Assert.Equal("#3b5900", picker.HexField.Text);
        Assert.Equal(Color.FromArgb("#3b5900"), picker.Swatch.BackgroundColor);
        Assert.Equal([events.Single()], new[] { "#3b5900" });
    }

    [Fact]
    public void ColorPicker_accepts_a_short_hex_value_and_blanks_a_value_that_is_not_a_color()
    {
        var picker = new KlyColorPicker { Value = "#abc" };
        Assert.Equal("#aabbcc", picker.HexField.Text);

        picker.Value = "not a color";
        Assert.Equal(string.Empty, picker.HexField.Text);
        Assert.Equal(Colors.Transparent, picker.Swatch.BackgroundColor);
    }

    [Theory]
    [InlineData(0, 0, 200, 100, 0, 1)]
    [InlineData(200, 100, 200, 100, 1, 0)]
    [InlineData(100, 50, 200, 100, 0.5, 0.5)]
    [InlineData(-10, 500, 200, 100, 0, 0)]
    [InlineData(10, 10, 0, 0, 0, 0)]
    public void ColorPicker_the_area_reads_saturation_to_the_right_and_brightness_upward(double x, double y, double width, double height, double saturation, double brightness)
    {
        Assert.Equal((saturation, brightness), InputsColorArea.At(x, y, width, height));
    }

    [Theory]
    [InlineData(0, 300, 0)]
    [InlineData(300, 300, 359)]
    [InlineData(150, 300, 180)]
    [InlineData(-5, 300, 0)]
    public void ColorPicker_the_hue_bar_reads_the_hue(double x, double width, double hue)
    {
        Assert.Equal(hue, InputsHueBar.At(x, width));
    }

    [Fact]
    public void ColorPicker_touching_the_area_and_the_bar_sets_the_value()
    {
        var picker = new KlyColorPicker { Value = "#ff0000" };

        picker.Area.Move(new PointF(0, 0), 200, 100);
        Assert.Equal("#ffffff", picker.Value);

        picker.Area.Move(new PointF(200, 0), 200, 100);
        Assert.Equal("#ff0000", picker.Value);

        picker.Area.Move(new PointF(200, 100), 200, 100);
        Assert.Equal("#000000", picker.Value);

        picker.Area.Move(new PointF(200, 0), 200, 100);
        picker.HueBar.Move(new PointF(150, 10), 300);
        Assert.Equal("#00ffff", picker.Value);
    }

    [Fact]
    public void ColorPicker_a_gray_chosen_on_the_area_keeps_the_hue_but_a_gray_set_by_code_does_not()
    {
        var picker = new KlyColorPicker { Value = "#00ff00" };

        picker.Area.Move(new PointF(0, 0), 200, 100);
        Assert.Equal("#ffffff", picker.Value);
        picker.Area.Move(new PointF(200, 0), 200, 100);
        Assert.Equal("#00ff00", picker.Value);

        picker.Value = "#ffffff";
        picker.Value = "#808080";
        picker.Area.Move(new PointF(200, 0), 200, 100);
        Assert.Equal("#ff0000", picker.Value);
    }

    [Fact]
    public void ColorPicker_typed_hex_text_becomes_the_value_when_it_is_valid_and_is_put_back_when_not()
    {
        var picker = new KlyColorPicker { Value = "#112233" };

        picker.HexField.Text = "#abc";
        picker.CommitText();
        Assert.Equal("#aabbcc", picker.Value);

        picker.HexField.Text = "oops";
        picker.CommitText();
        Assert.Equal("#aabbcc", picker.Value);
        Assert.Equal("#aabbcc", picker.HexField.Text);
    }

    [Fact]
    public void ColorPicker_names_its_parts_forwards_its_look_and_disables_its_controls()
    {
        var picker = new KlyColorPicker { Variant = FieldVariant.Soft, Size = KlySize.Lg, Invalid = true, AccessibleName = "Brand color" };

        Assert.Equal("Hex color", picker.HexField.AccessibleName);
        Assert.Equal("Saturation and brightness", SemanticProperties.GetDescription(picker.Area));
        Assert.Equal("Hue", SemanticProperties.GetDescription(picker.HueBar));
        Assert.Equal("Brand color", SemanticProperties.GetDescription(picker));
        Assert.Equal(FieldVariant.Soft, picker.HexField.Variant);
        Assert.Equal(KlySize.Lg, picker.HexField.Size);
        Assert.True(picker.HexField.Invalid);
        Assert.Equal(7, picker.HexField.MaxLength);

        picker.IsEnabled = false;
        Assert.False(picker.Area.IsEnabled);
        Assert.False(picker.HueBar.IsEnabled);
        Assert.False(picker.HexField.IsEnabled);
        Assert.Equal(0.6, picker.Opacity);
    }

    // ---- KlyFileUpload

    private static string TempFile(string name, int bytes)
    {
        var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "klyroo-upload-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = System.IO.Path.Combine(directory, name);
        File.WriteAllBytes(path, new byte[bytes]);
        return path;
    }

    private static FileResult NewFile(string name, int bytes) => new(TempFile(name, bytes));

    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(512, "512 B")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(2 * 1024 * 1024, "2 MB")]
    [InlineData(3L * 1024 * 1024 * 1024, "3 GB")]
    public void FileUpload_formats_sizes_like_the_web_component(long bytes, string expected)
    {
        Assert.Equal(expected, KlyFileUpload.FormatSize(bytes, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void FileUpload_accepts_a_file_and_lists_it_with_its_size_and_a_remove_button()
    {
        var upload = new KlyFileUpload();
        var events = new List<int>();
        upload.FilesChanged += (_, e) => events.Add(e.NewValue!.Count);

        upload.Take([NewFile("a.txt", 2048)]);

        Assert.Equal(["a.txt"], upload.Files.Select(f => f.FileName));
        Assert.Null(upload.Error);
        Assert.False(upload.ErrorLabel.IsVisible);
        Assert.Single(upload.RemoveButtons);
        Assert.Equal("Remove a.txt", SemanticProperties.GetDescription(upload.RemoveButtons[0]));
        Assert.Equal([1], events);
    }

    [Fact]
    public void FileUpload_keeps_one_file_unless_multiple_and_names_the_ones_left_out()
    {
        var upload = new KlyFileUpload();

        upload.Take([NewFile("a.txt", 1), NewFile("b.txt", 1), NewFile("c.txt", 1)]);

        Assert.Equal(["a.txt"], upload.Files.Select(f => f.FileName));
        Assert.Equal("At most 1 files are allowed.", upload.Error);
        Assert.True(upload.ErrorLabel.IsVisible);

        upload.Multiple = true;
        upload.Take([NewFile("a.txt", 1), NewFile("b.txt", 1), NewFile("c.txt", 1)]);
        Assert.Equal(3, upload.Files.Count);
        Assert.Null(upload.Error);
    }

    [Fact]
    public void FileUpload_max_files_limits_a_multiple_upload_with_one_message()
    {
        var upload = new KlyFileUpload { Multiple = true, MaxFiles = 2 };

        upload.Take([NewFile("a.txt", 1), NewFile("b.txt", 1), NewFile("c.txt", 1), NewFile("d.txt", 1)]);

        Assert.Equal(["a.txt", "b.txt"], upload.Files.Select(f => f.FileName));
        Assert.Equal("At most 2 files are allowed.", upload.Error);
    }

    [Fact]
    public void FileUpload_leaves_out_files_that_are_too_large_or_not_of_an_accepted_type()
    {
        var upload = new KlyFileUpload { Multiple = true, MaxSize = 1024, Accept = ".png,image/jpeg" };

        upload.Take([NewFile("small.png", 100), NewFile("big.png", 5000), NewFile("photo.jpg", 10), NewFile("notes.txt", 10)]);

        Assert.Equal(["small.png", "photo.jpg"], upload.Files.Select(f => f.FileName));
        Assert.Equal("big.png is larger than 1 KB. notes.txt is not an accepted file type.", upload.Error);
    }

    [Fact]
    public void FileUpload_remove_button_removes_its_file_and_clears_the_error()
    {
        var upload = new KlyFileUpload { Multiple = true, MaxFiles = 1 };
        upload.Take([NewFile("a.txt", 1), NewFile("b.txt", 1)]);
        var events = new List<int>();
        upload.FilesChanged += (_, e) => events.Add(e.NewValue!.Count);

        upload.RemoveButtons[0].Press();

        Assert.Empty(upload.Files);
        Assert.Null(upload.Error);
        Assert.Equal([0], events);
    }

    [Fact]
    public async Task FileUpload_a_new_pick_replaces_the_files_and_a_cancelled_pick_keeps_them()
    {
        var upload = new KlyFileUpload { Multiple = true };
        upload.Take([NewFile("a.txt", 1)]);
        PickOptions? seen = null;
        var multiple = false;
        IEnumerable<FileResult>? answer = [NewFile("b.txt", 1), NewFile("c.txt", 1)];
        upload.Picker = (options, many) =>
        {
            seen = options;
            multiple = many;
            return Task.FromResult<IEnumerable<FileResult>?>(answer);
        };

        await upload.PickAsync();
        Assert.Equal(["b.txt", "c.txt"], upload.Files.Select(f => f.FileName));
        Assert.True(multiple);
        Assert.Equal("Drop files here or click to browse", seen!.PickerTitle);

        answer = null;
        await upload.PickAsync();
        Assert.Equal(2, upload.Files.Count);
    }

    [Fact]
    public async Task FileUpload_a_disabled_upload_does_not_open_the_picker_and_dims()
    {
        var upload = new KlyFileUpload { IsEnabled = false };
        var opened = false;
        upload.Picker = (_, _) =>
        {
            opened = true;
            return Task.FromResult<IEnumerable<FileResult>?>(null);
        };

        await upload.PickAsync();

        Assert.False(opened);
        Assert.Equal(0.6, upload.Opacity);
    }

    [Fact]
    public void FileUpload_clearing_removes_every_file()
    {
        var upload = new KlyFileUpload { Multiple = true };
        upload.Take([NewFile("a.txt", 1), NewFile("b.txt", 1)]);

        upload.ClearFiles();

        Assert.Empty(upload.Files);
        Assert.Empty(upload.RemoveButtons);
    }

    [Fact]
    public void FileUpload_shows_its_texts_in_the_drop_zone_and_a_dashed_border()
    {
        var upload = new KlyFileUpload { Label = "Add a photo", Hint = "PNG or JPEG", MaxSize = 2 * 1024 * 1024, Invalid = true };

        var texts = ((VerticalStackLayout)upload.DropZone.Content!).Children.OfType<Label>().Select(l => l.Text).ToList();
        Assert.Equal(["Add a photo", "PNG or JPEG", "Maximum size 2 MB"], texts);
        Assert.NotNull(upload.DropZone.StrokeDashArray);
        Assert.Equal("Add a photo", SemanticProperties.GetDescription(upload.DropZone));
    }

    [Fact]
    public void FileUpload_accept_lists_are_read_as_extensions_mime_types_and_type_identifiers()
    {
        var (extensions, mimes, utis) = InputsFileTypes.Parse(".png, image/*, application/pdf");

        Assert.Equal([".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp", ".svg", ".pdf"], extensions);
        Assert.Equal(["image/png", "image/*", "application/pdf"], mimes);
        Assert.Equal(["public.png", "public.image", "com.adobe.pdf"], utis);
        Assert.Null(InputsFileTypes.ToPickerType(null));
        Assert.Null(InputsFileTypes.ToPickerType("  "));
        Assert.NotNull(InputsFileTypes.ToPickerType(".png"));
    }

    [Theory]
    [InlineData("photo.PNG", "image/png", ".png", true)]
    [InlineData("photo.png", "image/png", "image/*", true)]
    [InlineData("photo.png", "image/png", "application/pdf", false)]
    [InlineData("doc.pdf", "application/pdf", "application/pdf,.png", true)]
    [InlineData("doc.pdf", null, "image/*", false)]
    [InlineData("anything", null, null, true)]
    public void FileUpload_matches_a_file_against_the_accept_list(string name, string? type, string? accept, bool expected)
    {
        Assert.Equal(expected, InputsFileTypes.Matches(name, type, accept));
    }

    // ---- KlyForm and KlyFormField

    private sealed class Person : INotifyPropertyChanged
    {
        private string? _name;
        private string? _email;

        public event PropertyChangedEventHandler? PropertyChanged;

        [Required(ErrorMessage = "Name is required.")]
        public string? Name
        {
            get => _name;
            set => Set(ref _name, value);
        }

        [EmailAddress(ErrorMessage = "Email is not valid.")]
        [Required(ErrorMessage = "Email is required.")]
        public string? Email
        {
            get => _email;
            set => Set(ref _email, value);
        }

        private void Set(ref string? field, string? value, [CallerMemberName] string? property = null)
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }

    [Fact]
    public void Form_submitting_an_invalid_model_raises_invalid_submit_and_names_the_messages_of_each_field()
    {
        var form = new KlyForm { Model = new Person() };
        var raised = new List<string>();
        form.InvalidSubmit += (_, _) => raised.Add("invalid");
        form.ValidSubmit += (_, _) => raised.Add("valid");

        var valid = form.Submit();

        Assert.False(valid);
        Assert.Equal(["invalid"], raised);
        Assert.Equal(["Name is required."], form.GetMessages("Name"));
        Assert.Contains("Email is required.", form.GetMessages("Email"));
        Assert.Empty(form.GetMessages("Other"));
    }

    [Fact]
    public void Form_submitting_a_valid_model_raises_valid_submit()
    {
        var form = new KlyForm { Model = new Person { Name = "Ada", Email = "ada@example.com" } };
        var raised = new List<string>();
        form.InvalidSubmit += (_, _) => raised.Add("invalid");
        form.ValidSubmit += (_, _) => raised.Add("valid");

        Assert.True(form.Submit());

        Assert.Equal(["valid"], raised);
        Assert.Empty(form.Messages);
    }

    [Fact]
    public void Form_validates_a_property_when_the_model_raises_a_change_for_it_and_leaves_the_other_messages()
    {
        var person = new Person();
        var form = new KlyForm { Model = person };
        form.ValidateModel();

        person.Name = "Ada";

        Assert.Empty(form.GetMessages("Name"));
        Assert.NotEmpty(form.GetMessages("Email"));

        person.Email = "nope";
        Assert.Equal(["Email is not valid."], form.GetMessages("Email"));
    }

    [Fact]
    public void Form_does_not_validate_when_validate_is_off_or_there_is_no_model()
    {
        var off = new KlyForm { Model = new Person(), Validate = false };
        var none = new KlyForm();
        var raised = 0;
        off.ValidSubmit += (_, _) => raised++;

        Assert.True(off.Submit());
        Assert.True(none.Submit());
        Assert.Equal(1, raised);
        Assert.Empty(off.Messages);
    }

    [Fact]
    public void Form_stops_listening_to_the_model_it_no_longer_has()
    {
        var first = new Person();
        var form = new KlyForm { Model = first };
        form.Model = new Person();

        first.Name = null;

        Assert.Empty(form.Messages);
    }

    [Fact]
    public void Form_shows_the_summary_of_every_message_only_when_asked_to_and_there_are_some()
    {
        var form = new KlyForm { Model = new Person(), ShowSummary = true };
        Assert.False(form.Summary.IsVisible);

        form.Submit();
        Assert.True(form.Summary.IsVisible);
        var lines = ((VerticalStackLayout)form.Summary.Content!).Children.OfType<Label>().Select(l => l.Text).ToList();
        Assert.Equal("Please fix the following errors", lines[0]);
        Assert.Contains("• Name is required.", lines);

        form.ShowSummary = false;
        Assert.False(form.Summary.IsVisible);
    }

    [Fact]
    public void Form_busy_or_disabled_disables_its_content_and_dims_it()
    {
        var input = new KlyInput();
        var form = new KlyForm { Content = input };

        form.IsBusy = true;
        Assert.False(form.Host.IsEnabled);
        Assert.Equal(0.6, form.Host.Opacity);

        form.IsBusy = false;
        Assert.True(form.Host.IsEnabled);

        form.IsEnabled = false;
        Assert.False(form.Host.IsEnabled);
    }

    [Fact]
    public void Form_a_busy_form_does_not_submit_again()
    {
        var form = new KlyForm { Model = new Person { Name = "a", Email = "a@b.co" }, IsBusy = true };
        var raised = 0;
        form.ValidSubmit += (_, _) => raised++;

        Assert.False(form.Submit());
        Assert.Equal(0, raised);
    }

    [Fact]
    public void FormField_shows_its_label_hint_description_and_help_and_hides_what_it_has_not()
    {
        var field = new KlyFormField { Label = "Email", Hint = "Optional", Description = "We never share it.", Help = "Use your work address.", Content = new KlyInput() };

        Assert.Equal("Email", field.LabelView.FormattedText!.ToString());
        Assert.Equal("Optional", field.HintView.Text);
        Assert.Equal("We never share it.", field.DescriptionView.Text);
        Assert.True(field.DescriptionView.IsVisible);
        Assert.Equal("Use your work address.", field.MessageView.Text);

        var empty = new KlyFormField();
        Assert.False(empty.DescriptionView.IsVisible);
        Assert.False(empty.MessageView.IsVisible);
        Assert.False(((Grid)empty.Children[0]).IsVisible);
    }

    [Fact]
    public void FormField_marks_a_required_label_with_an_asterisk()
    {
        var field = new KlyFormField { Label = "Name", Required = true };

        Assert.Equal("Name *", field.LabelView.FormattedText!.ToString());
    }

    [Fact]
    public void FormField_an_error_replaces_the_help_text_and_marks_the_field_invalid()
    {
        var input = new KlyInput();
        var field = new KlyFormField { Label = "Name", Help = "Your full name", Content = input };
        Assert.Equal("Your full name", field.MessageView.Text);
        Assert.False(input.Invalid);

        field.Error = "Name is required.";

        Assert.Equal("Name is required.", field.MessageView.Text);
        Assert.True(input.Invalid);

        field.Error = null;
        Assert.Equal("Your full name", field.MessageView.Text);
        Assert.False(input.Invalid);
    }

    [Fact]
    public void FormField_does_not_clear_an_error_the_app_set_on_the_control()
    {
        var own = new KlyInput { Invalid = true };
        var other = new KlyListbox();
        var field = new KlyFormField { Content = new VerticalStackLayout { Children = { own, other } } };

        field.Error = "Taken";
        Assert.True(own.Invalid);
        Assert.True(other.Invalid);

        field.Error = null;
        Assert.True(own.Invalid);
        Assert.False(other.Invalid);
    }

    [Fact]
    public void FormField_gives_the_control_its_label_as_name_and_its_texts_as_hint_unless_the_control_has_a_name()
    {
        var plain = new KlyInput();
        var named = new KlyInput { AccessibleName = "Own name" };
        var first = new KlyFormField { Label = "Email", Description = "Work address", Help = "Required for login", Content = plain };
        var second = new KlyFormField { Label = "Email", Content = named };

        Assert.Equal("Email", plain.AccessibleName);
        Assert.Equal("Work address, Required for login", SemanticProperties.GetHint(plain.Content));
        Assert.Equal("Own name", named.AccessibleName);

        first.Label = "Mail";
        Assert.Equal("Mail", plain.AccessibleName);
        second.Label = null;
        Assert.Equal("Own name", named.AccessibleName);
    }

    [Fact]
    public void FormField_gives_its_size_to_a_control_that_has_none_of_its_own()
    {
        var plain = new KlyInput();
        var sized = new KlyInput { Size = KlySize.Xs };

        _ = new KlyFormField { Size = KlySize.Lg, Content = plain };
        _ = new KlyFormField { Size = KlySize.Lg, Content = sized };

        Assert.Equal(KlySize.Lg, plain.Size);
        Assert.Equal(KlySize.Xs, sized.Size);

        var untouched = new KlyInput();
        _ = new KlyFormField { Content = untouched };
        Assert.False(untouched.IsSet(KlyFieldBase.SizeProperty));
    }

    [Theory]
    [InlineData(KlySize.Xs, 12)]
    [InlineData(KlySize.Md, 14)]
    [InlineData(KlySize.Xl, 16)]
    public void FormField_size_is_the_font_of_its_texts(KlySize size, double font)
    {
        var field = new KlyFormField { Label = "L", Size = size };

        Assert.Equal(font, field.LabelView.FontSize);
        Assert.Equal(font, field.MessageView.FontSize);
    }

    [Fact]
    public void FormField_tells_every_kind_of_input_inside_it_about_its_error_and_label()
    {
        var slider = new KlySlider();
        var group = new KlyCheckboxGroup();
        var menu = new KlySelectMenu();
        var stack = new VerticalStackLayout { Children = { slider, group, menu } };

        _ = new KlyFormField { Label = "Volume", Error = "Too loud", Content = stack };

        Assert.Equal("Volume", slider.AccessibleName);
        Assert.True(group.Invalid);
        Assert.Equal("Volume", group.AccessibleName);
        Assert.True(menu.Invalid);
        Assert.Equal("Too loud", SemanticProperties.GetHint(slider.Native));
    }

    [Fact]
    public void FormField_inside_a_form_shows_the_validation_message_of_its_field_and_clears_it_once_fixed()
    {
        var person = new Person();
        var input = new KlyInput();
        var field = new KlyFormField { Label = "Name", Field = nameof(Person.Name), Content = input };
        var form = new KlyForm { Model = person, Content = new VerticalStackLayout { Children = { field } } };

        Assert.Null(field.ErrorText);
        Assert.False(input.Invalid);

        form.Submit();
        Assert.Equal("Name is required.", field.ErrorText);
        Assert.Equal("Name is required.", field.MessageView.Text);
        Assert.True(input.Invalid);

        person.Name = "Ada";
        Assert.Null(field.ErrorText);
        Assert.False(field.MessageView.IsVisible);
        Assert.False(input.Invalid);
    }

    [Fact]
    public void FormField_an_explicit_error_wins_over_the_message_of_the_form()
    {
        var field = new KlyFormField { Field = nameof(Person.Name), Error = "Taken", Content = new KlyInput() };
        _ = new KlyForm { Model = new Person(), Content = field };

        Assert.Equal("Taken", field.ErrorText);
    }

    [Fact]
    public void FormField_label_and_description_are_read_with_the_control_through_a_slider_and_a_toggle_group()
    {
        var group = new KlyToggleGroup();
        var field = new KlyFormField { Label = "Align", Description = "Text alignment", Content = group };

        Assert.Equal("Align", group.AccessibleName);
        Assert.Equal("Text alignment", SemanticProperties.GetHint(group));
        Assert.NotNull(field.Container.Content);
    }
}
