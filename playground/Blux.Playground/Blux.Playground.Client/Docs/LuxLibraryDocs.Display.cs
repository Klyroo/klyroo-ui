using Blux.UI.Tools;

namespace Blux.Playground.Client.Docs;

public static partial class LuxLibraryDocs
{
    private static LibraryDocTree BuildDisplay() => new(
        "display", "Lux.Display", "#ec4899",
        [
            new("", "Introduction", "Getting Started",
                "19 read-only display blocks (BxDisplay), constrained Audio/Video media players with clip bounds and play quotas (BxAudioPlayer, BxVideoPlayer), and a multi-format modal file previewer (FilePreviewService & BxFilePreviewProvider).",
                [
                    new("overview", "What is Lux.Display?",
                        Paragraphs:
                        [
                            "Lux.Display provides a consistent visual vocabulary for read-only business data, assessment-grade media playback, and multi-format document lightboxes across Blazor Web and .NET MAUI Blazor Hybrid.",
                            "All MIME/extension kind detection, file size formatting, RFC 4180 CSV parsing, and media track/progress contracts live in 100% NativeAOT C# (Lux.Display.Core)."
                        ],
                        LiveDemoKey: "display-blocks",
                        LiveDemoTitle: "Lux.Display · Live Stat, Threshold Meter, Secret Mask & Diff"),
                    new("all-kinds", "All 19 DisplayKind Values",
                        TableHeaders: ["Category", "Exact DisplayKind Values", "Highlights"],
                        TableRows:
                        [
                            new("Editorial & Status (4)", "Heading, Text, Alert, Empty", "Section titles with count pills, collapsible Alert details, empty states"),
                            new("Metrics & Visuals (5)", "Facts, Stat, Breakdown, Meter, Chart", "Copyable fact grids, sparklines, breakdown lines, threshold-colored meters"),
                            new("Structured Data (6)", "Table, Diff, Record, Person, List, Accordion", "Before/after diffs, record cards, progressive Reveal More lists, accordions"),
                            new("Rich & Technical (4)", "Timeline, Date, Media, Code", "Timeline states (Done/Current/Upcoming), relative dates, image galleries & Variant=\"secret\" code")
                        ]),
                    new("layers", "Two-Layer Architecture",
                        TableHeaders: ["Package", "Primary Exports", "Compatibility"],
                        TableRows:
                        [
                            new("Lux.Display.Core", "FilePreviewService, FilePreviewFile, FilePreviewOptions, ParsedCsvPreview, MediaPlayerControl, VideoPlayerTrack, MediaPlayerProgress", "Headless C# · IsAotCompatible=true"),
                            new("Lux.Display", "BxDisplay, BxAudioPlayer, BxVideoPlayer, BxFilePreviewProvider, AddLuxDisplay()", "browser, ios, android, maccatalyst, windows")
                        ])
                ]),
            new("installation", "Installation", "Getting Started",
                "Install Lux.Display, register FilePreviewService, and mount BxFilePreviewProvider once in your root layout.",
                [
                    new("nuget", "1. Install Packages",
                        CodeSamples:
                        [
                            new("Terminal", """
                                dotnet add package Lux.Display.Core   # Headless NativeAOT MIME detector, CSV parser & media models
                                dotnet add package Lux.Display        # BxDisplay, BxAudioPlayer, BxVideoPlayer & Lightbox
                                """)
                        ]),
                    new("di", "2. Register Services & Mount Lightbox Host",
                        CodeSamples:
                        [
                            new("Program.cs / MauiProgram.cs", "builder.Services.AddLuxDisplay();"),
                            new("MainLayout.razor", "<BxFilePreviewProvider />")
                        ])
                ]),
            new("content-blocks", "Typography, Alerts & Facts", "Display Blocks",
                "Render headings with counts, expandable status alerts, and copyable key/value fact lists with unified DisplaySurface tokens.",
                [
                    new("surfaces", "DisplaySurface & Common Parameters",
                        Paragraphs: ["Every BxDisplay block shares Surface (Plain, Muted, Bordered, Elevated), Tone (semantic BxColor), Loading (automatic skeleton placeholder), Title, Description, Chip (DisplayChip), Action, and Actions (DisplayAction)."],
                        TableHeaders: ["DisplaySurface", "Visual Style", "Best For"],
                        TableRows:
                        [
                            new("Plain", "Transparent background, no border", "Inline embedding inside existing cards"),
                            new("Muted", "Soft tinted surface", "Callouts, alert details, subtle groupings"),
                            new("Bordered", "1px hairline border on card background", "Standalone KPI and fact blocks"),
                            new("Elevated", "Raised surface with subtle shadow", "Primary focus cards and featured summaries")
                        ]),
                    new("examples", "Heading, Alert & Copyable Facts",
                        Paragraphs: ["DisplayFact supports Copy = true (with optional custom CopyText), Mono = true for IDs/hashes, Emphasis = true, Tone, Chip, and graceful empty placeholders when Value is null."],
                        CodeSamples:
                        [
                            new("ContentBlocks.razor", """
                                <BxDisplay Kind="DisplayKind.Heading"
                                           Title="Active Subscriptions"
                                           Count="42"
                                           Description="Updated 5 minutes ago" />

                                <BxDisplay Kind="DisplayKind.Alert"
                                           Tone="BxColor.Warning"
                                           Title="Quota warning"
                                           Details="85% of monthly API volume has been consumed." />

                                <BxDisplay Kind="DisplayKind.Facts"
                                           Surface="DisplaySurface.Bordered"
                                           Facts="@([
                                               new DisplayFact { Label = "Environment", Value = "Production", Copy = true, Mono = true },
                                               new DisplayFact { Label = "Cluster", Value = "eu-west-3", Chip = new DisplayChip("Healthy", BxColor.Success) },
                                               new DisplayFact { Label = "Fallback Region", Value = null }
                                           ])" />
                                """)
                        ])
                ]),
            new("metrics-and-diff", "Stats, Breakdown, Meter & Diff", "Display Blocks",
                "Present inline KPIs with sparklines, itemized breakdowns, threshold meters, and before/after diffs.",
                [
                    new("metrics", "Stat, Breakdown, Meter & Diff",
                        Paragraphs:
                        [
                            "DisplayKind.Meter automatically transitions data-tone to warning when MeterValue >= WarningThreshold and to error when MeterValue >= ErrorThreshold.",
                            "DisplayKind.Diff hides unchanged rows by default (override with ShowUnchanged=\"true\") and highlights before/after values side-by-side."
                        ],
                        LiveDemoKey: "display-blocks",
                        LiveDemoTitle: "Live Stat, Threshold Meter, Secret Mask & Diff",
                        CodeSamples:
                        [
                            new("MetricsAndDiff.razor", """
                                <BxDisplay Kind="DisplayKind.Stat"
                                           Title="MRR" Value="€148,900"
                                           Delta="+14.2%" Trend="@([102.0, 118.0, 134.0, 148.9])" />

                                <BxDisplay Kind="DisplayKind.Meter"
                                           Title="Storage Quota"
                                           MeterValue="92" MeterMax="100"
                                           WarningThreshold="75" ErrorThreshold="90" />

                                <BxDisplay Kind="DisplayKind.Diff"
                                           Title="Subscription Changes"
                                           DiffItems="@([
                                               new DisplayDiffItem("Plan", "Starter", "Enterprise", Changed: true),
                                               new DisplayDiffItem("Seats", "5", "25", Changed: true),
                                               new DisplayDiffItem("Currency", "EUR", "EUR", Changed: false)
                                           ])" />
                                """)
                        ])
                ]),
            new("code-and-lists", "Code, Secret Masking & Lists", "Display Blocks",
                "Mask sensitive API keys with reveal toggles and progressively reveal long lists using Max and Step.",
                [
                    new("secret", "Code & Secret Masking",
                        Paragraphs: ["Setting Kind=\"DisplayKind.Code\" with Variant=\"secret\" masks every leading character with bullets while leaving the last VisibleChars (e.g. 4 -> \"••••••••3210\") visible. Clicking the toggle button reveals or re-masks the secret in place, and Copy=\"true\" copies the unmasked value."],
                        CodeSamples:
                        [
                            new("CodeAndList.razor", """
                                <BxDisplay Kind="DisplayKind.Code"
                                           Variant="secret"
                                           Title="Webhook Signing Secret"
                                           Value="sk_live_9876543210"
                                           VisibleChars="4"
                                           Copy="true" />

                                <BxDisplay Kind="DisplayKind.List"
                                           Title="Deployment Steps"
                                           Max="2" Step="2"
                                           ListItems="@([
                                               new DisplayListItem { Text = "1. Build NativeAOT core" },
                                               new DisplayListItem { Text = "2. Run xUnit & bUnit suites" },
                                               new DisplayListItem { Text = "3. Publish container image" }
                                           ])" />
                                """)
                        ])
                ]),
            new("structured-blocks", "Timeline, People, Contacts & Accordion", "Display Blocks",
                "Display status timelines, team directories, contact cards, records, and collapsible accordions.",
                [
                    new("timeline", "Timeline, People, Contacts & Accordion Examples",
                        Paragraphs: ["DisplayTimelineState provides Done, Current, and Upcoming visual markers. Accordion supports single-open (default) or Multiple=\"true\" expansion."],
                        CodeSamples:
                        [
                            new("StructuredBlocks.razor", """
                                <BxDisplay Kind="DisplayKind.Timeline"
                                           TimelineItems="@([
                                               new DisplayTimelineItem("Order placed", "09:15", DisplayTimelineState.Done),
                                               new DisplayTimelineItem("Provisioning cluster", "09:18", DisplayTimelineState.Current),
                                               new DisplayTimelineItem("DNS propagation", "Pending", DisplayTimelineState.Upcoming)
                                           ])" />

                                <BxDisplay Kind="DisplayKind.Accordion"
                                           Multiple="true"
                                           AccordionItems="@([
                                               new DisplayAccordionItem("sla", "SLA Guarantee", "99.95% uptime across multi-region clusters."),
                                               new DisplayAccordionItem("support", "Enterprise Support", "24/7 response within 15 minutes.")
                                           ])" />
                                """)
                        ])
                ]),
            new("audio-player", "Constrained Audio Player", "Media & Files",
                "Play bounded audio segments with play-count quotas, rate selection, keyboard shortcuts, and seek locking.",
                [
                    new("audio", "BxAudioPlayer Constraints & Controls",
                        Paragraphs:
                        [
                            "BxAudioPlayer enforces assessment-grade constraints: Start and End slice playback to an exact sub-range of a longer recording, Seekable=\"false\" blocks scrubbing, and MaxPlays tracks completed plays (two-way bindable via @bind-Plays) before firing OnLimitReached and locking controls.",
                            "Hide specific controls via Hide=\"@([MediaPlayerControl.Download, MediaPlayerControl.Rate])\"."
                        ],
                        TableHeaders: ["Parameter", "Type", "Purpose"],
                        TableRows:
                        [
                            new("Start / End", "double / double?", "Clip playback window in seconds"),
                            new("MaxPlays / Plays", "int / int", "Maximum allowed plays (0 = unlimited); fires OnLimitReached when reached"),
                            new("Seekable", "bool (true)", "Set false to prevent scrubbing forward or backward"),
                            new("Rate / Rates", "double / IReadOnlyList<double>?", "Playback speed control (e.g. 0.75x, 1x, 1.25x, 1.5x)"),
                            new("Hide", "IReadOnlyList<MediaPlayerControl>?", "Hide any of: Time, Volume, Rate, Captions, PictureInPicture, Fullscreen, Download")
                        ],
                        LiveDemoKey: "display-audio",
                        LiveDemoTitle: "BxAudioPlayer · Bounded Clip (15s–45s) & MaxPlays=2",
                        CodeSamples:
                        [
                            new("ExamAudio.razor", """
                                <BxAudioPlayer Src="/media/listening-test.mp3"
                                               Title="Listening Comprehension · Extract 1"
                                               Start="15" End="45"
                                               MaxPlays="2"
                                               @bind-Plays="_playsUsed"
                                               Seekable="false"
                                               Hide="@([MediaPlayerControl.Download])"
                                               OnLimitReached="@HandleQuotaReached" />
                                """)
                        ])
                ]),
            new("video-player", "Constrained Video Player", "Media & Files",
                "Play bounded video segments with WebVTT subtitle tracks (VideoPlayerTrack), play quotas, and seek guards.",
                [
                    new("video", "BxVideoPlayer & VideoPlayerTrack",
                        Paragraphs: ["BxVideoPlayer shares the exact same clip-bounds (Start/End), MaxPlays, Seekable, Rate, and MediaPlayerControl hiding rules as BxAudioPlayer, and adds Poster images plus WebVTT caption/subtitle tracks."],
                        CodeSamples:
                        [
                            new("TrainingVideo.razor", """
                                <BxVideoPlayer Src="/media/safety-briefing.mp4"
                                               Title="Annual Safety Briefing"
                                               Start="0" End="90"
                                               MaxPlays="3"
                                               Seekable="false"
                                               Tracks="@([new VideoPlayerTrack("/media/en.vtt", "English", "en", Default: true)])" />
                                """)
                        ])
                ]),
            new("file-preview", "FilePreviewService & Lightbox", "Media & Files",
                "Open single files or navigable galleries in Modal, Drawer, or Fullscreen lightboxes with headless MIME detection and RFC 4180 CSV parsing.",
                [
                    new("kinds", "All 10 FilePreviewKind Values & 3 Container Modes",
                        TableHeaders: ["Enum", "Exact Values", "Behavior"],
                        TableRows:
                        [
                            new("FilePreviewKind", "Pdf, Image, Video, Audio, Csv, Markdown, Text, Office, Archive, Other", "Auto-detected via DetectKind(fileNameOrUrl, mime) in Lux.Display.Core"),
                            new("FilePreviewContainer", "Modal, Drawer, Fullscreen", "DefaultContainerForKind() picks Fullscreen for PDFs/Office and Modal for Images/Media; override via FilePreviewOptions.Mode")
                        ]),
                    new("service", "Complete FilePreviewService API",
                        Paragraphs: ["FilePreviewFile supports Src, Name, Mime, Size, UpdatedAt, Poster, inline Content, fallback Rendition, metadata Details (FilePreviewDetail), custom DownloadUrl, and custom toolbar Actions (FilePreviewAction)."],
                        CodeSamples:
                        [
                            new("OpenGallery.cs", """
                                // Open a multi-file gallery starting at index 0 with loop navigation
                                filePreview.OpenGallery(
                                    [
                                        new FilePreviewFile
                                        {
                                            Src = "/files/architecture.png",
                                            Name = "Architecture Diagram",
                                            Size = 248_900,
                                            Details = [new FilePreviewDetail("Resolution", "2560 × 1440")]
                                        },
                                        new FilePreviewFile
                                        {
                                            Src = "/files/spec-v2.pdf",
                                            Name = "Specification v2.pdf",
                                            Mime = "application/pdf"
                                        }
                                    ],
                                    new FilePreviewOptions { Index = 0, Mode = FilePreviewContainer.Modal, Loop = true, PdfPage = 1 });

                                // Headless utilities available in Lux.Display.Core (NativeAOT):
                                var kind = FilePreviewService.DetectKind("report.csv");                 // FilePreviewKind.Csv
                                var size = FilePreviewService.FormatFileSize(1_572_864);                // "1.5 MB"
                                var csv  = FilePreviewService.ParseCsv("Region,MRR\nEU,148900\nUS,192400", maxRows: 500);
                                """)
                        ])
                ]),
            MauiAndAotPage("Lux.Display", "Lux.Display.Core", "Lux.Display", "builder.Services.AddLuxDisplay();",
                "var kind = FilePreviewService.DetectKind(filePath); var csv = FilePreviewService.ParseCsv(rawText);"),
            new("examples", "Live Examples", "Examples",
                "Interactive content viewers exercising BxDisplay auto-detection, structured CSV/JSON rendering, and the BxAudioPlayer media transport.",
                [
                    new("format-blocks", "1. Interactive BxDisplay Blocks (Stat, Meter Thresholds, Masked Secret & Diff)",
                        Paragraphs: ["Toggle the storage quota meter across Normal (55%), Warning (82%), and Error (95%) thresholds alongside Stat sparklines, masked secret code blocks with copy button, and structured Diff rows."],
                        LiveDemoKey: "display-blocks",
                        LiveDemoTitle: "BxDisplay · Stat, Meter, Secret Code & Diff",
                        CodeSamples:
                        [
                            new("DisplayBlocks.razor", """
                                <BxDisplay Kind="DisplayKind.Stat" Title="MRR" Value="€148,900" Delta="+14.2%" Trend="@([102.0, 118.0, 134.0, 148.9])" />
                                <BxDisplay Kind="DisplayKind.Meter" Title="Storage Quota" MeterValue="@_val" MeterMax="100" WarningThreshold="75" ErrorThreshold="90" />
                                <BxDisplay Kind="DisplayKind.Code" Variant="secret" Title="API Token" Value="sk_live_luxstack_3210" VisibleChars="4" Copy="true" />
                                <BxDisplay Kind="DisplayKind.Diff" Title="Plan Upgrade" DiffItems="@([new DisplayDiffItem("Tier", "Starter", "Enterprise", Changed: true)])" />
                                """)
                        ]),
                    new("structured-inspector", "2. Structured Facts & Release Timeline Blocks",
                        Paragraphs: ["Combine DisplayKind.Facts (copyable monospace metadata, status chips, null-safe fallbacks) with DisplayKind.Timeline (Done, Current, Upcoming pipeline stages)."],
                        LiveDemoKey: "display-structured",
                        LiveDemoTitle: "BxDisplay · Facts & Timeline",
                        CodeSamples:
                        [
                            new("StructuredBlocks.razor", """
                                <BxDisplay Kind="DisplayKind.Facts" Surface="DisplaySurface.Bordered" Title="Cluster Metadata" Facts="@([
                                    new DisplayFact { Label = "Environment", Value = "Production", Copy = true, Mono = true },
                                    new DisplayFact { Label = "Region", Value = "eu-west-3", Chip = new DisplayChip("Healthy", BxColor.Success) }
                                ])" />
                                <BxDisplay Kind="DisplayKind.Timeline" Surface="DisplaySurface.Bordered" Title="Release Pipeline" TimelineItems="@([
                                    new DisplayTimelineItem("Build NativeAOT Core", Time: "09:12", State: DisplayTimelineState.Done),
                                    new DisplayTimelineItem("Rollout Canary", Time: "In progress", State: DisplayTimelineState.Current)
                                ])" />
                                """)
                        ]),
                    new("audio-player", "3. Constrained BxAudioPlayer & Headless FilePreviewService",
                        Paragraphs: ["Render a constrained exam-mode BxAudioPlayer (Start/End clip window, MaxPlays=2, Seekable=false) paired with live Lux.Display.Core FilePreviewService.DetectKind, FormatFileSize, and ParseCsv output."],
                        LiveDemoKey: "display-audio",
                        LiveDemoTitle: "BxAudioPlayer & Headless FilePreviewService",
                        CodeSamples:
                        [
                            new("AudioAndPreview.razor", """
                                <BxAudioPlayer Src="/media/sample.mp3" Title="Exam Listening Extract"
                                               Start="15" End="45" MaxPlays="2" Seekable="false" />

                                @code {
                                    var kind  = FilePreviewService.DetectKind("report.csv");                 // FilePreviewKind.Csv
                                    var size  = FilePreviewService.FormatFileSize(1_572_864);                // "1.5 MB"
                                    var table = FilePreviewService.ParseCsv("Region,MRR\nEU,148900\nUS,192400");
                                }
                                """)
                        ])
                ]),
            new("api", "API Reference", "Reference",
                "Complete public API reference for Lux.Display.Core and Lux.Display.",
                [],
                ApiTypes: [typeof(BxDisplay), typeof(BxAudioPlayer), typeof(BxVideoPlayer), typeof(BxFilePreviewProvider), typeof(FilePreviewService), typeof(FilePreviewFile), typeof(FilePreviewOptions)])
        ]);
}
