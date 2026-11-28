using System.Collections.ObjectModel;
using System.Globalization;
using System.Reflection;
using Microsoft.Maui.Controls.Shapes;

namespace Klyroo.UI.Maui.Tests;

public class ChatShimmerTests
{
    [Fact]
    public void Shimmer_defaults_to_two_seconds_and_a_band_half_the_text_wide()
    {
        var shimmer = new KlyChatShimmer { Text = "Thinking" };

        Assert.Equal(2, shimmer.Duration);
        Assert.Equal(2, shimmer.Spread);
        Assert.Equal(0.5, shimmer.BandWidth, 6);
        Assert.Equal("Thinking", shimmer.Label.Text);
    }

    [Theory]
    [InlineData(0, 0.2)]
    [InlineData(0.5, 0.2)]
    [InlineData(1, 0.3)]
    [InlineData(4, 0.9)]
    public void Shimmer_band_grows_with_the_spread_and_never_gets_narrower_than_the_web_minimum(double spread, double band) =>
        Assert.Equal(band, new KlyChatShimmer { Spread = spread }.BandWidth, 6);

    [Fact]
    public void Shimmer_highlight_is_off_the_text_at_the_start_and_on_its_middle_halfway()
    {
        var shimmer = new KlyChatShimmer();

        Assert.All(shimmer.Weights(9, 0), w => Assert.Equal(0, w));
        var half = shimmer.Weights(9, 0.5);
        Assert.Equal(half.Max(), half[4]);
        Assert.True(half[4] > 0.9);
        Assert.Equal(0, half[0]);
        Assert.Equal(0, half[8]);
    }

    [Fact]
    public void Shimmer_colors_one_span_per_character_and_only_the_band_differs_from_muted()
    {
        var shimmer = new KlyChatShimmer { Text = "Searching" };
        shimmer.ApplyPhase(0.5);

        var spans = shimmer.Label.FormattedText!.Spans;
        Assert.Equal(9, spans.Count);
        Assert.Equal("Searching", string.Concat(spans.Select(s => s.Text)));
        Assert.Equal(KlyTheme.TextMuted.Resolve(), spans[0].TextColor);
        Assert.NotEqual(KlyTheme.TextMuted.Resolve(), spans[4].TextColor);
    }

    [Fact]
    public void Shimmer_keeps_surrogate_pairs_in_one_span()
    {
        var shimmer = new KlyChatShimmer { Text = "a\U0001F600b" };
        shimmer.ApplyPhase(0.5);

        Assert.Equal(3, shimmer.Label.FormattedText!.Spans.Count);
    }

    [Fact]
    public void Shimmer_is_plain_muted_text_while_it_does_not_animate()
    {
        var shimmer = new KlyChatShimmer { Text = "One", FontSize = 12, FontAttributes = FontAttributes.Bold };
        shimmer.ApplyPhase(0.5);
        shimmer.Text = "Two";

        Assert.Null(shimmer.Label.FormattedText);
        Assert.Equal("Two", shimmer.Label.Text);
        Assert.Equal(12, shimmer.Label.FontSize);
        Assert.Equal(FontAttributes.Bold, shimmer.Label.FontAttributes);
        Assert.Equal("Two", SemanticProperties.GetDescription(shimmer));
    }
}

public class ChatAttachmentTests
{
    [Theory]
    [InlineData(0L, "0 B")]
    [InlineData(512L, "512 B")]
    [InlineData(1536L, "1.5 KB")]
    [InlineData(2L * 1024 * 1024, "2 MB")]
    [InlineData(3L * 1024 * 1024 * 1024, "3 GB")]
    [InlineData(5L * 1024 * 1024 * 1024 * 1024, "5120 GB")]
    public void Size_is_a_short_text_in_the_largest_unit(long bytes, string text) =>
        Assert.Equal(text, KlyChatAttachment.FormatSize(bytes, CultureInfo.InvariantCulture));

    [Fact]
    public void Size_uses_the_digits_of_the_culture() =>
        Assert.Equal("1,5 KB", KlyChatAttachment.FormatSize(1536, new CultureInfo("fr-FR")));

    [Fact]
    public void Description_is_the_extension_then_the_size_unless_it_is_replaced()
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        try
        {
            var chip = new KlyChatAttachment { Name = "report.pdf", FileSize = 1536 };
            Assert.Equal("PDF · 1.5 KB", chip.DescriptionLabel.Text);
            Assert.True(chip.DescriptionLabel.IsVisible);

            chip.Description = "Too big";
            Assert.Equal("Too big", chip.DescriptionLabel.Text);

            var bare = new KlyChatAttachment { Name = "README" };
            Assert.False(bare.DescriptionLabel.IsVisible);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Theory]
    [InlineData(KlySize.Xs, 24, 12, 40)]
    [InlineData(KlySize.Sm, 28, 12, 48)]
    [InlineData(KlySize.Md, 36, 14, 64)]
    [InlineData(KlySize.Lg, 40, 14, 80)]
    [InlineData(KlySize.Xl, 48, 16, 96)]
    public void Size_sets_the_leading_box_the_font_and_the_thumbnail(KlySize size, double box, double font, double thumbnail)
    {
        var chip = new KlyChatAttachment { Name = "a.txt", Size = size };
        Assert.Equal(box, chip.Leading.WidthRequest);
        Assert.Equal(box, chip.Leading.HeightRequest);
        Assert.Equal(font, chip.NameLabel.FontSize);

        chip.Variant = ChatAttachmentVariant.Thumbnail;
        Assert.Equal(thumbnail, chip.WidthRequest);
        Assert.Equal(thumbnail, chip.HeightRequest);
    }

    [Fact]
    public void An_image_with_an_address_shows_a_preview_and_other_files_show_an_icon()
    {
        var image = new KlyChatAttachment { Name = "cat.png", Type = "image/PNG", Url = "https://example.com/cat.png" };
        Assert.True(image.ShowsPreview);
        Assert.True(image.Preview.IsVisible);
        Assert.False(image.Glyph.IsVisible);
        Assert.IsType<UriImageSource>(image.Preview.Source);

        var local = new KlyChatAttachment { Name = "cat.png", Type = "image/png", Url = "cat.png" };
        Assert.IsType<FileImageSource>(local.Preview.Source);

        var noUrl = new KlyChatAttachment { Name = "cat.png", Type = "image/png" };
        Assert.False(noUrl.ShowsPreview);
        var pdf = new KlyChatAttachment { Name = "a.pdf", Type = "application/pdf", Url = "https://example.com/a.pdf" };
        Assert.False(pdf.ShowsPreview);
        Assert.True(pdf.Glyph.IsVisible);
        Assert.Equal(KlyIconNames.File, pdf.Glyph.Name);
    }

    [Fact]
    public void Uploading_shimmers_the_name_and_shows_a_spinner_and_reads_out_the_state()
    {
        var chip = new KlyChatAttachment { Name = "a.zip", Status = ChatAttachmentStatus.Uploading };

        Assert.True(chip.NameShimmer.IsVisible);
        Assert.False(chip.NameLabel.IsVisible);
        Assert.Equal("a.zip", chip.NameShimmer.Text);
        Assert.Equal(KlyIconNames.Loading, chip.Glyph.Name);
        Assert.Equal("a.zip (uploading)", SemanticProperties.GetDescription(chip.NameShimmer));

        chip.UploadingLabel = "envoi";
        Assert.Equal("a.zip (envoi)", SemanticProperties.GetDescription(chip.NameShimmer));

        chip.Status = ChatAttachmentStatus.Idle;
        Assert.False(chip.NameShimmer.IsVisible);
        Assert.True(chip.NameLabel.IsVisible);
    }

    [Fact]
    public void A_failed_upload_shows_an_alert_and_the_error_ring_and_reads_out_the_failure()
    {
        var chip = new KlyChatAttachment { Name = "a.zip", Status = ChatAttachmentStatus.Error, Description = "Too big", Icon = new Label() };

        Assert.Equal(KlyIconNames.Caution, chip.Glyph.Name);
        Assert.True(chip.Glyph.IsVisible);
        Assert.Equal(KlyTheme.Resolve(KlyColor.Error).Text, chip.Glyph.IconColor);
        Assert.Equal("a.zip (failed)", SemanticProperties.GetDescription(chip.NameLabel));
        Assert.Equal(1, chip.StrokeThickness);
    }

    [Fact]
    public void The_remove_button_shows_on_demand_names_the_file_and_raises_one_event_per_press()
    {
        var chip = new KlyChatAttachment { Name = "a.txt" };
        var removed = 0;
        chip.Removed += (_, _) => removed++;

        Assert.False(chip.RemoveButton.IsVisible);
        chip.Removable = true;
        Assert.True(chip.RemoveButton.IsVisible);
        Assert.Equal("Remove a.txt", chip.RemoveButton.Description);
        Assert.Equal(KlyIconNames.Close, chip.RemoveButton.IconName);
        Assert.True(chip.RemoveButton.Dimension >= 40);

        chip.RemoveButton.SendTap();
        Assert.Equal(1, removed);

        chip.RemoveLabel = "Retirer {0}";
        Assert.Equal("Retirer a.txt", chip.RemoveButton.Description);
    }

    [Fact]
    public void A_thumbnail_is_the_picture_alone_with_its_name_for_a_screen_reader()
    {
        var chip = new KlyChatAttachment { Name = "cat.png", Type = "image/png", Url = "cat.png", Variant = ChatAttachmentVariant.Thumbnail, Removable = true };

        Assert.Equal("cat.png", SemanticProperties.GetDescription(chip.Preview));
        Assert.Equal(0, chip.Padding.Left);
        Assert.Equal(LayoutOptions.End, chip.RemoveButton.HorizontalOptions);
        Assert.Equal(ButtonVariant.Solid, chip.RemoveButton.Variant);

        var card = new KlyChatAttachment { Name = "cat.png", Type = "image/png", Url = "cat.png" };
        Assert.Null(SemanticProperties.GetDescription(card.Preview));
        Assert.Equal(6, card.Padding.Left);
    }

    [Fact]
    public void A_vertical_card_gives_an_image_a_large_preview_and_fills_the_width()
    {
        var chip = new KlyChatAttachment { Name = "cat.png", Type = "image/png", Url = "cat.png", Orientation = StackOrientation.Vertical };

        Assert.Equal(128, chip.Leading.HeightRequest);
        Assert.Equal(LayoutOptions.Fill, chip.Leading.HorizontalOptions);
        Assert.Equal(LayoutOptions.Fill, chip.HorizontalOptions);

        chip.Type = "application/pdf";
        Assert.Equal(36, chip.Leading.HeightRequest);
    }

    [Fact]
    public void A_custom_icon_replaces_the_file_icon()
    {
        var icon = new KlyIcon { Name = KlyIconNames.Star };
        var chip = new KlyChatAttachment { Name = "a.txt", Icon = icon };

        Assert.False(chip.Glyph.IsVisible);
        Assert.Same(icon, ((Grid)chip.Leading.Content!).Children.OfType<ContentView>().Single().Content);
    }
}

public class ChatMessageTests
{
    [Theory]
    [InlineData(ChatRole.User, ChatSide.Right, ChatMessageVariant.Soft)]
    [InlineData(ChatRole.Assistant, ChatSide.Left, ChatMessageVariant.Naked)]
    [InlineData(ChatRole.System, ChatSide.Left, ChatMessageVariant.Naked)]
    public void Role_sets_the_default_side_and_surface(ChatRole role, ChatSide side, ChatMessageVariant variant)
    {
        var message = new KlyChatMessage { Role = role };

        Assert.Equal(side, message.EffectiveSide);
        Assert.Equal(variant, message.EffectiveVariant);
    }

    [Fact]
    public void Side_and_variant_override_the_role()
    {
        var message = new KlyChatMessage { Role = ChatRole.User, Side = ChatSide.Left, Variant = ChatMessageVariant.Solid };

        Assert.Equal(ChatSide.Left, message.EffectiveSide);
        Assert.Equal(ChatMessageVariant.Solid, message.EffectiveVariant);
        Assert.Equal(HorizontalAlignment(message.Container), LayoutOptions.Start);
    }

    private static LayoutOptions HorizontalAlignment(View view) => view.HorizontalOptions;

