using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Blux.UI.Tools.Tests;

public sealed class DisplayMediaFilePreviewTests : BunitContext
{
    public DisplayMediaFilePreviewTests()
    {
        Services.AddBluxUiTools();
    }

    [Fact]
    public void BxDisplay_renders_Heading_Text_Alert_and_Callout()
    {
        var heading = Render<BxDisplay>(p => p
            .Add(x => x.Kind, DisplayKind.Heading)
            .Add(x => x.Title, "Active Subscriptions")
            .Add(x => x.Count, "42")
            .Add(x => x.Description, "Updated 5m ago"));

        Assert.Contains("Active Subscriptions", heading.Find("[data-slot=heading-title]").TextContent);
        Assert.Equal("42", heading.Find("[data-slot=heading-count]").TextContent);

        var alert = Render<BxDisplay>(p => p
            .Add(x => x.Kind, DisplayKind.Alert)
            .Add(x => x.Tone, BxColor.Warning)
            .Add(x => x.Title, "Quota warning")
            .Add(x => x.Details, "85% of monthly volume used"));

        Assert.Equal("status", alert.Find("[data-slot=display-alert]").GetAttribute("role"));
        Assert.Empty(alert.FindAll("[data-slot=alert-details]"));
        alert.Find("[data-slot=alert-details-toggle]").Click();
        Assert.Contains("85% of monthly volume used", alert.Find("[data-slot=alert-details]").TextContent);
    }

    [Fact]
    public void BxDisplay_renders_Facts_Stat_Breakdown_Meter_Chart_Table_and_Diff()
    {
        var facts = Render<BxDisplay>(p => p
            .Add(x => x.Kind, DisplayKind.Facts)
            .Add(x => x.Facts, [
                new DisplayFact { Label = "Environment", Value = "Production", Copy = true },
                new DisplayFact { Label = "Region", Value = null }
            ]));
        Assert.Equal(2, facts.FindAll("[data-slot=fact-item]").Count);
        Assert.NotNull(facts.Find("[data-slot=fact-copy]"));
        Assert.NotNull(facts.Find("[data-slot=fact-empty]"));

        var stat = Render<BxDisplay>(p => p
            .Add(x => x.Kind, DisplayKind.Stat)
            .Add(x => x.Title, "MRR")
            .Add(x => x.Value, "€12,400")
            .Add(x => x.Delta, "+14%")
            .Add(x => x.Trend, [10, 14, 13, 19]));
        Assert.Equal("€12,400", stat.Find("[data-slot=stat-value]").TextContent);
        Assert.NotNull(stat.Find("[data-slot=stat-sparkline]"));

        var meter = Render<BxDisplay>(p => p
            .Add(x => x.Kind, DisplayKind.Meter)
            .Add(x => x.Title, "Storage")
            .Add(x => x.MeterValue, 92)
            .Add(x => x.MeterMax, 100)
            .Add(x => x.WarningThreshold, 75)
            .Add(x => x.ErrorThreshold, 90));
        Assert.Equal("error", meter.Find("[data-slot=display-meter]").GetAttribute("data-tone"));

        var diff = Render<BxDisplay>(p => p
            .Add(x => x.Kind, DisplayKind.Diff)
            .Add(x => x.DiffItems, [
                new DisplayDiffItem("Plan", "Starter", "Enterprise", Changed: true),
                new DisplayDiffItem("Currency", "EUR", "EUR", Changed: false)
            ]));
        Assert.Single(diff.FindAll("[data-slot=diff-item]"));
        Assert.Equal("Starter", diff.Find("[data-slot=diff-before]").TextContent);
        Assert.Equal("Enterprise", diff.Find("[data-slot=diff-after]").TextContent);
    }

    [Fact]
    public void BxDisplay_renders_Code_secret_mask_Record_Reveal_and_Accordion()
    {
        var code = Render<BxDisplay>(p => p
            .Add(x => x.Kind, DisplayKind.Code)
            .Add(x => x.Variant, "secret")
            .Add(x => x.Value, "sk_live_9876543210")
            .Add(x => x.VisibleChars, 4));

        Assert.Equal("••••••••3210", code.Find("[data-slot=code-value]").TextContent);
        code.Find("[data-slot=code-secret-toggle]").Click();
        Assert.Equal("sk_live_9876543210", code.Find("[data-slot=code-value]").TextContent);

        var list = Render<BxDisplay>(p => p
            .Add(x => x.Kind, DisplayKind.List)
            .Add(x => x.Max, 2)
            .Add(x => x.Step, 2)
            .Add(x => x.ListItems, [
                new DisplayListItem { Text = "Step 1" },
                new DisplayListItem { Text = "Step 2" },
                new DisplayListItem { Text = "Step 3" }
            ]));

        Assert.Equal(2, list.FindAll("[data-slot=list-item]").Count);
        list.Find("[data-slot=display-more]").Click();
        Assert.Equal(3, list.FindAll("[data-slot=list-item]").Count);

        var accordion = Render<BxDisplay>(p => p
            .Add(x => x.Kind, DisplayKind.Accordion)
            .Add(x => x.AccordionItems, [
                new DisplayAccordionItem("Section A", "Content A", DefaultOpen: true),
                new DisplayAccordionItem("Section B", "Content B")
            ]));

        Assert.Single(accordion.FindAll("[data-slot=accordion-content]"));
        accordion.FindAll("[data-slot=accordion-trigger]")[1].Click();
        Assert.Single(accordion.FindAll("[data-slot=accordion-content]"));
        Assert.Contains("Content B", accordion.Find("[data-slot=accordion-content]").TextContent);
    }

