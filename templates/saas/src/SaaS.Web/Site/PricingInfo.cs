using Lux.UI;

namespace SaaS.Web.Site;

/// <summary>A question of the FAQ.</summary>
public sealed record Question(string Ask, string Answer);

/// <summary>A plan of the pricing page. The prices are per month; <see cref="YearlyMonthlyPrice"/> is the monthly price when billed yearly.</summary>
public sealed record Plan(string Id, string Name, string Description, int MonthlyPrice, int YearlyMonthlyPrice, IReadOnlyList<string> Features, string ButtonLabel, string ButtonHref)
{
    public string? Badge { get; init; }
    public bool Highlight { get; init; }

    public int YearlyTotal => YearlyMonthlyPrice * 12;

    /// <summary>The months of a year that the yearly price does not charge (2 for 29 a month or 288 a year); 0 for a free plan.</summary>
    public int MonthsFree => MonthlyPrice == 0 ? 0 : (MonthlyPrice * 12 - YearlyTotal) / MonthlyPrice;

    public string Price(bool yearly) => $"${(yearly ? YearlyMonthlyPrice : MonthlyPrice)}";

    public string Terms(bool yearly) =>
        MonthlyPrice == 0 ? "No payment details needed."
        : yearly ? $"${YearlyTotal} billed once a year."
        : "Billed monthly, cancel at any time.";
}

// The plans, the comparison table and the questions of the pricing page, as typed C#. Change a price here and the label of the yearly option, the
// totals and the "months free" follow.
public static class PricingInfo
{
    public const string Headline = "Pricing";
    public const string Title = "Pay for the monitors you use";
    public const string Description = "Start on the free plan and move up when you need more monitors, more people or a longer history. Prices are in US dollars.";

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
        new("hobby", "Hobby", "For a side project or a single server.", 0, 0,
            ["5 monitors", "1 user", "Email alerts", "7 days of history"],
            "Start free", SiteInfo.SignUpHref),
        new("team", "Team", "For a team whose jobs matter to customers.", 29, 24,
            ["50 monitors", "Up to 10 users", "Slack, webhook and PagerDuty alerts", "Status page", "90 days of history"],
            "Choose Team", SiteInfo.SignUpHref) { Badge = "Most chosen", Highlight = true },
        new("scale", "Scale", "For several teams and an on-call rotation.", 99, 82,
            ["Unlimited monitors", "Up to 50 users", "Roles and an audit log", "Escalation policies", "1 year of history", "Priority support"],
            "Choose Scale", SiteInfo.SignUpHref),
    ];

    public const string CompareHeadline = "Compare plans";
    public const string CompareTitle = "Every difference, in one table";
    public const string CompareCaption = "What each plan includes";

    public static IReadOnlyList<BxPricingTier> Tiers { get; } =
    [
        .. Plans.Select(plan => new BxPricingTier(plan.Id, plan.Name)
        {
            Highlight = plan.Highlight,
            ButtonLabel = plan.ButtonLabel,
            ButtonHref = plan.ButtonHref,
        }),
    ];

    public static IReadOnlyList<BxPricingSection> Sections { get; } =
    [
        new("Monitoring",
        [
            new("Monitors", "5", "50", "Unlimited"),
            new("Shortest schedule", "5 minutes", "1 minute", "1 minute"),
            new("Start and fail signals", true, true, true),
            new("History", "7 days", "90 days", "1 year"),
        ]),
        new("Alerts",
        [
            new("Email", true, true, true),
            new("Slack and webhooks", false, true, true),
            new("PagerDuty", false, true, true),
            new("Escalation policies", false, false, true),
        ]),
        new("Teams",
        [
            new("Users", "1", "10", "50"),
            new("Public status page", false, true, true),
            new("Roles and audit log", false, false, true),
        ]),
        new("Support",
        [
            new("Documentation and community", true, true, true),
            new("Email support", false, true, true),
            new("Priority support", false, false, true),
        ]),
    ];

    public const string FaqHeadline = "FAQ";
    public const string FaqTitle = "Questions before you start";

    public static IReadOnlyList<Question> Questions { get; } =
    [
        new("Can I try Tideline before paying?", "Yes. The Hobby plan has no time limit and includes five monitors, so you can run it on real jobs before you decide."),
        new("What counts as a monitor?", "One monitor is one job or one schedule you expect a ping from. A job that runs on ten servers is one monitor if you want one alert, ten if you want to tell the servers apart."),
        new("What happens when I go over my plan?", "Nothing is switched off. The monitors above the limit stay paused until you move up or remove some, and Tideline emails you before it pauses anything."),
        new("Can I change plan whenever I want?", "Yes. Moving up takes effect at once and you pay the difference for the rest of the period. Moving down takes effect at the end of it."),
        new("How is yearly billing different?", "You pay for twelve months at once and the price per month is lower: two months of every paid plan are free."),
        new("Do you offer a discount for open source projects?", "Yes, the Team plan is free for open source projects with a public repository. Write to us from the address of the project and we will set it up."),
    ];

    public const string CtaTitle = "Start with five monitors, free";
    public const string CtaDescription = "Create an account in a minute. Upgrade when your jobs outgrow it.";

    public static IReadOnlyList<PageLinkItem> CtaLinks { get; } =
    [
        new("Start free", SiteInfo.SignUpHref),
        new("Talk to us", SiteInfo.ContactUrl) { Color = BxColor.Neutral, Variant = ButtonVariant.Outline },
    ];
}
