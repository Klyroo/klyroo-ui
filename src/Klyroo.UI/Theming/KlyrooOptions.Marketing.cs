using Klyroo.UI.Theming;

namespace Klyroo.UI;

public sealed partial class KlyrooOptions
{
    /// <summary>
    /// Theme of <see cref="KlyAuthForm"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme AuthForm { get; } = Themes.AuthForm();
    /// <summary>
    /// Theme of <see cref="KlyPricingPlan"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme PricingPlan { get; } = Themes.PricingPlan();
    /// <summary>
    /// Theme of <see cref="KlyPricingPlans"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme PricingPlans { get; } = Themes.PricingPlans();
    /// <summary>
    /// Theme of <see cref="KlyPricingTable"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme PricingTable { get; } = Themes.PricingTable();
    /// <summary>
    /// Theme of <see cref="KlyBlogPost"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme BlogPost { get; } = Themes.BlogPost();
    /// <summary>
    /// Theme of <see cref="KlyBlogPosts"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme BlogPosts { get; } = Themes.BlogPosts();
    /// <summary>
    /// Theme of <see cref="KlyChangelogVersion"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme ChangelogVersion { get; } = Themes.ChangelogVersion();
    /// <summary>
    /// Theme of <see cref="KlyChangelogVersions"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme ChangelogVersions { get; } = Themes.ChangelogVersions();
}
