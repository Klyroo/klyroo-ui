namespace Klyroo.UI;

/// <summary>The <c>type</c> attribute of a <see cref="KlyInput"/>: the kinds of single-line text control the browser offers.</summary>
public enum InputType
{
    /// <summary>Plain text (the default).</summary>
    Text,
    /// <summary>An email address.</summary>
    Email,
    /// <summary>A secret: the browser hides the characters.</summary>
    Password,
    /// <summary>A search term; the browser may add a clear button.</summary>
    Search,
    /// <summary>A telephone number.</summary>
    Tel,
    /// <summary>A web address.</summary>
    Url,
    /// <summary>A number; the value stays text in <see cref="KlyInput"/>, use <see cref="KlyInputNumber{TNumber}"/> for a typed number.</summary>
    Number,
    /// <summary>A date picked by the browser (<c>yyyy-MM-dd</c>); <see cref="KlyInputDate{TValue}"/> is the typed field.</summary>
    Date,
    /// <summary>A time of day picked by the browser (<c>HH:mm</c>).</summary>
    Time,
    /// <summary>A date and a time without a time zone (<c>datetime-local</c>).</summary>
    DateTimeLocal,
    /// <summary>A month of a year (<c>yyyy-MM</c>).</summary>
    Month,
    /// <summary>A week of a year (<c>yyyy-Www</c>).</summary>
    Week,
}
