using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TrainService.Core.Geometry;
using TrainService.Core.Entities;
using TrainService.Core.Enums;
using TrainService.Cad;
using TrainService.Cad.Snapping;
using TrainService.Cad.Selection;
using TrainService.Cad.Tools;
using TrainService.App.Resources;
using TrainService.App.Controls.RadialMenu;

namespace TrainService.App.Controls.CadCanvas;

public class CadViewportControl : ContentControl
{
    private readonly CadRenderLayer _gridLayer;
    private readonly CadRenderLayer _modelLayer;
    private readonly CadRenderLayer _toolLayer;
    private readonly DrawingVisual _gridVisual;
    private readonly DrawingVisual _modelVisual;
    private readonly DrawingVisual _toolVisual;
    private readonly DrawingVisual _crosshairVisual;
    private TrainService.App.Controls.CadCanvas.Adorners.GripAdorner? _gripAdorner;
    
    public ToolController? ToolController { get; set; }
    public TrainService.Cad.UndoRedo.CommandStack? CommandStack { get; set; }
    public ViewportTransform Transform { get; } = new ViewportTransform();
    public event Action<int>? FpsUpdated;
    public event Action<SnapKind>? SnapKindChanged;
    
    private Point _lastMousePos;
    private bool _isPanning;
    private Stopwatch _fpsTimer = new();
    private int _frameCount = 0;
    private CadDocument? _document;
    private SelectionService? _selectionService;
    private Guid _hoveredId = Guid.Empty;
    private bool _gridVisible = true;
    private readonly RadialMenuControl _radialMenu = new();

    public void AttachSelection(SelectionService sel)
    {
        _selectionService = sel;
        sel.SelectionChanged += (s, e) => { RenderModelBake(); RequestRender(); };
    }

    private static Brush CreateFrozenAccentBrush()
    {
        var c = (Application.Current?.TryFindResource("SystemAccentColor") as Color?) ?? Color.FromRgb(0xFF, 0xB9, 0x00);
        var b = new SolidColorBrush(c); b.Freeze(); return b;
    }

    private static Pen CreateFrozenPen(Brush b, double thickness) { var p = new Pen(b, thickness); p.Freeze(); return p; }

    private static readonly Brush MarkerBrush = CreateFrozenAccentBrush();
    private static readonly Pen MarkerPen = CreateFrozenPen(MarkerBrush, 1.5);

    public CadViewportControl()
    {
        var rootGrid = new Grid { Background = Brushes.Transparent };
        _gridLayer = new CadRenderLayer(); _modelLayer = new CadRenderLayer(); _toolLayer = new CadRenderLayer();
        _gridVisual = new DrawingVisual(); _gridLayer.AddVisual(_gridVisual);
        _modelVisual = new DrawingVisual(); _modelLayer.AddVisual(_modelVisual);
        _toolVisual = new DrawingVisual(); _toolLayer.AddVisual(_toolVisual);
        _crosshairVisual = new DrawingVisual(); _toolLayer.AddVisual(_crosshairVisual);
        rootGrid.Children.Add(_gridLayer); rootGrid.Children.Add(_modelLayer); rootGrid.Children.Add(_toolLayer);
        Content = rootGrid;
        ClipToBounds = true; Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
        PreviewMouseWheel += OnMouseWheel; MouseDown += OnMouseDown; MouseUp += OnMouseUp;
        MouseMove += OnMouseMove; MouseLeave += OnMouseLeave; PreviewKeyDown += OnPreviewKeyDown;
        SizeChanged += (s, e) => RequestRender(); Focusable = true;
        _fpsTimer.Start();
        CompositionTarget.Rendering += (s, e) => { _frameCount++; if (_fpsTimer.ElapsedMilliseconds >= 1000) { FpsUpdated?.Invoke(_frameCount); _frameCount = 0; _fpsTimer.Restart(); } };
    }

    public void AttachDocument(CadDocument doc) { if (_document != null) _document.Changed -= OnDocumentChanged; _document = doc; _document.Changed += OnDocumentChanged; RenderModelBake(); RequestRender(); }

    private void OnDocumentChanged(object? sender, DocumentChangedEventArgs e) { if (e.Kind == DocumentChangeKind.GridChanged) { RequestRender(); return; } RenderModelBake(); RequestRender(); }