    [Fact]
    public async Task BxAudioPlayer_and_BxVideoPlayer_enforce_clip_rules_and_max_plays()
    {
        var plays = 0;
        var limitReached = false;
        var cut = Render<BxVideoPlayer>(p => p
            .Add(x => x.Src, "/media/demo.mp4")
            .Add(x => x.Start, 5)
            .Add(x => x.End, 25)
            .Add(x => x.Seekable, false)
            .Add(x => x.MaxPlays, 1)
            .Add(x => x.Plays, plays)
            .Add(x => x.PlaysChanged, v => plays = v)
            .Add(x => x.OnLimitReached, () => { limitReached = true; }));

        Assert.Equal(20, cut.Instance.ClipLength);
        Assert.NotNull(cut.Find("[data-slot=media-progress]"));
        Assert.Empty(cut.FindAll("[data-slot=media-seek]"));
        Assert.Contains("1 play left", cut.Find("[data-slot=video-plays-badge]").TextContent);

        await cut.InvokeAsync(() => cut.Instance.TogglePlayAsync());
        Assert.Equal(1, plays);

        // Re-render with updated Plays=1 and finish playback
        cut.Render(p => p
            .Add(x => x.Src, "/media/demo.mp4")
            .Add(x => x.Start, 5)
            .Add(x => x.End, 25)
            .Add(x => x.Seekable, true)
            .Add(x => x.MaxPlays, 1)
            .Add(x => x.Plays, plays)
            .Add(x => x.OnLimitReached, () => { limitReached = true; }));

        await cut.InvokeAsync(() => cut.Instance.SeekAsync(20));
        Assert.True(limitReached);
        Assert.True(cut.Instance.IsLocked);
        Assert.Contains("No plays left", cut.Find("[data-slot=video-locked-text]").TextContent);
    }

    [Fact]
    public void FilePreviewService_detects_kinds_parses_csv_and_drives_BxFilePreviewProvider()
    {
        Assert.Equal(FilePreviewKind.Pdf, FilePreviewService.DetectKind("invoice.pdf"));
        Assert.Equal(FilePreviewKind.Image, FilePreviewService.DetectKind("avatar.webp"));
        Assert.Equal(FilePreviewKind.Video, FilePreviewService.DetectKind("clip.mp4"));
        Assert.Equal(FilePreviewKind.Audio, FilePreviewService.DetectKind("voice.opus"));
        Assert.Equal(FilePreviewKind.Csv, FilePreviewService.DetectKind("export.csv"));
        Assert.Equal(FilePreviewKind.Markdown, FilePreviewService.DetectKind("README.md"));
        Assert.Equal(FilePreviewKind.Text, FilePreviewService.DetectKind("payload.json"));
        Assert.Equal(FilePreviewKind.Office, FilePreviewService.DetectKind("report.xlsx"));
        Assert.Equal(FilePreviewKind.Archive, FilePreviewService.DetectKind("bundle.tar.gz"));

        var csv = FilePreviewService.ParseCsv("\uFEFFName;Amount;Note\r\n\"Alice; Bob\";125.50;\"Line 1\nLine 2\"\r\nCharlie;80;OK", "export.csv");
        Assert.Equal(';', csv.Delimiter);
        Assert.Equal(["Name", "Amount", "Note"], csv.Headers);
        Assert.Equal(2, csv.Rows.Count);
        Assert.Equal("Alice; Bob", csv.Rows[0][0]);
        Assert.True(csv.NumericColumns[1]);
        Assert.False(csv.NumericColumns[0]);

        var provider = Render<BxFilePreviewProvider>();
        var service = Services.GetRequiredService<FilePreviewService>();
        Assert.True(service.IsMounted);

        service.OpenGallery([
            new FilePreviewFile
            {
                Src = "/files/report.docx",
                Name = "report.docx",
                Size = 20480,
                Rendition = new FilePreviewFile { Src = "/files/report.pdf", Name = "report.pdf", Kind = FilePreviewKind.Pdf }
            },
            new FilePreviewFile
            {
                Src = "/files/data.csv",
                Name = "data.csv",
                Content = "ColA,ColB\n1,10\n2,20"
            }
        ], new FilePreviewOptions { Loop = true });

        provider.Render();
        Assert.NotNull(provider.Find("[data-slot=file-preview-rendition-banner]"));
        Assert.NotNull(provider.Find("[data-slot=renderer-pdf]"));
        Assert.Equal("1 / 2", provider.Find("[data-slot=file-preview-counter]").TextContent.Trim());

        provider.Find("[data-slot=file-preview-next]").Click();
        Assert.NotNull(provider.Find("[data-slot=renderer-csv]"));
        Assert.Equal(2, provider.FindAll("[data-slot=csv-row]").Count);

        // Loop wrap around to index 0
        provider.Find("[data-slot=file-preview-next]").Click();
        Assert.Equal(0, service.CurrentIndex);

        provider.Find("[data-slot=file-preview-close]").Click();
        Assert.False(service.IsOpen);
    }
}