    [Fact]
    public void A_user_message_sits_on_the_right_in_the_last_three_quarters_with_the_avatar_at_the_end()
    {
        var message = new KlyChatMessage { Role = ChatRole.User, Text = "Hi", AvatarText = "Ada Lovelace" };

        Assert.Equal(LayoutOptions.End, message.Container.HorizontalOptions);
        Assert.Equal(1, Grid.GetColumn(message.Container));
        Assert.Equal(1, Grid.GetColumnSpan(message.Container));
        Assert.Equal(3, message.ColumnDefinitions[1].Width.Value);
        Assert.Equal(1, Grid.GetColumn(message.LeadingHost));
        Assert.Equal(new Thickness(12, 0, 0, 0), message.LeadingHost.Margin);

        message.Side = ChatSide.Left;
        Assert.Equal(0, Grid.GetColumn(message.Container));
        Assert.Equal(2, Grid.GetColumnSpan(message.Container));
        Assert.Equal(0, Grid.GetColumn(message.LeadingHost));
        Assert.Equal(new Thickness(0, 0, 12, 0), message.LeadingHost.Margin);
    }

    [Fact]
    public void A_system_message_is_a_centered_muted_notice()
    {
        var message = new KlyChatMessage { Role = ChatRole.System, Text = "Context was reset" };

        Assert.Equal(LayoutOptions.Center, message.Container.HorizontalOptions);
        Assert.Equal(12, message.TextLabel.FontSize);
        Assert.Equal(TextAlignment.Center, message.TextLabel.HorizontalTextAlignment);
    }

    [Fact]
    public void Text_shows_in_a_label_unless_a_content_view_is_given()
    {
        var message = new KlyChatMessage { Text = "Hello" };
        Assert.True(message.Bubble.IsVisible);
        Assert.True(message.TextLabel.IsVisible);
        Assert.Equal("Hello", message.TextLabel.Text);
        Assert.Equal(14, message.TextLabel.FontSize);

        message.Content = new Image();
        Assert.False(message.TextLabel.IsVisible);

        var empty = new KlyChatMessage();
        Assert.False(empty.Bubble.IsVisible);
    }

    [Theory]
    [InlineData(ChatMessageVariant.Solid, 1)]
    [InlineData(ChatMessageVariant.Outline, 1)]
    [InlineData(ChatMessageVariant.Soft, 0)]
    [InlineData(ChatMessageVariant.Subtle, 1)]
    [InlineData(ChatMessageVariant.Naked, 0)]
    public void Variant_sets_the_border_of_the_bubble(ChatMessageVariant variant, double border)
    {
        var message = new KlyChatMessage { Text = "Hi", Variant = variant };

        Assert.Equal(border, message.Bubble.StrokeThickness);
        Assert.Equal(KlyTheme.RadiusOf(1), ((RoundRectangle)message.Bubble.StrokeShape!).CornerRadius.TopLeft);
    }

    [Fact]
    public void Padding_follows_the_variant_and_the_compact_flag()
    {
        var message = new KlyChatMessage { Text = "Hi", Variant = ChatMessageVariant.Soft };
        Assert.Equal(new Thickness(12, 8), message.Bubble.Padding);

        message.Compact = true;
        Assert.Equal(new Thickness(10, 6), message.Bubble.Padding);

        message.Variant = ChatMessageVariant.Naked;
        Assert.Equal(new Thickness(0, 4), message.Bubble.Padding);
    }

    [Fact]
    public void Streaming_shows_the_cursor_only_in_a_bubble_with_something_in_it()
    {
        var message = new KlyChatMessage { Text = "Hel", Streaming = true };
        Assert.True(message.Cursor.IsVisible);

        message.Streaming = false;
        Assert.False(message.Cursor.IsVisible);

        Assert.False(new KlyChatMessage { Streaming = true }.Cursor.IsVisible);
    }

    [Fact]
    public void The_avatar_shows_with_any_of_its_properties_and_is_named_by_the_author()
    {
        var plain = new KlyChatMessage { Text = "Hi" };
        Assert.False(plain.HasLeading);
        Assert.False(plain.LeadingHost.IsVisible);

        var message = new KlyChatMessage { Text = "Hi", AvatarText = "Ada" };
        Assert.True(message.LeadingHost.IsVisible);
        Assert.Same(message.Avatar, message.LeadingHost.Content);
        Assert.Equal("Assistant", message.Avatar.Alt);
        Assert.Equal("A", message.Avatar.Text![..1]);
        Assert.Equal(KlySize.Sm, message.Avatar.Size);

        message.AvatarAlt = "Ada Lovelace";
        Assert.Equal("Ada Lovelace", message.Avatar.Alt);

        message.AuthorLabel = "Assistante";
        message.AvatarAlt = null;
        Assert.Equal("Assistante", message.Avatar.Alt);
    }

    [Theory]
    [InlineData(ChatRole.User, "You")]
    [InlineData(ChatRole.Assistant, "Assistant")]
    [InlineData(ChatRole.System, "System")]
    public void The_author_is_named_after_the_role(ChatRole role, string name) =>
        Assert.Equal(name, new KlyChatMessage { Role = role }.Author);

    [Fact]
    public void An_icon_or_a_leading_view_replaces_the_avatar()
    {
        var leading = new Label();
        var message = new KlyChatMessage { Icon = new KlyIcon { Name = KlyIconNames.Star } };
        Assert.True(message.LeadingHost.IsVisible);
        Assert.IsType<Border>(message.LeadingHost.Content);

        message.Leading = leading;
        Assert.Same(leading, message.LeadingHost.Content);
    }

    [Fact]
    public void Header_files_and_actions_show_only_when_given()
    {
        var header = new Label();
        var message = new KlyChatMessage { Text = "Hi" };
        var hosts = ((VerticalStackLayout)message.Bubble.Parent!).Children.OfType<ContentView>().ToList();

        Assert.All(hosts, h => Assert.False(h.IsVisible));

        message.Header = header;
        message.Files = new Label();
        message.Actions = new Label();
        Assert.All(hosts, h => Assert.True(h.IsVisible));
        Assert.Same(header, hosts[0].Content);
    }
}

public class ChatMessagesTests
{
    [Theory]
    [InlineData(0, 400, 1000, false)]
    [InlineData(580, 400, 1000, true)]
    [InlineData(600, 400, 1000, true)]
    [InlineData(500, 400, 1000, false)]
    [InlineData(0, 400, 300, true)]
    public void At_the_bottom_means_the_viewport_ends_within_a_few_units_of_the_content(double scrollY, double viewport, double content, bool expected) =>
        Assert.Equal(expected, KlyChatMessages.IsAtBottom(scrollY, viewport, content));

    [Fact]
    public void Scrolling_up_stops_following_and_shows_the_jump_button_and_jumping_follows_again()
    {
        var list = new KlyChatMessages();
        Assert.True(list.IsPinned);
        Assert.False(list.JumpButton.IsVisible);

        list.UpdatePinned(100, 400, 1000);
        Assert.False(list.IsPinned);
        Assert.True(list.JumpButton.IsVisible);

        list.JumpToLatest();
        Assert.True(list.IsPinned);
        Assert.False(list.JumpButton.IsVisible);

        list.UpdatePinned(100, 400, 1000);
        list.UpdatePinned(600, 400, 1000);
        Assert.True(list.IsPinned);
        Assert.False(list.JumpButton.IsVisible);
    }

    [Fact]
    public void Without_auto_scroll_there_is_no_jump_button()
    {
        var list = new KlyChatMessages { AutoScroll = false };
        list.UpdatePinned(0, 400, 1000);

        Assert.False(list.JumpButton.IsVisible);
        list.AutoScroll = true;
        Assert.True(list.JumpButton.IsVisible);
    }

    [Fact]
    public void The_jump_button_is_named_and_a_press_of_it_follows_the_latest_message()
    {
        var list = new KlyChatMessages { JumpToLatestLabel = "Aller au dernier" };
        list.UpdatePinned(0, 400, 1000);

        Assert.Equal("Aller au dernier", list.JumpTap.Description);
        list.JumpTap.SendTap();
        Assert.True(list.IsPinned);
    }

    [Fact]
    public void Messages_go_in_the_list_in_order_and_the_log_has_an_accessible_name()
    {
        var list = new KlyChatMessages();
        var first = new KlyChatMessage { Text = "1" };
        var second = new KlyChatMessage { Text = "2" };
        list.Messages.Add(first);
        list.Messages.Add(second);

        Assert.Equal([first, second], list.MessageStack.Children.Cast<KlyChatMessage>());
        Assert.Equal("Conversation", SemanticProperties.GetDescription(list.Scroll));

        list.Label = "Chat";
        Assert.Equal("Chat", SemanticProperties.GetDescription(list.Scroll));
    }

    [Theory]
    [InlineData(ChatStatus.Idle, false, false)]
    [InlineData(ChatStatus.Submitted, true, false)]
    [InlineData(ChatStatus.Streaming, true, false)]
    [InlineData(ChatStatus.Error, false, true)]
    public void Status_shows_the_typing_dots_or_the_error(ChatStatus status, bool dots, bool error)
    {
        var list = new KlyChatMessages { Status = status };

        Assert.Equal(dots, list.IndicatorHost.IsVisible);
        Assert.Equal(error, list.ErrorBox.IsVisible);
        Assert.Equal(3, list.Dots.Children.Count);
    }

    [Fact]
    public void Indicator_and_error_views_replace_the_defaults()
    {
        var indicator = new Label();
        var error = new Label();
        var list = new KlyChatMessages { Status = ChatStatus.Streaming, Indicator = indicator };
        Assert.Same(indicator, list.IndicatorHost.Content);

        list.Status = ChatStatus.Error;
        list.Error = error;
        Assert.Same(error, list.ErrorBox.Content);
    }

    [Fact]
    public void An_assistant_message_is_announced_once_when_it_stops_streaming()
    {
        var list = new KlyChatMessages();
        var announced = new List<string>();
        list.Announce = announced.Add;
        var message = new KlyChatMessage { Text = "Hello", Streaming = true };
        list.Messages.Add(message);

        message.Text = "Hello there";
        Assert.Empty(announced);

        message.Streaming = false;
        Assert.Equal(["Hello there"], announced);

        message.Text = "Edited";
        Assert.Equal(["Hello there"], announced);
    }

    [Fact]
    public void Only_assistant_messages_with_text_are_announced()
    {
        var list = new KlyChatMessages();
        var announced = new List<string>();
        list.Announce = announced.Add;
        var user = new KlyChatMessage { Role = ChatRole.User, Text = "Hi", Streaming = true };
        var empty = new KlyChatMessage { Streaming = true };
        list.Messages.Add(user);
        list.Messages.Add(empty);

        user.Streaming = false;
        empty.Streaming = false;

        Assert.Empty(announced);
    }

    [Fact]
    public void A_message_that_arrives_complete_is_announced_only_once_the_list_is_on_screen()
    {
        var list = new KlyChatMessages();
        var announced = new List<string>();
        list.Announce = announced.Add;

        list.Messages.Add(new KlyChatMessage { Text = "History" });
        Assert.Empty(announced);

        list.Ready = true;
        list.Messages.Add(new KlyChatMessage { Text = "New" });
        Assert.Equal(["New"], announced);
    }

    [Fact]
    public void The_screen_reader_hears_when_an_answer_starts_and_when_it_fails()
    {
        var list = new KlyChatMessages { Ready = true, RespondingLabel = "Réponse en cours", ErrorText = "Échec" };
        var announced = new List<string>();
        list.Announce = announced.Add;

        list.Status = ChatStatus.Submitted;
        list.Status = ChatStatus.Streaming;
        list.Status = ChatStatus.Error;

        Assert.Equal(["Réponse en cours", "Échec"], announced);
    }
}

public class ChatPaletteTests
{
    [Fact]
    public void The_conversation_fills_the_room_and_the_prompt_is_pinned_under_it_over_a_line()
    {
        var palette = new KlyChatPalette();
        Assert.False(palette.PromptHost.IsVisible);
        Assert.False(palette.Line.IsVisible);

        var messages = new KlyChatMessages();
        var prompt = new KlyChatPrompt();
        palette.Content = messages;
        palette.Prompt = prompt;

        Assert.Same(messages, palette.ContentHost.Content);
        Assert.Same(prompt, palette.PromptHost.Content);
        Assert.True(palette.PromptHost.IsVisible);
        Assert.True(palette.Line.IsVisible);
        Assert.Equal(GridUnitType.Star, palette.RowDefinitions[0].Height.GridUnitType);
        Assert.Equal(GridUnitType.Auto, palette.RowDefinitions[2].Height.GridUnitType);
        Assert.Equal(new Thickness(8), palette.PromptHost.Padding);

        palette.Prompt = null;
        Assert.False(palette.Line.IsVisible);
    }
}

