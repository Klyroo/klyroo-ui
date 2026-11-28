using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Controls.Shapes;

namespace Klyroo.UI.Maui.Tests;

public class OverlaysTests
{
    // A page: the content wrapped in a host, with the given controls in a stack, as an app places them.
    private static (KlyOverlayHost Host, VerticalStackLayout Page) Hosted(params View[] children)
    {
        var page = new VerticalStackLayout();
        foreach (var child in children) page.Add(child);
        return (new KlyOverlayHost { Content = page }, page);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static void Click(Button button) => ((IButtonController)button).SendClicked();

    // TapGestureRecognizer.SendTapped is internal: this is what a platform tap calls.
    private static void Tap(View view, TapGestureRecognizer tap)
    {
        var send = typeof(TapGestureRecognizer).GetMethod("SendTapped", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)!;
        send.Invoke(tap, send.GetParameters().Select(p => p.ParameterType == typeof(View) ? view : (object?)null).ToArray());
    }

    private static void Arrange(View view, double x, double y, double width, double height) => ((IView)view).Arrange(new Rect(x, y, width, height));

    // Without a platform view nothing measures a view, so a test gives it its size by arranging it to fill that size.
    private static void Layout(View view, double width, double height)
    {
        view.HorizontalOptions = LayoutOptions.Fill;
        view.VerticalOptions = LayoutOptions.Fill;
        ((IView)view).Arrange(new Rect(0, 0, width, height));
    }

    static OverlaysTests() => DispatcherProvider.SetCurrent(new InlineDispatcherProvider());

    // A binding needs a dispatcher to update its source; a test has no UI thread, so this one runs the work where it is called.
    private sealed class InlineDispatcherProvider : IDispatcherProvider
    {
        public IDispatcher GetForCurrentThread() => InlineDispatcher.Instance;
    }

    private sealed class InlineDispatcher : IDispatcher
    {
        public static InlineDispatcher Instance { get; } = new();

        public bool IsDispatchRequired => false;

        public bool Dispatch(Action action)
        {
            action();
            return true;
        }

        public bool DispatchDelayed(TimeSpan delay, Action action) => throw new NotSupportedException();

        public IDispatcherTimer CreateTimer() => throw new NotSupportedException();
    }

    private sealed class ViewModel : INotifyPropertyChanged
    {
        private bool _open;
        private string _text = "";

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool Open { get => _open; set => Set(ref _open, value); }

        public string Text { get => _text; set => Set(ref _text, value); }

        private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    // ---- the host

    [Fact]
    public void Host_puts_its_content_under_the_layers_and_finds_the_nearest_host_from_a_view()
    {
        var modal = new KlyModal();
        var (host, page) = Hosted(modal);

        Assert.Same(page, host.Content);
        Assert.Same(page, host.Children[0]);
        Assert.True(KlyOverlayHost.TryFind(modal, out var found));
        Assert.Same(host, found);
        Assert.Same(host, KlyOverlayHost.Find(modal));

        var other = new VerticalStackLayout();
        host.Content = other;
        Assert.DoesNotContain(page, host.Children);
        Assert.Same(other, host.Children[0]);
    }

    [Fact]
    public void Finding_a_host_throws_a_clear_error_when_the_view_is_not_inside_one()
    {
        var lonely = new VerticalStackLayout { new KlyModal() };
        var modal = (KlyModal)lonely.Children[0];

        Assert.False(KlyOverlayHost.TryFind(modal, out _));
        var error = Assert.Throws<InvalidOperationException>(() => KlyOverlayHost.Find(modal));
        Assert.Contains("wrap the page content in KlyOverlayHost", error.Message);
    }

    [Fact]
    public void Show_registers_a_layer_and_hide_unregisters_it_without_telling_it_was_dismissed()
    {
        var (host, _) = Hosted();
        var changes = 0;
        host.LayersChanged += (_, _) => changes++;
        var dismissed = 0;

        var layer = host.Show(new Label { Text = "hi" });
        layer.Dismissed += (_, _) => dismissed++;

        Assert.Same(layer, Assert.Single(host.Layers));
        Assert.True(layer.IsShown);
        Assert.Contains(layer.Container, host.Children);
        Assert.Same(host, layer.Host);
        Assert.Equal(1, changes);

        Assert.True(host.Hide(layer));
        Assert.False(layer.IsShown);
        Assert.Empty(host.Layers);
        Assert.DoesNotContain(layer.Container, host.Children);
        Assert.Equal(0, dismissed);
        Assert.Equal(2, changes);
        Assert.False(host.Hide(layer));
    }

    [Fact]
    public void Dismiss_removes_the_layer_and_raises_dismissed_once()
    {
        var (host, _) = Hosted();
        var layer = host.Show(new Label());
        var dismissed = 0;
        layer.Dismissed += (_, _) => dismissed++;

        Assert.True(host.Dismiss(layer));
        Assert.False(host.Dismiss(layer));

        Assert.Equal(1, dismissed);
        Assert.Empty(host.Layers);
    }

    [Fact]
    public void A_modal_layer_has_a_backdrop_that_dismisses_on_tap_when_it_is_dismissible()
    {
        var (host, _) = Hosted();
        var layer = host.Show(new Label(), new KlyOverlayLayerOptions { Modal = true, Dim = true });
        var backdrop = Assert.IsType<BoxView>(layer.Backdrop);
        var tap = Assert.IsType<TapGestureRecognizer>(Assert.Single(backdrop.GestureRecognizers));

        layer.Dismissible = false;
        Tap(backdrop, tap);
        Assert.True(layer.IsShown);

        layer.Dismissible = true;
        Tap(backdrop, tap);
        Assert.False(layer.IsShown);
    }

    [Fact]
    public void A_layer_that_is_not_modal_has_no_backdrop_and_lets_touches_through()
    {
        var (host, _) = Hosted();
        var content = new Label();
        var layer = host.Show(content, new KlyOverlayLayerOptions { Modal = false });

        Assert.Null(layer.Backdrop);
        Assert.True(layer.Container.InputTransparent);
        Assert.False(layer.Container.CascadeInputTransparent);
        Assert.Same(content, Assert.Single(layer.Container.Children));
    }

    [Fact]
    public void An_undimmed_backdrop_is_transparent_and_a_dimmed_one_is_not()
    {
        var (host, _) = Hosted();
        var plain = host.Show(new Label(), new KlyOverlayLayerOptions { Dim = false });
        var dim = host.Show(new Label(), new KlyOverlayLayerOptions { Dim = true });

        Assert.Equal(Colors.Transparent, plain.Backdrop!.Color);
        Assert.NotEqual(Colors.Transparent, dim.Backdrop!.Color);
        Assert.True(dim.Backdrop.Color.Alpha is > 0.5f and < 0.7f);
    }

    [Fact]
    public void Dismiss_top_takes_the_back_button_for_the_top_layer_that_can_be_dismissed()
    {
        var (host, _) = Hosted();
        Assert.False(host.DismissTop());

        var below = host.Show(new Label());
        var tooltip = host.Show(new Label(), new KlyOverlayLayerOptions { Modal = false, Dismissible = false });

        // The tooltip is skipped: the layer below goes.
        Assert.True(host.DismissTop());
        Assert.False(below.IsShown);
        Assert.True(tooltip.IsShown);
        Assert.False(host.DismissTop());
    }

    [Fact]
    public void A_modal_layer_that_cannot_be_dismissed_takes_the_back_button_and_stays()
    {
        var (host, _) = Hosted();
        var below = host.Show(new Label());
        var forced = host.Show(new Label(), new KlyOverlayLayerOptions { Dismissible = false });

        Assert.True(host.DismissTop());
        Assert.True(forced.IsShown);
        Assert.True(below.IsShown);
    }

    [Fact]
    public void A_modal_layer_hides_the_page_from_screen_readers_until_the_last_one_goes()
    {
        var (host, page) = Hosted();
        Assert.Null(AutomationProperties.GetExcludedWithChildren(page));

        var first = host.Show(new Label());
        var second = host.Show(new Label());
        var toast = host.Show(new Label(), new KlyOverlayLayerOptions { Modal = false });
        Assert.True(AutomationProperties.GetExcludedWithChildren(page));

        host.Hide(first);
        Assert.True(AutomationProperties.GetExcludedWithChildren(page));
        host.Hide(second);
        Assert.Null(AutomationProperties.GetExcludedWithChildren(page));
        Assert.True(toast.IsShown);
    }

    [Fact]
    public void Showing_a_view_that_is_already_in_a_parent_takes_it_out_of_it_first()
    {
        var (host, _) = Hosted();
        var label = new Label();
        var holder = new VerticalStackLayout { label };

        var layer = host.Show(label);

        Assert.Empty(holder.Children);
        Assert.Same(layer.Container, label.Parent);
    }

    [Fact]
    public void Bounds_of_adds_the_positions_of_the_parents_up_to_the_host_and_follows_scrolling()
    {
        var target = new BoxView();
        var inner = new VerticalStackLayout { target };
        var scroll = new ScrollView { Content = inner };
        var (host, _) = Hosted(scroll);
        Arrange(scroll, 10, 20, 300, 400);
        Arrange(inner, 0, 0, 300, 1000);
        Arrange(target, 5, 600, 80, 30);

        var bounds = host.BoundsOf(target);

        // The host itself is the origin; the page sits at 0,0 inside it.
        Assert.Equal(new Rect(15, 620, 80, 30), bounds);
        Assert.Throws<InvalidOperationException>(() => host.BoundsOf(new BoxView()));
    }

    [Fact]
    public void A_layer_is_told_the_size_of_the_host_when_it_is_shown_and_when_the_host_resizes()
    {
        var (host, _) = Hosted();
        Arrange(host, 0, 0, 400, 800);
        var sizes = new List<Size>();

        var layer = host.Show(new Label());
        layer.HostSizeChanged += (_, size) => sizes.Add(size);
        Arrange(host, 0, 0, 500, 900);

        Assert.Contains(new Size(500, 900), sizes);
    }

    [Theory]
    [InlineData(KlyPlacement.Bottom, 120, 148)]
    [InlineData(KlyPlacement.BottomStart, 100, 148)]
    [InlineData(KlyPlacement.BottomEnd, 140, 148)]
    [InlineData(KlyPlacement.Top, 120, 52)]
    [InlineData(KlyPlacement.RightStart, 208, 100)]
    [InlineData(KlyPlacement.LeftEnd, 32, 100)]
    public void Placement_puts_the_panel_on_the_side_and_the_alignment_asked_for(KlyPlacement placement, double x, double y)
    {
        // A 100x40 trigger at 100,100 in a 400x800 host, a 60x40 panel, 8 points away.
        var point = OverlaysPlacement.Position(new Rect(100, 100, 100, 40), new Size(60, 40), new Size(400, 800), placement, 8);

        Assert.Equal(new Point(x, y), point);
    }

    [Fact]
    public void Placement_flips_to_the_opposite_side_when_only_that_one_has_room_and_stays_inside_the_host()
    {
        var host = new Size(400, 800);
        var panel = new Size(100, 120);

        var flippedUp = OverlaysPlacement.Position(new Rect(100, 740, 100, 40), panel, host, KlyPlacement.Bottom, 8);
        Assert.Equal(740 - 8 - 120, flippedUp.Y);

        var flippedDown = OverlaysPlacement.Position(new Rect(100, 20, 100, 40), panel, host, KlyPlacement.Top, 8);
        Assert.Equal(20 + 40 + 8, flippedDown.Y);

        var clamped = OverlaysPlacement.Position(new Rect(350, 100, 100, 40), panel, host, KlyPlacement.BottomEnd, 8);
        Assert.Equal(400 - 100 - 8, clamped.X);

        var corner = OverlaysPlacement.Position(new Rect(0, 0, 40, 40), panel, host, KlyPlacement.Left, 8);
        Assert.True(corner.X >= 8 && corner.Y >= 8);
    }

    [Fact]
    public void Placement_swaps_start_and_end_for_a_right_to_left_flow()
    {
        var anchor = new Rect(100, 100, 100, 40);
        var panel = new Size(60, 40);
        var host = new Size(400, 800);

        Assert.Equal(100, OverlaysPlacement.Position(anchor, panel, host, KlyPlacement.BottomStart, 8, rtl: false).X);
        Assert.Equal(140, OverlaysPlacement.Position(anchor, panel, host, KlyPlacement.BottomStart, 8, rtl: true).X);
    }

    [Fact]
    public void An_anchored_layer_is_placed_against_its_anchor_once_the_content_has_a_size()
    {
        var anchor = new BoxView();
        var (host, _) = Hosted(anchor);
        Arrange(host, 0, 0, 400, 800);
        Arrange(anchor, 100, 100, 100, 40);

        var content = new BoxView();
        var layer = host.Show(content, new KlyOverlayLayerOptions { Anchor = anchor, Placement = KlyPlacement.BottomStart, Offset = 8 });
        Assert.Equal(LayoutOptions.Start, content.HorizontalOptions);
        Assert.Equal(0, content.Opacity);

        Layout(content, 60, 40);

        Assert.True(layer.IsShown);
        Assert.Equal(100, content.TranslationX);
        Assert.Equal(148, content.TranslationY);
        Assert.Equal(1, content.Opacity);
    }

    [Fact]
    public void An_anchor_point_places_the_content_at_that_point_of_the_anchor()
    {
        var anchor = new BoxView();
        var (host, _) = Hosted(anchor);
        Arrange(host, 0, 0, 400, 800);
        Arrange(anchor, 50, 200, 300, 300);

        var content = new BoxView();
        host.Show(content, new KlyOverlayLayerOptions { Anchor = anchor, AnchorPoint = new Point(30, 40), Placement = KlyPlacement.BottomStart, Offset = 0 });
        Layout(content, 100, 50);

        Assert.Equal(80, content.TranslationX);
        Assert.Equal(240, content.TranslationY);
    }

    [Fact]
    public void The_back_button_registration_does_nothing_off_android()
    {
        using var registration = OverlaysBackButton.Register(() => true);

        Assert.Null(registration);
    }

    // ---- dialogs opened from code

    [Fact]
    public async Task ShowAsync_completes_with_the_value_the_view_closes_it_with()
    {
        var (host, _) = Hosted();
        KlyOverlayContext<int>? context = null;

        var pending = host.ShowAsync<int>(c =>
        {
            context = c;
            return new Label { Text = "body" };
        }, new KlyOverlayOptions { Title = "Pick", Description = "A number", Size = KlySize.Lg }, Ct);

        var layer = Assert.Single(host.Layers);
        var panel = Assert.IsType<OverlaysPanel>(layer.Content);
        Assert.Equal("Pick", panel.TitleLabel.Text);
        Assert.Equal("A number", panel.DescriptionLabel.Text);
        Assert.Equal(672, panel.WidthRequest);
        Assert.False(context!.IsClosed);

        context.Close(42);
        context.Close(7);

        var result = await pending;
        Assert.False(result.Canceled);
        Assert.Equal(42, result.Value);
        Assert.True(result.TryGetValue(out var value));
        Assert.Equal(42, value);
        Assert.True(context.IsClosed);
        Assert.Empty(host.Layers);
    }

    [Fact]
    public async Task A_dialog_opened_from_code_keeps_its_size_class_and_fits_a_narrow_screen()
    {
        var (host, _) = Hosted();
        Arrange(host, 0, 0, 390, 800);

        _ = host.ShowAsync<bool>(_ => new Label(), new KlyOverlayOptions { Size = KlySize.Lg }, Ct);

        Assert.Equal(390 - 32, Assert.IsType<OverlaysPanel>(Assert.Single(host.Layers).Content).WidthRequest);
        host.DismissTop();

        Arrange(host, 0, 0, 1000, 800);
        _ = host.ShowAsync<bool>(_ => new Label(), new KlyOverlayOptions { Size = KlySize.Lg }, Ct);
        Assert.Equal(672, Assert.IsType<OverlaysPanel>(Assert.Single(host.Layers).Content).WidthRequest);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task ShowAsync_is_canceled_by_cancel_the_back_button_the_close_button_and_a_token()
    {
        var (host, _) = Hosted();

        KlyOverlayContext<bool>? context = null;
        var byCancel = host.ShowAsync<bool>(c => new Label { Text = (context = c).ToString() }, cancellationToken: Ct);
        context!.Cancel();
        Assert.True((await byCancel).Canceled);

        var byBack = host.ShowAsync<bool>(_ => new Label(), cancellationToken: Ct);
        Assert.True(host.DismissTop());
        var back = await byBack;
        Assert.True(back.Canceled);
        Assert.False(back.TryGetValue(out _));

        var byClose = host.ShowAsync<bool>(_ => new Label(), cancellationToken: Ct);
        Assert.IsType<OverlaysPanel>(Assert.Single(host.Layers).Content).RequestClose();
        Assert.True((await byClose).Canceled);
        Assert.Empty(host.Layers);

        using var source = CancellationTokenSource.CreateLinkedTokenSource(Ct);
        var byToken = host.ShowAsync<bool>(_ => new Label(), cancellationToken: source.Token);
        await source.CancelAsync();
        Assert.True((await byToken).Canceled);
        Assert.Empty(host.Layers);

        var already = await host.ShowAsync<bool>(_ => new Label(), cancellationToken: new CancellationToken(true));
        Assert.True(already.Canceled);
        Assert.Empty(host.Layers);
    }

    [Fact]
    public async Task ShowAsync_in_a_slideover_docks_the_panel_to_the_edge()
    {
        var (host, _) = Hosted();

        var pending = host.ShowAsync<bool>(_ => new Label(), new KlyOverlayOptions { Kind = KlyOverlayKind.Slideover, Side = SlideoverSide.Left, Dismissible = false, Close = false }, Ct);
        var layer = Assert.Single(host.Layers);
        var panel = Assert.IsType<OverlaysPanel>(layer.Content);

        Assert.Equal(OverlaysPanelMode.Left, panel.Mode);
        Assert.Equal(KlyOverlayTransition.SlideFromLeft, layer.Options.Transition);
        Assert.False(layer.Dismissible);
        Assert.False(panel.CloseButton.IsVisible);

        host.Hide(layer);
        Assert.False(pending.IsCompleted);
    }

    [Fact]
    public async Task ConfirmAsync_answers_true_on_confirm_and_false_on_cancel_or_dismiss()
    {
        var (host, _) = Hosted();

        var yes = host.ConfirmAsync("Delete?", "It is permanent", confirmLabel: "Delete", color: KlyColor.Error, cancellationToken: Ct);
        var panel = Assert.IsType<OverlaysPanel>(Assert.Single(host.Layers).Content);
        var buttons = Assert.IsType<HorizontalStackLayout>(panel.FooterView).Children.Cast<KlyButton>().ToArray();
        Assert.Equal(["Cancel", "Delete"], buttons.Select(b => b.Text).ToArray());
        Assert.Equal(KlyColor.Error, buttons[1].Color);
        Assert.Equal(ButtonVariant.Outline, buttons[0].Variant);
        Assert.Equal("Delete?", panel.TitleLabel.Text);
        Click(buttons[1]);
        Assert.True(await yes);
        Assert.Empty(host.Layers);

        var no = host.ConfirmAsync("Sure?", cancellationToken: Ct);
        var second = Assert.IsType<OverlaysPanel>(Assert.Single(host.Layers).Content);
        Click(Assert.IsType<HorizontalStackLayout>(second.FooterView).Children.Cast<KlyButton>().First());
        Assert.False(await no);

        var dismissed = host.ConfirmAsync("Again?", cancellationToken: Ct);
        host.DismissTop();
        Assert.False(await dismissed);
    }

    // ---- the overlay controls: opening, closing, events, binding

    [Fact]
    public void Modal_defaults_match_the_blazor_component()
    {
        var modal = new KlyModal();

        Assert.False(modal.IsOpen);
        Assert.True(modal.Dismissible);
        Assert.True(modal.Close);
        Assert.Equal(KlySize.Md, modal.Size);
        Assert.Equal("Close", modal.CloseLabel);
        Assert.Null(modal.Title);
        Assert.Null(modal.Body);
        Assert.Null(modal.Footer);
    }

    [Fact]
    public void Opening_a_modal_adds_a_layer_to_its_host_and_closing_removes_it_with_one_event_each()
    {
        var modal = new KlyModal { Title = "Settings", Description = "Change things", Body = new Label { Text = "body" }, Footer = new Label { Text = "footer" } };
        var (host, _) = Hosted(modal);
        var opened = 0;
        var closed = 0;
        modal.Opened += (_, _) => opened++;
        modal.Closed += (_, _) => closed++;

        modal.IsOpen = true;
        modal.IsOpen = true;

        var layer = Assert.Single(host.Layers);
        Assert.True(layer.Options.Modal);
        Assert.True(layer.Options.Dim);
        Assert.Equal(KlyOverlayTransition.Fade, layer.Options.Transition);
        Assert.Equal(1, opened);

        modal.IsOpen = false;
        modal.IsOpen = false;

        Assert.Empty(host.Layers);
        Assert.Equal(1, closed);
        Assert.Equal(1, opened);
    }

    [Fact]
    public void A_modal_shows_its_texts_body_and_footer_and_follows_them_while_open()
    {
        var body = new Label { Text = "body" };
        var footer = new Label { Text = "footer" };
        var modal = new KlyModal { Body = body, Footer = footer };
        var (host, _) = Hosted(modal);
        modal.IsOpen = true;
        var panel = Assert.IsType<OverlaysPanel>(Assert.Single(host.Layers).Content);

        Assert.True(panel.CloseButton.IsVisible);
        Assert.True(panel.HeaderView.IsVisible);
        Assert.False(panel.TitleLabel.IsVisible);
        Assert.Same(body, panel.BodyScroll.Content);
        Assert.Same(footer, Assert.IsType<ContentView>(((VerticalStackLayout)panel.FooterRegion).Children[1]).Content);

        modal.Title = "Settings";
        modal.Description = "Change things";
        modal.Close = false;

        Assert.Equal("Settings", panel.TitleLabel.Text);
        Assert.True(panel.TitleLabel.IsVisible);
        Assert.Equal("Change things", panel.DescriptionLabel.Text);
        Assert.False(panel.CloseButton.IsVisible);

        modal.Description = null;
        modal.Title = null;
        Assert.False(panel.HeaderView.IsVisible);

        modal.Footer = null;
        modal.Body = null;
        Assert.False(panel.FooterRegion.IsVisible);
        Assert.False(panel.BodyScroll.IsVisible);
    }

    [Fact]
    public void A_modal_names_its_dialog_and_its_close_button_for_a_screen_reader()
    {
        var modal = new KlyModal { Title = "Settings", Description = "Change things", CloseLabel = "Fermer" };
        var (host, _) = Hosted(modal);
        modal.IsOpen = true;
        var panel = Assert.IsType<OverlaysPanel>(Assert.Single(host.Layers).Content);

        Assert.Equal("Settings", SemanticProperties.GetDescription(panel));
        Assert.Equal("Change things", SemanticProperties.GetHint(panel));
        Assert.Equal(SemanticHeadingLevel.Level2, SemanticProperties.GetHeadingLevel(panel.TitleLabel));
        Assert.Equal("Fermer", SemanticProperties.GetDescription(panel.CloseButton));
        Assert.True(panel.CloseButton.MinimumWidthRequest >= 40);
        Assert.True(panel.CloseButton.MinimumHeightRequest >= 40);

        modal.Title = null;
        modal.AccessibleName = "Preferences";
        Assert.Equal("Preferences", SemanticProperties.GetDescription(panel));
    }

    [Theory]
    [InlineData(KlySize.Xs, 320)]
    [InlineData(KlySize.Sm, 384)]
    [InlineData(KlySize.Md, 512)]
    [InlineData(KlySize.Lg, 672)]
    [InlineData(KlySize.Xl, 896)]
    public void Modal_size_sets_the_width_of_the_dialog_and_never_exceeds_the_screen(KlySize size, double width)
    {
        var modal = new KlyModal { Size = size };
        var (host, _) = Hosted(modal);
        modal.IsOpen = true;
        var panel = Assert.IsType<OverlaysPanel>(Assert.Single(host.Layers).Content);

        Assert.Equal(width, panel.WidthRequest);
        Assert.Equal(LayoutOptions.Center, panel.HorizontalOptions);
        Assert.Equal(LayoutOptions.Center, panel.VerticalOptions);
        Assert.Equal(KlyTheme.RadiusOf(2), ((RoundRectangle)panel.StrokeShape!).CornerRadius.TopLeft);

        // On a phone 390 points wide the dialog keeps 16 points on each side.
        Arrange(host, 0, 0, 390, 800);
        host.Hide(host.Layers[0]);
        modal.IsOpen = false;
        modal.IsOpen = true;
        var narrow = Assert.IsType<OverlaysPanel>(Assert.Single(host.Layers).Content);
        Assert.Equal(Math.Min(width, 390 - 32), narrow.WidthRequest);
    }

    [Fact]
    public void A_modal_that_is_not_dismissible_ignores_the_back_button_and_the_backdrop()
    {
        var modal = new KlyModal { Dismissible = false };
        var (host, _) = Hosted(modal);
        modal.IsOpen = true;

        Assert.True(host.DismissTop());
        Assert.True(modal.IsOpen);

        modal.Dismissible = true;
        Assert.True(host.DismissTop());
        Assert.False(modal.IsOpen);
    }

    [Fact]
    public void Dismissing_a_modal_sets_is_open_to_false_through_a_two_way_binding_and_raises_closed_once()
    {
        var viewModel = new ViewModel();
        var modal = new KlyModal { BindingContext = viewModel };
        modal.SetBinding(KlyOverlayBase.IsOpenProperty, nameof(ViewModel.Open));
        var (host, _) = Hosted(modal);
        var closed = 0;
        modal.Closed += (_, _) => closed++;

        viewModel.Open = true;
        Assert.True(modal.IsOpen);
        Assert.Single(host.Layers);

        host.DismissTop();

        Assert.False(viewModel.Open);
        Assert.False(modal.IsOpen);
        Assert.Empty(host.Layers);
        Assert.Equal(1, closed);

        viewModel.Open = true;
        Assert.Single(host.Layers);
        viewModel.Open = false;
        Assert.Empty(host.Layers);
        Assert.Equal(2, closed);
    }

    [Fact]
    public void Opening_a_modal_inside_a_tree_without_a_host_throws_and_leaves_it_closed()
    {
        var modal = new KlyModal();
        var page = new VerticalStackLayout { modal };
        Assert.NotNull(page);

        var error = Assert.Throws<InvalidOperationException>(() => modal.IsOpen = true);

        Assert.Contains("wrap the page content in KlyOverlayHost", error.Message);
        Assert.False(modal.IsOpen);
        Assert.Throws<InvalidOperationException>(modal.Show);
        Assert.False(modal.IsOpen);
    }

    [Fact]
    public void A_modal_opened_before_it_is_in_a_tree_opens_once_it_is_in_a_host()
    {
        var modal = new KlyModal { IsOpen = true };
        var opened = 0;
        modal.Opened += (_, _) => opened++;
        var page = new VerticalStackLayout();
        var host = new KlyOverlayHost { Content = page };
        Assert.Empty(host.Layers);

        page.Add(modal);

        Assert.Single(host.Layers);
        Assert.Equal(1, opened);
        Assert.True(modal.IsOpen);
    }

    [Fact]
    public void Taking_an_open_modal_out_of_the_tree_closes_it()
    {
        var modal = new KlyModal();
        var (host, page) = Hosted(modal);
        modal.IsOpen = true;

        page.Remove(modal);

        Assert.False(modal.IsOpen);
        Assert.Empty(host.Layers);
    }

    [Fact]
    public void Show_and_hide_open_and_close_the_overlay_and_a_second_show_does_nothing()
    {
        var modal = new KlyModal();
        var (host, _) = Hosted(modal);
        var opened = 0;
        modal.Opened += (_, _) => opened++;

        modal.Show();
        modal.Show();
        Assert.True(modal.IsOpen);
        Assert.Single(host.Layers);
        Assert.Equal(1, opened);

        modal.Hide();
        Assert.False(modal.IsOpen);
        Assert.Empty(host.Layers);
    }

    [Fact]
    public void The_first_button_of_the_trigger_opens_and_closes_the_overlay()
    {
        var button = new KlyButton { Text = "Open" };
        var modal = new KlyModal { Content = new VerticalStackLayout { new Label { Text = "label" }, button } };
        var (host, _) = Hosted(modal);

        Click(button);
        Assert.True(modal.IsOpen);
        Assert.Single(host.Layers);

        Click(button);
        Assert.False(modal.IsOpen);
    }

    [Fact]
    public void A_trigger_without_a_button_opens_with_a_tap_and_a_new_trigger_replaces_the_old_wiring()
    {
        var first = new Label { Text = "first" };
        var modal = new KlyModal { Content = first };
        var (host, _) = Hosted(modal);
        var tap = Assert.IsType<TapGestureRecognizer>(Assert.Single(first.GestureRecognizers));

        Tap(first, tap);
        Assert.True(modal.IsOpen);

        var second = new Label { Text = "second" };
        modal.Content = second;
        Assert.Empty(first.GestureRecognizers);
        Assert.Single(second.GestureRecognizers);

        modal.IsOpen = false;
        Tap(second, Assert.IsType<TapGestureRecognizer>(second.GestureRecognizers[0]));
        Assert.True(modal.IsOpen);
        Assert.Single(host.Layers);
    }

    [Fact]
    public void A_disabled_overlay_ignores_its_trigger()
    {
        var button = new KlyButton { Text = "Open" };
        var modal = new KlyModal { Content = button, IsEnabled = false };
        var (host, _) = Hosted(modal);

        Click(button);

        Assert.False(modal.IsOpen);
        Assert.Empty(host.Layers);
    }

    [Fact]
    public void A_modal_inside_another_modals_body_opens_above_it()
    {
        var inner = new KlyModal { Title = "inner" };
        var outer = new KlyModal { Title = "outer", Body = new VerticalStackLayout { inner } };
        var (host, _) = Hosted(outer);
        outer.IsOpen = true;

        inner.IsOpen = true;

        Assert.Equal(2, host.Layers.Count);
        Assert.Equal("inner", ((OverlaysPanel)host.Layers[1].Content).TitleLabel.Text);

        host.DismissTop();
        Assert.False(inner.IsOpen);
        Assert.True(outer.IsOpen);
    }

    [Fact]
    public void A_modal_can_open_again_after_it_closed_with_the_same_body()
    {
        var body = new Label { Text = "body" };
        var modal = new KlyModal { Body = body };
        var (host, _) = Hosted(modal);

        modal.IsOpen = true;
        modal.IsOpen = false;
        modal.IsOpen = true;

        var panel = Assert.IsType<OverlaysPanel>(Assert.Single(host.Layers).Content);
        Assert.Same(body, panel.BodyScroll.Content);
    }

    [Fact]
    public void Slideover_docks_the_panel_to_the_side_it_comes_from()
    {
        var slideover = new KlySlideover { Title = "Details" };
        var (host, _) = Hosted(slideover);
        slideover.IsOpen = true;
        var layer = Assert.Single(host.Layers);
        var panel = Assert.IsType<OverlaysPanel>(layer.Content);

        Assert.Equal(SlideoverSide.Right, slideover.Side);
        Assert.Equal(LayoutOptions.End, panel.HorizontalOptions);
        Assert.Equal(LayoutOptions.Fill, panel.VerticalOptions);
        Assert.Equal(448, panel.WidthRequest);
        Assert.Equal(KlyOverlayTransition.SlideFromRight, layer.Options.Transition);
        Assert.Equal(new Thickness(0), panel.Margin);
        Assert.Equal(0, ((RoundRectangle)panel.StrokeShape!).CornerRadius.TopLeft);

        slideover.Side = SlideoverSide.Left;
        Assert.Equal(LayoutOptions.Start, panel.HorizontalOptions);

        slideover.Side = SlideoverSide.Top;
        var radius = KlyTheme.RadiusOf(2);
        Assert.Equal(LayoutOptions.Fill, panel.HorizontalOptions);
        Assert.Equal(LayoutOptions.Start, panel.VerticalOptions);
        Assert.Equal(new CornerRadius(0, 0, radius, radius), ((RoundRectangle)panel.StrokeShape!).CornerRadius);

        slideover.Side = SlideoverSide.Bottom;
        Assert.Equal(LayoutOptions.End, panel.VerticalOptions);
        Assert.Equal(new CornerRadius(radius, radius, 0, 0), ((RoundRectangle)panel.StrokeShape!).CornerRadius);
    }

    [Fact]
    public void Slideover_inset_floats_the_panel_away_from_the_edges_with_all_corners_rounded()
    {
        var slideover = new KlySlideover { Inset = true };
        var (host, _) = Hosted(slideover);
        slideover.IsOpen = true;
        var panel = Assert.IsType<OverlaysPanel>(Assert.Single(host.Layers).Content);

        Assert.Equal(new Thickness(8), panel.Margin);
        Assert.Equal(new CornerRadius(KlyTheme.RadiusOf(2)), ((RoundRectangle)panel.StrokeShape!).CornerRadius);

        slideover.Inset = false;
        Assert.Equal(new Thickness(0), panel.Margin);
    }

    [Fact]
    public void A_side_panel_fits_the_width_of_the_screen_and_a_sheet_is_at_most_85_percent_high()
    {
        var slideover = new KlySlideover();
        var drawer = new KlyDrawer();
        var (host, _) = Hosted(slideover, drawer);
        Arrange(host, 0, 0, 390, 800);

        slideover.IsOpen = true;
        var side = Assert.IsType<OverlaysPanel>(host.Layers[0].Content);
        Assert.Equal(390, side.WidthRequest);

        drawer.IsOpen = true;
        var sheet = Assert.IsType<OverlaysPanel>(host.Layers[1].Content);
        Assert.Equal(680, sheet.MaximumHeightRequest);

        Arrange(host, 0, 0, 1000, 400);
        Assert.Equal(448, side.WidthRequest);
        Assert.Equal(340, sheet.MaximumHeightRequest);
    }

    [Fact]
    public void Drawer_defaults_to_the_bottom_with_a_handle_and_no_close_button()
    {
        var drawer = new KlyDrawer { Title = "Share" };
        var (host, _) = Hosted(drawer);

        Assert.Equal(DrawerDirection.Bottom, drawer.Direction);
        Assert.True(drawer.Handle);
        Assert.False(drawer.Close);
        Assert.False(drawer.Inset);
        Assert.True(drawer.Dismissible);

        drawer.IsOpen = true;
        var layer = Assert.Single(host.Layers);
        var panel = Assert.IsType<OverlaysPanel>(layer.Content);
        Assert.Equal(KlyOverlayTransition.SlideFromBottom, layer.Options.Transition);
        Assert.True(panel.Handle.IsVisible);
        Assert.Equal(0, Grid.GetRow(panel.Handle));
        Assert.False(panel.CloseButton.IsVisible);
        Assert.Equal(LayoutOptions.End, panel.VerticalOptions);
    }

    [Fact]
    public void A_top_drawer_puts_the_handle_last_and_a_side_drawer_has_none()
    {
        var drawer = new KlyDrawer { Direction = DrawerDirection.Top };
        var (host, _) = Hosted(drawer);
        drawer.IsOpen = true;
        var panel = Assert.IsType<OverlaysPanel>(Assert.Single(host.Layers).Content);

        Assert.True(panel.Handle.IsVisible);
        Assert.Equal(4, Grid.GetRow(panel.Handle));
        Assert.Equal(KlyOverlayTransition.SlideFromTop, host.Layers[0].Options.Transition);

        drawer.Direction = DrawerDirection.Left;
        Assert.False(panel.Handle.IsVisible);
        Assert.Equal(LayoutOptions.Start, panel.HorizontalOptions);

        drawer.Direction = DrawerDirection.Right;
        Assert.Equal(LayoutOptions.End, panel.HorizontalOptions);

        drawer.Direction = DrawerDirection.Bottom;
        drawer.Handle = false;
        Assert.False(panel.Handle.IsVisible);
    }

    [Fact]
    public void Popover_places_its_panel_content_against_the_trigger_and_gives_it_back_on_close()
    {
        var trigger = new KlyButton { Text = "Open" };
        var content = new Label { Text = "hello" };
        var popover = new KlyPopover { Content = trigger, PanelContent = content, Placement = KlyPlacement.TopEnd, Offset = 12, AccessibleName = "Details" };
        var (host, _) = Hosted(popover);
        var opened = 0;
        var closed = 0;
        popover.Opened += (_, _) => opened++;
        popover.Closed += (_, _) => closed++;

        Click(trigger);

        var layer = Assert.Single(host.Layers);
        Assert.Same(trigger, layer.Options.Anchor);
        Assert.Equal(KlyPlacement.TopEnd, layer.Options.Placement);
        Assert.Equal(12, layer.Options.Offset);
        Assert.True(layer.Options.Modal);
        Assert.False(layer.Options.Dim);
        Assert.Equal(Colors.Transparent, layer.Backdrop!.Color);
        var panel = Assert.IsType<Border>(layer.Content);
        Assert.Same(content, panel.Content);
        Assert.Equal("Details", SemanticProperties.GetDescription(panel));
        Assert.Equal(1, opened);

        popover.AccessibleName = "More";
        Assert.Equal("More", SemanticProperties.GetDescription(panel));

        host.DismissTop();
        Assert.False(popover.IsOpen);
        Assert.Equal(1, closed);
        Assert.Null(content.Parent);

        popover.IsOpen = true;
        Assert.Same(content, Assert.IsType<Border>(Assert.Single(host.Layers).Content).Content);
    }

    [Fact]
    public void Popover_defaults_and_panel_content_swap_while_open()
    {
        var popover = new KlyPopover();
        var (host, _) = Hosted(popover);

        Assert.Equal(KlyPlacement.Bottom, popover.Placement);
        Assert.Equal(8, popover.Offset);
        Assert.Null(popover.PanelContent);

        popover.IsOpen = true;
        // With no trigger the control is its own anchor.
        Assert.Same(popover, host.Layers[0].Options.Anchor);
        var panel = Assert.IsType<Border>(host.Layers[0].Content);
        Assert.Null(panel.Content);

        var content = new Label();
        popover.PanelContent = content;
        Assert.Same(content, panel.Content);
    }

    [Fact]
    public void Tooltip_defaults_and_it_never_opens_with_a_tap()
    {
        var button = new KlyButton { Text = "Save" };
        var tooltip = new KlyTooltip { Content = button, Text = "Save the file" };
        var (host, _) = Hosted(tooltip);

        Assert.Equal(300, new KlyTooltip().Delay);
        Assert.Equal(KlyPlacement.Top, tooltip.Placement);

        Click(button);

        Assert.False(tooltip.IsOpen);
        Assert.Empty(host.Layers);
    }

    [Fact]
    public void A_long_press_shows_the_tooltip_without_a_backdrop_and_without_taking_the_back_button()
    {
        var button = new KlyButton { Text = "Save" };
        var tooltip = new KlyTooltip { Content = button, Text = "Save the file", Placement = KlyPlacement.Right };
        var (host, _) = Hosted(tooltip);
        var opened = 0;
        tooltip.Opened += (_, _) => opened++;

        tooltip.LongPressed();

        var layer = Assert.Single(host.Layers);
        Assert.Equal(1, opened);
        Assert.False(layer.Options.Modal);
        Assert.False(layer.Dismissible);
        Assert.Null(layer.Backdrop);
        Assert.Same(button, layer.Options.Anchor);
        Assert.Equal(KlyPlacement.Right, layer.Options.Placement);
        Assert.Equal(6, layer.Options.Offset);
        Assert.Equal("Save the file", Assert.IsType<Label>(tooltip.Bubble!.Content).Text);
        Assert.Equal(320, tooltip.Bubble.MaximumWidthRequest);
        Assert.False(host.DismissTop());
        Assert.True(tooltip.IsOpen);

        tooltip.PointerExited();
        Assert.Empty(host.Layers);
    }

    [Fact]
    public void The_tooltip_text_is_the_accessible_hint_of_the_trigger_unless_the_app_set_one()
    {
        var plain = new KlyButton { Text = "Save" };
        var tooltip = new KlyTooltip { Content = plain, Text = "Save the file" };
        Assert.Equal("Save the file", SemanticProperties.GetHint(plain));

        tooltip.Text = "Save it";
        Assert.Equal("Save it", SemanticProperties.GetHint(plain));

        var hinted = new KlyButton { Text = "Open" };
        SemanticProperties.SetHint(hinted, "Mine");
        _ = new KlyTooltip { Content = hinted, Text = "Open the file" };
        Assert.Equal("Mine", SemanticProperties.GetHint(hinted));
    }

    [Fact]
    public void A_tooltip_can_show_a_view_instead_of_text()
    {
        var custom = new Label { Text = "rich" };
        var tooltip = new KlyTooltip { Content = new KlyButton(), Text = "plain", PanelContent = custom };
        var (host, _) = Hosted(tooltip);

        tooltip.LongPressed();

        Assert.Same(custom, tooltip.Bubble!.Content);
        tooltip.IsOpen = false;
        Assert.Null(custom.Parent);
        Assert.Empty(host.Layers);
    }

    [Fact]
    public async Task The_tooltip_shows_after_the_hover_delay_and_not_at_all_when_the_pointer_leaves_first()
    {
        var tooltip = new KlyTooltip { Content = new KlyButton(), Text = "tip", Delay = 40 };
        var (host, _) = Hosted(tooltip);

        tooltip.PointerEntered();
        tooltip.PointerExited();
        await Task.Delay(150, Ct);
        Assert.Empty(host.Layers);

        tooltip.PointerEntered();
        Assert.Empty(host.Layers);
        await WaitUntil(() => host.Layers.Count == 1);
        Assert.True(tooltip.IsOpen);
    }

    private static async Task WaitUntil(Func<bool> condition)
    {
        for (var i = 0; i < 100 && !condition(); i++) await Task.Delay(20, Ct);
        Assert.True(condition());
    }

    private static readonly KlyMenuItem[] Items =
    [
        KlyMenuItem.Heading("File"),
        new() { Label = "New", Icon = KlyIconNames.Check, Shortcut = "Ctrl+N", Value = "new" },
        new() { Label = "Open", Description = "Needs a project", IsEnabled = false },
        new() { Label = "Export", Children = [new() { Label = "PDF" }, new() { Label = "Image", Children = [new() { Label = "PNG" }] }] },
        KlyMenuItem.Separator(),
        new() { Label = "Delete", Color = KlyColor.Error },
        new() { Label = "Docs", Href = "https://example.com/docs" },
    ];

    [Fact]
    public void A_menu_item_is_an_item_by_default_and_the_factories_make_headings_and_separators()
    {
        var item = new KlyMenuItem { Label = "New" };

        Assert.Equal(KlyMenuItemKind.Item, item.Kind);
        Assert.True(item.IsEnabled);
        Assert.Equal(KlyMenuItemKind.Separator, KlyMenuItem.Separator().Kind);
        var heading = KlyMenuItem.Heading("File");
        Assert.Equal(KlyMenuItemKind.Heading, heading.Kind);
        Assert.Equal("File", heading.Label);
    }

    [Fact]
    public void Dropdown_menu_lists_rows_for_items_only_and_opens_below_its_trigger()
    {
        var trigger = new KlyButton { Text = "Menu" };
        var menu = new KlyDropdownMenu { Content = trigger, Items = Items };
        var (host, _) = Hosted(menu);

        Assert.Equal(KlyPlacement.BottomStart, menu.Placement);
        Click(trigger);

        var layer = Assert.Single(host.Layers);
        Assert.Same(trigger, layer.Options.Anchor);
        Assert.Equal(KlyPlacement.BottomStart, layer.Options.Placement);
        Assert.Equal(4, layer.Options.Offset);
        Assert.False(layer.Options.Dim);
        Assert.Equal(5, menu.Menu!.Rows.Count);
        Assert.Equal(["New", "Open", "Export", "Delete", "Docs"], menu.Menu.Rows.Select(r => r.Item.Label).ToArray());
        Assert.Equal("Menu", SemanticProperties.GetDescription(menu.Menu));
        Assert.True(menu.Menu.MinimumWidthRequest >= 176);
    }

    [Fact]
    public void Selecting_an_item_closes_the_menu_then_raises_the_selection_once()
    {
        var menu = new KlyDropdownMenu { Content = new KlyButton(), Items = Items };
        var (host, _) = Hosted(menu);
        var selected = new List<KlyMenuItem>();
        var wasOpen = new List<bool>();
        menu.ItemSelected += (_, item) =>
        {
            selected.Add(item);
            wasOpen.Add(menu.IsOpen);
        };
        menu.IsOpen = true;

        menu.Menu!.Rows[0].Activate();

        Assert.Equal("new", Assert.Single(selected).Value);
        Assert.Equal([false], wasOpen);
        Assert.Empty(host.Layers);
        Assert.False(menu.IsOpen);
    }

    [Fact]
    public void A_disabled_item_is_dimmed_with_its_hint_and_cannot_be_selected()
    {
        var menu = new KlyDropdownMenu { Content = new KlyButton(), Items = Items };
        var (host, _) = Hosted(menu);
        var selected = 0;
        menu.ItemSelected += (_, _) => selected++;
        menu.IsOpen = true;
        var row = menu.Menu!.Rows[1];

        row.Activate();

        Assert.Equal(0, selected);
        Assert.True(menu.IsOpen);
        Assert.Equal(0.6, row.Opacity);
        Assert.False(row.IsEnabled);
        Assert.Equal("Open", SemanticProperties.GetDescription(row));
        Assert.Equal("Needs a project", SemanticProperties.GetHint(row));
        Assert.Single(host.Layers);
    }

    [Fact]
    public void Menu_rows_are_touch_targets_and_error_items_use_the_error_color()
    {
        var menu = new KlyDropdownMenu { Content = new KlyButton(), Items = Items };
        _ = Hosted(menu);
        menu.IsOpen = true;

        Assert.All(menu.Menu!.Rows, row => Assert.True(row.MinimumHeightRequest >= 40));
        var normal = FindLabel(menu.Menu.Rows[0], "New");
        var error = FindLabel(menu.Menu.Rows[3], "Delete");
        Assert.NotEqual(normal.TextColor, error.TextColor);
        Assert.Equal(KlyTheme.Resolve(KlyColor.Error).Text.Light, error.TextColor);
    }

    private static Label FindLabel(Border row, string text) =>
        ((Grid)row.Content!).Children.OfType<Label>().First(l => l.Text == text);

    [Fact]
    public void An_item_with_children_opens_a_submenu_next_to_it_and_selecting_inside_closes_both()
    {
        var menu = new KlyDropdownMenu { Content = new KlyButton(), Items = Items };
        var (host, _) = Hosted(menu);
        var selected = new List<string>();
        menu.ItemSelected += (_, item) => selected.Add(item.Label);
        menu.IsOpen = true;
        var export = menu.Menu!.Rows[2];
        Assert.True(export.HasSubmenu);
        Assert.False(menu.Menu.Rows[0].HasSubmenu);

        export.Activate();

        Assert.Equal(2, host.Layers.Count);
        Assert.Empty(selected);
        var sub = host.Layers[1];
        Assert.False(sub.Options.Modal);
        Assert.Same(export, sub.Options.Anchor);
        Assert.Equal(KlyPlacement.RightStart, sub.Options.Placement);
        var subMenu = Assert.IsType<OverlaysMenu>(sub.Content);
        Assert.Equal(["PDF", "Image"], subMenu.Rows.Select(r => r.Item.Label).ToArray());
        Assert.True(subMenu.Rows[1].HasSubmenu);

        subMenu.Rows[1].Activate();
        Assert.Equal(3, host.Layers.Count);

        // Picking another row of the first submenu closes the deeper one.
        subMenu.Rows[0].Activate();

        Assert.Empty(host.Layers);
        Assert.Equal(["PDF"], selected);
        Assert.False(menu.IsOpen);
        Assert.Empty(menu.Submenus);
    }

    [Fact]
    public void Closing_the_menu_closes_its_submenus_and_the_back_button_closes_the_submenu_first()
    {
        var menu = new KlyDropdownMenu { Content = new KlyButton(), Items = Items };
        var (host, _) = Hosted(menu);
        menu.IsOpen = true;
        menu.Menu!.Rows[2].Activate();
        Assert.Equal(2, host.Layers.Count);

        Assert.True(host.DismissTop());
        Assert.Single(host.Layers);
        Assert.True(menu.IsOpen);
        Assert.Empty(menu.Submenus);

        menu.Menu.Rows[2].Activate();
        menu.IsOpen = false;
        Assert.Empty(host.Layers);
    }

    [Fact]
    public void Context_menu_opens_at_the_pointer_and_not_when_disabled()
    {
        var area = new BoxView();
        var menu = new KlyContextMenu { Content = area, Items = Items };
        var (host, _) = Hosted(menu);
        Arrange(host, 0, 0, 400, 800);

        menu.OpenAt(new Point(30, 40));

        var layer = Assert.Single(host.Layers);
        Assert.Same(area, layer.Options.Anchor);
        Assert.Equal(new Point(30, 40), layer.Options.AnchorPoint);
        Assert.Equal(KlyPlacement.BottomStart, layer.Options.Placement);
        Assert.Equal(0, layer.Options.Offset);
        Assert.True(menu.IsOpen);
        Assert.Equal("Context menu", SemanticProperties.GetDescription(menu.Menu!));

        menu.IsOpen = false;
        menu.Disabled = true;
        menu.OpenAt(new Point(1, 1));
        Assert.False(menu.IsOpen);
        Assert.Empty(host.Layers);
    }

    [Fact]
    public void Context_menu_opens_at_the_center_of_the_area_when_set_from_code_and_has_no_submenus()
    {
        var area = new BoxView();
        var menu = new KlyContextMenu { Content = area, Items = Items, Label = "Actions" };
        var (host, _) = Hosted(menu);
        Arrange(area, 0, 0, 200, 100);

        menu.IsOpen = true;

        Assert.Equal(new Point(100, 50), host.Layers[0].Options.AnchorPoint);
        Assert.Equal("Actions", SemanticProperties.GetDescription(menu.Menu!));
        Assert.DoesNotContain(menu.Menu!.Rows, r => r.HasSubmenu);
    }

    [Fact]
    public void Context_menu_selection_closes_it_and_raises_the_event_once_and_a_right_click_is_wired()
    {
        var area = new BoxView();
        var menu = new KlyContextMenu { Content = area, Items = Items };
        var (host, _) = Hosted(menu);
        var selected = new List<string>();
        menu.ItemSelected += (_, item) => selected.Add(item.Label);

        var secondary = Assert.IsType<TapGestureRecognizer>(Assert.Single(area.GestureRecognizers));
        Assert.Equal(ButtonsMask.Secondary, secondary.Buttons);

        menu.OpenAt(null);
        menu.Menu!.Rows[3].Activate();

        Assert.Equal(["Delete"], selected);
        Assert.Empty(host.Layers);
        Assert.False(menu.IsOpen);
    }

    // ---- the command palette

    private static readonly KlyCommandGroup[] Groups =
    [
        new("Suggestions", [
            new KlyCommandItem { Label = "Open file", Description = "Pick from disk", Icon = KlyIconNames.Search, Shortcut = "Ctrl+O", Value = "open" },
            new KlyCommandItem { Label = "Save file", Keywords = "write persist", Count = 12345 },
        ]),
        new(null, [
            new KlyCommandItem { Label = "Quit", IsEnabled = false },
            new KlyCommandItem { Label = "Settings", Href = "https://example.com/settings" },
        ]),
    ];

    [Theory]
    [InlineData("", true)]
    [InlineData("  ", true)]
    [InlineData("open", true)]
    [InlineData("OPEN FILE", true)]
    [InlineData("file open", true)]
    [InlineData("disk", true)]
    [InlineData("sugg", true)]
    [InlineData("open write", false)]
    [InlineData("nothing", false)]
    public void The_palette_search_needs_every_word_in_the_label_description_keywords_or_group(string query, bool expected)
    {
        var group = Groups[0];
        var item = group.Items[0];

        Assert.Equal(expected, KlyCommandPalette.Matches(group, item, query));
    }

    [Fact]
    public void The_palette_matches_keywords_that_are_not_shown()
    {
        var group = Groups[0];

        Assert.True(KlyCommandPalette.Matches(group, group.Items[1], "persist"));
        Assert.False(KlyCommandPalette.Matches(group, group.Items[0], "persist"));
    }

    [Fact]
    public void A_palette_lists_every_command_with_the_first_selectable_one_highlighted()
    {
        var palette = new KlyCommandPalette { Groups = Groups };

        Assert.Equal(4, palette.Rows.Count);
        Assert.Equal(4, palette.VisibleCount);
        Assert.Equal("open", palette.HighlightedItem!.Key);
        Assert.True(palette.Rows[0].Highlighted);
        Assert.False(palette.EmptyLabel.IsVisible);
        Assert.Equal("", palette.Text);
        Assert.Equal("Search commands", palette.Placeholder);
        Assert.Equal("Search commands", palette.Label);
        Assert.Equal("No results found", palette.EmptyText);
        Assert.False(palette.CloseOnSelect);
    }

    [Fact]
    public void Typing_filters_the_commands_hides_empty_groups_and_shows_the_empty_text()
    {
        var palette = new KlyCommandPalette { Groups = Groups };

        palette.Text = "file";
        Assert.Equal(2, palette.VisibleCount);
        Assert.True(palette.GroupViews[0].Group.IsVisible);
        Assert.False(palette.GroupViews[1].Group.IsVisible);
        Assert.False(palette.EmptyLabel.IsVisible);
        Assert.Equal("open", palette.HighlightedItem!.Key);

        palette.Text = "save";
        Assert.Equal(1, palette.VisibleCount);
        Assert.Equal("Save file", palette.HighlightedItem!.Label);

        palette.Text = "zzz";
        Assert.Equal(0, palette.VisibleCount);
        Assert.True(palette.EmptyLabel.IsVisible);
        Assert.Null(palette.HighlightedItem);

        palette.Text = "";
        Assert.Equal(4, palette.VisibleCount);
        Assert.False(palette.EmptyLabel.IsVisible);
    }

    [Fact]
    public void The_line_between_groups_is_hidden_for_the_first_group_that_is_shown()
    {
        var palette = new KlyCommandPalette { Groups = Groups };
        Assert.Null(palette.GroupViews[0].Separator);
        Assert.True(palette.GroupViews[1].Separator!.IsVisible);

        palette.Text = "settings";

        Assert.False(palette.GroupViews[0].Group.IsVisible);
        Assert.False(palette.GroupViews[1].Separator!.IsVisible);
    }

    [Fact]
    public void A_tap_or_enter_activates_a_command_and_raises_the_selection_once()
    {
        var palette = new KlyCommandPalette { Groups = Groups };
        var selected = new List<string>();
        palette.ItemSelected += (_, item) => selected.Add(item.Key);

        palette.Rows[1].Activate();
        Assert.Equal(["Save file"], selected);
        Assert.Equal("Save file", palette.HighlightedItem!.Label);

        var entry = FindEntry(palette);
        ((IEntryController)entry).SendCompleted();
        Assert.Equal(["Save file", "Save file"], selected);
    }

    [Fact]
    public void A_disabled_command_is_dimmed_never_highlighted_and_never_selected()
    {
        var palette = new KlyCommandPalette { Groups = Groups };
        var selected = 0;
        palette.ItemSelected += (_, _) => selected++;
        var quit = palette.Rows[2];

        quit.Activate();

        Assert.Equal(0, selected);
        Assert.Equal(0.6, quit.Opacity);
        Assert.False(quit.IsEnabled);
        Assert.NotEqual("Quit", palette.HighlightedItem!.Label);

        palette.Text = "quit";
        Assert.Equal(1, palette.VisibleCount);
        Assert.Null(palette.HighlightedItem);
        ((IEntryController)FindEntry(palette)).SendCompleted();
        Assert.Equal(0, selected);
    }

    [Fact]
    public void The_palette_text_is_two_way_bound_and_follows_the_search_field()
    {
        var viewModel = new ViewModel();
        var palette = new KlyCommandPalette { Groups = Groups, BindingContext = viewModel };
        palette.SetBinding(KlyCommandPalette.TextProperty, nameof(ViewModel.Text));
        var entry = FindEntry(palette);

        viewModel.Text = "quit";
        Assert.Equal("quit", entry.Text);
        Assert.Equal(1, palette.VisibleCount);

        entry.Text = "save";
        Assert.Equal("save", viewModel.Text);
        Assert.Equal(1, palette.VisibleCount);
    }

    [Fact]
    public void The_palette_names_its_field_and_rows_for_a_screen_reader_and_shows_icons_counts_and_shortcuts()
    {
        var palette = new KlyCommandPalette { Groups = Groups, Label = "Find a command", Placeholder = "Type…" };
        var entry = FindEntry(palette);

        Assert.Equal("Find a command", SemanticProperties.GetDescription(entry));
        Assert.Equal("Type…", entry.Placeholder);
        Assert.Equal("Pick from disk", SemanticProperties.GetHint(palette.Rows[0]));
        Assert.Equal("Open file", SemanticProperties.GetDescription(palette.Rows[0]));
        Assert.StartsWith("Save file, 12", SemanticProperties.GetDescription(palette.Rows[1]));
        Assert.All(palette.Rows, row => Assert.True(row.MinimumHeightRequest >= 40));
        var cells = ((Grid)palette.Rows[0].Content!).Children;
        Assert.Contains(cells, c => c is KlyIcon);
        Assert.Contains(cells, c => c is Label { Text: "Ctrl+O" });
    }

    [Fact]
    public void Close_on_select_closes_the_modal_the_palette_is_in()
    {
        var palette = new KlyCommandPalette { Groups = Groups, CloseOnSelect = true };
        var modal = new KlyModal { Body = palette, Close = false };
        var (host, _) = Hosted(modal);
        modal.IsOpen = true;
        var selected = 0;
        palette.ItemSelected += (_, _) => selected++;

        palette.Rows[0].Activate();

        Assert.Equal(1, selected);
        Assert.False(modal.IsOpen);
        Assert.Empty(host.Layers);
    }

    [Fact]
    public void Without_close_on_select_the_modal_stays_open()
    {
        var palette = new KlyCommandPalette { Groups = Groups };
        var modal = new KlyModal { Body = palette };
        var (host, _) = Hosted(modal);
        modal.IsOpen = true;

        palette.Rows[0].Activate();

        Assert.True(modal.IsOpen);
        Assert.Single(host.Layers);
    }

    private static Entry FindEntry(Element root)
    {
        var queue = new Queue<IVisualTreeElement>();
        queue.Enqueue((IVisualTreeElement)root);
        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            if (node is Entry entry) return entry;
            foreach (var child in node.GetVisualChildren()) queue.Enqueue(child);
        }
        throw new InvalidOperationException("No entry.");
    }

    // ---- links

    [Theory]
    [InlineData("https://example.com", true)]
    [InlineData("HTTP://example.com", true)]
    [InlineData("mailto:a@b.c", true)]
    [InlineData("tel:+33123456789", true)]
    [InlineData("/settings", false)]
    [InlineData("settings", false)]
    [InlineData("file:///etc/passwd", false)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData(null, false)]
    public void Only_web_mail_phone_and_sms_addresses_are_opened_by_the_system(string? href, bool expected) =>
        Assert.Equal(expected, OverlaysSupport.IsExternalLink(href));

    // ---- toasts

    private static (KlyOverlayHost Host, KlyToaster Toaster, KlyToastService Service) ToastPage(Func<TimeSpan, CancellationToken, Task>? delay = null)
    {
        var service = new KlyToastService();
        var toaster = new KlyToaster { Service = service, Delay = delay };
        var (host, _) = Hosted(toaster);
        return (host, toaster, service);
    }

    private static readonly Func<TimeSpan, CancellationToken, Task> Never = (_, token) => Task.Delay(Timeout.Infinite, token);

    [Fact]
    public void Toaster_defaults_and_it_serves_a_service_only_while_it_is_in_a_tree()
    {
        var service = new KlyToastService();
        var toaster = new KlyToaster { Service = service };

        Assert.Equal(TimeSpan.FromSeconds(5), toaster.Duration);
        Assert.Equal("Close", toaster.CloseLabel);
        Assert.Empty(toaster.Toasts);
        Assert.Equal(0, service.ToasterCount);

        var page = new VerticalStackLayout { toaster };
        Assert.Equal(1, service.ToasterCount);

        page.Remove(toaster);
        Assert.Equal(0, service.ToasterCount);
    }

    [Fact]
    public void A_toaster_without_its_own_service_serves_the_default_one()
    {
        var toaster = new KlyToaster();
        var before = KlyToastService.Default.ToasterCount;

        var page = new VerticalStackLayout { toaster };
        Assert.Equal(before + 1, KlyToastService.Default.ToasterCount);

        var own = new KlyToastService();
        toaster.Service = own;
        Assert.Equal(before, KlyToastService.Default.ToasterCount);
        Assert.Equal(1, own.ToasterCount);

        page.Remove(toaster);
        Assert.Equal(0, own.ToasterCount);
    }

    [Fact]
    public async Task Adding_a_toast_shows_it_in_a_layer_that_lets_the_page_work()
    {
        var (host, toaster, service) = ToastPage(Never);

        await service.Add(new KlyToast { Title = "Saved", Description = "All good", Color = KlyColor.Success });

        var layer = Assert.Single(host.Layers);
        Assert.False(layer.Options.Modal);
        Assert.False(layer.Dismissible);
        Assert.Null(layer.Backdrop);
        var toast = Assert.Single(toaster.Toasts);
        Assert.Equal("Saved", toast.Title);
        var viewport = Assert.IsType<VerticalStackLayout>(layer.Content);
        Assert.Equal(LayoutOptions.End, viewport.HorizontalOptions);
        Assert.Equal(LayoutOptions.End, viewport.VerticalOptions);
        Assert.Equal(384, viewport.WidthRequest);
        Assert.Equal("Saved. All good", SemanticProperties.GetDescription(toaster.Views[0]));
        Assert.False(host.DismissTop());
    }

    [Fact]
    public async Task Toasts_stack_with_the_newest_last_and_at_most_five_stay()
    {
        var (host, toaster, service) = ToastPage(Never);

        for (var i = 1; i <= 7; i++) await service.Add(new KlyToast { Title = $"Toast {i}" });

        Assert.Equal(["Toast 3", "Toast 4", "Toast 5", "Toast 6", "Toast 7"], toaster.Toasts.Select(t => t.Title).ToArray());
        var viewport = Assert.IsType<VerticalStackLayout>(Assert.Single(host.Layers).Content);
        Assert.Equal(5, viewport.Children.Count);
        Assert.Same(toaster.Views[4], viewport.Children[4]);
    }

    [Fact]
    public async Task A_toast_closes_by_itself_when_its_countdown_ends_and_the_layer_goes_with_the_last_one()
    {
        var gates = new List<TaskCompletionSource>();
        var (host, toaster, service) = ToastPage((_, token) =>
        {
            var gate = new TaskCompletionSource();
            token.Register(() => gate.TrySetCanceled(token));
            gates.Add(gate);
            return gate.Task;
        });

        await service.Add(new KlyToast { Title = "One" });
        await service.Add(new KlyToast { Title = "Two" });
        Assert.Equal(2, gates.Count);
        Assert.All(toaster.Views, v => Assert.True(v.CountdownRunning));

        gates[0].SetResult();
        Assert.Equal(["Two"], toaster.Toasts.Select(t => t.Title).ToArray());
        Assert.Single(host.Layers);

        gates[1].SetResult();
        Assert.Empty(toaster.Toasts);
        Assert.Empty(host.Layers);

        await service.Add(new KlyToast { Title = "Three" });
        Assert.Single(host.Layers);
    }

    [Fact]
    public async Task A_toast_uses_the_duration_of_the_toaster_unless_it_sets_its_own_and_zero_keeps_it_open()
    {
        var spans = new List<TimeSpan>();
        var (_, toaster, service) = ToastPage((span, token) =>
        {
            spans.Add(span);
            return Task.Delay(Timeout.Infinite, token);
        });
        toaster.Duration = TimeSpan.FromSeconds(9);

        await service.Add(new KlyToast { Title = "Default" });
        await service.Add(new KlyToast { Title = "Own", Duration = TimeSpan.FromSeconds(2) });
        await service.Add(new KlyToast { Title = "Sticky", Duration = TimeSpan.Zero });

        Assert.Equal([TimeSpan.FromSeconds(9), TimeSpan.FromSeconds(2)], spans);
        Assert.False(toaster.Views[2].CountdownRunning);
    }

    [Fact]
    public async Task The_close_button_closes_a_toast_and_it_is_named_for_a_screen_reader()
    {
        var (host, toaster, service) = ToastPage(Never);
        toaster.CloseLabel = "Fermer";
        await service.Add(new KlyToast { Title = "One" });
        var view = toaster.Views[0];
        var closed = 0;
        view.Closed += (_, _) => closed++;

        var close = FindClose(view);
        Assert.Equal("Fermer", SemanticProperties.GetDescription(close));
        view.Close();
        view.Close();

        Assert.Equal(1, closed);
        Assert.True(view.IsClosed);
        Assert.Empty(host.Layers);
    }

    private static OverlaysCloseButton FindClose(Element root)
    {
        var queue = new Queue<IVisualTreeElement>();
        queue.Enqueue((IVisualTreeElement)root);
        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            if (node is OverlaysCloseButton button) return button;
            foreach (var child in node.GetVisualChildren()) queue.Enqueue(child);
        }
        throw new InvalidOperationException("No close button.");
    }

    [Fact]
    public async Task A_long_swipe_dismisses_a_toast_and_a_short_one_springs_back()
    {
        var (_, toaster, service) = ToastPage(Never);
        await service.Add(new KlyToast { Title = "One" });
        var view = toaster.Views[0];

        view.OnPan(GestureStatus.Started, 0);
        Assert.False(view.CountdownRunning);
        view.OnPan(GestureStatus.Running, 30);
        Assert.Equal(30, view.TranslationX);
        Assert.True(view.Opacity < 1);
        view.OnPan(GestureStatus.Completed, 0);

        Assert.False(view.IsClosed);
        Assert.Equal(0, view.TranslationX);
        Assert.Equal(1, view.Opacity);
        Assert.True(view.CountdownRunning);

        view.OnPan(GestureStatus.Started, 0);
        view.OnPan(GestureStatus.Running, -(OverlaysToastView.SwipeDistance + 20));
        view.OnPan(GestureStatus.Completed, 0);

        Assert.True(view.IsClosed);
        Assert.Empty(toaster.Toasts);
    }

    [Fact]
    public async Task A_toast_without_a_toaster_in_a_host_throws_a_clear_error()
    {
        var service = new KlyToastService();
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await service.Add(new KlyToast { Title = "No one" }));

        var toaster = new KlyToaster { Service = service };
        var page = new VerticalStackLayout { toaster };
        Assert.NotNull(page);
        var error = Assert.Throws<InvalidOperationException>(() => toaster.Show(new KlyToast { Title = "No host" }));
        Assert.Contains("wrap the page content in KlyOverlayHost", error.Message);
    }

