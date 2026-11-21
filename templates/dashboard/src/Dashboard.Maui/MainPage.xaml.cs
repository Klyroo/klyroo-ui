namespace Dashboard.Maui;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        Current = this;
    }

    /// <summary>The page that hosts the web view: the system bars take their color from it (see Services/SystemBars.cs).</summary>
    public static MainPage? Current { get; private set; }
}