public class ChatPromptTests
{
    [Fact]
    public void Defaults_follow_the_web_prompt()
    {
        var prompt = new KlyChatPrompt();

        Assert.Equal(1, prompt.Rows);
        Assert.Equal(6, prompt.MaxRows);
        Assert.True(prompt.SubmitOnEnter);
        Assert.False(prompt.Autofocus);
        Assert.Equal(FieldVariant.Outline, prompt.Variant);
        Assert.Equal(KlyColor.Primary, prompt.Color);
        Assert.Equal(ChatStatus.Idle, prompt.Status);
        Assert.Equal("Type a message...", prompt.Editor.Placeholder);
        Assert.Equal("Message", SemanticProperties.GetDescription(prompt.Editor));
        Assert.Equal(12, ((RoundRectangle)prompt.StrokeShape!).CornerRadius.TopLeft);
    }

    [Fact]
    public void Text_goes_both_ways_with_the_editor()
    {
        var prompt = new KlyChatPrompt();

        prompt.Text = "draft";
        Assert.Equal("draft", prompt.Editor.Text);

        prompt.Editor.Text = "draft 2";
        Assert.Equal("draft 2", prompt.Text);
    }

    [Fact]
    public void Placeholder_and_label_are_forwarded_to_the_editor()
    {
        var prompt = new KlyChatPrompt { Placeholder = "Écrire…", Label = "Message à l'assistant" };

        Assert.Equal("Écrire…", prompt.Editor.Placeholder);
        Assert.Equal("Message à l'assistant", SemanticProperties.GetDescription(prompt.Editor));
    }

    [Fact]
    public void Rows_and_max_rows_bound_the_height_of_the_editor_in_lines_of_twenty()
    {
        var prompt = new KlyChatPrompt();
        Assert.Equal(20, prompt.Editor.MinimumHeightRequest);
        Assert.Equal(120, prompt.Editor.MaximumHeightRequest);

        prompt.Rows = 3;
        prompt.MaxRows = 8;
        Assert.Equal(60, prompt.Editor.MinimumHeightRequest);
        Assert.Equal(160, prompt.Editor.MaximumHeightRequest);

        prompt.MaxRows = 2;
        Assert.Equal(60, prompt.Editor.MaximumHeightRequest);
    }

    [Fact]
    public void Submit_reports_the_text_once_and_clears_the_field()
    {
        var prompt = new KlyChatPrompt { Text = "Hello" };
        var sent = new List<string>();
        prompt.Submitted += (_, e) => sent.Add(e.Text);

        Assert.True(prompt.Submit());

        Assert.Equal(["Hello"], sent);
        Assert.Equal(string.Empty, prompt.Text);
        Assert.Equal(string.Empty, prompt.Editor.Text);
    }

    [Fact]
    public void The_send_button_submits()
    {
        var prompt = new KlyChatPrompt { Text = "Hello" };
        var sent = new List<string>();
        prompt.Submitted += (_, e) => sent.Add(e.Text);

        prompt.SubmitButton.Button.SendTap();

        Assert.Equal(["Hello"], sent);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   \n ")]
    public void An_empty_message_is_not_sent(string? text)
    {
        var prompt = new KlyChatPrompt { Text = text };
        var sent = 0;
        prompt.Submitted += (_, _) => sent++;

        Assert.False(prompt.Submit());
        Assert.Equal(0, sent);
    }

    [Fact]
    public void Attachments_make_an_empty_message_worth_sending()
    {
        var prompt = new KlyChatPrompt { Attachments = new Label() };
        var sent = new List<string>();
        prompt.Submitted += (_, e) => sent.Add(e.Text);

        Assert.True(prompt.Submit());
        Assert.Equal([string.Empty], sent);
    }

    [Theory]
    [InlineData(ChatStatus.Submitted)]
    [InlineData(ChatStatus.Streaming)]
    public void Nothing_is_sent_while_the_answer_is_on_its_way_but_the_field_stays_editable(ChatStatus status)
    {
        var prompt = new KlyChatPrompt { Text = "next", Status = status };
        var sent = 0;
        prompt.Submitted += (_, _) => sent++;

        Assert.False(prompt.Submit());
        Assert.Equal(0, sent);
        Assert.True(prompt.Editor.IsEnabled);
        Assert.Equal("next", prompt.Text);
    }

    [Fact]
    public void A_disabled_prompt_disables_the_editor_and_the_button_dims_and_sends_nothing()
    {
        var prompt = new KlyChatPrompt { Text = "Hello", IsEnabled = false };
        var sent = 0;
        prompt.Submitted += (_, _) => sent++;

        Assert.False(prompt.Editor.IsEnabled);
        Assert.Equal(0.6, prompt.Opacity);
        Assert.False(prompt.SubmitButton.Button.IsEnabled);
        Assert.False(prompt.Submit());
        Assert.Equal(0, sent);

        prompt.IsEnabled = true;
        Assert.Equal(1, prompt.Opacity);
        Assert.True(prompt.SubmitButton.Button.IsEnabled);
    }

    [Theory]
    [InlineData(true, false, false, true)]
    [InlineData(true, true, false, false)]
    [InlineData(true, false, true, true)]
    [InlineData(false, false, false, false)]
    [InlineData(false, true, false, false)]
    [InlineData(false, false, true, true)]
    [InlineData(false, true, true, true)]
    public void Enter_sends_or_adds_a_line_by_the_modifiers(bool submitOnEnter, bool shift, bool command, bool sends)
    {
        var prompt = new KlyChatPrompt { Text = "Hello", SubmitOnEnter = submitOnEnter };
        var sent = 0;
        prompt.Submitted += (_, _) => sent++;

        Assert.Equal(sends, prompt.HandleEnter(shift, command));
        Assert.Equal(sends ? 1 : 0, sent);
    }

    [Fact]
    public void Enter_on_an_empty_field_is_used_without_sending_anything()
    {
        var prompt = new KlyChatPrompt();
        var sent = 0;
        prompt.Submitted += (_, _) => sent++;

        Assert.True(prompt.HandleEnter(false, false));
        Assert.Equal(0, sent);
    }

    [Theory]
    [InlineData(FieldVariant.Outline, 1)]
    [InlineData(FieldVariant.Soft, 0)]
    [InlineData(FieldVariant.Subtle, 1)]
    [InlineData(FieldVariant.Ghost, 0)]
    [InlineData(FieldVariant.None, 0)]
    public void Variant_sets_the_ring(FieldVariant variant, double ring)
    {
        var prompt = new KlyChatPrompt { Variant = variant };

        Assert.Equal(ring, prompt.StrokeThickness);
    }

    [Fact]
    public void Attachments_leading_and_footer_show_only_when_given_and_trailing_replaces_the_button()
    {
        var prompt = new KlyChatPrompt();
        Assert.False(prompt.AttachmentsHost.IsVisible);
        Assert.False(prompt.LeadingHost.IsVisible);
        Assert.False(prompt.FooterHost.IsVisible);
        Assert.Same(prompt.SubmitButton, prompt.TrailingHost.Content);

        var custom = new Label();
        prompt.Attachments = new Label();
        prompt.Leading = new Label();
        prompt.Footer = new Label();
        prompt.Trailing = custom;

        Assert.True(prompt.AttachmentsHost.IsVisible);
        Assert.True(prompt.LeadingHost.IsVisible);
        Assert.True(prompt.FooterHost.IsVisible);
        Assert.Same(custom, prompt.TrailingHost.Content);
    }

    [Fact]
    public void The_button_follows_the_status_of_the_prompt_and_stop_needs_a_handler()
    {
        var prompt = new KlyChatPrompt { Status = ChatStatus.Streaming };
        var stops = 0;

        Assert.Equal(KlyIconNames.Stop, prompt.SubmitButton.Button.IconName);
        Assert.False(prompt.SubmitButton.Button.IsEnabled);

        prompt.Stopped += (_, _) => stops++;
        Assert.True(prompt.SubmitButton.Button.IsEnabled);

        prompt.SubmitButton.Button.SendTap();
        Assert.Equal(1, stops);
    }

    [Fact]
    public void After_an_error_the_button_retries_when_someone_listens_and_sends_otherwise()
    {
        var prompt = new KlyChatPrompt { Status = ChatStatus.Error, Text = "again" };
        Assert.Equal(KlyIconNames.ArrowUp, prompt.SubmitButton.Button.IconName);

        var reloads = 0;
        prompt.Reloaded += (_, _) => reloads++;
        Assert.Equal(KlyIconNames.Reload, prompt.SubmitButton.Button.IconName);

        prompt.SubmitButton.Button.SendTap();
        Assert.Equal(1, reloads);
        Assert.Equal("again", prompt.Text);
    }

    [Fact]
    public void Removing_a_handler_takes_the_retry_button_away()
    {
        var prompt = new KlyChatPrompt { Status = ChatStatus.Error };
        void Handler(object? s, EventArgs e) { }
        prompt.Reloaded += Handler;
        prompt.Reloaded -= Handler;

        Assert.Equal(KlyIconNames.ArrowUp, prompt.SubmitButton.Button.IconName);
    }
}

public class ChatPromptSubmitTests
{
    [Fact]
    public void Idle_is_a_solid_primary_send_button()
    {
        var button = new KlyChatPromptSubmit();

        Assert.Equal(KlyIconNames.ArrowUp, button.Button.IconName);
        Assert.Equal(ButtonVariant.Solid, button.Button.Variant);
        Assert.Equal(KlyColor.Primary, button.Button.Color);
        Assert.Equal("Send", button.Button.Description);
        Assert.Equal(KlySize.Md, button.Button.IconSize);
        Assert.True(button.Button.IsEnabled);
    }

    [Fact]
    public void The_color_of_the_send_button_can_change_but_the_stop_and_retry_buttons_keep_theirs()
    {
        var button = new KlyChatPromptSubmit { Color = KlyColor.Secondary };
        Assert.Equal(KlyColor.Secondary, button.Button.Color);

        button.Status = ChatStatus.Streaming;
        Assert.Equal(KlyColor.Neutral, button.Button.Color);
        Assert.Equal(ButtonVariant.Subtle, button.Button.Variant);
        Assert.Equal("Stop", button.Button.Description);
        Assert.Equal(KlySize.Xs, button.Button.IconSize);

        button.Reloaded += (_, _) => { };
        button.Status = ChatStatus.Error;
        Assert.Equal(KlyColor.Error, button.Button.Color);
        Assert.Equal(ButtonVariant.Soft, button.Button.Variant);
        Assert.Equal("Retry", button.Button.Description);
    }

    [Theory]
    [InlineData(KlySize.Xs, 28)]
    [InlineData(KlySize.Sm, 32)]
    [InlineData(KlySize.Md, 40)]
    [InlineData(KlySize.Lg, 44)]
    [InlineData(KlySize.Xl, 48)]
    public void Size_is_a_square_of_the_height_of_the_web_button(KlySize size, double dimension)
    {
        var button = new KlyChatPromptSubmit { Size = size };

        Assert.Equal(dimension, button.Button.Dimension);
        Assert.Equal(dimension, button.Button.HeightRequest);
    }

    [Fact]
    public void A_press_raises_the_event_of_the_status_once()
    {
        var button = new KlyChatPromptSubmit();
        var log = new List<string>();
        button.Clicked += (_, _) => log.Add("send");
        button.Stopped += (_, _) => log.Add("stop");
        button.Reloaded += (_, _) => log.Add("retry");

        button.Button.SendTap();
        button.Status = ChatStatus.Submitted;
        button.Button.SendTap();
        button.Status = ChatStatus.Streaming;
        button.Button.SendTap();
        button.Status = ChatStatus.Error;
        button.Button.SendTap();

        Assert.Equal(["send", "stop", "stop", "retry"], log);
    }

    [Fact]
    public void An_error_nobody_can_retry_shows_the_send_button()
    {
        var button = new KlyChatPromptSubmit { Status = ChatStatus.Error };
        var sent = 0;
        button.Clicked += (_, _) => sent++;

        Assert.Equal(ChatStatus.Idle, button.EffectiveStatus);
        button.Button.SendTap();
        Assert.Equal(1, sent);
    }

    [Fact]
    public void Stop_without_a_handler_is_disabled()
    {
        var button = new KlyChatPromptSubmit { Status = ChatStatus.Streaming };
        Assert.False(button.Button.IsEnabled);

        button.Stopped += (_, _) => { };
        Assert.True(button.Button.IsEnabled);
    }

