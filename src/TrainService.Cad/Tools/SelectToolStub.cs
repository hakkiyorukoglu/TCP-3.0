using TrainService.Cad.Snapping;

namespace TrainService.Cad.Tools;

public sealed class SelectToolStub : ITool
{
    public string Name => "Seç";

    public PreviewShape? Preview => null;

    public void Activate(ToolContext ctx) { }

    public void Deactivate(ToolContext ctx) { }

    public void OnPointerMove(SnapResult snapped, ToolContext ctx) { }

    public void OnPointerDown(SnapResult snapped, ToolMouseButton button, ToolContext ctx) { }

    public void OnKeyDown(ToolKey key, ToolContext ctx) { }
}
