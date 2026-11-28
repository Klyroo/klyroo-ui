namespace Klyroo.UI.Maui;

/// <summary>A timeline of <see cref="KlyChangelogVersion"/> entries, newest first. The line and the dots come from each entry; this control tells the last one to end the line.</summary>
public class KlyChangelogVersions : VerticalStackLayout
{
    /// <summary>Creates a timeline.</summary>
    public KlyChangelogVersions()
    {
        ChildAdded += (_, _) => MarkLast();
        ChildRemoved += (_, _) => MarkLast();
    }

    private void MarkLast()
    {
        var versions = Children.OfType<KlyChangelogVersion>().ToList();
        for (var i = 0; i < versions.Count; i++) versions[i].IsLast = i == versions.Count - 1;
    }
}