    [Fact]
    public void The_status_of_the_prompt_is_used_until_one_is_given()
    {
        var button = new KlyChatPromptSubmit { ContextStatus = ChatStatus.Streaming };
        button.Stopped += (_, _) => { };
        Assert.Equal(ChatStatus.Streaming, button.EffectiveStatus);

        button.Status = ChatStatus.Idle;
        Assert.Equal(ChatStatus.Idle, button.EffectiveStatus);
    }

    [Fact]
    public void A_disabled_button_or_prompt_disables_the_native_button_and_dims()
    {
        var button = new KlyChatPromptSubmit { IsEnabled = false };
        Assert.False(button.Button.IsEnabled);

        var inPrompt = new KlyChatPromptSubmit { ContextDisabled = true };
        Assert.False(inPrompt.Button.IsEnabled);
    }

    [Fact]
    public void Labels_can_be_set_in_the_language_of_the_app()
    {
        var button = new KlyChatPromptSubmit { SendLabel = "Envoyer", StopLabel = "Arrêter", RetryLabel = "Réessayer" };
        Assert.Equal("Envoyer", button.Button.Description);

        button.Label = "Go";
        Assert.Equal("Go", button.Button.Description);

        button.Label = null;
        button.Stopped += (_, _) => { };
        button.Status = ChatStatus.Streaming;
        Assert.Equal("Arrêter", button.Button.Description);
    }
}

public class ChatReasoningTests
{
    private sealed class Clock
    {
        public long Now;
        public long Read() => Now;
    }

    private static (KlyChatReasoning Reasoning, Clock Clock, List<(TimeSpan Delay, Action Action)> Pending) Make()
    {
        var clock = new Clock();
        var pending = new List<(TimeSpan, Action)>();
        var reasoning = new KlyChatReasoning { Timestamp = clock.Read, Delay = (d, a) => pending.Add((d, a)) };
        return (reasoning, clock, pending);
    }

    [Fact]
    public void Defaults_follow_the_web_block()
    {
        var reasoning = new KlyChatReasoning();

        Assert.False(reasoning.Open);
        Assert.False(reasoning.Streaming);
        Assert.True(reasoning.AutoOpen);
        Assert.Equal(500, reasoning.AutoCloseDelay);
        Assert.Equal(ChatChevron.Trailing, reasoning.Chevron);
        Assert.False(reasoning.Body.IsVisible);
        Assert.Equal("Thought", reasoning.LabelText);
    }

    [Fact]
    public void The_label_tells_what_is_happening()
    {
        var (reasoning, clock, _) = Make();
        reasoning.Streaming = true;
        Assert.Equal("Thinking...", reasoning.LabelText);
        Assert.True(reasoning.Shimmer.IsVisible);
        Assert.False(reasoning.PlainLabel.IsVisible);

        reasoning.Text = "Reading the file";
        Assert.Equal("Reading the file", reasoning.LabelText);

        clock.Now += 3 * System.Diagnostics.Stopwatch.Frequency;
        reasoning.Streaming = false;
        Assert.Equal("Thought for 3 seconds", reasoning.LabelText);
        Assert.False(reasoning.Shimmer.IsVisible);
        Assert.True(reasoning.PlainLabel.IsVisible);
    }

    [Fact]
    public void A_given_duration_wins_over_the_measured_one_and_a_quick_thought_is_one_second()
    {
        var (reasoning, clock, _) = Make();
        reasoning.Streaming = true;
        clock.Now += System.Diagnostics.Stopwatch.Frequency / 10;
        reasoning.Streaming = false;
        Assert.Equal("Thought for 1 seconds", reasoning.LabelText);

        reasoning.Duration = 42;
        Assert.Equal("Thought for 42 seconds", reasoning.LabelText);

        reasoning.ThoughtForFormat = "Réfléchi {0} s";
        Assert.Equal("Réfléchi 42 s", reasoning.LabelText);
    }

    [Fact]
    public void Text_is_the_label_after_it_finished_when_no_duration_is_known()
    {
        var reasoning = new KlyChatReasoning { Text = "Plan", ThoughtText = "Pensé", ThinkingText = "Je pense" };
        Assert.Equal("Plan", reasoning.LabelText);

        reasoning.Text = null;
        Assert.Equal("Pensé", reasoning.LabelText);
        reasoning.Streaming = true;
        Assert.Equal("Je pense", reasoning.LabelText);
    }

    [Fact]
    public void Streaming_opens_the_block_once_and_closes_it_after_the_delay()
    {
        var (reasoning, _, pending) = Make();
        var changes = new List<bool>();
        reasoning.OpenChanged += (_, e) => changes.Add(e.Value);

        reasoning.Streaming = true;
        Assert.True(reasoning.Open);
        Assert.True(reasoning.Body.IsVisible);
        Assert.Equal(180, reasoning.ChevronIcon.Rotation);

        reasoning.Streaming = false;
        var (delay, close) = Assert.Single(pending);
        Assert.Equal(TimeSpan.FromMilliseconds(500), delay);
        Assert.True(reasoning.Open);

        close();
        Assert.False(reasoning.Open);
        Assert.False(reasoning.Body.IsVisible);
        Assert.Equal(0, reasoning.ChevronIcon.Rotation);
        Assert.Equal([true, false], changes);
    }

    [Fact]
    public void A_toggle_by_the_user_wins_over_the_automatic_close()
    {
        var (reasoning, _, pending) = Make();
        reasoning.Streaming = true;
        reasoning.Trigger.SendTap();
        Assert.False(reasoning.Open);
        reasoning.Trigger.SendTap();
        Assert.True(reasoning.Open);

        reasoning.Streaming = false;
        pending.Single().Action();

        Assert.True(reasoning.Open);
    }

    [Fact]
    public void A_block_that_was_already_open_stays_open_and_a_zero_delay_keeps_it_open()
    {
        var (reasoning, _, pending) = Make();
        reasoning.Open = true;
        reasoning.Streaming = true;
        reasoning.Streaming = false;
        Assert.Empty(pending);

        var (zero, _, zeroPending) = Make();
        zero.AutoCloseDelay = 0;
        zero.Streaming = true;
        zero.Streaming = false;
        Assert.Empty(zeroPending);
        Assert.True(zero.Open);
    }

    [Fact]
    public void Without_auto_open_streaming_leaves_the_block_alone()
    {
        var (reasoning, _, pending) = Make();
        reasoning.AutoOpen = false;
        reasoning.Streaming = true;
        reasoning.Streaming = false;

        Assert.False(reasoning.Open);
        Assert.Empty(pending);
    }

    [Fact]
    public void A_tap_toggles_and_each_change_raises_one_event()
    {
        var reasoning = new KlyChatReasoning();
        var changes = new List<bool>();
        reasoning.OpenChanged += (_, e) => changes.Add(e.Value);

        reasoning.Trigger.SendTap();
        reasoning.Trigger.SendTap();
        reasoning.Open = false;

        Assert.Equal([true, false], changes);
    }

    [Fact]
    public void Open_is_two_way_bindable_by_default()
    {
        Assert.Equal(BindingMode.TwoWay, KlyChatReasoning.OpenProperty.DefaultBindingMode);
        Assert.Equal(BindingMode.TwoWay, KlyChatTool.OpenProperty.DefaultBindingMode);
        Assert.Equal(BindingMode.TwoWay, KlyChatPrompt.TextProperty.DefaultBindingMode);
    }

    [Fact]
    public void The_chevron_sits_before_or_after_the_label_and_the_icon_is_between()
    {
        var icon = new KlyIcon { Name = KlyIconNames.Star };
        var reasoning = new KlyChatReasoning { Icon = icon };

        Assert.Equal(3, Grid.GetColumn(reasoning.ChevronIcon));
        Assert.True(reasoning.Row.Children.OfType<ContentView>().Single(c => c is not KlyChatShimmer).IsVisible);

        reasoning.Chevron = ChatChevron.Leading;
        Assert.Equal(0, Grid.GetColumn(reasoning.ChevronIcon));
        Assert.Equal(new Thickness(0, 0, 6, 0), reasoning.ChevronIcon.Margin);
    }

    [Fact]
    public void The_content_goes_in_the_body_and_a_disabled_block_cannot_be_toggled()
    {
        var content = new Label { Text = "step 1" };
        var reasoning = new KlyChatReasoning { Content = content, IsEnabled = false };

        Assert.Same(content, ((ScrollView)reasoning.Body.Children[1]).Content);
        Assert.False(reasoning.Trigger.IsEnabled);
        Assert.Equal(200, ((ScrollView)reasoning.Body.Children[1]).MaximumHeightRequest);

        reasoning.IsEnabled = true;
        Assert.True(reasoning.Trigger.IsEnabled);
    }

    [Fact]
    public void The_trigger_is_named_by_the_label_and_a_touch_target()
    {
        var reasoning = new KlyChatReasoning { Text = "Plan" };

        Assert.Equal("Plan", reasoning.Trigger.Description);
        Assert.True(reasoning.Trigger.MinimumTarget >= 40);
    }
}

public class ChatToolTests
{
    [Fact]
    public void Without_content_it_is_a_plain_line_and_with_content_it_is_a_collapsible_block()
    {
        var tool = new KlyChatTool { Text = "Searching the web", Suffix = "klyroo" };

        Assert.False(tool.Collapsible);
        Assert.False(tool.Trigger.Interactive);
        Assert.False(tool.ChevronIcon.IsVisible);

        tool.Content = new Label { Text = "{}" };
        Assert.True(tool.Trigger.Interactive);
        Assert.True(tool.ChevronIcon.IsVisible);
        Assert.False(tool.Body.IsVisible);

        tool.Open = true;
        Assert.True(tool.Body.IsVisible);
        Assert.Equal(180, tool.ChevronIcon.Rotation);
    }

    [Fact]
    public void The_name_and_the_quieter_suffix_show_and_the_trigger_reads_them_both()
    {
        var tool = new KlyChatTool { Text = "Searching", Suffix = "klyroo" };

        Assert.Equal("Searching", tool.NameLabel.Text);
        Assert.Equal(FontAttributes.Bold, tool.NameLabel.FontAttributes);
        Assert.True(tool.SuffixLabel.IsVisible);
        Assert.Equal("klyroo", tool.SuffixLabel.Text);
        Assert.Equal(FontAttributes.None, tool.SuffixLabel.FontAttributes);
        Assert.Equal("Searching klyroo", tool.Trigger.Description);

        tool.Suffix = null;
        Assert.False(tool.SuffixLabel.IsVisible);
    }

    [Theory]
    [InlineData(ChatToolVariant.Inline, 0, true)]
    [InlineData(ChatToolVariant.Card, 1, false)]
    public void Variant_is_a_plain_line_with_a_bar_or_a_bordered_card_with_a_line(ChatToolVariant variant, double border, bool leftBar)
    {
        var tool = new KlyChatTool { Text = "Run", Variant = variant, Content = new Label() };

        Assert.Equal(border, tool.Surface.StrokeThickness);
        Assert.Equal(leftBar, tool.LeftBar.IsVisible);
        Assert.Equal(!leftBar, tool.TopLine.IsVisible);
        Assert.Equal(KlyTheme.RadiusOf(1), ((RoundRectangle)tool.Surface.StrokeShape!).CornerRadius.TopLeft);
    }

    [Fact]
    public void Loading_replaces_the_icon_with_a_spinner_and_reads_out_that_it_runs()
    {
        var icon = new KlyIcon { Name = KlyIconNames.Star };
        var tool = new KlyChatTool { Text = "Run", Icon = icon, Loading = true };

        Assert.True(tool.StatusIcon.IsVisible);
        Assert.Equal(KlyIconNames.Loading, tool.StatusIcon.Name);
        Assert.Equal("Run (running)", tool.Trigger.Description);

        tool.Loading = false;
        Assert.False(tool.StatusIcon.IsVisible);
        Assert.Equal("Run", tool.Trigger.Description);
    }

    [Fact]
    public void An_error_shows_an_alert_in_the_error_color_and_reads_out_the_failure()
    {
        var tool = new KlyChatTool { Text = "Run", Error = true, FailedLabel = "échec" };
        var error = KlyTheme.Resolve(KlyColor.Error).Text;

        Assert.True(tool.StatusIcon.IsVisible);
        Assert.Equal(KlyIconNames.Caution, tool.StatusIcon.Name);
        Assert.Equal(error, tool.StatusIcon.IconColor);
        Assert.Equal("Run (échec)", tool.Trigger.Description);
    }

    [Fact]
    public void Streaming_shimmers_the_name()
    {
        var tool = new KlyChatTool { Text = "Run", Streaming = true };

        Assert.True(tool.NameShimmer.IsVisible);
        Assert.False(tool.NameLabel.IsVisible);
        Assert.Equal("Run", tool.NameShimmer.Text);
    }

