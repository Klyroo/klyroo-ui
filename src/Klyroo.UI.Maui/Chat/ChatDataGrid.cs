namespace Klyroo.UI.Maui;

/// <summary>Places a view in a grid cell. <c>Grid.SetRow</c> is ambiguous for a <see cref="View"/>, which is both a <see cref="BindableObject"/> and an <see cref="IView"/>.</summary>
internal static class ChatDataGrid
{
    public static void Row(View view, int row) => Grid.SetRow((BindableObject)view, row);

    public static void Column(View view, int column) => Grid.SetColumn((BindableObject)view, column);

    public static void ColumnSpan(View view, int span) => Grid.SetColumnSpan((BindableObject)view, span);
}
