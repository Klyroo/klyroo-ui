using System.Globalization;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Lux.UI.Tests;

public class PickersTests : LuxTestContext
{
    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");

    [Fact]
    public void Calendar_renders_its_data_for_the_browser_with_names_from_the_culture()
    {
        var cut = Render<LxCalendar>(p => p
            .Add(c => c.Value, new DateOnly(2026, 11, 19)).Add(c => c.Min, new DateOnly(2026, 11, 3)).Add(c => c.Max, new DateOnly(2026, 12, 20))
            .Add(c => c.DisabledDates, [new DateOnly(2026, 11, 25)]).Add(c => c.Culture, French).Add(c => c.Name, "day").Add(c => c.AriaLabel, "Date"));

        var host = cut.Find("lx-calendar");
        Assert.Equal("2026-11-19", host.GetAttribute("value"));
        Assert.Equal("2026-11-03", host.GetAttribute("min"));
        Assert.Equal("2026-12-20", host.GetAttribute("max"));
        Assert.Equal("2026-11-25", host.GetAttribute("disabled-dates"));
        Assert.Equal("1", host.GetAttribute("week-start"));
        Assert.Equal("fr-FR", host.GetAttribute("data-locale"));
        Assert.Equal("Date", host.GetAttribute("aria-label"));
        var months = host.GetAttribute("months")!.Split('|');
        Assert.Equal(12, months.Length);
        Assert.Equal("novembre", months[10]);
        Assert.Equal(7, host.GetAttribute("weekdays-long")!.Split('|').Length);
        Assert.Contains("size-9", host.GetAttribute("data-cls-day"));
        var hidden = cut.Find("input[type=hidden]");
        Assert.Equal("day", hidden.GetAttribute("name"));
        Assert.Equal("2026-11-19", hidden.GetAttribute("value"));
    }

    [Fact]
    public void Calendar_week_start_can_be_chosen()
    {
        var cut = Render<LxCalendar>(p => p.Add(c => c.WeekStart, DayOfWeek.Sunday).Add(c => c.Culture, French));

        Assert.Equal("0", cut.Find("lx-calendar").GetAttribute("week-start"));
    }

    [Fact]
    public void Calendar_range_and_multiple_values_are_serialized_for_the_form()
    {
        var range = Render<LxCalendar>(p => p.Add(c => c.Mode, LxCalendarMode.Range).Add(c => c.Range, new LxDateRange(new DateOnly(2026, 11, 9), null)).Add(c => c.Name, "stay"));
        var multiple = Render<LxCalendar>(p => p.Add(c => c.Mode, LxCalendarMode.Multiple).Add(c => c.Values, [new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 20)]).Add(c => c.Name, "days"));

