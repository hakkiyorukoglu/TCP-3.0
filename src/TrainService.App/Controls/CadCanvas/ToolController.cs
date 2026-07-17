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
    private readonly ToolContext _ctxBase;
    private readonly SnapEngine _snapEngine;
    private readonly ViewportTransform _transform;
    private const double ClickTolerancePx = 6.0;

    public ITool ActiveTool { get; private set; }
    public event EventHandler? ActiveToolChanged;

    public ToolController(ToolContext ctx, SnapEngine snapEngine, ViewportTransform transform, ITool initialTool)
    {
        _ctxBase = ctx;
        _snapEngine = snapEngine;
        _transform = transform;
        ActiveTool = initialTool;
        ActiveTool.Activate(CtxWith());
    }

    public void SetTool(ITool tool)
    {
        ActiveTool.Deactivate(CtxWith());
        ActiveTool = tool;
        tool.Activate(CtxWith());
        ActiveToolChanged?.Invoke(this, EventArgs.Empty);
    }

    public SnapResult PointerMove(Point screen, double snapTolerancePx)
    {
        var world = _transform.ScreenToWorld(screen);
        var tol = SnapEngine.ScreenToleranceToWorld(snapTolerancePx, _transform.Scale);
        var lastSnap = _snapEngine.Resolve(world, tol, _ctxBase.Document);
        ActiveTool.OnPointerMove(lastSnap, CtxWith());
        return lastSnap;
    }

    public void PointerDown(SnapResult lastSnap, MouseButton button)
    {
        ToolMouseButton tb = Map(button);
        ActiveTool.OnPointerDown(lastSnap, tb, CtxWith());
    }

    public void PointerUp(SnapResult lastSnap, MouseButton button)
    {
        ToolMouseButton tb = Map(button);
        ActiveTool.OnPointerUp(lastSnap, tb, CtxWith());
    }

    public TrainService.Cad.Clipboard.ClipboardService Clipboard { get; set; } = default!;

    public bool KeyDown(Key key)
    {
        bool ctrl = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
        switch (key)
        {
            case Key.Escape: return Send(ToolKey.Escape);
            case Key.Enter:  return Send(ToolKey.Enter);
            case Key.Delete: return Send(ToolKey.Delete);
            case Key.C when ctrl: return Send(ToolKey.Copy);
            case Key.X when ctrl: return Send(ToolKey.Cut);
            case Key.V when ctrl: return Send(ToolKey.Paste);
            default: return false;
        }
    }

    private bool Send(ToolKey key) { ActiveTool.OnKeyDown(key, CtxWith()); return true; }

    private ToolContext CtxWith() => _ctxBase with
    {
        ModifierAdd = (Keyboard.Modifiers & (ModifierKeys.Shift | ModifierKeys.Control)) != 0,
        ClickToleranceWorld = SnapEngine.ScreenToleranceToWorld(ClickTolerancePx, _transform.Scale),
        Clipboard = Clipboard
    };

    private static ToolMouseButton Map(MouseButton button) => button switch
    {
        MouseButton.Left   => ToolMouseButton.Left,
        MouseButton.Right  => ToolMouseButton.Right,
        MouseButton.Middle => ToolMouseButton.Middle,
        _                  => ToolMouseButton.Left
    };
}
