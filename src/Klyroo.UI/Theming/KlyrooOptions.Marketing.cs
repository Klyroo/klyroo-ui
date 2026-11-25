using Klyroo.UI.Theming;

namespace Klyroo.UI;

public sealed partial class KlyrooOptions
{
    public ComponentTheme AuthForm { get; } = Themes.AuthForm();
    public ComponentTheme PricingPlan { get; } = Themes.PricingPlan();
    public ComponentTheme PricingPlans { get; } = Themes.PricingPlans();
    public ComponentTheme PricingTable { get; } = Themes.PricingTable();
    public ComponentTheme BlogPost { get; } = Themes.BlogPost();
    public ComponentTheme BlogPosts { get; } = Themes.BlogPosts();
    public ComponentTheme ChangelogVersion { get; } = Themes.ChangelogVersion();
    public ComponentTheme ChangelogVersions { get; } = Themes.ChangelogVersions();
}
