using Klyroo.UI.Maui;

namespace Klyroo.Maui.Native;

/// <summary>The controls beyond the basics: navigation, display blocks, inputs, overlays and a chart.</summary>
public class Showcase2Page : ContentPage
{
    private sealed record Sale(string Month, double Online, double Store);

    public Showcase2Page()
    {
        Title = "More controls";
        this.SetAppThemeColor(BackgroundColorProperty, KlyTheme.Background.Light, KlyTheme.Background.Dark);

        var toaster = new KlyToaster();
        var modal = new KlyModal { Title = "Delete project", Description = "This action cannot be undone.", Dismissible = true };
        var cancel = new KlyButton { Text = "Cancel", Variant = ButtonVariant.Outline, Color = KlyColor.Neutral };
        cancel.Clicked += (_, _) => modal.IsOpen = false;
        var confirm = new KlyButton { Text = "Delete", Color = KlyColor.Error };
        confirm.Clicked += async (_, _) =>
        {
            modal.IsOpen = false;
            await KlyToastService.Default.Add(new KlyToast { Title = "Deleted", Description = "The project is gone.", Color = KlyColor.Success });
        };
        modal.Footer = new HorizontalStackLayout { Spacing = 8, Children = { cancel, confirm } };

        var open = new KlyButton { Text = "Open modal" };
        open.Clicked += (_, _) => modal.IsOpen = true;
        var toast = new KlyButton { Text = "Show toast", Variant = ButtonVariant.Soft };
        toast.Clicked += async (_, _) => await KlyToastService.Default.Add(new KlyToast { Title = "Hello", Description = "A native toast.", Color = KlyColor.Info });

        var number = new KlyInputNumber { Value = 3, Min = 0, Max = 10, Step = 1, AccessibleName = "Quantity" };
        var slider = new KlySlider { Value = 40, Min = 0, Max = 100, AccessibleName = "Volume" };
        var sliderLabel = new Label { FontSize = 14 };
        sliderLabel.SetAppThemeColor(Label.TextColorProperty, KlyTheme.TextMuted.Light, KlyTheme.TextMuted.Dark);
        slider.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(KlySlider.Value)) sliderLabel.Text = $"Volume {slider.Value:0}";
        };
        sliderLabel.Text = $"Volume {slider.Value:0}";

        var sales = new List<Sale> { new("Jan", 12, 8), new("Feb", 18, 9), new("Mar", 15, 14), new("Apr", 24, 12), new("May", 22, 18), new("Jun", 30, 16) };
        var chart = new KlyChart<Sale, string>
        {
            Data = sales,
            Index = s => s.Month,
            Categories = [new("online", s => s.Online) { Label = "Online" }, new("store", s => s.Store) { Label = "Store" }],
            Type = KlyChartType.Line,
            Points = true,
            PlotHeight = 220,
            PlotWidth = 320,
        };
        var bars = new KlyChart<Sale, string>
        {
            Data = sales,
            Index = s => s.Month,
            Categories = [new("online", s => s.Online) { Label = "Online" }],
            Type = KlyChartType.Bar,
            PlotHeight = 180,
            PlotWidth = 320,
        };

        var tabs = new KlyTabs
        {
            Items =
            [
                new KlyTabItem("Overview") { Value = "overview", Content = Padded(new Label { Text = "Overview tab content." }.Muted()) },
                new KlyTabItem("Activity") { Value = "activity", Content = Padded(new Label { Text = "Activity tab content." }.Muted()) },
                new KlyTabItem("Settings") { Value = "settings", Content = Padded(new Label { Text = "Settings tab content." }.Muted()) },
            ],
            Value = "overview",
        };
        var accordion = new KlyAccordion
        {
            Items =
            [
                new KlyAccordionItem("What is Klyroo?") { Value = "a", Text = "A set of accessible components for .NET, now native on MAUI." },
                new KlyAccordionItem("Does it follow dark mode?") { Value = "b", Text = "Yes, every color follows the app theme live." },
            ],
        };
        var stat = new KlyStat<decimal> { Title = "Revenue", Value = 12480m, Unit = "EUR", Delta = 12.5m, DeltaUnit = "%", Caption = "vs last month", Trend = [4, 6, 5, 8, 7, 11, 12] };

        var stack = new VerticalStackLayout
        {
            Spacing = 20,
            Padding = new Thickness(16, 24),
            Children =
            {
                Section("Tabs", tabs),
                Section("Accordion", accordion),
                Section("Progress and stat", new KlyProgress { Value = 60, AccessibleName = "Upload" }, stat),
                Section("Inputs", number, slider, sliderLabel),
                Section("Chart", chart, bars),
                Section("Overlays", new HorizontalStackLayout { Spacing = 8, Children = { open, toast } }),
                modal,
                toaster,
            },
        };
        Content = new KlyOverlayHost { Content = new ScrollView { Content = stack } };
    }

    private static View Padded(View view)
    {
        view.Margin = new Thickness(0, 12);
        return view;
    }

    private static VerticalStackLayout Section(string title, params View[] children)
    {
        var heading = new Label { Text = title, FontSize = 18, FontAttributes = FontAttributes.Bold };
        heading.SetAppThemeColor(Label.TextColorProperty, KlyTheme.TextHighlighted.Light, KlyTheme.TextHighlighted.Dark);
        var section = new VerticalStackLayout { Spacing = 12 };
        section.Add(heading);
        foreach (var child in children) section.Add(child);
        return section;
    }
}

internal static class Showcase2Extensions
{
    public static Label Muted(this Label label)
    {
        label.SetAppThemeColor(Label.TextColorProperty, KlyTheme.TextMuted.Light, KlyTheme.TextMuted.Dark);
        return label;
    }
}
