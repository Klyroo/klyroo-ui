using System.ComponentModel.DataAnnotations;

namespace Portfolio.Web.Site;

/// <summary>
/// What the contact form posts. The rules are checked by the server on every post, and by <c>BxForm</c> the same way when the page is interactive.
/// This type is also where a real form would start: add a field here, in the form of <c>Components/Pages/Contact.razor</c> and in the check of <see cref="Topic"/>.
/// </summary>
public sealed class ContactMessage
{
    public const int MaxMessageLength = 2000;

    [Required(ErrorMessage = "Enter your name.")]
    [StringLength(80, ErrorMessage = "Use at most 80 characters.")]
    public string? Name { get; set; }

    [Required(ErrorMessage = "Enter your email address.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(200, ErrorMessage = "Use at most 200 characters.")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Pick what this is about.")]
    [AllowedTopic]
    public string? Topic { get; set; }

    [Required(ErrorMessage = "Write a message.")]
    [StringLength(MaxMessageLength, MinimumLength = 10, ErrorMessage = "Write between 10 and 2000 characters.")]
    public string? Message { get; set; }

    /// <summary>A value that is not one of the choices of the form, which a browser would never send, is refused like a missing one.</summary>
    private sealed class AllowedTopicAttribute : ValidationAttribute
    {
        public AllowedTopicAttribute() : base("Pick what this is about.")
        {
        }

        public override bool IsValid(object? value) =>
            value is null or "" || SiteInfo.Topics.Any(topic => topic.Value == (string)value);
    }
}
