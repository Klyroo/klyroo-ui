namespace Klyroo.UI.Maui;

/// <summary>Where the toolbar shows.</summary>
public enum EditorToolbarLayout
{
    /// <summary>Always visible, above the text.</summary>
    Fixed,

    /// <summary>Shown above the text only while some text is selected. A native editor has no floating layer to draw it on the selection, so it appears in the place of the fixed toolbar.</summary>
    Bubble,
}