    private void OnMouseWheel(object sender, MouseWheelEventArgs e) { var pos = e.GetPosition(this); Transform.ZoomAt(pos, e.Delta > 0 ? 1.15 : 1 / 1.15); if (ToolController != null) { _lastSnap = ToolController.PointerMove(pos, 25.0); RenderToolLayer(_lastSnap); } RequestRender(); e.Handled = true; }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        Focus();
        if (e.MiddleButton == MouseButtonState.Pressed) { _isPanning = true; _lastMousePos = e.GetPosition(this); CaptureMouse(); }
        else if (e.RightButton == MouseButtonState.Pressed) { var rpos = PointToScreen(e.GetPosition(this)); _radialMenu.ShowAt(rpos, BuildRadialMenuItems(e.GetPosition(this))); e.Handled = true; }
        else if (ToolController != null && _lastSnap != null) ToolController.PointerDown(_lastSnap, e.ChangedButton);
    }

    private SnapResult? _lastSnap;

    private void OnPreviewKeyDown(object sender, KeyEventArgs e) { if (ToolController != null && ToolController.KeyDown(e.Key)) e.Handled = true; }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Middle && _isPanning) { _isPanning = false; ReleaseMouseCapture(); return; }
        if (!_isPanning && ToolController != null && _lastSnap != null) { ToolController.PointerUp(_lastSnap, e.ChangedButton); RenderModelBake(); RequestRender(); }
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        var cur = e.GetPosition(this);
        RenderCrosshair(cur);
        if (_isPanning) { var dw = new Vector2D((cur.X - _lastMousePos.X) / Transform.Scale, (cur.Y - _lastMousePos.Y) / Transform.Scale); Transform.PanOffset = new Vector2D(Transform.PanOffset.X - dw.X, Transform.PanOffset.Y - dw.Y); _lastMousePos = cur; RequestRender(); }
        else if (ToolController != null) { _lastSnap = ToolController.PointerMove(cur, 25.0); RenderToolLayer(_lastSnap); SnapKindChanged?.Invoke(_lastSnap?.Kind ?? SnapKind.None); }
    }

    private void OnMouseLeave(object sender, MouseEventArgs e) { using var dc = _toolVisual.RenderOpen(); using var dc2 = _crosshairVisual.RenderOpen(); }

    private void RenderCrosshair(Point pos) { using var dc = _crosshairVisual.RenderOpen(); double s = 20; var p2 = new Pen(new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)), 1) { DashStyle = new DashStyle(new[] { 3.0, 3.0 }, 0) }; p2.Freeze(); dc.DrawLine(p2, new Point(pos.X - s, pos.Y), new Point(pos.X + s, pos.Y)); dc.DrawLine(p2, new Point(pos.X, pos.Y - s), new Point(pos.X, pos.Y + s)); dc.DrawEllipse(null, p2, pos, 2, 2); }

    private void RenderToolLayer(SnapResult r)
    {
        using var dc = _toolVisual.RenderOpen();
        // Preview shapes
        if (ToolController?.ActiveTool?.Preview is PreviewLine prevLine)
        {
            var p1 = Transform.WorldToScreen(prevLine.From); var p2 = Transform.WorldToScreen(prevLine.To);
            dc.DrawLine(prevLine.IsValid ? new Pen(MarkerBrush, 1.5) { DashStyle = new DashStyle(new[] { 4.0, 4.0 }, 0) } : new Pen(Brushes.Red, 1.5), p1, p2);
        }

        // Snap marker
        if (r.Kind == SnapKind.None) return;
        Point pt = Transform.WorldToScreen(r.Point);
        double sz = Math.Max(Transform.Scale * 8, 6);
        double hsz = sz / 2;

        // Tracking line
        if (ToolController?.ActiveTool?.Preview is PreviewLine trackPl)
        {
            var tFrom = Transform.WorldToScreen(trackPl.From);
            var tPen = new Pen(Brushes.Orange, 1) { DashStyle = new DashStyle(new[] { 4.0, 3.0 }, 0) }; tPen.Freeze();
            dc.DrawLine(tPen, tFrom, pt);
        }

        switch (r.Kind)
        {
            case SnapKind.Grid:
                dc.DrawEllipse(null, MarkerPen, pt, sz, sz);
                dc.DrawEllipse(MarkerBrush, null, pt, 2, 2);
                break;
            case SnapKind.Endpoint:
                dc.DrawRectangle(null, MarkerPen, new Rect(pt.X - hsz, pt.Y - hsz, sz, sz));
                dc.DrawRectangle(MarkerBrush, null, new Rect(pt.X - 2, pt.Y - 2, 4, 4));
                break;
            case SnapKind.Midpoint:
                var tri2 = new StreamGeometry();
                using (var ctx2 = tri2.Open()) { ctx2.BeginFigure(new Point(pt.X, pt.Y - hsz), true, true); ctx2.LineTo(new Point(pt.X + hsz * 0.85, pt.Y + hsz * 0.6), true, false); ctx2.LineTo(new Point(pt.X - hsz * 0.85, pt.Y + hsz * 0.6), true, false); }
                tri2.Freeze(); dc.DrawGeometry(null, MarkerPen, tri2); dc.DrawEllipse(MarkerBrush, null, pt, 2, 2);
                break;
            case SnapKind.OnSegment:
                dc.DrawEllipse(null, MarkerPen, pt, sz, sz);
                dc.DrawEllipse(MarkerBrush, null, pt, 2, 2);
                if (r.TargetId.HasValue && _document != null && _document.TryGetEntity(r.TargetId.Value, out var ent) && ent is TrackSegment ts && _document.TryGetEntity(ts.StartNodeId, out var sa) && sa is TrackNode a && _document.TryGetEntity(ts.EndNodeId, out var sb) && sb is TrackNode b)
                { var hp2 = new Pen(Brushes.Orange, 3); hp2.Freeze(); dc.DrawLine(hp2, Transform.WorldToScreen(a.Position), Transform.WorldToScreen(b.Position)); }
                break;
        }
    }

    private bool _isRenderQueued;
    public void RequestRender() { if (_isRenderQueued) return; _isRenderQueued = true; Dispatcher.InvokeAsync(() => { _isRenderQueued = false; PerformRender(); }, System.Windows.Threading.DispatcherPriority.Render); }

    private void PerformRender() { RenderGrid(); var m = new Matrix(); m.Translate(-Transform.PanOffset.X, -Transform.PanOffset.Y); m.Scale(Transform.Scale, Transform.Scale); _modelVisual.Transform = new MatrixTransform(m); }

    private void RenderModelBake()
    {
        using var dc = _modelVisual.RenderOpen();
        if (_document == null) return;
        var sel = _selectionService?.SelectedIds;
        var segGeo = new StreamGeometry();
        using (var ctx = segGeo.Open())
            foreach (var e in _document.Entities)
                if (_document.IsVisible(e.Id) && e is TrackSegment seg && _document.TryGetEntity(seg.StartNodeId, out var sn) && sn is TrackNode sn2 && _document.TryGetEntity(seg.EndNodeId, out var en) && en is TrackNode en2) { ctx.BeginFigure(new Point(sn2.Position.X, sn2.Position.Y), false, false); ctx.LineTo(new Point(en2.Position.X, en2.Position.Y), true, false); }
        segGeo.Freeze();
        var linePen = new Pen(Brushes.DodgerBlue, 2); linePen.Freeze();
        dc.DrawGeometry(null, linePen, segGeo);
        foreach (var e in _document.Entities) { if (!_document.IsVisible(e.Id) || e is not TrackNode n) continue; bool s = sel?.Contains(n.Id) ?? false, h = n.Id == _hoveredId, sw = n.Role == NodeRole.SwitchNode; dc.DrawRectangle(s || h ? Brushes.White : sw ? Brushes.Orange : Brushes.Yellow, s ? new Pen(Brushes.White, 2) : h ? new Pen(Brushes.Cyan, 2) : sw ? new Pen(Brushes.Orange, 2) : new Pen(Brushes.DarkOrange, 1), new Rect(n.Position.X - 5, n.Position.Y - 5, 10, 10)); }
        foreach (var e in _document.Entities) { if (!_document.IsVisible(e.Id)) continue; if (e is RailSwitch rs) { double cx = rs.Position.X, cy = rs.Position.Y; var dm = new StreamGeometry(); using (var c2 = dm.Open()) { c2.BeginFigure(new Point(cx, cy - 7), true, true); c2.LineTo(new Point(cx + 7, cy), true, false); c2.LineTo(new Point(cx, cy + 7), true, false); c2.LineTo(new Point(cx - 7, cy), true, false); } dm.Freeze(); dc.DrawGeometry(Brushes.Orange, new Pen(Brushes.DarkOrange, 1), dm); } else if (e is Ramp rmp) { dc.DrawRectangle(Brushes.Orange, new Pen(Brushes.DarkOrange, 1), new Rect(rmp.Position.X - 10, rmp.Position.Y - 5, 20, 10)); } }
    }

    private void RenderGrid()
    {
        using var dc = _gridVisual.RenderOpen();
        if (!_gridVisible) return;
        double g = _document?.GridSizeMm ?? 100;
        while (g * Transform.Scale < 20) g *= 5;
        while (g * Transform.Scale > 200 && g > 100) g /= 5;
        var gp = new Pen(new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), 1); gp.Freeze();
        var tl = Transform.ScreenToWorld(new Point(0, 0)); var br = Transform.ScreenToWorld(new Point(ActualWidth, ActualHeight));
        var gg = new StreamGeometry();
        using (var ctx = gg.Open()) { for (double x = Math.Floor(tl.X / g) * g; x <= br.X; x += g) { ctx.BeginFigure(Transform.WorldToScreen(new Vector2D(x, tl.Y)), false, false); ctx.LineTo(Transform.WorldToScreen(new Vector2D(x, br.Y)), true, false); } for (double y = Math.Floor(tl.Y / g) * g; y <= br.Y; y += g) { ctx.BeginFigure(Transform.WorldToScreen(new Vector2D(tl.X, y)), false, false); ctx.LineTo(Transform.WorldToScreen(new Vector2D(br.X, y)), true, false); } }
        gg.Freeze(); dc.DrawGeometry(null, gp, gg);
        var o = Transform.WorldToScreen(new Vector2D(0, 0)); dc.DrawLine(new Pen(Brushes.Red, 2) { }, o, Transform.WorldToScreen(new Vector2D(1000, 0))); dc.DrawLine(new Pen(Brushes.LimeGreen, 2) { }, o, Transform.WorldToScreen(new Vector2D(0, 1000)));
    }

    private IReadOnlyList<RadialMenuItem> BuildRadialMenuItems(Point sp) { return Array.Empty<RadialMenuItem>(); }

    public void ZoomExtents() { } public void ZoomWindow() { } public void ToggleGrid() { _gridVisible = !_gridVisible; RequestRender(); } public void ZoomToEntity(Guid id, CadDocument d) { }
}