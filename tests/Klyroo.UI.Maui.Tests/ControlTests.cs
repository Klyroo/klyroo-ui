namespace Klyroo.UI.Maui.Tests;

public class ControlTests
{
    [Theory]
    [InlineData(KlySize.Xs, 12, 28)]
    [InlineData(KlySize.Sm, 12, 32)]
    [InlineData(KlySize.Md, 14, 40)]
    [InlineData(KlySize.Lg, 14, 44)]
    [InlineData(KlySize.Xl, 16, 48)]
    public void Button_size_sets_the_font_and_a_touch_target(KlySize size, double fontSize, double minHeight)
    {
        var button = new KlyButton { Text = "Go", Size = size };

        Assert.Equal(fontSize, button.FontSize);
        Assert.Equal(minHeight, button.MinimumHeightRequest);
        Assert.Equal((int)KlyTheme.Radius, button.CornerRadius);
    }

    [Fact]
    public void Button_is_content_wide_until_it_is_a_block_and_dims_while_disabled()
    {
        var button = new KlyButton { Text = "Go" };
        Assert.Equal(LayoutOptions.Start, button.HorizontalOptions);

        button.Block = true;
        Assert.Equal(LayoutOptions.Fill, button.HorizontalOptions);

        button.IsEnabled = false;
        Assert.Equal(0.6, button.Opacity);
        button.IsEnabled = true;
        Assert.Equal(1, button.Opacity);
    }

    [Fact]
    public void Button_variant_changes_the_border()
    {
        var button = new KlyButton { Text = "Go" };
        Assert.Equal(1, button.BorderWidth);

        button.Variant = ButtonVariant.Ghost;
        Assert.Equal(0, button.BorderWidth);
    }

    [Fact]
    public void Badge_shows_its_text_and_small_sizes_have_the_small_radius()
    {
        var badge = new KlyBadge { Text = "New", Size = KlySize.Xs };

        var label = Assert.IsType<Label>(badge.Content);
        Assert.Equal("New", label.Text);
        Assert.Equal(10, label.FontSize);
        Assert.Equal(KlyTheme.Radius * 0.5, ((Microsoft.Maui.Controls.Shapes.RoundRectangle)badge.StrokeShape!).CornerRadius.TopLeft);

        badge.Size = KlySize.Md;
        Assert.Equal(KlyTheme.Radius, ((Microsoft.Maui.Controls.Shapes.RoundRectangle)badge.StrokeShape!).CornerRadius.TopLeft);
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("  ", "")]
    [InlineData("ada", "A")]
    [InlineData("ada lovelace", "AL")]
    [InlineData("  grace   brewster  hopper ", "GB")]
    public void Avatar_initials_are_the_first_letters_of_the_first_two_words(string? text, string expected) =>
        Assert.Equal(expected, KlyAvatar.Initials(text));

    [Fact]
    public void Avatar_is_a_circle_of_its_size()
    {
        var avatar = new KlyAvatar { Text = "Ada Lovelace", Size = KlySize.Lg };

        Assert.Equal(40, avatar.WidthRequest);
        Assert.Equal(40, avatar.HeightRequest);
        Assert.Equal(20, ((Microsoft.Maui.Controls.Shapes.RoundRectangle)avatar.StrokeShape!).CornerRadius.TopLeft);
    }

    [Fact]
    public void Alert_hides_its_empty_texts_and_the_close_button_hides_the_alert()
    {
        var alert = new KlyAlert { Title = "Saved" };
        var closed = 0;
        alert.Closed += (_, _) => closed++;

        var texts = Assert.IsType<VerticalStackLayout>(((Grid)alert.Content!).Children[0]);
        Assert.True(((Label)texts.Children[0]).IsVisible);
        Assert.False(((Label)texts.Children[1]).IsVisible);

        var close = (Button)((Grid)alert.Content!).Children[1];
        Assert.False(close.IsVisible);
        alert.Closable = true;
        Assert.True(close.IsVisible);

        close.SendClicked();
        Assert.False(alert.IsVisible);
        Assert.Equal(1, closed);
    }

    [Fact]
    public void Separator_is_as_thick_as_its_size_in_the_right_direction()
    {
        var line = new KlySeparator { Size = KlySize.Md };
        Assert.Equal(3, line.HeightRequest);

        line.Orientation = StackOrientation.Vertical;
        Assert.Equal(3, line.WidthRequest);
        Assert.Equal(-1, line.HeightRequest);
    }

