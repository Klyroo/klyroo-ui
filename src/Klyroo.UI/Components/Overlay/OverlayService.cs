using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Klyroo.UI;

/// <summary>How <see cref="IKlyOverlayService"/> presents the component it opens.</summary>
public enum KlyOverlayKind
{
    /// <summary>A centered modal dialog (<see cref="KlyModal"/>).</summary>
    Modal,
    /// <summary>A panel on an edge of the screen (<see cref="KlySlideover"/>).</summary>
    Slideover,
}

/// <summary>Presentation of a dialog opened with <see cref="IKlyOverlayService"/>.</summary>
public sealed record KlyOverlayOptions
{
    /// <summary>Title of the dialog, which also names it for assistive technology.</summary>
    public string? Title { get; init; }
    /// <summary>Text under the title.</summary>
    public string? Description { get; init; }
    /// <summary>Accessible name when there is no <see cref="Title"/>.</summary>
    public string? AriaLabel { get; init; }
    /// <summary>A centered modal (the default) or a slideover.</summary>
    public KlyOverlayKind Kind { get; init; } = KlyOverlayKind.Modal;
    /// <summary>Size of a modal, from <c>Xs</c> to <c>Xl</c>. Ignored for a slideover.</summary>
    public KlySize Size { get; init; } = KlySize.Md;
    /// <summary>Edge a slideover comes from. Ignored for a modal.</summary>
    public SlideoverSide Side { get; init; } = SlideoverSide.Right;
    /// <summary>Escape and a click on the backdrop cancel the dialog. Turn it off to force an explicit answer.</summary>
    public bool Dismissible { get; init; } = true;
    /// <summary>Shows the close button of the header, which cancels the dialog.</summary>
    public bool Close { get; init; } = true;
}

/// <summary>How a dialog opened with <see cref="IKlyOverlayService"/> ended.</summary>
/// <typeparam name="TResult">The type of the value the dialog completes with.</typeparam>
public sealed record KlyOverlayResult<TResult>
{
    internal KlyOverlayResult(bool canceled, TResult? value)
    {
        Canceled = canceled;
        Value = value;
    }

    /// <summary>True when the dialog was dismissed (Escape, backdrop, close button, <see cref="KlyOverlayContext{TResult}.Cancel"/> or a cancellation token) instead of completed.</summary>
    public bool Canceled { get; }

    /// <summary>The value passed to <see cref="KlyOverlayContext{TResult}.Close"/>, or <c>default</c> when the dialog was canceled.</summary>
    public TResult? Value { get; }

    /// <summary>Gets the value when the dialog completed with one (it was not canceled).</summary>
    public bool TryGetValue([MaybeNullWhen(false)] out TResult value)
    {
        if (!Canceled)
        {
            value = Value!;
            return true;
        }
        value = default;
        return false;
    }
}

/// <summary>What a dialog ended with, before it is typed for the caller. Internal: the public types are <see cref="KlyOverlayResult{TResult}"/> and <see cref="KlyOverlayContext{TResult}"/>.</summary>
internal sealed record OverlayOutcome(bool Canceled, object? Value)
{
    /// <summary>The outcome of a dialog that was dismissed.</summary>
    public static OverlayOutcome Dismissed { get; } = new(true, null);
}

/// <summary>
/// Handed to the component shown by <see cref="IKlyOverlayService"/> as a cascading parameter, so it can end its own dialog:
/// <c>[CascadingParameter] public KlyOverlayContext&lt;Person&gt; Overlay { get; set; } = default!;</c> with the same <typeparamref name="TResult"/> the caller of <c>ShowAsync</c> gave.
/// </summary>
/// <typeparam name="TResult">The type of the value the dialog completes with.</typeparam>
public sealed class KlyOverlayContext<TResult>
{
    private readonly OverlayEntry _entry;

    internal KlyOverlayContext(OverlayEntry entry) => _entry = entry;

    /// <summary>Completes the dialog: closes it and hands <paramref name="value"/> to the caller of <c>ShowAsync</c>. Does nothing once the dialog has ended.</summary>
    public void Close(TResult value) => _entry.Complete(value);

