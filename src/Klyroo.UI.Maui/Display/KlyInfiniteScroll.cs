using System.Windows.Input;

namespace Klyroo.UI.Maui;

/// <summary>
/// Wraps a list that grows and raises <see cref="LoadMore"/> when the user scrolls near its end. Put a <see cref="CollectionView"/> (it reports through its own
/// <see cref="ItemsView.RemainingItemsThreshold"/>), a <see cref="ScrollView"/> or any other view (it is scrolled in a <see cref="ScrollView"/> the control adds) in <see cref="Content"/>.
/// Under the list, a button loads the next items by hand, and the end of the list is announced. While <see cref="Loading"/> nothing is requested again:
/// set it while you fetch, and clear <see cref="HasMore"/> when there is nothing left.
/// </summary>
[ContentProperty(nameof(Content))]
public class KlyInfiniteScroll : Grid
{
    /// <summary>Identifies <see cref="Content"/>.</summary>
    public static readonly BindableProperty ContentProperty = BindableProperty.Create(nameof(Content), typeof(View), typeof(KlyInfiniteScroll), null, propertyChanged: (b, o, n) => ((KlyInfiniteScroll)b).ShowContent((View?)o, (View?)n));
    /// <summary>Identifies <see cref="HasMore"/>.</summary>
    public static readonly BindableProperty HasMoreProperty = BindableProperty.Create(nameof(HasMore), typeof(bool), typeof(KlyInfiniteScroll), true, propertyChanged: (b, _, _) => ((KlyInfiniteScroll)b).Refresh());
    /// <summary>Identifies <see cref="Loading"/>.</summary>
    public static readonly BindableProperty LoadingProperty = BindableProperty.Create(nameof(Loading), typeof(bool), typeof(KlyInfiniteScroll), false, propertyChanged: (b, o, n) => ((KlyInfiniteScroll)b).OnLoadingChanged((bool)o, (bool)n));
    /// <summary>Identifies <see cref="Threshold"/>.</summary>
    public static readonly BindableProperty ThresholdProperty = BindableProperty.Create(nameof(Threshold), typeof(double), typeof(KlyInfiniteScroll), 200d);
    /// <summary>Identifies <see cref="RemainingItemsThreshold"/>.</summary>
    public static readonly BindableProperty RemainingItemsThresholdProperty = BindableProperty.Create(nameof(RemainingItemsThreshold), typeof(int), typeof(KlyInfiniteScroll), 3, propertyChanged: (b, _, _) => ((KlyInfiniteScroll)b).ConfigureCollection());
    /// <summary>Identifies <see cref="Label"/>.</summary>
    public static readonly BindableProperty LabelProperty = BindableProperty.Create(nameof(Label), typeof(string), typeof(KlyInfiniteScroll), "Load more", propertyChanged: (b, _, v) => ((KlyInfiniteScroll)b)._more.Text = (string?)v);
    /// <summary>Identifies <see cref="LoadingLabel"/>.</summary>
    public static readonly BindableProperty LoadingLabelProperty = BindableProperty.Create(nameof(LoadingLabel), typeof(string), typeof(KlyInfiniteScroll), "Loading more...");
    /// <summary>Identifies <see cref="EndLabel"/>.</summary>
    public static readonly BindableProperty EndLabelProperty = BindableProperty.Create(nameof(EndLabel), typeof(string), typeof(KlyInfiniteScroll), "No more items", propertyChanged: (b, _, v) => ((KlyInfiniteScroll)b)._end.Text = (string?)v);
    /// <summary>Identifies <see cref="ShowEnd"/>.</summary>
    public static readonly BindableProperty ShowEndProperty = BindableProperty.Create(nameof(ShowEnd), typeof(bool), typeof(KlyInfiniteScroll), false, propertyChanged: (b, _, _) => ((KlyInfiniteScroll)b).Refresh());
    /// <summary>Identifies <see cref="LoadMoreCommand"/>.</summary>
    public static readonly BindableProperty LoadMoreCommandProperty = BindableProperty.Create(nameof(LoadMoreCommand), typeof(ICommand), typeof(KlyInfiniteScroll), null);

