namespace Klyroo.UI.Maui;

/// <summary>What a <see cref="KlyDateTile"/> says beside its calendar tile.</summary>
public enum KlyDateTileMode
{
    /// <summary>The date in the long format of the culture, or the title.</summary>
    Date,
    /// <summary>The days until the date, or since it (today, tomorrow, in 5 days, 3 days overdue), with the title as a caption.</summary>
    Countdown,
    /// <summary>The start and end dates with the number of days, with the title as a caption. Without an end date the tile shows the date.</summary>
    Range,
}