        // The element writes one hidden input per date (multiple) or name.Start and name.End (range) into an empty span.
        Assert.Equal("range", range.Find("lx-calendar").GetAttribute("mode"));
        Assert.Equal("2026-11-09/", range.Find("lx-calendar").GetAttribute("value"));
        Assert.Equal("stay", range.Find("lx-calendar").GetAttribute("name"));
        Assert.Empty(range.FindAll("input[type=hidden]"));
        Assert.Empty(range.Find("[data-slot=values]").Children);
        Assert.Equal("2026-11-02,2026-11-20", multiple.Find("lx-calendar").GetAttribute("value"));
        Assert.Equal("days", multiple.Find("lx-calendar").GetAttribute("name"));
        Assert.Empty(multiple.FindAll("input[type=hidden]"));
    }

    [Fact]
    public void Calendar_updates_bound_values_from_the_browser()
    {
        DateOnly? single = null;
        var cut = Render<LxCalendar>(p => p.Add(c => c.Value, single).Add(c => c.ValueChanged, (DateOnly? v) => single = v));
        cut.Find("lx-calendar").TriggerEvent("onlxchange", new LxChangeEventArgs { Value = "2026-11-20", Sequence = 3 });
        Assert.Equal(new DateOnly(2026, 11, 20), single);
        Assert.Equal("3", cut.Find("lx-calendar").GetAttribute("data-lx-seq"));

        LxDateRange? range = null;
        var ranged = Render<LxCalendar>(p => p.Add(c => c.Mode, LxCalendarMode.Range).Add(c => c.RangeChanged, (LxDateRange v) => range = v));
        ranged.Find("lx-calendar").TriggerEvent("onlxchange", new LxChangeEventArgs { Value = "2026-11-09/2026-11-13", Sequence = 1 });
        Assert.Equal(new LxDateRange(new DateOnly(2026, 11, 9), new DateOnly(2026, 11, 13)), range);

        IReadOnlyList<DateOnly>? dates = null;
        var multiple = Render<LxCalendar>(p => p.Add(c => c.Mode, LxCalendarMode.Multiple).Add(c => c.ValuesChanged, (IReadOnlyList<DateOnly> v) => dates = v));
        multiple.Find("lx-calendar").TriggerEvent("onlxchange", new LxChangeEventArgs { Value = "2026-11-02,2026-11-20", Sequence = 1 });
        Assert.Equal([new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 20)], dates);
    }

    [Fact]
    public void Unbound_calendar_has_no_sequence()
    {
        var cut = Render<LxCalendar>();

        Assert.False(cut.Find("lx-calendar").HasAttribute("data-lx-seq"));
    }

    [Fact]
    public void InputDate_is_a_native_date_input_with_a_calendar_popover()
    {
        var cut = Render<LxInputDate>(p => p.Add(d => d.Value, new DateOnly(2026, 11, 10)).Add(d => d.Name, "when")
            .Add(d => d.Min, new DateOnly(2026, 1, 1)).Add(d => d.Culture, French));

        var input = cut.Find("lx-date-field > input");
        Assert.Equal("date", input.GetAttribute("type"));
        Assert.Equal("2026-11-10", input.GetAttribute("value"));
        Assert.Equal("2026-01-01", input.GetAttribute("min"));
        Assert.Equal("when", input.GetAttribute("name"));
        Assert.Equal("Choose date", cut.Find("lx-popover button").GetAttribute("aria-label"));
        var calendar = cut.Find("lx-popover lx-calendar");
        Assert.Equal("2026-11-10", calendar.GetAttribute("value"));
        // The calendar inside must not post: only the input carries the name.
        Assert.False(cut.Find("lx-calendar input[type=hidden]").HasAttribute("name"));
    }

    [Fact]
    public void InputDate_in_an_EditForm_posts_under_the_field_name_and_follows_the_model()
    {
        var model = new Booking { Date = new DateOnly(2026, 11, 10) };
        var cut = Render<EditForm>(p => p.Add(f => f.Model, model).Add(f => f.ChildContent, (EditContext _) => (RenderFragment)(b =>
        {
            b.OpenComponent<LxInputDate>(0);
            b.AddAttribute(1, "Value", model.Date);
            b.AddAttribute(2, "ValueChanged", EventCallback.Factory.Create<DateOnly?>(this, v => model.Date = v));
            b.AddAttribute(3, "ValueExpression", (Expression<Func<DateOnly?>>)(() => model.Date));
            b.CloseComponent();
        })));

        var input = cut.Find("lx-date-field > input");
        Assert.Equal("model.Date", input.GetAttribute("name"));
        Assert.Equal("2026-11-10", input.GetAttribute("value"));

        input.Change("2026-12-01");

        Assert.Equal(new DateOnly(2026, 12, 1), model.Date);
    }

    [Fact]
    public void InputTime_24_hour_renders_labelled_spinbutton_segments()
    {
        var cut = Render<LxInputTime>(p => p.Add(t => t.Value, new TimeOnly(14, 5)).Add(t => t.Hour12, false).Add(t => t.Name, "at").Add(t => t.AriaLabel, "Start"));

        var host = cut.Find("lx-input-time");
        Assert.Equal("group", host.GetAttribute("role"));
        Assert.Equal("14:05", host.GetAttribute("value"));
        Assert.False(host.HasAttribute("hour12"));
        var segments = cut.FindAll("[role=spinbutton]");
        Assert.Equal(["Hours", "Minutes"], segments.Select(s => s.GetAttribute("aria-label")));
        Assert.Equal(["14", "05"], segments.Select(s => s.GetAttribute("data-label")));
        Assert.All(segments, s => Assert.Equal("0", s.GetAttribute("tabindex")));
        var hidden = cut.Find("input[type=hidden]");
        Assert.Equal("at", hidden.GetAttribute("name"));
        Assert.Equal("14:05", hidden.GetAttribute("value"));
    }

    [Fact]
    public void InputTime_12_hour_adds_a_period_segment_and_empty_segments_are_placeholders()
    {
        var cut = Render<LxInputTime>(p => p.Add(t => t.Hour12, true).Add(t => t.Step, 15));

        var segments = cut.FindAll("[role=spinbutton]");
        Assert.Equal(3, segments.Count);
        Assert.Equal("AM/PM", segments[2].GetAttribute("aria-label"));
        Assert.Equal("--", segments[0].GetAttribute("data-label"));
        Assert.True(segments[0].HasAttribute("data-placeholder"));
        Assert.Equal("15", cut.Find("lx-input-time").GetAttribute("step"));
        Assert.True(cut.Find("lx-input-time").HasAttribute("hour12"));

        var afternoon = Render<LxInputTime>(p => p.Add(t => t.Hour12, true).Add(t => t.Value, new TimeOnly(15, 30)));
        Assert.Equal(["03", "30", "PM"], afternoon.FindAll("[role=spinbutton]").Select(s => s.GetAttribute("data-label")));
    }

    [Fact]
    public void InputTime_updates_the_bound_value_from_the_browser()
    {
        TimeOnly? value = null;
        var cut = Render<LxInputTime>(p => p.Add(t => t.Value, value).Add(t => t.ValueChanged, (TimeOnly? v) => value = v));

        cut.Find("lx-input-time").TriggerEvent("onlxchange", new LxChangeEventArgs { Value = "09:30", Sequence = 2 });
        Assert.Equal(new TimeOnly(9, 30), value);

        cut.Find("lx-input-time").TriggerEvent("onlxchange", new LxChangeEventArgs { Value = "", Sequence = 3 });
        Assert.Null(value);
    }

    [Fact]
    public void InputTime_in_a_disabled_state_is_not_focusable()
    {
        var cut = Render<LxInputTime>(p => p.Add(t => t.Disabled, true));

        Assert.All(cut.FindAll("[role=spinbutton]"), s =>
        {
            Assert.Equal("true", s.GetAttribute("aria-disabled"));
            Assert.False(s.HasAttribute("tabindex"));
        });
    }

    [Fact]
    public void InputTime_in_an_EditForm_posts_under_the_field_name_and_reports_validity()
    {
        var model = new Meeting();
        var cut = Render<EditForm>(p => p.Add(f => f.Model, model).Add(f => f.ChildContent, (EditContext _) => (RenderFragment)(b =>
        {
            b.OpenComponent<DataAnnotationsValidator>(0);
            b.CloseComponent();
            b.OpenComponent<LxInputTime>(1);
            b.AddAttribute(2, "Value", model.Start);
            b.AddAttribute(3, "ValueChanged", EventCallback.Factory.Create<TimeOnly?>(this, v => model.Start = v));
            b.AddAttribute(4, "ValueExpression", (Expression<Func<TimeOnly?>>)(() => model.Start));
            b.CloseComponent();
        })));
        Assert.Equal("model.Start", cut.Find("input[type=hidden]").GetAttribute("name"));

        cut.Find("lx-input-time").TriggerEvent("onlxchange", new LxChangeEventArgs { Value = "08:15", Sequence = 1 });

        Assert.Equal(new TimeOnly(8, 15), model.Start);
    }

    [Fact]
    public void ColorPicker_renders_sliders_with_names_a_hex_field_and_the_posted_value()
    {
        var cut = Render<LxColorPicker>(p => p.Add(c => c.Value, "#FFD23F").Add(c => c.Name, "accent").Add(c => c.AriaLabel, "Accent"));

        var host = cut.Find("lx-color-picker");
        Assert.Equal("#ffd23f", host.GetAttribute("value"));
        Assert.Equal("group", host.GetAttribute("role"));
        var sliders = cut.FindAll("input[type=range]");
        Assert.Equal(["Saturation", "Brightness", "Hue"], sliders.Select(s => s.GetAttribute("aria-label")));
        Assert.Equal("359", sliders[2].GetAttribute("max"));
        Assert.Equal("Hex color", cut.Find("input[type=text]").GetAttribute("aria-label"));
        var hidden = cut.Find("input[type=hidden]");
        Assert.Equal("accent", hidden.GetAttribute("name"));
        Assert.Equal("#ffd23f", hidden.GetAttribute("value"));
    }

    [Theory]
    [InlineData("#abc", "#aabbcc")]
    [InlineData("ABCDEF", "#abcdef")]
    [InlineData(" #1F6F7A ", "#1f6f7a")]
    [InlineData("#12", null)]
    [InlineData("red", null)]
    [InlineData(null, null)]
    public void ColorPicker_normalizes_hex_colors(string? text, string? expected) =>
        Assert.Equal(expected, LxColorPicker.Normalize(text));

    [Fact]
    public void ColorPicker_reports_a_normalized_bound_value()
    {
        string? value = null;
        var cut = Render<LxColorPicker>(p => p.Add(c => c.Value, value).Add(c => c.ValueChanged, (string? v) => value = v));

        cut.Find("lx-color-picker").TriggerEvent("onlxchange", new LxChangeEventArgs { Value = "#AABBCC", Sequence = 1 });

        Assert.Equal("#aabbcc", value);
    }

    [Fact]
    public void FileUpload_without_OnChange_renders_a_plain_named_file_input_inside_the_drop_zone()
    {
        var cut = Render<LxFileUpload>(p => p.Add(f => f.Name, "docs").Add(f => f.Multiple, true).Add(f => f.Accept, "image/*")
            .Add(f => f.MaxSize, 2 * 1024 * 1024).Add(f => f.MaxFiles, 3).Add(f => f.Hint, "PNG or JPG"));

        var input = cut.Find("label[data-slot=dropzone] > input[type=file]");
        Assert.Equal("docs", input.GetAttribute("name"));
        Assert.True(input.HasAttribute("multiple"));
        Assert.Equal("image/*", input.GetAttribute("accept"));
        Assert.Equal(cut.Find("label[data-slot=dropzone]").GetAttribute("for"), input.Id);
        var host = cut.Find("lx-file-upload");
        Assert.Equal("2097152", host.GetAttribute("max-size"));
        Assert.Equal("3", host.GetAttribute("max-files"));
        Assert.Contains("PNG or JPG", cut.Markup);
        Assert.Contains("Maximum size 2 MB", cut.Markup);
        Assert.NotNull(cut.Find("[data-slot=error][aria-live=polite]"));
    }

    [Fact]
    public void FileUpload_with_OnChange_renders_the_InputFile_component()
    {
        JSInterop.Mode = Bunit.JSRuntimeMode.Loose;
        var cut = Render<LxFileUpload>(p => p.Add(f => f.Name, "docs").Add(f => f.OnChange, EventCallback.Factory.Create<InputFileChangeEventArgs>(this, _ => { })));

        Assert.Single(cut.FindComponents<InputFile>());
        Assert.Equal("docs", cut.Find("input[type=file]").GetAttribute("name"));
    }

    [Theory]
    [InlineData(512L, "512 B")]
    [InlineData(2048L, "2 KB")]
    [InlineData(5L * 1024 * 1024, "5 MB")]
    public void FileUpload_formats_sizes(long bytes, string expected)
    {
        Assert.Equal(expected, Lux.UI.Internal.FileSizeText.Format(bytes, LxLocales.En));
    }

    [Theory]
    [InlineData("fr", "carrousel", "diapositive")]
    [InlineData("ja", "カルーセル", "スライド")]
    [InlineData("de", "Karussell", "Folie")]
    public void Carousel_role_descriptions_follow_the_locale(string code, string carousel, string slide)
    {
        var locale = LxLocales.All.Single(l => l.Code == code);

        var cut = Render<LxCarousel>(p => p.AddCascadingValue(locale).Add(c => c.Items, Slides(2)).Add(c => c.AriaLabel, "Places"));

        Assert.Equal(carousel, cut.Find("lx-carousel").GetAttribute("aria-roledescription"));
        Assert.All(cut.FindAll("[data-slot=viewport] > [role=group]"), s => Assert.Equal(slide, s.GetAttribute("aria-roledescription")));
    }

    [Fact]
    public void Carousel_follows_the_APG_pattern()
    {
        var cut = Render<LxCarousel>(p => p.Add(c => c.Items, Slides(3)).Add(c => c.AriaLabel, "Places").Add(c => c.Loop, true).Add(c => c.Index, 1));

        var host = cut.Find("lx-carousel");
        Assert.Equal("region", host.GetAttribute("role"));
        Assert.Equal(LxLocales.En.Messages.CarouselRoleDescription, host.GetAttribute("aria-roledescription"));
        Assert.Equal("Places", host.GetAttribute("aria-label"));
        Assert.True(host.HasAttribute("loop"));
        Assert.Equal("1", host.GetAttribute("index"));
        var slides = cut.FindAll("[data-slot=viewport] > [role=group]");
        Assert.Equal(3, slides.Count);
        Assert.All(slides, s => Assert.Equal(LxLocales.En.Messages.CarouselSlideRoleDescription, s.GetAttribute("aria-roledescription")));
        Assert.Equal(["1 of 3", "2 of 3", "3 of 3"], slides.Select(s => s.GetAttribute("aria-label")));
        var dots = cut.FindAll("[data-slot=dot]");
        Assert.Equal(["Go to slide 1", "Go to slide 2", "Go to slide 3"], dots.Select(d => d.GetAttribute("aria-label")));
        Assert.Equal([null, "true", null], dots.Select(d => d.GetAttribute("aria-current")));
        Assert.Equal("Previous slide", cut.Find("[data-slot=prev]").GetAttribute("aria-label"));
        Assert.Equal("Next slide", cut.Find("[data-slot=next]").GetAttribute("aria-label"));
        Assert.Equal("0", cut.Find("[data-slot=viewport]").GetAttribute("tabindex"));
    }

    [Fact]
    public void Carousel_autoplay_has_a_pause_control_before_the_other_controls()
    {
        var cut = Render<LxCarousel>(p => p.Add(c => c.Items, Slides(2)).Add(c => c.AriaLabel, "News").Add(c => c.AutoPlay, 3000));

        Assert.Equal("3000", cut.Find("lx-carousel").GetAttribute("autoplay"));
        var controls = cut.FindAll("[data-slot=controls] button");
        Assert.Equal("toggle", controls[0].GetAttribute("data-slot"));
        Assert.Equal("Pause automatic slide show", controls[0].GetAttribute("aria-label"));

        var still = Render<LxCarousel>(p => p.Add(c => c.Items, Slides(2)).Add(c => c.AriaLabel, "News"));
        Assert.Empty(still.FindAll("[data-slot=toggle]"));
        Assert.False(still.Find("lx-carousel").HasAttribute("autoplay"));
    }

    [Fact]
    public void Carousel_reports_the_index_to_a_bound_parent()
    {
        var index = 0;
        var cut = Render<LxCarousel>(p => p.Add(c => c.Items, Slides(3)).Add(c => c.AriaLabel, "Places").Add(c => c.Index, index).Add(c => c.IndexChanged, (int v) => index = v));

        cut.Find("lx-carousel").TriggerEvent("onlxchange", new LxChangeEventArgs { Value = "2", Sequence = 4 });

        Assert.Equal(2, index);
        Assert.Equal("4", cut.Find("lx-carousel").GetAttribute("data-lx-seq"));
    }

    [Fact]
    public void Carousel_can_hide_dots_and_arrows()
    {
        var cut = Render<LxCarousel>(p => p.Add(c => c.Items, Slides(2)).Add(c => c.AriaLabel, "x").Add(c => c.Dots, false).Add(c => c.Arrows, false));

        Assert.Empty(cut.FindAll("[data-slot=dot]"));
        Assert.Empty(cut.FindAll("[data-slot=prev]"));
        Assert.Empty(cut.FindAll("[data-slot=next]"));
    }

    private static LxCarouselItem[] Slides(int count) =>
        Enumerable.Range(1, count).Select(i => new LxCarouselItem((RenderFragment)(b => b.AddContent(0, $"Slide {i}")))).ToArray();

    private sealed class Booking
    {
        public DateOnly? Date { get; set; }
    }

    private sealed class Meeting
    {
        public TimeOnly? Start { get; set; }
    }
}
