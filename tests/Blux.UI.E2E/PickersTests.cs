using System.Text;

namespace Blux.UI.E2E;

public class PickersTests(PlaygroundServer server) : PlaygroundTest(server)
{
    private static readonly string[] Slugs = ["calendar", "input-date", "input-time", "color-picker", "file-upload", "carousel"];

    public static TheoryData<string, string> EverySlugInEveryMode()
    {
        var data = new TheoryData<string, string>();
        foreach (var slug in Slugs)
        {
            foreach (var mode in new[] { "ssr", "server", "wasm", "auto" }) data.Add(slug, mode);
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(EverySlugInEveryMode))]
    public async Task Picker_page_has_no_accessibility_violations(string slug, string mode)
    {
        await Open(mode, slug);

        await AssertNoAccessibilityViolations();
    }

    private ILocator Day(string testId, string date) => Page.Locator($"[data-testid={testId}] button[data-date='{date}']");

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Calendar_is_a_date_grid_operable_with_the_keyboard(string mode)
    {
        await Open(mode, "calendar");
        var grid = Page.Locator("[data-testid=calendar] [role=grid]");
        await Assertions.Expect(grid).ToBeVisibleAsync();
        await Assertions.Expect(grid.Locator("th[scope=col]")).ToHaveCountAsync(7);

        await Day("calendar", "2026-11-19").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Assertions.Expect(Day("calendar", "2026-11-20")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Assertions.Expect(Day("calendar", "2026-11-27")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowUp");
        await Page.Keyboard.PressAsync("Enter");

        await Assertions.Expect(Day("calendar", "2026-11-20")).ToHaveAttributeAsync("data-selected", "");
        await Assertions.Expect(Day("calendar", "2026-11-20").Locator("xpath=..")).ToHaveAttributeAsync("aria-selected", "true");
        await Assertions.Expect(Day("calendar", "2026-11-20")).ToBeFocusedAsync();
        await Assertions.Expect(Page.Locator("[data-testid=calendar] input[type=hidden]")).ToHaveValueAsync("2026-11-20");
        if (mode != "ssr") await Assertions.Expect(Page.GetByTestId("calendar-state")).ToHaveTextAsync("Selected: 2026-11-20");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Calendar_pages_by_month_and_refuses_disabled_and_out_of_range_dates(string mode)
    {
        await Open(mode, "calendar");
        var heading = Page.Locator("[data-testid=calendar] [aria-live=polite]");
        await Assertions.Expect(heading).ToHaveTextAsync("November 2026");

        // aria-disabled days are not actionable for Playwright, so force the click to prove the component refuses it.
        await Day("calendar", "2026-11-25").ClickAsync(new() { Force = true });
        await Assertions.Expect(Day("calendar", "2026-11-25")).ToHaveAttributeAsync("aria-disabled", "true");
        await Assertions.Expect(Day("calendar", "2026-11-19")).ToHaveAttributeAsync("data-selected", "");

        await Day("calendar", "2026-11-19").FocusAsync();
        await Page.Keyboard.PressAsync("PageDown");
        await Assertions.Expect(heading).ToHaveTextAsync("December 2026");
        await Assertions.Expect(Day("calendar", "2026-12-19")).ToBeFocusedAsync();

        // The maximum is the 20th: further paging stops there.
        await Page.Keyboard.PressAsync("PageDown");
        await Assertions.Expect(Day("calendar", "2026-12-20")).ToBeFocusedAsync();

        await Page.GetByRole(AriaRole.Button, new() { Name = "Previous month" }).First.ClickAsync();
        await Assertions.Expect(heading).ToHaveTextAsync("November 2026");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Calendar_range_and_multiple_selection_update_the_bound_values(string mode)
    {
        await Open(mode, "calendar");

        await Day("range", "2026-11-16").ClickAsync();
        await Assertions.Expect(Page.GetByTestId("range-state")).ToHaveTextAsync("Range: 2026-11-16 to ...");
        await Day("range", "2026-11-18").ClickAsync();
        await Assertions.Expect(Page.GetByTestId("range-state")).ToHaveTextAsync("Range: 2026-11-16 to 2026-11-18");
        await Assertions.Expect(Day("range", "2026-11-17")).ToHaveAttributeAsync("data-in-range", "");

        await Day("multiple", "2026-11-05").ClickAsync();
        await Day("multiple", "2026-11-02").ClickAsync();
        await Assertions.Expect(Page.GetByTestId("multiple-state")).ToHaveTextAsync("Days: 11-05, 11-20");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task InputDate_picking_a_day_posts_an_ISO_date(string mode)
    {
        await Open(mode, "input-date");
        var field = Page.Locator("bx-date-field", new() { Has = Page.GetByTestId("appointment") });
        var input = Page.GetByTestId("appointment");
        await Assertions.Expect(input).ToHaveValueAsync("2026-11-10");

        await field.GetByRole(AriaRole.Button, new() { Name = "Choose date" }).ClickAsync();
        var day = field.Locator("button[data-date='2026-11-19']");
        await Assertions.Expect(day).ToBeVisibleAsync();
        await day.ClickAsync();

        await Assertions.Expect(input).ToHaveValueAsync("2026-11-19");
        await Assertions.Expect(day).ToBeHiddenAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Book" }).ClickAsync();

        await Assertions.Expect(Page.GetByTestId("appointment-saved")).ToHaveTextAsync("Booked: 2026-11-19");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task InputDate_typed_value_updates_the_bound_value(string mode)
    {
        await Open(mode, "input-date");

        await Page.GetByTestId("birthday").FillAsync("1990-04-02");

        await Assertions.Expect(Page.GetByTestId("birthday-state")).ToHaveTextAsync("Selected: 1990-04-02");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task InputTime_typing_digits_fills_the_segments_and_posts_HH_mm(string mode)
    {
        await Open(mode, "input-time");
        var field = Page.GetByTestId("meeting");
        var hours = field.GetByRole(AriaRole.Spinbutton, new() { Name = "Hours" });

        await hours.ClickAsync();
        await Page.Keyboard.TypeAsync("0930");

        await Assertions.Expect(hours).ToHaveAttributeAsync("aria-valuenow", "9");
        await Assertions.Expect(field.GetByRole(AriaRole.Spinbutton, new() { Name = "Minutes" })).ToHaveAttributeAsync("aria-valuenow", "30");
        await Assertions.Expect(field.Locator("input[type=hidden]")).ToHaveValueAsync("09:30");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Schedule" }).ClickAsync();

        await Assertions.Expect(Page.GetByTestId("meeting-saved")).ToHaveTextAsync("Scheduled: 09:30");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task InputTime_arrow_keys_step_segments_and_the_12_hour_clock_has_a_period(string mode)
    {
        await Open(mode, "input-time");
        var alarm = Page.GetByTestId("alarm");
        await alarm.GetByRole(AriaRole.Spinbutton, new() { Name = "Hours" }).FocusAsync();
        await Page.Keyboard.PressAsync("ArrowUp");
        await Assertions.Expect(Page.GetByTestId("time-state")).ToContainTextAsync("Alarm: 15:30");

        var reminder = Page.GetByTestId("reminder");
        await reminder.GetByRole(AriaRole.Spinbutton, new() { Name = "Hours" }).ClickAsync();
        await Page.Keyboard.TypeAsync("0245");
        await reminder.GetByRole(AriaRole.Spinbutton, new() { Name = "AM/PM" }).FocusAsync();
        await Page.Keyboard.PressAsync("p");

        await Assertions.Expect(Page.GetByTestId("time-state")).ToContainTextAsync("reminder: 14:45");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task ColorPicker_sliders_are_operable_with_the_keyboard(string mode)
    {
        await Open(mode, "color-picker");
        var picker = Page.GetByTestId("brand");
        var hex = picker.GetByRole(AriaRole.Textbox, new() { Name = "Hex color" });
        await Assertions.Expect(hex).ToHaveValueAsync("#ffd23f");

        var saturation = picker.GetByRole(AriaRole.Slider, new() { Name = "Saturation" });
        await saturation.FocusAsync();
        await Page.Keyboard.PressAsync("Home");

        await Assertions.Expect(hex).ToHaveValueAsync("#ffffff");
        await Assertions.Expect(saturation).ToHaveAttributeAsync("aria-valuetext", "0%");
        await Assertions.Expect(picker.Locator("input[type=hidden]")).ToHaveValueAsync("#ffffff");
        if (mode != "ssr") await Assertions.Expect(Page.GetByTestId("brand-state")).ToHaveTextAsync("Color: #ffffff");

        var brightness = picker.GetByRole(AriaRole.Slider, new() { Name = "Brightness" });
        await brightness.FocusAsync();
        await Page.Keyboard.PressAsync("End");
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Assertions.Expect(hex).Not.ToHaveValueAsync("#ffffff");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task ColorPicker_hex_field_sets_the_bound_value(string mode)
    {
        await Open(mode, "color-picker");

        await Page.GetByTestId("brand").GetByRole(AriaRole.Textbox, new() { Name = "Hex color" }).FillAsync("#336699");

        await Assertions.Expect(Page.GetByTestId("brand-state")).ToHaveTextAsync("Color: #336699");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Carousel_buttons_dots_and_keys_change_the_slide(string mode)
    {
        await Open(mode, "carousel");
        var carousel = Page.GetByTestId("places");
        var dots = carousel.Locator("[data-slot=dot]");
        await Assertions.Expect(carousel).ToHaveAttributeAsync("aria-roledescription", "carousel");
        await Assertions.Expect(dots.Nth(0)).ToHaveAttributeAsync("aria-current", "true");

        await carousel.GetByRole(AriaRole.Button, new() { Name = "Next slide" }).ClickAsync();
        await Assertions.Expect(dots.Nth(1)).ToHaveAttributeAsync("aria-current", "true");
        await Assertions.Expect(carousel.Locator("[role=group][aria-roledescription=slide]").Nth(0)).ToHaveAttributeAsync("inert", "");
        await Assertions.Expect(carousel.Locator("[role=group][aria-roledescription=slide]").Nth(1)).Not.ToHaveAttributeAsync("inert", "");

        await carousel.GetByRole(AriaRole.Button, new() { Name = "Previous slide" }).ClickAsync();
        await Assertions.Expect(dots.Nth(0)).ToHaveAttributeAsync("aria-current", "true");

        // Looping: previous from the first slide goes to the last.
        await carousel.GetByRole(AriaRole.Button, new() { Name = "Previous slide" }).ClickAsync();
        await Assertions.Expect(dots.Nth(3)).ToHaveAttributeAsync("aria-current", "true");

        await dots.Nth(1).ClickAsync();
        await Assertions.Expect(dots.Nth(1)).ToHaveAttributeAsync("aria-current", "true");

        await carousel.Locator("[data-slot=viewport]").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Assertions.Expect(dots.Nth(2)).ToHaveAttributeAsync("aria-current", "true");
        if (mode != "ssr") await Assertions.Expect(Page.GetByTestId("places-state")).ToHaveTextAsync("Slide: 3");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Carousel_autoplay_can_be_paused_and_resumed(string mode)
    {
        await Open(mode, "carousel");
        var carousel = Page.GetByTestId("news");
        var dots = carousel.Locator("[data-slot=dot]");
        var pause = carousel.GetByRole(AriaRole.Button, new() { Name = "Pause automatic slide show" });

        // It rotates on its own while nobody points at it or focuses it.
        await Assertions.Expect(dots.Nth(1)).ToHaveAttributeAsync("aria-current", "true", new() { Timeout = 6_000 });

        await pause.ClickAsync();
        var play = carousel.GetByRole(AriaRole.Button, new() { Name = "Start automatic slide show" });
        await Assertions.Expect(play).ToBeVisibleAsync();
        await Assertions.Expect(carousel.Locator("[data-slot=viewport]")).ToHaveAttributeAsync("aria-live", "polite");
        await Page.EvaluateAsync("document.activeElement.blur()");
        await Page.Mouse.MoveAsync(0, 0);
        var current = await carousel.Locator("[data-slot=dot][aria-current=true]").GetAttributeAsync("data-index");
        await Page.WaitForTimeoutAsync(2_600);
        Assert.Equal(current, await carousel.Locator("[data-slot=dot][aria-current=true]").GetAttributeAsync("data-index"));

        await play.ClickAsync();
        await Page.EvaluateAsync("document.activeElement.blur()");
        await Page.Mouse.MoveAsync(0, 0);
        await Assertions.Expect(carousel.Locator("[data-slot=viewport]")).ToHaveAttributeAsync("aria-live", "off");
        await Assertions.Expect(carousel.Locator($"[data-slot=dot]:not([data-index='{current}'])[aria-current=true]")).ToHaveCountAsync(1, new() { Timeout = 6_000 });
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task FileUpload_lists_chosen_files_and_removes_them(string mode)
    {
        await Open(mode, "file-upload");
        var upload = Page.GetByTestId("documents");
        var input = upload.Locator("input[type=file]");

        await input.SetInputFilesAsync(new FilePayload { Name = "notes.txt", MimeType = "text/plain", Buffer = Encoding.UTF8.GetBytes("hello") });

        var item = upload.Locator("[data-slot=files] li");
        await Assertions.Expect(item).ToHaveCountAsync(1);
        await Assertions.Expect(item).ToContainTextAsync("notes.txt");
        await Assertions.Expect(item).ToContainTextAsync("5 B");

        await upload.GetByRole(AriaRole.Button, new() { Name = "Remove notes.txt" }).ClickAsync();
        await Assertions.Expect(item).ToHaveCountAsync(0);
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task FileUpload_refuses_files_over_the_size_limit_and_of_other_types(string mode)
    {
        await Open(mode, "file-upload");
        var upload = Page.GetByTestId("documents");

        await upload.Locator("input[type=file]").SetInputFilesAsync(
        [
            new FilePayload { Name = "big.txt", MimeType = "text/plain", Buffer = new byte[1_500_000] },
            new FilePayload { Name = "tool.exe", MimeType = "application/octet-stream", Buffer = new byte[10] },
            new FilePayload { Name = "ok.txt", MimeType = "text/plain", Buffer = Encoding.UTF8.GetBytes("ok") },
        ]);

        await Assertions.Expect(upload.Locator("[data-slot=error]")).ToContainTextAsync("big.txt is larger than 1 MB.");
        await Assertions.Expect(upload.Locator("[data-slot=error]")).ToContainTextAsync("tool.exe is not an accepted file type.");
        await Assertions.Expect(upload.Locator("[data-slot=files] li")).ToHaveCountAsync(1);
        await Assertions.Expect(upload.Locator("[data-slot=files] li")).ToContainTextAsync("ok.txt");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task FileUpload_form_receives_the_chosen_file(string mode)
    {
        await Open(mode, "file-upload");

        await Page.GetByTestId("documents").Locator("input[type=file]").SetInputFilesAsync(
            new FilePayload { Name = "report.pdf", MimeType = "application/pdf", Buffer = Encoding.UTF8.GetBytes("%PDF") });
        // Interactive modes hand the file to .NET through InputFile once it has been read in.
        await Page.WaitForTimeoutAsync(mode == "ssr" ? 0 : 500);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Upload" }).ClickAsync();

        await Assertions.Expect(Page.GetByTestId("documents-saved")).ToHaveTextAsync("Received: report.pdf");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task FileUpload_interactive_example_reads_the_file_in_dotnet(string mode)
    {
        await Open(mode, "file-upload");

        await Page.GetByTestId("avatar").Locator("input[type=file]").SetInputFilesAsync(
            new FilePayload { Name = "me.png", MimeType = "image/png", Buffer = new byte[64] });

        await Assertions.Expect(Page.GetByTestId("avatar-state")).ToHaveTextAsync("Read 64 bytes of me.png.");
    }
}
