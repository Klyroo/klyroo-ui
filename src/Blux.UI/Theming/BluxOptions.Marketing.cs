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

public sealed partial class BluxStrings
{
    /// <summary>Accessible name of the button of an auth form that shows or hides a password.</summary>
    public string AuthFormShowPassword { get; set; } = "Show password";

    /// <summary>Label of the submit button of an auth form that has none.</summary>
    public string AuthFormSubmit { get; set; } = "Continue";

    /// <summary>Text between the providers and the fields of an auth form, unless <c>Separator</c> is set.</summary>
    public string AuthFormSeparator { get; set; } = "or";

    /// <summary>Error of an empty required field of an auth form; <c>{0}</c> is the field label.</summary>
    public string AuthFormRequired { get; set; } = "{0} is required.";
    public string AuthFormInvalidEmail { get; set; } = "Enter a valid email address.";

    /// <summary>Error of an incomplete one-time code; <c>{0}</c> is the number of characters.</summary>
    public string AuthFormIncompleteCode { get; set; } = "Enter all {0} characters.";

    /// <summary>Hidden text of a pricing table cell that has a check mark.</summary>
    public string PricingIncluded { get; set; } = "Included";

    /// <summary>Hidden text of a pricing table cell that has a minus sign.</summary>
    public string PricingNotIncluded { get; set; } = "Not included";

    /// <summary>Hidden name of the column of a pricing table that lists the features.</summary>
    public string PricingFeatures { get; set; } = "Features";

    /// <summary>Hidden text before the original price of a pricing plan that has a discount.</summary>
    public string PricingOriginalPrice { get; set; } = "Original price";

    /// <summary>Hidden text before the discounted price of a pricing plan.</summary>
    public string PricingDiscountedPrice { get; set; } = "Now";
}
