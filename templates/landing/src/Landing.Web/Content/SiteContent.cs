using Blux.UI;

namespace Landing.Web.Content;

// All the copy of the page, in one typed file. Edit the text here, and the shape of a section in Components/Sections: a section is a
// component that reads this file and nothing else, so the markup stays free of sentences. Icon names (Icon = "ph:...") are found by the build,
// which bundles them: add one here and it is available, no other step.

/// <summary>A customer quote of the testimonials section.</summary>
public sealed record Testimonial(string Quote, string Name, string Role);

/// <summary>A question of the FAQ.</summary>
public sealed record Question(string Ask, string Answer);

/// <summary>A step of the "how it works" section.</summary>
public sealed record Step(string Title, string Text, string Icon);

/// <summary>A plan of the pricing section. The prices are per month; <see cref="YearlyMonthlyPrice"/> is the monthly price when billed yearly.</summary>
public sealed record Plan(string Name, string Description, int MonthlyPrice, int YearlyMonthlyPrice, IReadOnlyList<string> Features, string ButtonLabel, string ButtonHref)
{
    public string? Badge { get; init; }
    public bool Highlight { get; init; }

    public int YearlyTotal => YearlyMonthlyPrice * 12;

    /// <summary>The months of a year that the yearly price does not charge (2 for 18 a month or 180 a year); 0 for a free plan.</summary>
    public int MonthsFree => MonthlyPrice == 0 ? 0 : (MonthlyPrice * 12 - YearlyTotal) / MonthlyPrice;

    public string Price(bool yearly) => $"${(yearly ? YearlyMonthlyPrice : MonthlyPrice)}";

    public string Terms(bool yearly) =>
        MonthlyPrice == 0 ? "No payment details needed."
        : yearly ? $"${YearlyTotal} billed once a year."
        : "Billed monthly, cancel at any time.";
}

public static class SiteContent
{
    public const string Name = "Paperlane";

    /// <summary>Where "Start free" leads. A placeholder: point it at the sign-up page of your product.</summary>
    public const string SignUpUrl = "https://example.com/signup";

    /// <summary>Where "Talk to us" leads. A placeholder address, example.com is reserved for documentation.</summary>
    public const string ContactUrl = "mailto:hello@example.com";

    public const string Tagline = "Invoicing for freelancers and small studios.";

    // Layout

    public const string HeaderLabel = "Main";
    public const string HeaderMenuTitle = "Menu";
    public const string HeaderButtonLabel = "Start free";
    public const string FooterLabel = "Footer";

    public static string Copyright => $"\u00a9 {DateTime.UtcNow.Year} {Name}. Built with Blux UI.";

    public static IReadOnlyList<BxNavigationItem> Navigation { get; } =
    [
        new("Features") { Href = "./#features", Active = false },
        new("How it works") { Href = "./#how-it-works", Active = false },
        new("Pricing") { Href = "./#pricing", Active = false },
        new("FAQ") { Href = "./#faq", Active = false },
    ];

    // Hero

    public const string HeroHeadline = "Invoicing for small studios";
    public const string HeroTitle = "Send an invoice in minutes and know when it is paid";
    public const string HeroDescription =
        "Paperlane turns a finished job into a clear invoice, sends it, and follows up until it is paid. Made for freelancers and studios of up to twenty people.";

    public static IReadOnlyList<PageLinkItem> HeroLinks { get; } =
    [
        new("Start free", SignUpUrl) { TrailingIcon = "ph:arrow-right" },
        new("See pricing", "./#pricing") { Color = BxColor.Neutral, Variant = ButtonVariant.Outline },
    ];

    // Logos. The marks are Assets/marks/*.svg, bundled as the icons mark:<file name>; the names are made up.

    public const string LogosTitle = "Used by small teams at";

    public static IReadOnlyList<PageLogoItem> Logos { get; } =
    [
        new("Alder Studio") { Icon = "mark:alder" },
        new("Brightwater Design") { Icon = "mark:brightwater" },
        new("Copperline Print") { Icon = "mark:copperline" },
        new("Dunmore Architects") { Icon = "mark:dunmore" },
        new("Everpine Outdoors") { Icon = "mark:everpine" },
        new("Fieldmark Surveys") { Icon = "mark:fieldmark" },
    ];

    // Features

    public const string FeaturesHeadline = "Features";
    public const string FeaturesTitle = "Everything between the job and the payment";
    public const string FeaturesDescription = "The parts of getting paid that cost an hour each week, done once and left alone.";

    public static IReadOnlyList<PageFeatureItem> Features { get; } =
    [
        new("Invoices from templates", "Start from a saved template, fill in the lines, and the totals and taxes are worked out for you.") { Icon = "ph:receipt" },
        new("Automatic reminders", "Choose when a late invoice gets a nudge. Paperlane sends it and stops as soon as the payment arrives.") { Icon = "ph:bell" },
        new("Online payment", "Clients pay by card or bank transfer from a link in the email, and the invoice is marked as paid the same minute.") { Icon = "ph:credit-card" },
        new("Currencies and taxes", "Bill in 30 currencies, with a tax rate per client that you can change on any line.") { Icon = "ph:globe" },
        new("Time and expenses", "Turn tracked hours and receipts into invoice lines without typing them twice.") { Icon = "ph:clock" },
        new("Export for your accountant", "Download a CSV or a PDF of any period, with the tax breakdown, in one click.") { Icon = "ph:download-simple" },
    ];