    private readonly Button _more;
    private readonly Label _end = DisplayParts.NewLabel(14, KlyTheme.TextMuted);
    private readonly ActivityIndicator _spinner = new() { HorizontalOptions = LayoutOptions.Center, WidthRequest = 20, HeightRequest = 20 };
    private readonly VerticalStackLayout _footer = new() { Spacing = 8, Padding = new Thickness(0, 8), HorizontalOptions = LayoutOptions.Center };
    private ScrollView? _wrapper;
    private ScrollView? _scroll;
    private CollectionView? _collection;
    private bool _armed = true;

    /// <summary>Creates an infinite scroll: set <see cref="Content"/>.</summary>
    public KlyInfiniteScroll()
    {
        RowDefinitions.Add(new RowDefinition(GridLength.Star));
        RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        _end.Text = EndLabel;
        _end.HorizontalTextAlignment = TextAlignment.Center;
        _spinner.SetThemeColor(ActivityIndicator.ColorProperty, KlyTheme.Resolve(KlyColor.Primary).Text);
        _more = DisplayParts.TextButton(Label, KlyTheme.Resolve(KlyColor.Neutral).Text, 14, RequestMore);
        _more.HorizontalOptions = LayoutOptions.Center;
        _more.Padding = new Thickness(16, 0);
        _more.BorderWidth = 1;
        _more.CornerRadius = (int)KlyTheme.RadiusOf(1);
        _more.SetThemeColor(Button.BorderColorProperty, KlyTheme.BorderAccented);
        _footer.Add(_spinner);
        _footer.Add(_more);
        _footer.Add(_end);
        this.Add(_footer, 0, 1);
        Refresh();
    }

    /// <summary>Raised when the user scrolls near the end of the list or presses the button, never twice for one visit of the end until the load is over (<see cref="Loading"/> goes back to false) or the user has scrolled away.</summary>
    public event EventHandler? LoadMore;

    /// <summary>The list: a <see cref="CollectionView"/>, a <see cref="ScrollView"/>, or any view, which is then scrolled.</summary>
    public View? Content { get => (View?)GetValue(ContentProperty); set => SetValue(ContentProperty, value); }

    /// <summary>Whether there is anything left to load. When <see langword="false"/> the button goes away and the end is announced.</summary>
    public bool HasMore { get => (bool)GetValue(HasMoreProperty); set => SetValue(HasMoreProperty, value); }

    /// <summary>A load is running. While it is true nothing is requested; when it goes back to false the end of the list is checked again, so an end still in view asks for the next items.</summary>
    public bool Loading { get => (bool)GetValue(LoadingProperty); set => SetValue(LoadingProperty, value); }

    /// <summary>How far before the end of a <see cref="ScrollView"/> the load starts, in device-independent units (200 by default, as the web margin of 200px).</summary>
    public double Threshold { get => (double)GetValue(ThresholdProperty); set => SetValue(ThresholdProperty, value); }

    /// <summary>How many items before the end of a <see cref="CollectionView"/> the load starts (3 by default).</summary>
    public int RemainingItemsThreshold { get => (int)GetValue(RemainingItemsThresholdProperty); set => SetValue(RemainingItemsThresholdProperty, value); }

    /// <summary>Text of the button that loads the next items by hand ("Load more").</summary>
    public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    /// <summary>What a screen reader is told while the next items load ("Loading more...").</summary>
    public string LoadingLabel { get => (string)GetValue(LoadingLabelProperty); set => SetValue(LoadingLabelProperty, value); }

    /// <summary>What is shown and announced when there is no more to load ("No more items").</summary>
    public string EndLabel { get => (string)GetValue(EndLabelProperty); set => SetValue(EndLabelProperty, value); }

    /// <summary>Shows <see cref="EndLabel"/> when <see cref="HasMore"/> is false. It is announced either way.</summary>
    public bool ShowEnd { get => (bool)GetValue(ShowEndProperty); set => SetValue(ShowEndProperty, value); }

    /// <summary>A command executed together with <see cref="LoadMore"/>.</summary>
    public ICommand? LoadMoreCommand { get => (ICommand?)GetValue(LoadMoreCommandProperty); set => SetValue(LoadMoreCommandProperty, value); }

