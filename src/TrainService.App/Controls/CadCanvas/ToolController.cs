using System;
using System.Windows;
using System.Windows.Input;
using TrainService.Cad;
using TrainService.Cad.Snapping;
using TrainService.Cad.Tools;
using TrainService.Core.Geometry;

namespace TrainService.App.Controls.CadCanvas;

public sealed class ToolController
{
    private readonly ToolContext _ctx;
    private readonly SnapEngine _snapEngine;
    private readonly ViewportTransform _transform;

    public ITool ActiveTool { get; private set; }
    public event EventHandler? ActiveToolChanged;

    public ToolController(ToolContext ctx, SnapEngine snapEngine, ViewportTransform transform, ITool initialTool)
    {
        _ctx = ctx;
        _snapEngine = snapEngine;
        _transform = transform;
        ActiveTool = initialTool;
        ActiveTool.Activate(_ctx);
    }

    public void SetTool(ITool tool)
    {
        ActiveTool.Deactivate(_ctx);
        ActiveTool = tool;
        tool.Activate(_ctx);
        ActiveToolChanged?.Invoke(this, EventArgs.Empty);
    }

    public SnapResult PointerMove(Point screen, double snapTolerancePx)
    {
        var world = _transform.ScreenToWorld(screen);
        var tol = SnapEngine.ScreenToleranceToWorld(snapTolerancePx, _transform.Scale);
        var lastSnap = _snapEngine.Resolve(world, tol, _ctx.Document);
        ActiveTool.OnPointerMove(lastSnap, _ctx);
        return lastSnap;
    }

    public void PointerDown(SnapResult lastSnap, MouseButton button)
    {
        ToolMouseButton tb = button switch
        {
            MouseButton.Left => ToolMouseButton.Left,
            MouseButton.Right => ToolMouseButton.Right,
            MouseButton.Middle => ToolMouseButton.Middle,
            _ => ToolMouseButton.Left
        };
        ActiveTool.OnPointerDown(lastSnap, tb, _ctx);
    }

    public bool KeyDown(Key key)
    {
        switch (key)
        {
            case Key.Escape:
                ActiveTool.OnKeyDown(ToolKey.Escape, _ctx);
                return true;
            case Key.Enter:
                ActiveTool.OnKeyDown(ToolKey.Enter, _ctx);
                return true;
            default:
                return false;
        }
    }
}
