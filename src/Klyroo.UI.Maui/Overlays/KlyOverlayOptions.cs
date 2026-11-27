namespace Klyroo.UI.Maui;

/// <summary>Presentation of a dialog opened with <see cref="KlyOverlayHost.ShowAsync{TResult}"/> or <see cref="KlyOverlayHost.ConfirmAsync"/>.</summary>
public sealed record KlyOverlayOptions
{
    /// <summary>Title of the dialog, which also names it for assistive technology.</summary>
    public string? Title { get; init; }

    /// <summary>Text under the title.</summary>
    public string? Description { get; init; }

    /// <summary>Accessible name when there is no <see cref="Title"/>.</summary>
    public string? AccessibleName { get; init; }

    /// <summary>A centered modal (the default) or a slideover.</summary>
    public KlyOverlayKind Kind { get; init; } = KlyOverlayKind.Modal;

    /// <summary>Size of a modal, from <c>Xs</c> to <c>Xl</c>. Ignored for a slideover.</summary>
    public KlySize Size { get; init; } = KlySize.Md;

    /// <summary>Edge a slideover comes from. Ignored for a modal.</summary>
    public SlideoverSide Side { get; init; } = SlideoverSide.Right;

    /// <summary>A tap on the backdrop and the back button cancel the dialog. Turn it off to force an explicit answer.</summary>
    public bool Dismissible { get; init; } = true;

    /// <summary>Shows the close button of the header, which cancels the dialog.</summary>
    public bool Close { get; init; } = true;
}
