namespace TrainService.Cad.Tools;

/// <summary>
/// Selection mode for the SelectTool.
/// </summary>
public enum SelectionMode
{
    /// <summary>Left-to-right drag: only entities fully contained in the bounding box are selected.</summary>
    Window,

    /// <summary>Right-to-left drag: entities intersecting the bounding box are selected (default).</summary>
    Crossing,

    /// <summary>Freeform polygon (lasso): entities whose center falls inside the polygon are selected.</summary>
    Fence
}