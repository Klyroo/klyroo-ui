using Microsoft.Maui.Layouts;

namespace Klyroo.UI.Maui;

/// <summary>The trail of pages that leads to the current one. The items wrap onto several lines when they do not fit.</summary>
public class KlyBreadcrumb : FlexLayout
{
    /// <summary>Identifies <see cref="Items"/>.</summary>
    public static readonly BindableProperty ItemsProperty = BindableProperty.Create(nameof(Items), typeof(IReadOnlyList<KlyBreadcrumbItem>), typeof(KlyBreadcrumb), Array.Empty<KlyBreadcrumbItem>(), propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="SeparatorIcon"/>.</summary>
    public static readonly BindableProperty SeparatorIconProperty = BindableProperty.Create(nameof(SeparatorIcon), typeof(string), typeof(KlyBreadcrumb), KlyIconNames.ChevronRight, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="SeparatorText"/>.</summary>
    public static readonly BindableProperty SeparatorTextProperty = BindableProperty.Create(nameof(SeparatorText), typeof(string), typeof(KlyBreadcrumb), null, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="AccessibleName"/>.</summary>
    public static readonly BindableProperty AccessibleNameProperty = BindableProperty.Create(nameof(AccessibleName), typeof(string), typeof(KlyBreadcrumb), "Breadcrumb", propertyChanged: (b, _, v) => SemanticProperties.SetDescription((KlyBreadcrumb)b, (string?)v));

    /// <summary>Creates a breadcrumb.</summary>
    public KlyBreadcrumb()
    {
        Direction = FlexDirection.Row;
        Wrap = FlexWrap.Wrap;
        AlignItems = FlexAlignItems.Center;
        SemanticProperties.SetDescription(this, AccessibleName);
        Rebuild();
    }

    /// <summary>Raised when the user taps an item that has an <see cref="KlyBreadcrumbItem.Href"/>.</summary>
    public event EventHandler<KlyBreadcrumbItem>? ItemClicked;

    /// <summary>The pages of the trail, from the root to the current page, as <see cref="KlyBreadcrumbItem"/>. The last one is the current page.</summary>
    public IReadOnlyList<KlyBreadcrumbItem> Items { get => (IReadOnlyList<KlyBreadcrumbItem>)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }

    /// <summary>Lucide name of the icon between the items. Replaces the chevron.</summary>
    public string? SeparatorIcon { get => (string?)GetValue(SeparatorIconProperty); set => SetValue(SeparatorIconProperty, value); }

    /// <summary>A text between the items, such as <c>/</c>. When set it replaces <see cref="SeparatorIcon"/>.</summary>
    public string? SeparatorText { get => (string?)GetValue(SeparatorTextProperty); set => SetValue(SeparatorTextProperty, value); }

    /// <summary>What a screen reader says for the trail. Defaults to "Breadcrumb".</summary>
    public string? AccessibleName { get => (string?)GetValue(AccessibleNameProperty); set => SetValue(AccessibleNameProperty, value); }

    private static void Rebuild(BindableObject bindable, object oldValue, object newValue) => ((KlyBreadcrumb)bindable).Rebuild();

    private void Rebuild()
    {
        Children.Clear();
        var items = Items;
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var last = i == items.Count - 1;
            var color = last ? KlyTheme.Text : KlyTheme.TextMuted;

            var content = new HorizontalStackLayout { Spacing = 6, VerticalOptions = LayoutOptions.Center };
            if (item.Icon is { } icon) content.Children.Add(NavigationLayout.Icon(icon, KlySize.Sm, color));
            var label = NavigationLayout.TextLabel(14, color);
            label.Text = item.Label;
            content.Children.Add(label);

            View entry;
            if (item.Href is not null && !last)
            {
                var button = new NavigationButton();
                button.Face.Content = content;
                button.Face.SetRadius(KlyTheme.RadiusOf(0.5));
                button.Clicked += (_, _) => ItemClicked?.Invoke(this, item);
                SemanticProperties.SetDescription(button, item.Label);
                entry = button;
            }
            else
            {
                entry = content;
            }

            var cell = new HorizontalStackLayout { Spacing = 6, VerticalOptions = LayoutOptions.Center, Margin = new Thickness(0, 0, 6, 0) };
            cell.Children.Add(entry);
            if (!last) cell.Children.Add(Separator());
            Children.Add(cell);
        }
    }

    private View Separator()
    {
        if (!string.IsNullOrEmpty(SeparatorText))
        {
            var text = NavigationLayout.TextLabel(14, KlyTheme.TextDimmed);
            text.Text = SeparatorText;
            AutomationProperties.SetIsInAccessibleTree(text, false);
            return text;
        }
        return NavigationLayout.Icon(SeparatorIcon ?? KlyIconNames.ChevronRight, KlySize.Sm, KlyTheme.TextDimmed);
    }
}