    [Fact]
    public void A_press_toggles_the_details_with_one_event_per_change_and_the_chevron_moves_with_its_side()
    {
        var tool = new KlyChatTool { Text = "Run", Content = new Label() };
        var changes = new List<bool>();
        tool.OpenChanged += (_, e) => changes.Add(e.Value);

        Assert.Equal(5, Grid.GetColumn(tool.ChevronIcon));
        tool.Trigger.SendTap();
        tool.Trigger.SendTap();
        Assert.Equal([true, false], changes);

        tool.Chevron = ChatChevron.Leading;
        Assert.Equal(0, Grid.GetColumn(tool.ChevronIcon));
    }

    [Fact]
    public void Actions_sit_below_the_name_outside_the_details()
    {
        var actions = new Label();
        var tool = new KlyChatTool { Text = "Run", Content = new Label() };
        Assert.False(tool.ActionsHost.IsVisible);

        tool.Actions = actions;
        Assert.True(tool.ActionsHost.IsVisible);
        Assert.Same(actions, tool.ActionsHost.Content);
        Assert.False(tool.Body.IsVisible);
    }

    [Fact]
    public void A_disabled_tool_cannot_be_toggled()
    {
        var tool = new KlyChatTool { Text = "Run", Content = new Label(), IsEnabled = false };

        Assert.False(tool.Trigger.IsEnabled);
    }

    [Fact]
    public void The_details_scroll_beyond_two_hundred_units()
    {
        var tool = new KlyChatTool { Content = new Label() };

        Assert.Equal(200, tool.BodyScroll.MaximumHeightRequest);
        Assert.Equal(ScrollOrientation.Both, tool.BodyScroll.Orientation);
    }
}

public class ChartLayoutTests
{
    [Fact]
    public void A_nice_scale_rounds_the_range_out_to_ticks_on_one_two_or_five()
    {
        var scale = ChartDataLayout.NiceScale(0, 97);
        Assert.Equal(0, scale.Min);
        Assert.Equal(100, scale.Max);
        Assert.Equal(20, scale.Step);
        Assert.Equal([0, 20, 40, 60, 80, 100], scale.Ticks);

        var flat = ChartDataLayout.NiceScale(5, 5);
        Assert.True(flat.Max > flat.Min);
        var none = ChartDataLayout.NiceScale(double.NaN, double.NaN);
        Assert.Equal((0d, 1d), (none.Min, none.Max));
    }

    private static ChartDataLayout Cartesian(KlyChartType type, bool stacked = false, bool horizontal = false, params double?[][] values) =>
        ChartDataLayout.Build(type, horizontal, stacked, KlyChartCurve.Linear, ["a", "b", "c"], values.Select(v => (IReadOnlyList<double?>)v).ToList(), 300, 200, true, true, false, false, 24, v => v.ToString("0.##", CultureInfo.InvariantCulture));

    [Fact]
    public void A_line_chart_has_a_point_per_value_a_gap_splits_the_line_and_rows_get_a_hit_band()
    {
        var layout = Cartesian(KlyChartType.Line, values: [[1, null, 3], [2, 2, 2]]);

        Assert.Equal(2, layout.Series[0].Points.Count);
        Assert.Equal(2, layout.Series[0].Lines.Count);
        Assert.Single(layout.Series[1].Lines);
        Assert.Equal(3, layout.Series[1].Points.Count);
        Assert.Equal(3, layout.Hits.Count);
        Assert.True(layout.Series[0].Points[0].Y > layout.Series[0].Points[1].Y);
    }

    [Fact]
    public void An_area_chart_closes_each_run_into_a_fill()
    {
        var layout = Cartesian(KlyChartType.Area, values: [[1, 2, 3]]);

        Assert.Single(layout.Series[0].Areas);
        Assert.Single(layout.Series[0].Lines);
    }

    [Fact]
    public void Bars_stand_on_the_baseline_and_stack_on_the_running_total()
    {
        var plain = Cartesian(KlyChartType.Bar, values: [[1, 2, 3], [3, 2, 1]]);
        var bar = plain.Series[0].Bars[0];
        Assert.Equal(plain.ValuePosition(0), bar.Y + bar.Height, 6);
        Assert.Equal(2, plain.Series[0].Bars.Count(b => b.Row <= 1));

        var stacked = Cartesian(KlyChartType.Bar, stacked: true, values: [[1, 2, 3], [3, 2, 1]]);
        Assert.Equal(4, stacked.DomainMax);
        Assert.Equal(stacked.Series[0].Bars[0].Y, stacked.Series[1].Bars[0].Y + stacked.Series[1].Bars[0].Height, 6);
        Assert.Equal(stacked.Series[0].Bars[0].X, stacked.Series[1].Bars[0].X, 6);
    }

    [Fact]
    public void Horizontal_bars_lie_along_the_x_axis()
    {
        var layout = Cartesian(KlyChartType.Bar, horizontal: true, values: [[1, 2, 3]]);

        Assert.True(layout.Horizontal);
        var widths = layout.Series[0].Bars.Select(b => b.Width).ToList();
        Assert.True(widths[0] < widths[1] && widths[1] < widths[2]);
        Assert.Equal(layout.ValuePosition(0), layout.Series[0].Bars[0].X, 6);
    }

    [Fact]
    public void A_bar_of_zero_keeps_a_sliver_so_its_value_stays_reachable()
    {
        var layout = Cartesian(KlyChartType.Bar, values: [[0, 1, 2]]);

        Assert.True(layout.Series[0].Bars[0].Height >= 1);
    }

    [Fact]
    public void A_row_is_found_by_the_band_under_the_point()
    {
        var layout = Cartesian(KlyChartType.Bar, values: [[1, 2, 3]]);
        var hits = layout.Hits;

        Assert.Equal(0, layout.RowAt(hits[0].X + 1, layout.Top + 5));
        Assert.Equal(2, layout.RowAt(hits[2].X + 1, layout.Top + 5));
        Assert.Equal(-1, layout.RowAt(1, 1));
    }

    [Fact]
    public void The_series_under_a_touch_is_the_bar_that_holds_it_else_the_nearest_one()
    {
        var layout = Cartesian(KlyChartType.Bar, values: [[1, 2, 3], [3, 2, 1]]);
        var first = layout.Series[0].Bars[0];
        var second = layout.Series[1].Bars[0];

        Assert.Equal(0, layout.SeriesAt(0, first.X + 1, first.Y + 1));
        Assert.Equal(1, layout.SeriesAt(0, second.X + 1, second.Y + 1));
        Assert.Equal(1, layout.SeriesAt(0, second.X + second.Width + 1, 0));
        Assert.Equal(-1, ChartDataLayout.Build(KlyChartType.Bar, false, false, KlyChartCurve.Linear, [], [], 300, 200, true, true, false, false, 24, v => v.ToString()).SeriesAt(0, 0, 0));
    }

    [Fact]
    public void The_nearest_point_names_the_series_of_a_line_chart()
    {
        var layout = Cartesian(KlyChartType.Line, values: [[0, 0, 0], [10, 10, 10]]);
        var low = layout.Series[0].Points[1];
        var high = layout.Series[1].Points[1];

        Assert.Equal(0, layout.SeriesAt(1, low.X, low.Y + 3));
        Assert.Equal(1, layout.SeriesAt(1, high.X, high.Y - 3));
    }

    [Fact]
    public void A_donut_slices_by_share_clockwise_from_the_top_and_skips_what_has_no_size()
    {
        var layout = ChartDataLayout.Build(KlyChartType.Donut, false, false, KlyChartCurve.Linear, ["a", "b", "c", "d"], [new double?[] { 1, 0, 3, null }], 200, 200, true, true, false, false, 30, v => v.ToString());

        Assert.Equal([0, 2], layout.Slices.Select(s => s.Row));
        Assert.Equal(0.25, layout.Slices[0].Fraction, 6);
        Assert.Equal(0.75, layout.Slices[1].Fraction, 6);
        Assert.Equal(0, layout.Slices[0].Start);
        Assert.Equal(Math.PI / 2, layout.Slices[0].Sweep, 6);
        Assert.Equal(Math.PI / 2, layout.Slices[1].Start, 6);
        Assert.Equal(layout.Radius - 30, layout.InnerRadius, 6);

        // Just right of the top is the first slice, just left of it the last: the ring starts at 12 o'clock.
        var mid = (layout.Radius + layout.InnerRadius) / 2;
        Assert.Equal(0, layout.RowAt(layout.CenterX + 5, layout.CenterY - mid));
        Assert.Equal(2, layout.RowAt(layout.CenterX - 5, layout.CenterY - mid));
        Assert.Equal(-1, layout.RowAt(layout.CenterX, layout.CenterY));
        Assert.Equal(-1, layout.RowAt(0, 0));
    }

    [Fact]
    public void A_whole_ring_in_one_slice_is_a_closed_shape()
    {
        var layout = ChartDataLayout.Build(KlyChartType.Donut, false, false, KlyChartCurve.Linear, ["a"], [new double?[] { 5 }], 200, 200, true, true, false, false, 30, v => v.ToString());

        var slice = Assert.Single(layout.Slices);
        Assert.Equal(1, slice.Fraction);
        Assert.False(layout.SlicePath(slice).Bounds.IsEmpty);
    }

    [Fact]
    public void A_curve_goes_through_the_points_straight_stepped_or_smooth()
    {
        (double X, double Y)[] points = [(0, 0), (10, 10), (20, 0)];
        var linear = new PathF();
        ChartDataLayout.AppendCurve(linear, points, KlyChartCurve.Linear, true);
        var step = new PathF();
        ChartDataLayout.AppendCurve(step, points, KlyChartCurve.Step, true);
        var smooth = new PathF();
        ChartDataLayout.AppendCurve(smooth, points, KlyChartCurve.Monotone, true);

        Assert.Equal(3, linear.Count);
        Assert.Equal(7, step.Count);
        Assert.Equal(7, smooth.Count);
        Assert.Equal(PathOperation.Cubic, smooth.GetSegmentType(1));
        Assert.Equal(PathOperation.Line, linear.GetSegmentType(1));
    }

    [Fact]
    public void The_chart_tokens_follow_the_semantic_colors_until_the_app_registers_them()
    {
        Assert.Equal(KlyTheme.Resolve(KlyColor.Primary), ChartDataColors.Resolve("chart-1"));
        Assert.Equal(KlyTheme.Resolve(KlyColor.Secondary), ChartDataColors.Resolve("chart-2"));
        Assert.Equal(KlyTheme.Resolve(KlyColor.Info), ChartDataColors.Resolve("chart-3"));
        Assert.Equal(KlyTheme.Resolve(KlyColor.Error), ChartDataColors.Resolve("chart-4"));
        Assert.Equal(KlyTheme.Resolve(KlyColor.Success), ChartDataColors.Resolve("chart-5"));
        Assert.Equal(KlyTheme.Resolve(KlyColor.Warning), ChartDataColors.Resolve("chart-6"));
        Assert.Equal(KlyTheme.Resolve(KlyColor.Neutral), ChartDataColors.Resolve(KlyColor.Neutral));
        Assert.Throws<InvalidOperationException>(() => ChartDataColors.Resolve("chart-99"));
    }

    [Fact]
    public void A_color_the_app_registers_wins()
    {
        var set = new KlyColorSet(KlyThemeColor.FromHex("#111111", "#eeeeee"), KlyThemeColor.FromHex("#222222", "#dddddd"), KlyThemeColor.FromHex("#ffffff", "#000000"));
        KlyTheme.Register("chartdata-brand", set);

        Assert.Equal(set, ChartDataColors.Resolve("chartdata-brand"));
    }

    [Fact]
    public void The_default_palette_is_the_six_chart_tokens_then_neutral() =>
        Assert.Equal(["chart-1", "chart-2", "chart-3", "chart-4", "chart-5", "chart-6", "neutral"], ChartDataColors.Palette.Select(c => c.Value));
}

public class ChartTests
{
    private sealed record Sale(string Month, double? Online, double? Store);

    private static readonly Sale[] Sales =
    [
        new("Jan", 10, 4),
        new("Feb", 20, 8),
        new("Mar", 30, null),
    ];

    private static KlyChart<Sale, string> Make(KlyChartType type = KlyChartType.Line, params KlyChartCategory<Sale>[] categories) => new()
    {
        Data = Sales,
        Index = s => s.Month,
        Categories = categories.Length > 0 ? categories : new KlyChartCategory<Sale>[] { new KlyChartCategory<Sale>("online", s => s.Online) { Label = "Online" } },
        Type = type,
    };