    /// <summary>Dismisses the dialog: closes it and gives the caller a canceled result. Does nothing once the dialog has ended.</summary>
    public void Cancel() => _entry.Cancel();

    /// <summary>True once the dialog has been completed or canceled.</summary>
    public bool IsClosed => _entry.Completed;
}

/// <summary>
/// Opens dialogs from code and awaits how they end. Needs an interactive render mode and a <see cref="KlyOverlayHost"/>
/// (<see cref="KlyApp"/> renders one), like <see cref="IToastService"/> needs a toaster. Call it from an event handler or <c>OnAfterRenderAsync</c>, never from <c>OnInitialized</c> or while prerendering.
/// </summary>
public interface IKlyOverlayService
{
    /// <summary>
    /// Shows <typeparamref name="TComponent"/> in a dialog and completes when the dialog ends: with the value the component
    /// passed to <see cref="KlyOverlayContext{TResult}.Close"/>, or with a canceled result after Escape, a click on the backdrop,
    /// the close button, <see cref="KlyOverlayContext{TResult}.Cancel"/> or <paramref name="cancellationToken"/>.
    /// The component reads its context with <c>[CascadingParameter] KlyOverlayContext&lt;TResult&gt;</c>. A dialog with nothing to return uses <c>bool</c>.
    /// </summary>
    /// <typeparam name="TComponent">The component shown in the dialog.</typeparam>
    /// <typeparam name="TResult">The type of the value the dialog completes with.</typeparam>
    /// <param name="parameters">Parameters of the component, by name (the Blazor parameter dictionary: the component is created by the renderer, so they cannot be typed here).</param>
    /// <param name="options">Title, size and behavior of the dialog.</param>
    /// <param name="cancellationToken">Cancels the dialog (it closes and the result is canceled).</param>
    /// <exception cref="InvalidOperationException">No <see cref="KlyOverlayHost"/> is attached: the page has not rendered yet (call it from <c>OnAfterRenderAsync</c> or an event handler, not <c>OnInitialized</c>), or the render is static or a prerender.</exception>
    Task<KlyOverlayResult<TResult>> ShowAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TComponent, TResult>(
        IReadOnlyDictionary<string, object?>? parameters = null, KlyOverlayOptions? options = null, CancellationToken cancellationToken = default)
        where TComponent : IComponent;

    /// <summary>
    /// Shows <paramref name="content"/> in a dialog and completes when the dialog ends, like <see cref="ShowAsync{TComponent, TResult}"/>.
    /// A component placed in the content ends the dialog through a <c>KlyOverlayContext&lt;TResult&gt;</c> cascading parameter.
    /// </summary>
    /// <typeparam name="TResult">The type of the value the dialog completes with.</typeparam>
    /// <param name="content">What the dialog shows.</param>
    /// <param name="options">Title, size and behavior of the dialog.</param>
    /// <param name="cancellationToken">Cancels the dialog (it closes and the result is canceled).</param>
    /// <exception cref="InvalidOperationException">No <see cref="KlyOverlayHost"/> is attached, as for <see cref="ShowAsync{TComponent, TResult}"/>.</exception>
    Task<KlyOverlayResult<TResult>> ShowAsync<TResult>(
        RenderFragment content, KlyOverlayOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asks a yes or no question in a dialog and returns true when the user confirms, false when they cancel or dismiss it.
    /// </summary>
    /// <param name="title">Title of the dialog.</param>
    /// <param name="message">Text under the title.</param>
    /// <param name="confirmLabel">Label of the confirm button. Defaults to the localized "Confirm".</param>
    /// <param name="cancelLabel">Label of the cancel button. Defaults to the localized "Cancel".</param>
    /// <param name="color">Color of the confirm button; use <see cref="KlyColor.Error"/> for a destructive action. Defaults to primary.</param>
    /// <param name="cancellationToken">Cancels the dialog (it closes and the answer is false).</param>
    Task<bool> ConfirmAsync(string title, string? message = null, string? confirmLabel = null, string? cancelLabel = null,
        KlyColor? color = null, CancellationToken cancellationToken = default);
}

internal sealed class OverlayEntry(OverlayService service, RenderFragment? body, KlyOverlayOptions options, ConfirmContent? confirm)
{
    public OverlayService Service { get; } = service;
    public RenderFragment? Body { get; set; } = body;
    public KlyOverlayOptions Options { get; } = options;
    public ConfirmContent? Confirm { get; } = confirm;
    public TaskCompletionSource<OverlayOutcome> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public bool Open { get; set; } = true;
    public bool Completed => Completion.Task.IsCompleted;

