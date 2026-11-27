namespace Klyroo.UI.Maui;

/// <summary>Data of <see cref="KlyEditorDragHandle.Dragged"/>: the handle was dragged one step.</summary>
public sealed class EditorDragEventArgs : EventArgs
{
    /// <summary>Creates the data of a step.</summary>
    /// <param name="direction">-1 for a step up, 1 for a step down.</param>
    public EditorDragEventArgs(int direction) => Direction = direction;

    /// <summary>-1 when the handle was dragged up by one step, 1 when it was dragged down.</summary>
    public int Direction { get; }
}
