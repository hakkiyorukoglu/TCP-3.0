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
        bool shift = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
        switch (key)
        {
            case Key.Escape: return Send(ToolKey.Escape);
            case Key.Enter:  return Send(ToolKey.Enter);
            case Key.Delete: return Send(ToolKey.Delete);
            case Key.C when ctrl: return Send(ToolKey.Copy);
            case Key.X when ctrl: return Send(ToolKey.Cut);
            case Key.V when ctrl: return Send(ToolKey.Paste);
            
            // --- GEÇİCİ KATMAN TEST TUŞLARI ---
            case Key.D1 or Key.NumPad1: return ToggleLayer(0, shift); // Zemin
            case Key.D2 or Key.NumPad2: return ToggleLayer(1, shift); // Alt Kat
            case Key.D3 or Key.NumPad3: return ToggleLayer(2, shift); // Üst Kat
            
            default: return false;
        }
    }

    public Action<string>? LayerStatusChanged;

    private bool ToggleLayer(int displayOrder, bool shift)
    {
        var doc = _ctxBase.Document;
        foreach (var l in doc.Layers)
        {
            if (l.DisplayOrder == displayOrder)
            {
                if (shift)
                {
                    doc.SetLayerLock(l.Id, !l.IsLocked);
                    LayerStatusChanged?.Invoke($"Katman: {l.Name} - Kilit: {(!l.IsLocked ? "AÇIK" : "KİLİTLİ")}");
                }
                else
                {
                    doc.SetLayerVisibility(l.Id, !l.IsVisible);
                    LayerStatusChanged?.Invoke($"Katman: {l.Name} - Görünürlük: {(!l.IsVisible ? "GİZLİ" : "GÖRÜNÜR")}");
                }
                return true;
            }
        }
        return false;
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