    public void Complete(object? value) => Service.Complete(this, new OverlayOutcome(false, value));

    public void Cancel() => Service.Complete(this, OverlayOutcome.Dismissed);
}

internal sealed record ConfirmContent(string? ConfirmLabel, string? CancelLabel, KlyColor? Color);

internal sealed class OverlayService : IKlyOverlayService, IDisposable
{
    // Time left to the browser to close the native dialog (and return the focus) before the markup goes away.
    private static readonly TimeSpan RemovalDelay = TimeSpan.FromMilliseconds(250);

    private const string NoHostMessage =
        "No KlyOverlayHost is attached. Place <KlyApp> (it renders one) or <KlyOverlayHost /> in the layout, and call the overlay service " +
        "from interactive code once the page has rendered: an event handler or OnAfterRenderAsync, not OnInitialized (the host attaches after " +
        "the page on the first load) and not during prerendering or static rendering.";

    /// <summary>One attached host: how to run code on its renderer, and how to ask it to render again.</summary>
    private sealed record HostRegistration(object Host, Func<Action, Task> Invoke, Action Changed);

    // The list of entries is only touched through Dispatch, so on the renderer that draws it. The list of hosts is read from any thread.
    private readonly List<OverlayEntry> _entries = [];
    private readonly List<HostRegistration> _hosts = [];
    private readonly object _hostsLock = new();

    public IReadOnlyList<OverlayEntry> Entries => _entries;

    /// <summary>The newest host that is still attached draws the dialogs.</summary>
    public bool IsActive(object host)
    {
        lock (_hostsLock) return _hosts.Count > 0 && ReferenceEquals(_hosts[^1].Host, host);
    }

    private HostRegistration? Active
    {
        get { lock (_hostsLock) return _hosts.Count > 0 ? _hosts[^1] : null; }
    }

    private HostRegistration[] Hosts()
    {
        lock (_hostsLock) return [.. _hosts];
    }

    /// <summary>Called on the renderer of the host. <paramref name="invoke"/> runs code on that renderer, <paramref name="changed"/> renders the host again.</summary>
    public void Attach(object host, Func<Action, Task> invoke, Action changed)
    {
        lock (_hostsLock) _hosts.Add(new HostRegistration(host, invoke, changed));
        NotifyAll();
    }

    /// <summary>Called on the renderer of the host. The dialogs are canceled only when no other host remains to draw them.</summary>
    public void Detach(object host)
    {
        bool none;
        lock (_hostsLock)
        {
            _hosts.RemoveAll(h => ReferenceEquals(h.Host, host));
            none = _hosts.Count == 0;
        }
        if (none) CancelAll();
        else NotifyAll();
    }

    private void NotifyAll()
    {
        foreach (var host in Hosts()) host.Changed();
    }

    /// <summary>Runs <paramref name="action"/> on the renderer of the active host, so the entries are never changed while it renders.</summary>
    private void Dispatch(Action action)
    {
        if (Active is { } host) _ = host.Invoke(action);
        else action();
    }

    public Task<KlyOverlayResult<TResult>> ShowAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TComponent, TResult>(
        IReadOnlyDictionary<string, object?>? parameters = null, KlyOverlayOptions? options = null, CancellationToken cancellationToken = default)
        where TComponent : IComponent =>
        ShowAsync<TResult>(builder => RenderComponent<TComponent>(builder, parameters), options, cancellationToken);