    [Fact]
    public void Defaults_follow_the_web_chart()
    {
        var chart = new KlyChart<Sale, string>();

        Assert.Equal(KlyChartType.Line, chart.Type);
        Assert.True(chart.Grid);
        Assert.True(chart.XAxis);
        Assert.True(chart.YAxis);
        Assert.True(chart.Tooltip);
        Assert.False(chart.Points);
        Assert.False(chart.Stacked);
        Assert.False(chart.Horizontal);
        Assert.Equal(KlyChartCurve.Linear, chart.Curve);
        Assert.Equal(KlyChartTable.Hidden, chart.Table);
        Assert.Equal(24, chart.Thickness);
        Assert.Equal(300, chart.PlotHeight);
        Assert.Equal(640, chart.PlotWidth);
        Assert.Null(chart.Legend);
        Assert.False(chart.LegendHost.IsVisible);
    }

    [Fact]
    public void The_model_carries_the_rows_the_values_and_the_series_with_their_colors()
    {
        var chart = Make(KlyChartType.Bar, new KlyChartCategory<Sale>("online", s => s.Online) { Label = "Online" }, new KlyChartCategory<Sale>("store", s => s.Store));

        Assert.Equal(["Jan", "Feb", "Mar"], chart.Model.Index);
        Assert.Equal([10d, 20d, 30d], chart.Model.Values[0]);
        Assert.Equal([4d, 8d, null], chart.Model.Values[1]);
        Assert.Equal(["Online", "store"], chart.Model.Series.Select(s => s.Name));
        Assert.Equal(KlyTheme.Resolve(KlyColor.Primary), chart.Model.Series[0].Colors);
        Assert.Equal(KlyTheme.Resolve(KlyColor.Secondary), chart.Model.Series[1].Colors);
        Assert.Same(chart.Model, chart.Canvas.Model);
    }

    [Fact]
    public void A_color_on_a_category_wins_over_the_colors_of_the_chart_and_both_win_over_the_palette()
    {
        var chart = Make(KlyChartType.Bar, new KlyChartCategory<Sale>("online", s => s.Online) { Color = KlyColor.Error }, new KlyChartCategory<Sale>("store", s => s.Store));
        chart.Colors = [KlyColor.Success, KlyColor.Warning];

        Assert.Equal(KlyTheme.Resolve(KlyColor.Error), chart.Model.Series[0].Colors);
        Assert.Equal(KlyTheme.Resolve(KlyColor.Warning), chart.Model.Series[1].Colors);
    }

    [Fact]
    public void Non_finite_values_are_gaps()
    {
        var chart = new KlyChart<double, int>
        {
            Data = new[] { 1d, double.NaN, double.PositiveInfinity, 4 },
            Index = (_) => 0,
            Categories = [new("v", v => v)],
        };

        Assert.Equal([1d, null, null, 4d], chart.Model.Values[0]);
    }

    [Fact]
    public void The_legend_shows_with_several_series_and_can_be_forced_either_way()
    {
        var one = Make();
        Assert.False(one.LegendHost.IsVisible);

        var two = Make(KlyChartType.Line, new KlyChartCategory<Sale>("online", s => s.Online), new KlyChartCategory<Sale>("store", s => s.Store));
        Assert.True(two.LegendHost.IsVisible);
        Assert.Equal(2, two.LegendHost.Children.Count);

        two.Legend = false;
        Assert.False(two.LegendHost.IsVisible);
        one.Legend = true;
        Assert.True(one.LegendHost.IsVisible);
    }

    [Fact]
    public void A_donut_has_a_series_per_slice_and_a_legend_with_the_values()
    {
        var chart = Make(KlyChartType.Donut);

        Assert.Equal(["Jan", "Feb", "Mar"], chart.Model.Series.Select(s => s.Name));
        Assert.Equal(3, chart.Model.Series.Select(s => s.Colors).Distinct().Count());
        Assert.True(chart.LegendHost.IsVisible);
        var item = (HorizontalStackLayout)chart.LegendHost.Children[0];
        Assert.Equal("10", ((Label)item.Children[2]).Text);
    }

    [Fact]
    public void A_donut_without_a_category_has_slices_of_no_size()
    {
        var chart = new KlyChart<Sale, string> { Data = Sales, Index = s => s.Month, Categories = [], Type = KlyChartType.Donut };

        Assert.Equal(3, chart.Model.Series.Count);
        Assert.All(chart.Model.Values[0], v => Assert.Null(v));
    }

    [Fact]
    public void A_donut_is_a_square_the_height_of_the_chart_with_its_center_over_the_hole()
    {
        var center = new Label { Text = "60" };
        var chart = Make(KlyChartType.Donut);
        chart.Center = center;

        Assert.Equal(300, chart.PlotHost.WidthRequest);
        Assert.Equal(300, chart.PlotHost.HeightRequest);
        Assert.True(chart.CenterHost.IsVisible);
        Assert.Same(center, chart.CenterHost.Content);

        chart.Type = KlyChartType.Line;
        Assert.False(chart.CenterHost.IsVisible);
        Assert.Equal(-1, chart.PlotHost.WidthRequest);
        Assert.Equal(640, chart.PlotHost.MaximumWidthRequest);
    }

    [Fact]
    public void The_size_options_set_the_drawing()
    {
        var chart = Make();
        chart.PlotHeight = 180;
        chart.PlotWidth = 400;

        Assert.Equal(180, chart.PlotHost.HeightRequest);
        Assert.Equal(400, chart.PlotHost.MaximumWidthRequest);
    }

    [Fact]
    public void The_description_of_a_line_chart_gives_its_extent_and_ranges()
    {
        var chart = Make();

        Assert.Equal("Line chart with 3 data points from Jan to Mar. Online (10 to 30).", chart.Summary);
        Assert.Equal(chart.Summary, SemanticProperties.GetDescription(chart.Canvas));

        chart.AccessibleName = "Sales";
        chart.AccessibleDescription = "Monthly sales";
        Assert.Equal("Sales", SemanticProperties.GetDescription(chart.Canvas));
        Assert.Equal("Monthly sales", SemanticProperties.GetHint(chart.Canvas));
    }

    [Fact]
    public void The_description_of_a_donut_lists_the_slices_with_their_shares_and_an_empty_chart_says_so()
    {
        var donut = Make(KlyChartType.Donut);
        donut.Culture = CultureInfo.InvariantCulture;
        Assert.StartsWith("Donut chart. Slices: Jan 10 (17", donut.Summary);

        var empty = new KlyChart<Sale, string> { Data = [], Index = s => s.Month, Categories = [new KlyChartCategory<Sale>("online", s => s.Online)] };
        Assert.Equal("Line chart. No data.", empty.Summary);

        var horizontal = Make(KlyChartType.Bar);
        horizontal.Horizontal = true;
        Assert.StartsWith("Horizontal bar chart", horizontal.Summary);
    }

    [Fact]
    public void Values_are_formatted_by_the_culture_or_by_the_format()
    {
        var chart = Make();
        chart.Culture = new CultureInfo("fr-FR");
        Assert.Equal("1 234,5", chart.FormatValue(1234.5));
        Assert.Equal("0", chart.FormatValue(-0d));

        chart.Format = v => $"{v} €";
        Assert.Equal("5 €", chart.FormatValue(5));
        Assert.Equal("5 €", chart.Model.Format(5));
    }

    [Fact]
    public void Dates_and_numbers_on_the_index_axis_use_the_culture_and_the_index_format()
    {
        var chart = new KlyChart<DateOnly, DateOnly> { Culture = CultureInfo.InvariantCulture };

        Assert.Equal("03/04/2026", chart.IndexText(new DateOnly(2026, 3, 4)));
        chart.IndexFormat = "MMM d";
        Assert.Equal("Mar 4", chart.IndexText(new DateOnly(2026, 3, 4)));

        var numbers = new KlyChart<int, double> { Culture = CultureInfo.InvariantCulture, IndexFormat = "0.0" };
        Assert.Equal("3.0", numbers.IndexText(3));
        Assert.Equal(string.Empty, new KlyChart<int, string>().IndexText(null!));
    }

    [Fact]
    public void A_pick_reports_the_row_the_series_and_the_value_once()
    {
        var chart = Make(KlyChartType.Bar, new KlyChartCategory<Sale>("online", s => s.Online), new KlyChartCategory<Sale>("store", s => s.Store));
        var picks = new List<KlyChartSelectEventArgs<Sale, string>>();
        chart.Selected += (_, e) => picks.Add(e);

        chart.Pick(1, 1);

        var pick = Assert.Single(picks);
        Assert.Equal(1, pick.Row);
        Assert.Equal("Feb", pick.Index);
        Assert.Equal("store", pick.SeriesKey);
        Assert.Equal(1, pick.SeriesIndex);
        Assert.Equal(8, pick.Value);
        Assert.Same(Sales[1], pick.Item);
    }

    [Fact]
    public void A_pick_that_names_no_series_leaves_the_choice_to_the_handler_and_a_single_series_is_always_named()
    {
        var several = Make(KlyChartType.Bar, new KlyChartCategory<Sale>("online", s => s.Online), new KlyChartCategory<Sale>("store", s => s.Store));
        var picks = new List<KlyChartSelectEventArgs<Sale, string>>();
        several.Selected += (_, e) => picks.Add(e);
        several.Pick(0, -1);

        Assert.Null(picks[0].SeriesKey);
        Assert.Equal(-1, picks[0].SeriesIndex);
        Assert.Null(picks[0].Value);

        var one = Make();
        one.Selected += (_, e) => picks.Add(e);
        one.Pick(2, -1);
        Assert.Equal("online", picks[1].SeriesKey);
        Assert.Equal(30, picks[1].Value);

        var donut = Make(KlyChartType.Donut);
        donut.Selected += (_, e) => picks.Add(e);
        donut.Pick(0, 5);
        Assert.Equal("online", picks[2].SeriesKey);

        one.Pick(99, 0);
        Assert.Equal(3, picks.Count);
    }

    [Fact]
    public void The_tooltip_has_the_row_and_one_line_per_value_and_a_donut_adds_the_share()
    {
        var chart = Make(KlyChartType.Bar, new KlyChartCategory<Sale>("online", s => s.Online) { Label = "Online" }, new KlyChartCategory<Sale>("store", s => s.Store));
        var (title, lines) = chart.Model.Tip(2);

        Assert.Equal("Mar", title);
        var line = Assert.Single(lines);
        Assert.Equal(("Online", "30"), (line.Label, line.Value));

        var donut = Make(KlyChartType.Donut, new KlyChartCategory<Sale>("online", s => s.Online) { Label = "Online" });
        donut.Culture = CultureInfo.InvariantCulture;
        var (sliceTitle, sliceLines) = donut.Model.Tip(0);
        Assert.Equal("Jan", sliceTitle);
        Assert.Equal("Online", sliceLines[0].Label);
        Assert.StartsWith("10 (17", sliceLines[0].Value);
    }

    [Fact]
    public void The_tooltip_can_be_turned_off()
    {
        var chart = Make();
        Assert.True(chart.Canvas.TooltipEnabled);

        chart.Tooltip = false;
        Assert.False(chart.Canvas.TooltipEnabled);
    }

    [Fact]
    public void A_funnel_is_a_list_of_steps_with_their_share_of_the_previous_step()
    {
        var chart = new KlyChart<(string Step, double Count), string>
        {
            Data = new (string, double)[] { ("Visits", 1000), ("Sign-ups", 250), ("Paid", 0), ("Refunded", 50) },
            Index = r => r.Step,
            Categories = [new("count", r => r.Count)],
            Type = KlyChartType.Funnel,
        };
        chart.Culture = CultureInfo.InvariantCulture;

        Assert.True(chart.FunnelHost.IsVisible);
        Assert.False(chart.PlotHost.IsVisible);
        Assert.False(chart.LegendHost.IsVisible);
        Assert.Equal(4, chart.FunnelHost.Children.Count);
        Assert.Equal(["Visits", "Sign-ups", "Paid", "Refunded"], chart.FunnelSteps.Select(s => s.Label));
        Assert.Equal(["1,000", "250 (25%)", "0 (0%)", "50"], chart.FunnelSteps.Select(s => s.Text));
        Assert.Equal([100d, 25d, 0d, 5d], chart.FunnelSteps.Select(s => s.Width));
        Assert.Equal("chart-1", chart.FunnelSteps[0].Color.Value);
    }

