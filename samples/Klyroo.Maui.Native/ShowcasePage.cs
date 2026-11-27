using Klyroo.UI.Maui;

namespace Klyroo.Maui.Native;

/// <summary>Every Klyroo.UI.Maui control, in its variants, in plain C#: no XAML and no web view.</summary>
public class ShowcasePage : ContentPage
{
    public ShowcasePage()
    {
        this.SetAppThemeColor(BackgroundColorProperty, KlyTheme.Background.Light, KlyTheme.Background.Dark);

        var themeToggle = new KlyButton { Text = "Toggle theme", Variant = ButtonVariant.Outline, Color = KlyColor.Neutral, Size = KlySize.Sm };
        themeToggle.Clicked += (_, _) =>
            Application.Current!.UserAppTheme = Application.Current.RequestedTheme == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark;

        var stack = new VerticalStackLayout
        {
            Spacing = 20,
            Padding = new Thickness(16, 24),
            Children =
            {
                Row(Heading("Klyroo.UI.Maui", 28), themeToggle),
                Section("Buttons", ButtonVariants(), ButtonColors(), ButtonSizes(), new KlyButton { Text = "Block", Block = true }, new KlyButton { Text = "Disabled", IsEnabled = false }),
                Section("Badges", BadgeRow(KlyVariant.Solid), BadgeRow(KlyVariant.Soft), BadgeRow(KlyVariant.Outline), BadgeRow(KlyVariant.Subtle)),
                Section("Card", Cards()),
                Section("Alerts", Alerts()),
                Section("Avatars", Avatars(), new KlySeparator(), new KlySeparator { Color = KlyColor.Primary, Size = KlySize.Md }),
                Section("Form", Form()),
            },
        };
        Content = new ScrollView { Content = stack };
    }

    private static Label Heading(string text, double size = 18) =>
        new Label { Text = text, FontSize = size, FontAttributes = FontAttributes.Bold, VerticalOptions = LayoutOptions.Center }
            .Themed(KlyTheme.TextHighlighted);

    private static VerticalStackLayout Section(string title, params View[] children)
    {
        var section = new VerticalStackLayout { Spacing = 12 };
        section.Add(Heading(title));
        foreach (var child in children) section.Add(child);
        return section;
    }

    private static FlexLayout Row(params View[] children)
    {
        var row = new FlexLayout { Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap, JustifyContent = Microsoft.Maui.Layouts.FlexJustify.SpaceBetween, AlignItems = Microsoft.Maui.Layouts.FlexAlignItems.Center };
        foreach (var child in children)
        {
            child.Margin = new Thickness(0, 0, 8, 8);
            row.Add(child);
        }
        return row;
    }

    private static FlexLayout Wrap(IEnumerable<View> children)
    {
        var row = new FlexLayout { Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap, AlignItems = Microsoft.Maui.Layouts.FlexAlignItems.Center };
        foreach (var child in children)
        {
            child.Margin = new Thickness(0, 0, 8, 8);
            row.Add(child);
        }
        return row;
    }

    private static FlexLayout ButtonVariants() =>
        Wrap(Enum.GetValues<ButtonVariant>().Select(v => (View)new KlyButton { Text = v.ToString(), Variant = v }));

    private static FlexLayout ButtonColors() =>
        Wrap(new[] { KlyColor.Primary, KlyColor.Secondary, KlyColor.Success, KlyColor.Info, KlyColor.Warning, KlyColor.Error, KlyColor.Neutral }
            .Select(c => (View)new KlyButton { Text = c.Value, Color = c }));

    private static FlexLayout ButtonSizes() =>
        Wrap(Enum.GetValues<KlySize>().Select(s => (View)new KlyButton { Text = s.ToString(), Size = s }));

    private static FlexLayout BadgeRow(KlyVariant variant) =>
        Wrap(new[] { KlyColor.Primary, KlyColor.Secondary, KlyColor.Success, KlyColor.Info, KlyColor.Warning, KlyColor.Error, KlyColor.Neutral }
            .Select(c => (View)new KlyBadge { Text = c.Value, Color = c, Variant = variant }));