    /// <summary>The button that loads by hand, for tests.</summary>
    internal Button MoreButton => _more;

    internal Label EndText => _end;

    internal ActivityIndicator Spinner => _spinner;

    internal ScrollView? Scroller => _scroll;

    /// <summary>Whether the next visit of the end raises <see cref="LoadMore"/>.</summary>
    internal bool IsArmed => _armed;

    /// <summary>
    /// Decides from the distance left to the end of the content, in device-independent units, whether to ask for more:
    /// at or below <see cref="Threshold"/>, once, until the load is over or the user has scrolled away.
    /// </summary>
    internal void Evaluate(double remaining)
    {
        if (remaining > Threshold)
        {
            _armed = true;
            return;
        }
        Request();
    }

    internal void SendRemainingItemsThresholdReached() => Request();

    private void ShowContent(View? old, View? content)
    {
        Detach();
        if (_wrapper is not null)
        {
            _wrapper.Content = null;
            Children.Remove(_wrapper);
            _wrapper = null;
        }
        if (old is not null && Children.Contains(old)) Children.Remove(old);
        if (content is null) return;

        View host = content;
        if (content is not ScrollView && content is not CollectionView)
        {
            _wrapper = new ScrollView { Content = content };
            host = _wrapper;
        }
        this.Add(host, 0, 0);
        Attach(content is ScrollView or CollectionView ? content : _wrapper!);
    }

    private void Attach(View view)
    {
        if (view is ScrollView scroll)
        {
            _scroll = scroll;
            scroll.Scrolled += OnScrolled;
        }
        else if (view is CollectionView collection)
        {
            _collection = collection;
            collection.RemainingItemsThresholdReached += OnRemainingItemsThresholdReached;
            ConfigureCollection();
        }
    }

    private void Detach()
    {
        if (_scroll is not null)
        {
            _scroll.Scrolled -= OnScrolled;
            _scroll = null;
        }
        if (_collection is not null)
        {
            _collection.RemainingItemsThresholdReached -= OnRemainingItemsThresholdReached;
            _collection = null;
        }
    }

    private void ConfigureCollection()
    {
        if (_collection is not null) _collection.RemainingItemsThreshold = RemainingItemsThreshold;
    }

    private void OnScrolled(object? sender, ScrolledEventArgs e)
    {
        if (sender is not ScrollView scroll) return;
        Evaluate(scroll.ContentSize.Height - scroll.Height - e.ScrollY);
    }

    private void OnRemainingItemsThresholdReached(object? sender, EventArgs e) => Request();

    private void OnLoadingChanged(bool old, bool loading)
    {
        _spinner.IsRunning = loading;
        Refresh();
        Announce(loading ? LoadingLabel : null);

        // The load is over: ask again at once when the end is still in view, as the web sentinel does.
        if (old && !loading)
        {
            _armed = true;
            if (_scroll is not null) OnScrolled(_scroll, new ScrolledEventArgs(_scroll.ScrollX, _scroll.ScrollY));
        }
    }

    private void RequestMore()
    {
        // The button is the same action by hand: it asks even when the end has not been left since the last request.
        if (Loading || !HasMore) return;
        Raise();
    }

    private void Request()
    {
        if (!_armed || Loading || !HasMore) return;
        Raise();
    }

    private void Raise()
    {
        _armed = false;
        LoadMore?.Invoke(this, EventArgs.Empty);
        if (LoadMoreCommand is { } command && command.CanExecute(null)) command.Execute(null);
    }

    private void Refresh()
    {
        _more.IsVisible = HasMore;
        _more.IsEnabled = !Loading;
        _spinner.IsVisible = Loading;
        _end.IsVisible = !HasMore && ShowEnd;
        if (!HasMore) Announce(EndLabel);
    }

    // Both texts are announced politely, as the web live region does. A control with no native view has nobody to tell.
    private void Announce(string? text)
    {
        if (string.IsNullOrEmpty(text) || Handler is null) return;
        try
        {
            SemanticScreenReader.Default.Announce(text);
        }
        catch (Exception)
        {
            // The platform has no screen reader to talk to.
        }
    }
}