    [Fact]
    public void A_funnel_step_never_vanishes_and_an_empty_funnel_says_so()
    {
        var chart = new KlyChart<double, int>
        {
            Data = new[] { 100000d, 1 },
            Index = (_) => 0,
            Categories = [new("v", v => v)],
            Type = KlyChartType.Funnel,
        };
        Assert.Equal(2, chart.FunnelSteps[1].Width);

        chart.Data = [];
        var empty = Assert.IsType<Label>(Assert.Single(chart.FunnelHost.Children));
        Assert.Equal("No data", empty.Text);
    }

    [Fact]
    public void A_visible_table_lists_the_numbers_under_the_chart()
    {
        var chart = Make(KlyChartType.Bar, new KlyChartCategory<Sale>("online", s => s.Online) { Label = "Online" }, new KlyChartCategory<Sale>("store", s => s.Store));
        Assert.False(chart.TableHost.IsVisible);

        chart.Table = KlyChartTable.Visible;
        chart.XLabel = "Month";

        Assert.True(chart.TableHost.IsVisible);
        var texts = TableTexts(chart);
        Assert.Equal(["Month", "Online", "store", "Jan", "10", "4", "Feb", "20", "8", "Mar", "30", ""], texts);
    }

    [Fact]
    public void A_toggled_table_opens_with_its_button()
    {
        var chart = Make();
        chart.Table = KlyChartTable.Toggle;

        var table = chart.TableHost.Children.OfType<Border>().Single();
        Assert.False(table.IsVisible);

        chart.TableHost.Children.OfType<ChatDataTapArea>().Single().SendTap();
        Assert.True(chart.TableHost.Children.OfType<Border>().Single().IsVisible);
        chart.TableHost.Children.OfType<ChatDataTapArea>().Single().SendTap();
        Assert.False(chart.TableHost.Children.OfType<Border>().Single().IsVisible);
    }

    private static List<string> TableTexts(KlyChart<Sale, string> chart)
    {
        var border = chart.TableHost.Children.OfType<Border>().Single();
        var grid = (Grid)((ScrollView)border.Content!).Content!;
        return grid.Children.Cast<Label>().Select(l => l.Text).ToList();
    }

    [Fact]
    public void The_hidden_table_is_replaced_by_the_description()
    {
        var chart = Make();

        Assert.Empty(chart.TableHost.Children);
        Assert.NotEmpty(SemanticProperties.GetDescription(chart.Canvas)!);
    }

    [Fact]
    public void A_property_change_redraws_the_model()
    {
        var chart = Make();
        var model = chart.Model;

        chart.Stacked = true;
        chart.Curve = KlyChartCurve.Monotone;
        chart.Points = true;

        Assert.NotSame(model, chart.Model);
        Assert.True(chart.Model.Stacked);
        Assert.Equal(KlyChartCurve.Monotone, chart.Model.Curve);
        Assert.True(chart.Model.Points);
    }

    [Fact]
    public void A_canvas_without_a_size_picks_nothing()
    {
        var chart = Make();
        var picks = 0;
        chart.Selected += (_, _) => picks++;

        chart.Canvas.TouchAt(new PointF(10, 10), true);

        Assert.Equal(0, picks);
        Assert.Equal(-1, chart.Canvas.ActiveRow);
    }
}

public class TableTests
{
    private sealed record Person(string Name, int Age, string City);

    private static readonly Person[] People =
    [
        new("Cleo", 31, "Paris"),
        new("Ada", 36, "London"),
        new("Bo", 24, "Oslo"),
    ];

    private static KlyTable<Person> Make(IEnumerable<Person>? items = null) => new()
    {
        ItemsSource = items ?? People,
        Columns =
        {
            new KlyColumn<Person, string> { Title = "Name", Field = p => p.Name, Sortable = true },
            new KlyColumn<Person, int> { Title = "Age", Field = p => p.Age, Numeric = true, Sortable = true },
            new KlyColumn<Person, string> { Title = "City", Field = p => p.City },
        },
    };

    private static List<string> Cells(KlyTable<Person> table) =>
        table.Body.Children.OfType<Label>().Select(l => l.Text).ToList();

    private static List<string> Names(KlyTable<Person> table) =>
        table.Body.Children.OfType<Label>().Where(l => Grid.GetColumn(l) == 0 && People.Any(p => p.Name == l.Text)).Select(l => l.Text).ToList();

    [Fact]
    public void Headers_and_cells_show_the_title_and_the_value_of_each_column()
    {
        var table = Make();

        Assert.Equal(["Cleo", "Ada", "Bo"], Names(table));
        Assert.Contains("Paris", Cells(table));
        Assert.Contains("36", Cells(table));
        // The header of a column that does not sort is a label; the one that does is a button.
        var headers = table.Body.Children.Where(c => Grid.GetRow((BindableObject)c) == 0).ToList();
        Assert.Equal(3, headers.Count);
        Assert.IsType<ChatDataTapArea>(headers[0]);
        Assert.IsType<ChatDataTapArea>(headers[1]);
        Assert.Equal("City", ((Label)headers[2]).Text);
        Assert.Equal(FontAttributes.Bold, ((Label)headers[2]).FontAttributes);
    }

    [Fact]
    public void A_line_separates_rows_but_there_is_none_after_the_last_one()
    {
        var table = Make();

        var lines = table.Body.Children.OfType<BoxView>().ToList();
        Assert.Equal(2, lines.Count);
        Assert.All(lines, l => Assert.Equal(3, Grid.GetColumnSpan(l)));
    }

    [Fact]
    public void Sorting_by_a_tap_goes_ascending_then_descending_and_changes_column()
    {
        var table = Make();
        var name = table.Columns[0];
        var age = table.Columns[1];

        table.Sort(name);
        Assert.Equal(["Ada", "Bo", "Cleo"], Names(table));
        Assert.True(table.SortAscending);

        table.Sort(name);
        Assert.Equal(["Cleo", "Bo", "Ada"], Names(table));
        Assert.False(table.SortAscending);

        table.Sort(age);
        Assert.Equal(["Bo", "Cleo", "Ada"], Names(table));
        Assert.True(table.SortAscending);
        Assert.Same(age, table.SortColumn);
    }

    [Fact]
    public void A_tap_on_a_sortable_header_sorts_and_the_header_tells_the_next_sort()
    {
        var table = Make();
        var header = (ChatDataTapArea)table.Body.Children.First(c => Grid.GetRow((BindableObject)c) == 0);
        Assert.Equal("Name", header.Description);
        Assert.Equal("Sort ascending", header.Hint);

        header.SendTap();

        Assert.Equal(["Ada", "Bo", "Cleo"], Names(table));
        header = (ChatDataTapArea)table.Body.Children.First(c => Grid.GetRow((BindableObject)c) == 0);
        Assert.Equal("Sort descending", header.Hint);
    }

    [Fact]
    public void The_sort_icon_shows_the_direction_of_the_sort_column_only()
    {
        var table = Make();
        string Icon(int column) => ((KlyIcon)((HorizontalStackLayout)((ChatDataTapArea)table.Body.Children.First(c => Grid.GetRow((BindableObject)c) == 0 && Grid.GetColumn((BindableObject)c) == column)).Children[0]).Children[1]).Name;

        Assert.Equal(KlyIconNames.ChevronUpDown, Icon(0));
        table.Sort(table.Columns[0]);
        Assert.Equal(KlyIconNames.ArrowUp, Icon(0));
        Assert.Equal(KlyIconNames.ChevronUpDown, Icon(1));
        table.Sort(table.Columns[0]);
        Assert.Equal(KlyIconNames.ArrowDown, Icon(0));
    }

    [Fact]
    public void A_template_column_shows_a_view_per_item_and_does_not_sort()
    {
        var table = Make();
        var template = new KlyTemplateColumn<Person> { Title = "Badge", Template = p => new KlyBadge { Text = p.Name } };
        table.Columns.Add(template);

        Assert.Equal(3, table.Body.Children.OfType<ContentView>().Count(c => c.Content is KlyBadge));
        Assert.False(template.CanSort);
        Assert.Equal(People, template.OrderItems(People, true));
        Assert.IsType<Label>(table.Body.Children.First(c => Grid.GetRow((BindableObject)c) == 0 && Grid.GetColumn((BindableObject)c) == 3));
    }

    [Fact]
    public void A_template_on_a_value_column_shows_the_view_while_the_field_still_sorts()
    {
        var table = Make();
        var column = (KlyColumn<Person, string>)table.Columns[0];
        column.Template = p => new KlyBadge { Text = p.Name };
        table.Sort(column);

        var order = table.Body.Children.OfType<ContentView>().Select(c => ((KlyBadge)c.Content!).Text).ToList();
        Assert.Equal(["Ada", "Bo", "Cleo"], order);
    }

    [Fact]
    public void Numeric_and_end_columns_align_to_the_end_and_mono_uses_the_monospace_font()
    {
        var table = Make();
        var age = table.Body.Children.OfType<Label>().First(l => l.Text == "36");
        var city = table.Body.Children.OfType<Label>().First(l => l.Text == "London");
        Assert.Equal(TextAlignment.End, age.HorizontalTextAlignment);
        Assert.Equal(TextAlignment.Start, city.HorizontalTextAlignment);

        table.Columns[2].Align = KlyColumnAlign.End;
        table.Columns[2].Mono = true;
        city = table.Body.Children.OfType<Label>().First(l => l.Text == "London");
        Assert.Equal(TextAlignment.End, city.HorizontalTextAlignment);
        Assert.Equal("monospace", city.FontFamily);
    }

    [Fact]
    public void Cells_have_the_padding_of_the_web_cell()
    {
        var table = Make();
        var cell = table.Body.Children.OfType<Label>().First(l => l.Text == "Paris");

        Assert.Equal(new Thickness(16, 16), cell.Padding);
        Assert.Equal(14, cell.FontSize);
    }

    [Fact]
    public void The_footer_row_shows_when_any_column_has_a_footer_over_an_accented_line()
    {
        var table = Make();
        Assert.Equal(2, table.Body.Children.OfType<BoxView>().Count());

        table.Columns[0].Footer = "Total";
        table.Columns[1].FooterTemplate = new KlyBadge { Text = "91" };

        Assert.Equal(3, table.Body.Children.OfType<BoxView>().Count());
        var total = table.Body.Children.OfType<Label>().First(l => l.Text == "Total");
        Assert.Equal(FontAttributes.Bold, total.FontAttributes);
        Assert.Contains(table.Body.Children.OfType<ContentView>(), c => c.Content is KlyBadge { Text: "91" });
    }

    [Fact]
    public void A_footer_view_survives_a_rebuild()
    {
        var table = Make();
        table.Columns[0].FooterTemplate = new KlyBadge { Text = "91" };
        table.Sort(table.Columns[0]);
        table.Sort(table.Columns[0]);

        Assert.Contains(table.Body.Children.OfType<ContentView>(), c => c.Content is KlyBadge { Text: "91" });
    }

    [Fact]
    public void Without_items_the_table_says_so_or_shows_the_empty_view()
    {
        var table = Make([]);
        var empty = table.Body.Children.OfType<Label>().Single(l => l.Text == "No data");
        Assert.Equal(3, Grid.GetColumnSpan(empty));

        table.EmptyText = "Aucune donnée";
        Assert.Contains(table.Body.Children.OfType<Label>(), l => l.Text == "Aucune donnée");

        var view = new Label { Text = "Nothing to show" };
        table.Empty = view;
        Assert.Contains(view, table.Body.Children);
        Assert.DoesNotContain(table.Body.Children.OfType<Label>(), l => l.Text == "Aucune donnée");
    }

    [Fact]
    public void Max_cuts_the_rows_after_sorting_and_each_press_reveals_a_step()
    {
        var table = Make();
        table.Max = 1;
        table.Step = 1;
        table.Sort(table.Columns[0]);

        Assert.Equal(["Ada"], Names(table));
        Assert.True(table.MoreButton.IsVisible);
        Assert.Equal("Show more · 1", table.MoreButton.Description);

        table.MoreButton.SendTap();
        Assert.Equal(["Ada", "Bo"], Names(table));
        table.MoreButton.SendTap();
        Assert.Equal(["Ada", "Bo", "Cleo"], Names(table));
        Assert.False(table.MoreButton.IsVisible);
    }

    [Fact]
    public void Without_a_step_the_press_reveals_everything_and_the_button_counts_what_is_left()
    {
        var table = Make();
        table.Max = 1;
        Assert.Equal("Show more · 2", table.MoreButton.Description);

        table.ShowMoreFormat = "Voir {0} de plus";
        Assert.Equal("Voir 2 de plus", table.MoreButton.Description);

        table.MoreButton.SendTap();
        Assert.Equal(3, Names(table).Count);
        Assert.False(table.MoreButton.IsVisible);
    }