    // How it works

    public const string HowHeadline = "How it works";
    public const string HowTitle = "From finished job to paid invoice in three steps";
    public const string HowDescription = "Most invoices take less than two minutes to write and send.";

    public static IReadOnlyList<Step> Steps { get; } =
    [
        new("Describe the job", "Pick a client, add lines or import tracked time. Taxes and totals update as you type.", "ph:pencil-simple-line"),
        new("Send it", "Email the invoice with a payment link, or share the link yourself. You see when it is opened.", "ph:paper-plane-tilt"),
        new("Get paid", "The client pays online. Paperlane marks the invoice as paid and stops the reminders.", "ph:check-circle"),
    ];

    // Testimonials. The people and the companies are made up.

    public const string TestimonialsHeadline = "Customers";
    public const string TestimonialsTitle = "What small teams say after a few months";

    public static IReadOnlyList<Testimonial> Testimonials { get; } =
    [
        new("I used to spend Friday afternoons chasing late payments. Now the reminders go out on their own and I only look at the list of what is still open.", "Maya Okafor", "Freelance illustrator"),
        new("We bill in three currencies. Paperlane keeps the tax rate right for each client, and our bookkeeper stopped sending us corrections.", "Tomas Reyes", "Co-founder, Brightwater Design"),
        new("Setting it up took one evening. The export for our accountant is the part we use every month.", "Priya Raman", "Office manager, Dunmore Architects"),
    ];

    // Pricing

    public const string PricingHeadline = "Pricing";
    public const string PricingTitle = "Pay for what you send";
    public const string PricingDescription = "Start on the Free plan and move up when you need more invoices, more people or more control. Prices are in US dollars.";

    public const string BillingLabel = "Billing period";
    public const string MonthlyLabel = "Monthly";
    public const string PerMonth = "/month";

    /// <summary>
    /// The label of the yearly option, worked out from the prices: the months that every paid plan gets free when billed yearly
    /// ("Yearly, 2 months free"), so changing a price changes the label. Just "Yearly" when there is no saving.
    /// </summary>
    public static string YearlyLabel
    {
        get
        {
            var saved = Plans.Where(p => p.MonthlyPrice > 0).Select(p => p.MonthsFree).DefaultIfEmpty(0).Min();
            return saved <= 0 ? "Yearly" : $"Yearly, {saved} month{(saved == 1 ? "" : "s")} free";
        }
    }

    public static IReadOnlyList<Plan> Plans { get; } =
    [
        new("Free", "For a freelancer sending a few invoices a month.", 0, 0,
            ["5 invoices a month", "1 user", "Card and bank payments", "PDF download"],
            "Start free", SignUpUrl),
        new("Studio", "For a studio that bills every week.", 18, 15,
            ["Unlimited invoices", "Up to 5 users", "Automatic reminders", "30 currencies", "Time and expense import"],
            "Choose Studio", SignUpUrl) { Badge = "Most chosen", Highlight = true },
        new("Business", "For a team with several clients and an accountant.", 48, 40,
            ["Everything in Studio", "Up to 20 users", "Roles and approvals", "Accountant access", "Priority support"],
            "Choose Business", SignUpUrl),
    ];

    // FAQ

    public const string FaqHeadline = "FAQ";
    public const string FaqTitle = "Questions before you start";

    public static IReadOnlyList<Question> Questions { get; } =
    [
        new("Can I try Paperlane before paying?", "Yes. The Free plan has no time limit and includes five invoices a month, so you can use it for real work before you decide."),
        new("Which payment methods can my clients use?", "Cards and bank transfers, in the countries Paperlane supports. You choose the methods each invoice offers."),
        new("How do reminders work?", "You set the schedule once, for example three days before the due date and seven days after it. Reminders stop when the invoice is paid or cancelled."),
        new("Can I invoice in another currency?", "Yes, in 30 currencies. The tax rate is set per client and can be changed on a single line."),
        new("What happens to my data if I leave?", "You can export clients, invoices and payments as CSV or PDF at any time. Deleting your workspace removes the data within 30 days."),
        new("Is there a discount for non-profits?", "Yes, 25 percent on the paid plans. Write to us from the address of the organization and we will set it up."),
    ];

    // Call to action

    public const string CtaTitle = "Send your first invoice today";
    public const string CtaDescription = "Create a workspace in a minute. The Free plan needs no payment details.";

    public static IReadOnlyList<PageLinkItem> CtaLinks { get; } =
    [
        new("Start free", SignUpUrl),
        new("Talk to us", ContactUrl) { Color = BxColor.Neutral, Variant = ButtonVariant.Outline },
    ];

    // Footer

    public static IReadOnlyList<BxFooterColumn> FooterColumns { get; } =
    [
        new("Product")
        {
            Links =
            [
                new("Features") { Href = "./#features" },
                new("How it works") { Href = "./#how-it-works" },
                new("Customers") { Href = "./#customers" },
            ],
        },
        new("Plans")
        {
            Links =
            [
                new("Pricing") { Href = "./#pricing" },
                new("FAQ") { Href = "./#faq" },
            ],
        },
        new("Contact")
        {
            Links =
            [
                new("hello@example.com") { Href = ContactUrl },
            ],
        },
    ];
}