    public async Task<KlyOverlayResult<TResult>> ShowAsync<TResult>(
        RenderFragment content, KlyOverlayOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        var entry = new OverlayEntry(this, null, options ?? new KlyOverlayOptions(), null);
        // The context is cascaded here, where TResult is known: a component asks for exactly KlyOverlayContext<TResult>.
        var context = new KlyOverlayContext<TResult>(entry);
        entry.Body = builder =>
        {
            builder.OpenComponent<CascadingValue<KlyOverlayContext<TResult>>>(0);
            builder.AddComponentParameter(1, "Value", context);
            builder.AddComponentParameter(2, "IsFixed", true);
            builder.AddComponentParameter(3, "ChildContent", content);
            builder.CloseComponent();
        };
        var outcome = await Open(entry, cancellationToken);
        return outcome.Canceled
            ? new KlyOverlayResult<TResult>(true, default)
            : new KlyOverlayResult<TResult>(false, outcome.Value is TResult value ? value : default);
    }

    public async Task<bool> ConfirmAsync(string title, string? message = null, string? confirmLabel = null, string? cancelLabel = null,
        KlyColor? color = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(title);
        var options = new KlyOverlayOptions { Title = title, Description = message };
        var result = await Open(new OverlayEntry(this, null, options, new ConfirmContent(confirmLabel, cancelLabel, color)), cancellationToken);
        return !result.Canceled && result.Value is true;
    }

    private static void RenderComponent<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TComponent>(
        RenderTreeBuilder builder, IReadOnlyDictionary<string, object?>? parameters) where TComponent : IComponent
    {
        builder.OpenComponent<TComponent>(0);
        if (parameters is not null)
        {
            foreach (var (name, value) in parameters) builder.AddComponentParameter(1, name, value);
        }
        builder.CloseComponent();
    }

    private Task<OverlayOutcome> Open(OverlayEntry entry, CancellationToken cancellationToken)
    {
        if (Active is not { } host) throw new InvalidOperationException(NoHostMessage);
        if (cancellationToken.IsCancellationRequested) return Task.FromResult(OverlayOutcome.Dismissed);

        if (cancellationToken.CanBeCanceled)
        {
            var registration = cancellationToken.Register(() => Complete(entry, OverlayOutcome.Dismissed));
            entry.Completion.Task.ContinueWith(_ => registration.Dispose(), TaskScheduler.Default);
        }
        // Possibly called from another thread than the renderer's: the list changes where the host renders.
        _ = host.Invoke(() =>
        {
            if (entry.Completed) return;
            if (Active is null)
            {
                // The last host went away between the call and now.
                entry.Completion.TrySetResult(OverlayOutcome.Dismissed);
                return;
            }
            _entries.Add(entry);
            NotifyAll();
        });
        return entry.Completion.Task;
    }

    /// <summary>Ends the entry: the caller gets the result now and the dialog is told to close.</summary>
    public void Complete(OverlayEntry entry, OverlayOutcome result)
    {
        if (!entry.Completion.TrySetResult(result)) return;
        Dispatch(() =>
        {
            entry.Open = false;
            NotifyAll();
            _ = RemoveLaterAsync(entry);
        });
    }

    /// <summary>The dialog is already closed in the browser (Escape, backdrop, close button): complete as canceled and drop it.</summary>
    public void Dismissed(OverlayEntry entry)
    {
        entry.Completion.TrySetResult(OverlayOutcome.Dismissed);
        entry.Open = false;
        Remove(entry);
    }

    /// <summary>The user went to another page: every open dialog ends as canceled.</summary>
    public void CancelForNavigation()
    {
        foreach (var entry in _entries.ToArray()) Complete(entry, OverlayOutcome.Dismissed);
    }

    private async Task RemoveLaterAsync(OverlayEntry entry)
    {
        await Task.Delay(RemovalDelay).ConfigureAwait(false);
        // Back on the renderer of the host: the list is not ours to change from a pool thread.
        Dispatch(() => Remove(entry));
    }

    private void Remove(OverlayEntry entry)
    {
        if (_entries.Remove(entry)) NotifyAll();
    }

    private void CancelAll()
    {
        foreach (var entry in _entries.ToArray()) entry.Completion.TrySetResult(OverlayOutcome.Dismissed);
        _entries.Clear();
    }

    public void Dispose()
    {
        lock (_hostsLock) _hosts.Clear();
        CancelAll();
    }
}
