namespace Klyroo.UI.Maui;

/// <summary>The kind of control of a <see cref="KlyAuthField"/>.</summary>
public enum KlyAuthFieldType
{
    /// <summary>A text input (the default).</summary>
    Text,
    /// <summary>An email address; the form checks its shape.</summary>
    Email,
    /// <summary>A secret with a button that shows it.</summary>
    Password,
    /// <summary>A search term.</summary>
    Search,
    /// <summary>A telephone number.</summary>
    Tel,
    /// <summary>A web address.</summary>
    Url,
    /// <summary>A number.</summary>
    Number,
    /// <summary>A date, chosen with the native date picker. The value is <c>yyyy-MM-dd</c>.</summary>
    Date,
    /// <summary>A time of day, chosen with the native time picker. The value is <c>HH:mm</c>.</summary>
    Time,
    /// <summary>A date and a time without a time zone, typed as text.</summary>
    DateTimeLocal,
    /// <summary>A month of a year, typed as text.</summary>
    Month,
    /// <summary>A week of a year, typed as text.</summary>
    Week,
    /// <summary>A checkbox; its value is <c>"true"</c> or <c>"false"</c>.</summary>
    Checkbox,
    /// <summary>A select over <see cref="KlyAuthField.Items"/>.</summary>
    Select,
    /// <summary>A one-time code of <see cref="KlyAuthField.Length"/> digits.</summary>
    Otp,
}