    [Fact]
    public void Input_text_goes_both_ways_and_raises_one_event_per_change()
    {
        var input = new KlyInput();
        var entry = (Entry)input.Content!;
        var changes = new List<(string?, string?)>();
        input.TextChanged += (_, e) => changes.Add((e.OldTextValue, e.NewTextValue));

        input.Text = "ada";
        Assert.Equal("ada", entry.Text);

        entry.Text = "ada l";
        Assert.Equal("ada l", input.Text);
        Assert.Equal([(null, "ada"), ("ada", "ada l")], changes);
    }

    [Fact]
    public void Input_forwards_its_options_to_the_entry_and_the_size_to_the_padding()
    {
        var input = new KlyInput { Placeholder = "Email", IsPassword = true, IsReadOnly = true, MaxLength = 12, Keyboard = Keyboard.Email, Size = KlySize.Lg };
        var entry = (Entry)input.Content!;

        Assert.Equal("Email", entry.Placeholder);
        Assert.True(entry.IsPassword);
        Assert.True(entry.IsReadOnly);
        Assert.Equal(12, entry.MaxLength);
        Assert.Equal(Keyboard.Email, entry.Keyboard);
        Assert.Equal(new Thickness(12, 8), input.Padding);
        Assert.Equal(14, entry.FontSize);
    }

    [Fact]
    public void Field_ring_is_two_wide_while_invalid_and_none_for_the_soft_variant()
    {
        var input = new KlyInput();
        Assert.Equal(1, input.StrokeThickness);

        input.Invalid = true;
        Assert.Equal(2, input.StrokeThickness);

        input.Invalid = false;
        input.Variant = FieldVariant.Soft;
        Assert.Equal(0, input.StrokeThickness);
    }

    [Fact]
    public void A_disabled_field_disables_its_control_and_dims()
    {
        var input = new KlyInput { IsEnabled = false };

        Assert.False(input.Content!.IsEnabled);
        Assert.Equal(0.6, input.Opacity);
    }

    [Fact]
    public void Textarea_keeps_its_rows_as_a_minimum_height()
    {
        var area = new KlyTextarea { Rows = 4, Size = KlySize.Md };

        Assert.Equal(4 * 14 * 1.5, ((Editor)area.Content!).MinimumHeightRequest);
        area.Text = "one\ntwo";
        Assert.Equal("one\ntwo", ((Editor)area.Content!).Text);
    }

    [Fact]
    public void Select_keeps_the_selection_whatever_the_order_of_items_and_selection()
    {
        var select = new KlySelect { SelectedItem = "Pro" };
        select.ItemsSource = new[] { "Free", "Pro", "Team" };

        var picker = (Picker)select.Content!;
        Assert.Equal("Pro", picker.SelectedItem);

        picker.SelectedIndex = 2;
        Assert.Equal("Team", select.SelectedItem);
    }

    [Fact]
    public void Checkbox_state_goes_both_ways_with_one_event_per_change()
    {
        var box = new KlyCheckbox { Text = "Accept" };
        var native = (CheckBox)box.Children[0];
        var events = new List<bool>();
        box.CheckedChanged += (_, e) => events.Add(e.Value);

        box.IsChecked = true;
        Assert.True(native.IsChecked);

        native.IsChecked = false;
        Assert.False(box.IsChecked);
        Assert.Equal([true, false], events);
    }

    [Fact]
    public void Switch_state_goes_both_ways_with_one_event_per_change()
    {
        var toggle = new KlySwitch { Text = "Newsletter" };
        var native = (Switch)toggle.Children[0];
        var events = new List<bool>();
        toggle.Toggled += (_, e) => events.Add(e.Value);

        toggle.IsToggled = true;
        Assert.True(native.IsToggled);

        native.IsToggled = false;
        Assert.False(toggle.IsToggled);
        Assert.Equal([true, false], events);
    }

    [Fact]
    public void A_disabled_choice_control_disables_the_native_one_and_dims()
    {
        var box = new KlyCheckbox { IsEnabled = false };
        var toggle = new KlySwitch { IsEnabled = false };

        Assert.False(((CheckBox)box.Children[0]).IsEnabled);
        Assert.False(((Switch)toggle.Children[0]).IsEnabled);
        Assert.Equal(0.6, box.Opacity);
        Assert.Equal(0.6, toggle.Opacity);
    }
}
