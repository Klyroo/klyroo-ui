using Lux.UI.Theming;

namespace Lux.UI;

public sealed partial class LuxOptions
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
