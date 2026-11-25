namespace Klyroo.UI;

public sealed partial record KlyMessages
{
    /// <summary>Accessible name of the button of an auth form that shows or hides a password.</summary>
    public string AuthFormShowPassword { get; init; } = "Show password";

    /// <summary>Label of the submit button of an auth form that has none.</summary>
    public string AuthFormSubmit { get; init; } = "Continue";

    /// <summary>Text between the providers and the fields of an auth form, unless <c>Separator</c> is set.</summary>
    public string AuthFormSeparator { get; init; } = "or";

    /// <summary>Error of an empty required field of an auth form; <c>{0}</c> is the field label.</summary>
    public string AuthFormRequired { get; init; } = "{0} is required.";
    public string AuthFormInvalidEmail { get; init; } = "Enter a valid email address.";

    /// <summary>Error of an incomplete one-time code; <c>{0}</c> is the number of characters.</summary>
    public string AuthFormIncompleteCode { get; init; } = "Enter all {0} characters.";

    /// <summary>Hidden text of a pricing table cell that has a check mark.</summary>
    public string PricingIncluded { get; init; } = "Included";

    /// <summary>Hidden text of a pricing table cell that has a minus sign.</summary>
    public string PricingNotIncluded { get; init; } = "Not included";

    /// <summary>Hidden name of the column of a pricing table that lists the features.</summary>
    public string PricingFeatures { get; init; } = "Features";

    /// <summary>Hidden text before the original price of a pricing plan that has a discount.</summary>
    public string PricingOriginalPrice { get; init; } = "Original price";

    /// <summary>Hidden text before the discounted price of a pricing plan.</summary>
    public string PricingDiscountedPrice { get; init; } = "Now";
}