    [Fact]
    public void A_toast_is_neutral_by_default()
    {
        var toast = new KlyToast();

        Assert.Equal(KlyColor.Neutral, toast.Color);
        Assert.Null(toast.Duration);
    }

    [Fact]
    public async Task The_newest_toaster_of_a_service_shows_the_toasts()
    {
        var service = new KlyToastService();
        var first = new KlyToaster { Service = service, Delay = Never };
        var second = new KlyToaster { Service = service, Delay = Never };
        var (_, _) = Hosted(first);
        var (_, _) = Hosted(second);

        await service.Add(new KlyToast { Title = "Hi" });

        Assert.Empty(first.Toasts);
        Assert.Single(second.Toasts);
    }

    [Fact]
    public async Task The_countdown_pauses_and_resumes_with_at_least_one_second_and_fires_once()
    {
        var elapsed = 0;
        var waits = new List<TimeSpan>();
        var gates = new List<TaskCompletionSource>();
        var countdown = new OverlaysCountdown(TimeSpan.FromMilliseconds(300), () => elapsed++, (span, token) =>
        {
            waits.Add(span);
            var gate = new TaskCompletionSource();
            token.Register(() => gate.TrySetCanceled(token));
            gates.Add(gate);
            return gate.Task;
        });

        countdown.Start();
        Assert.True(countdown.IsRunning);

        countdown.Pause();
        Assert.False(countdown.IsRunning);
        countdown.Pause();
        countdown.Resume();
        Assert.True(countdown.IsRunning);
        Assert.Equal(2, waits.Count);
        Assert.True(waits[1] >= TimeSpan.FromSeconds(1));

        gates[1].SetResult();
        await Task.Yield();
        Assert.Equal(1, elapsed);
        Assert.False(countdown.IsRunning);

        countdown.Resume();
        Assert.False(countdown.IsRunning);
        Assert.Equal(2, waits.Count);
    }

    [Fact]
    public void A_stopped_countdown_never_fires()
    {
        var elapsed = 0;
        var countdown = new OverlaysCountdown(TimeSpan.Zero, () => elapsed++, (_, token) => Task.Delay(Timeout.Infinite, token));

        countdown.Start();
        countdown.Stop();
        countdown.Resume();

        Assert.False(countdown.IsRunning);
        Assert.Equal(0, elapsed);
    }
}