    [Fact]
    public void A_list_that_tells_it_changed_updates_the_rows()
    {
        var list = new ObservableCollection<Person>(People);
        var table = Make(list);

        list.Add(new Person("Dee", 50, "Rome"));
        Assert.Contains("Dee", Names(table).Concat(table.Body.Children.OfType<Label>().Select(l => l.Text)));
        Assert.Equal(4, table.Body.Children.OfType<Label>().Count(l => l.Text is "Cleo" or "Ada" or "Bo" or "Dee"));

        list.RemoveAt(0);
        Assert.DoesNotContain(table.Body.Children.OfType<Label>(), l => l.Text == "Cleo");
    }

    [Fact]
    public void Columns_added_or_changed_later_show_up()
    {
        var table = Make();
        var city = table.Columns[2];

        city.Title = "Town";
        Assert.Contains(table.Body.Children.OfType<Label>(), l => l.Text == "Town");

        table.Columns.Remove(city);
        Assert.DoesNotContain(table.Body.Children.OfType<Label>(), l => l.Text == "Paris");
    }

    [Fact]
    public void Removing_the_sort_column_clears_the_sort()
    {
        var table = Make();
        table.Sort(table.Columns[0]);
        table.Columns.RemoveAt(0);

        Assert.Null(table.SortColumn);
    }

    [Fact]
    public void A_column_without_its_field_or_template_is_refused_with_the_reason()
    {
        var table = Make();

        var error = Assert.Throws<InvalidOperationException>(() => table.Columns.Add(new KlyColumn<Person, string> { Title = "Broken" }));
        Assert.Contains("'Broken' needs a Field", error.Message);
        table.Columns.Clear();
        var template = Assert.Throws<InvalidOperationException>(() => table.Columns.Add(new KlyTemplateColumn<Person> { Title = "Cell" }));
        Assert.Contains("'Cell' needs a Template", template.Message);
    }

    [Fact]
    public void Dates_and_numbers_are_shown_with_the_current_culture()
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
        try
        {
            var table = new KlyTable<double>
            {
                ItemsSource = new[] { 1234.5 },
                Columns = { new KlyColumn<double, double> { Title = "Value", Field = v => v } },
            };

            Assert.Contains(table.Body.Children.OfType<Label>(), l => l.Text == "1234,5");
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void A_null_value_shows_nothing()
    {
        var table = new KlyTable<string?>
        {
            ItemsSource = new string?[] { null },
            Columns = { new KlyColumn<string?, string?> { Title = "Value", Field = v => v } },
        };

        Assert.Contains(table.Body.Children.OfType<Label>(), l => l.Text == string.Empty);
    }

    [Fact]
    public void The_caption_describes_the_table_for_a_screen_reader()
    {
        var table = Make();
        table.Caption = "People";

        Assert.Equal("People", SemanticProperties.GetDescription(table.Body));
    }

    [Fact]
    public void A_table_without_columns_is_empty()
    {
        var table = new KlyTable<Person> { ItemsSource = People };

        Assert.Empty(table.Body.Children);
        Assert.False(table.MoreButton.IsVisible);
    }
}

/// <summary>An <see cref="ICanvas"/> that records its calls: a drawing runs end to end with no platform, and the test reads what was drawn.</summary>
public class RecordingCanvas : DispatchProxy
{
    public List<(string Method, object?[] Args)> Calls { get; } = [];

    public static (ICanvas Canvas, RecordingCanvas Recorder) Create()
    {
        var canvas = Create<ICanvas, RecordingCanvas>();
        return (canvas, (RecordingCanvas)(object)canvas);
    }

    public IEnumerable<string> Texts => Calls.Where(c => c.Method == nameof(ICanvas.DrawString) && c.Args[0] is string).Select(c => (string)c.Args[0]!);

    public int Count(string method) => Calls.Count(c => c.Method == method);

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        Calls.Add((targetMethod!.Name, args ?? []));
        return targetMethod.ReturnType.IsValueType && targetMethod.ReturnType != typeof(void) ? Activator.CreateInstance(targetMethod.ReturnType) : null;
    }
}

public class ChartDrawingTests
{
    private sealed record Sale(string Month, double? Online, double? Store);

    private static readonly Sale[] Sales = [new("Jan", 10, 4), new("Feb", 20, 8), new("Mar", 30, null)];

    private static KlyChart<Sale, string> Make(KlyChartType type, bool second = false)
    {
        var categories = new List<KlyChartCategory<Sale>> { new("online", s => s.Online) { Label = "Online" } };
        if (second) categories.Add(new("store", s => s.Store));
        return new KlyChart<Sale, string> { Data = Sales, Index = s => s.Month, Categories = categories, Type = type, Culture = CultureInfo.InvariantCulture };
    }

    private static RecordingCanvas Draw(KlyChart<Sale, string> chart, int activeRow = -1)
    {
        var (canvas, recorder) = RecordingCanvas.Create();
        chart.Canvas.ActiveRow = activeRow;
        chart.Canvas.Draw(canvas, new RectF(0, 0, 400, 300));
        return recorder;
    }

    [Fact]
    public void A_line_chart_draws_the_grid_the_labels_and_a_line_per_series()
    {
        var recorder = Draw(Make(KlyChartType.Line, true));

        Assert.True(recorder.Count(nameof(ICanvas.DrawLine)) >= 5);
        Assert.Equal(2, recorder.Count(nameof(ICanvas.DrawPath)));
        Assert.Contains("Jan", recorder.Texts);
        Assert.Contains("Mar", recorder.Texts);
        Assert.Contains("30", recorder.Texts);
        Assert.Contains("0", recorder.Texts);
    }

    [Fact]
    public void Series_differ_by_dash_without_color()
    {
        var recorder = Draw(Make(KlyChartType.Line, true));

        var dashes = recorder.Calls.Where(c => c.Method == "set_StrokeDashPattern").Select(c => c.Args[0] as float[]).ToList();
        Assert.Contains(dashes, d => d is null);
        Assert.Contains(dashes, d => d is [7, 4]);
    }

    [Fact]
    public void An_area_chart_fills_the_area_and_draws_the_line()
    {
        var recorder = Draw(Make(KlyChartType.Area));

        Assert.Equal(1, recorder.Count(nameof(ICanvas.FillPath)));
        Assert.Equal(1, recorder.Count(nameof(ICanvas.DrawPath)));
    }

    [Fact]
    public void A_bar_chart_fills_and_outlines_each_bar_and_the_second_series_gets_a_hatch()
    {
        var recorder = Draw(Make(KlyChartType.Bar, true));

        // Three bars and two (the third has no store value): five fills, five outlines.
        Assert.Equal(5, recorder.Count(nameof(ICanvas.FillPath)));
        Assert.Equal(5, recorder.Count(nameof(ICanvas.DrawPath)));
        Assert.True(recorder.Count(nameof(ICanvas.ClipPath)) >= 2);
    }

    [Fact]
    public void A_donut_draws_a_slice_per_row_and_the_active_one_is_lighter()
    {
        var recorder = Draw(Make(KlyChartType.Donut), activeRow: 1);

        Assert.Equal(3, recorder.Count(nameof(ICanvas.FillPath)));
        Assert.Equal(3, recorder.Count(nameof(ICanvas.DrawPath)));
    }

    [Fact]
    public void The_axis_titles_and_the_horizontal_layout_are_drawn()
    {
        var chart = Make(KlyChartType.Bar);
        chart.Horizontal = true;
        chart.XLabel = "Month";
        chart.YLabel = "Sales";
        var recorder = Draw(chart);

        Assert.Contains("Month", recorder.Texts);
        Assert.Contains("Sales", recorder.Texts);
        Assert.Contains("Jan", recorder.Texts);
    }

    [Fact]
    public void The_grid_and_the_axes_can_be_left_out()
    {
        var chart = Make(KlyChartType.Line);
        chart.Grid = false;
        chart.XAxis = false;
        chart.YAxis = false;
        var recorder = Draw(chart);

        Assert.Equal(0, recorder.Count(nameof(ICanvas.DrawLine)));
        Assert.Empty(recorder.Texts);
        Assert.Equal(1, recorder.Count(nameof(ICanvas.DrawPath)));
    }

    [Fact]
    public void A_tooltip_names_the_row_and_each_value_while_a_row_is_active()
    {
        var chart = Make(KlyChartType.Bar, true);

        Assert.DoesNotContain("Online", Draw(chart).Texts);
        var recorder = Draw(chart, activeRow: 1);
        Assert.Equal(2, recorder.Texts.Count(t => t == "Feb"));
        Assert.Contains("Online", recorder.Texts);
        Assert.Contains("store", recorder.Texts);
        Assert.Contains("20", recorder.Texts);
        Assert.Contains("8", recorder.Texts);

        chart.Tooltip = false;
        Assert.Single(Draw(chart, activeRow: 1).Texts, t => t == "Feb");
    }

    [Fact]
    public void A_line_chart_marks_the_active_row_and_every_value_with_points()
    {
        var chart = Make(KlyChartType.Line, true);
        var none = Draw(chart).Count(nameof(ICanvas.FillPath));
        var active = Draw(chart, activeRow: 1).Count(nameof(ICanvas.FillPath));
        chart.Points = true;
        var all = Draw(chart).Count(nameof(ICanvas.FillPath));

        Assert.Equal(0, none);
        Assert.Equal(2, active);
        Assert.Equal(5, all);
    }

    [Fact]
    public void Nothing_is_drawn_for_a_funnel_an_empty_chart_or_a_chart_without_series()
    {
        Assert.Empty(Draw(Make(KlyChartType.Funnel)).Calls);

        var empty = Make(KlyChartType.Line);
        empty.Data = [];
        Assert.Equal(0, Draw(empty).Count(nameof(ICanvas.DrawPath)));

        var bare = new KlyChart<Sale, string>();
        Assert.Empty(Draw(bare).Calls);
    }

    [Fact]
    public void Data_that_is_all_gaps_still_draws_without_failing()
    {
        var chart = new KlyChart<Sale, string>
        {
            Data = new Sale[] { new("a", null, null), new("b", null, null) },
            Index = s => s.Month,
            Categories = [new("online", s => s.Online)],
            Type = KlyChartType.Area,
        };

        Assert.Equal(0, Draw(chart).Count(nameof(ICanvas.FillPath)));
    }

    [Fact]
    public void A_touch_activates_the_row_and_a_lift_reports_the_pick_with_the_series_under_it()
    {
        var chart = Make(KlyChartType.Bar, true);
        var picks = new List<KlyChartSelectEventArgs<Sale, string>>();
        chart.Selected += (_, e) => picks.Add(e);
        var layout = chart.Canvas.LayoutFor(400, 300);
        var bar = layout.Series[1].Bars[1];
        var point = new PointF((float)(bar.X + bar.Width / 2), (float)(bar.Y + bar.Height / 2));

        chart.Canvas.TouchAt(point, false, new Size(400, 300));
        Assert.Equal(1, chart.Canvas.ActiveRow);
        Assert.Empty(picks);

        chart.Canvas.TouchAt(point, true, new Size(400, 300));
        var pick = Assert.Single(picks);
        Assert.Equal((1, "Feb", "store", 8d), (pick.Row, pick.Index, pick.SeriesKey, pick.Value));

        chart.Canvas.TouchAt(new PointF(1, 1), true, new Size(400, 300));
        Assert.Equal(-1, chart.Canvas.ActiveRow);
        Assert.Single(picks);
    }

    [Fact]
    public void A_touch_on_a_donut_slice_names_the_slice()
    {
        var chart = Make(KlyChartType.Donut);
        var picks = new List<KlyChartSelectEventArgs<Sale, string>>();
        chart.Selected += (_, e) => picks.Add(e);
        var layout = chart.Canvas.LayoutFor(300, 300);
        var mid = (layout.Radius + layout.InnerRadius) / 2;

        chart.Canvas.TouchAt(new PointF((float)layout.CenterX, (float)(layout.CenterY + mid)), true, new Size(300, 300));

        var pick = Assert.Single(picks);
        Assert.Equal("Mar", pick.Index);
        Assert.Equal("online", pick.SeriesKey);
    }

    [Fact]
    public void A_new_model_resets_the_active_row()
    {
        var chart = Make(KlyChartType.Line);
        chart.Canvas.ActiveRow = 2;

        chart.Stacked = true;

        Assert.Equal(-1, chart.Canvas.ActiveRow);
    }
}
