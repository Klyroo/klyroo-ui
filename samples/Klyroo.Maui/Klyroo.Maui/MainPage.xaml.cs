using Microsoft.AspNetCore.Components.WebView.Maui;

namespace Klyroo.Maui;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        Current = this;
    }

    /// <summary>The page that hosts the web view, for the Android TV key forwarding (see MainActivity).</summary>
    public static MainPage? Current { get; private set; }

    public BlazorWebView WebView => blazorWebView;
}
