using Blux.UI.Theming;

namespace Blux.UI;

public sealed partial class BluxOptions
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
