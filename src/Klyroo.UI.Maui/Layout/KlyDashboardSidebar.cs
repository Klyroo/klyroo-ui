namespace Klyroo.UI.Maui;

/// <summary>
/// A <see cref="KlySidebar"/> for a dashboard: the same column, icon rail and slide-in panel. What differs is that the controls live elsewhere:
/// <see cref="KlyDashboardSidebarCollapse"/> collapses it and <see cref="KlyDashboardSidebarToggle"/> opens its panel, both usually in a <see cref="KlyDashboardNavbar"/>,
/// so it has neither a menu button over the content nor a collapse button in its header unless you turn them on with <see cref="KlySidebar.MobileTrigger"/> and <see cref="KlySidebar.Collapsible"/>.
/// Place it in a <see cref="KlyDashboardGroup"/>, which tells it how wide the dashboard is.
/// </summary>
public class KlyDashboardSidebar : KlySidebar
{
    /// <summary>Creates a dashboard sidebar.</summary>
    public KlyDashboardSidebar()
    {
        MobileTrigger = false;
        Collapsible = false;
    }
}
