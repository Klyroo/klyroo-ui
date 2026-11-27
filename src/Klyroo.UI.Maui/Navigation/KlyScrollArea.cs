namespace Klyroo.UI.Maui;

/// <summary>
/// A scroll container: the native <see cref="ScrollView"/> with the scrolling direction of the web component. It scrolls vertically by default;
/// set <see cref="ScrollView.Orientation"/> to <c>Horizontal</c>, or <c>Both</c> for the web "null" orientation. Give it an <see cref="AccessibleName"/>.
/// </summary>
public class KlyScrollArea : ScrollView
{
    /// <summary>Identifies <see cref="AccessibleName"/>.</summary>
    public static readonly BindableProperty AccessibleNameProperty = BindableProperty.Create(nameof(AccessibleName), typeof(string), typeof(KlyScrollArea), null, propertyChanged: (b, _, v) => SemanticProperties.SetDescription((KlyScrollArea)b, (string?)v));

    /// <summary>Creates a scroll area.</summary>
    public KlyScrollArea()
    {
        Orientation = ScrollOrientation.Vertical;
    }

    /// <summary>What a screen reader says for the scrollable region.</summary>
    public string? AccessibleName { get => (string?)GetValue(AccessibleNameProperty); set => SetValue(AccessibleNameProperty, value); }
}