    private static View Cards()
    {
        var stack = new VerticalStackLayout { Spacing = 12 };
        foreach (var variant in Enum.GetValues<CardVariant>())
        {
            var body = new VerticalStackLayout
            {
                Spacing = 4,
                Children =
                {
                    Heading($"{variant} card", 16),
                    new Label { Text = "A container for any content, with the Klyroo.UI card radius and borders." }.Themed(KlyTheme.TextMuted),
                },
            };
            stack.Add(new KlyCard { Variant = variant, Content = body });
        }
        return stack;
    }

    private static View Alerts()
    {
        var stack = new VerticalStackLayout { Spacing = 12 };
        stack.Add(new KlyAlert { Title = "Heads up", Description = "A solid primary alert.", Color = KlyColor.Primary });
        stack.Add(new KlyAlert { Title = "Saved", Description = "Your changes were saved.", Color = KlyColor.Success, Variant = KlyVariant.Soft });
        stack.Add(new KlyAlert { Title = "Careful", Description = "This action cannot be undone.", Color = KlyColor.Warning, Variant = KlyVariant.Subtle });
        stack.Add(new KlyAlert { Title = "Closable", Description = "Tap the cross to dismiss it.", Color = KlyColor.Error, Variant = KlyVariant.Outline, Closable = true });
        return stack;
    }

    private static View Avatars() =>
        Wrap(new View[]
        {
            new KlyAvatar { Text = "Ada Lovelace", Size = KlySize.Xs },
            new KlyAvatar { Text = "Grace Hopper", Size = KlySize.Sm },
            new KlyAvatar { Text = "Alan Turing", Size = KlySize.Md, Color = KlyColor.Primary },
            new KlyAvatar { Text = "Linus Torvalds", Size = KlySize.Lg, Color = KlyColor.Info },
            new KlyAvatar { Text = "Margaret Hamilton", Size = KlySize.Xl, Color = KlyColor.Success },
        });

    private static View Form()
    {
        var name = new KlyInput { Placeholder = "Your name", AccessibleName = "Name" };
        var email = new KlyInput { Placeholder = "Email", Keyboard = Keyboard.Email, AccessibleName = "Email" };
        var password = new KlyInput { Placeholder = "Password", IsPassword = true, AccessibleName = "Password" };
        var invalid = new KlyInput { Placeholder = "Invalid", Invalid = true, Text = "not an email", AccessibleName = "Invalid example" };
        var soft = new KlyInput { Placeholder = "Soft", Variant = FieldVariant.Soft, AccessibleName = "Soft example" };
        var message = new KlyTextarea { Placeholder = "Message", Rows = 3, AccessibleName = "Message" };
        var plan = new KlySelect { Placeholder = "Choose a plan", ItemsSource = new[] { "Free", "Pro", "Team" }, AccessibleName = "Plan" };
        var terms = new KlyCheckbox { Text = "Accept the terms", Description = "You can change this later." };
        var news = new KlySwitch { Text = "Newsletter", Description = "One email a month.", IsToggled = true };

        var summary = new Label().Themed(KlyTheme.TextMuted);
        void Update(object? sender, EventArgs e) =>
            summary.Text = $"name={name.Text}  plan={plan.SelectedItem}  terms={terms.IsChecked}  newsletter={news.IsToggled}";
        name.TextChanged += Update;
        plan.SelectedItemChanged += Update;
        terms.CheckedChanged += Update;
        news.Toggled += Update;
        Update(null, EventArgs.Empty);

        return new VerticalStackLayout { Spacing = 12, Children = { name, email, password, invalid, soft, message, plan, terms, news, summary } };
    }
}

internal static class ShowcaseExtensions
{
    public static Label Themed(this Label label, KlyThemeColor color)
    {
        label.SetAppThemeColor(Label.TextColorProperty, color.Light, color.Dark);
        return label;
    }
}
