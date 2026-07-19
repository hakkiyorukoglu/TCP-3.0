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
    
    public ToolController? ToolController { get; set; }
    public TrainService.Cad.UndoRedo.CommandStack? CommandStack { get; set; }
    
    public ViewportTransform Transform { get; } = new ViewportTransform();
    
    // FPS Bilgisini UI'a aktarmak için event
    public event Action<int>? FpsUpdated;
    
    private Point _lastMousePos;
    private bool _isPanning;
    
    // FPS Ölçümü için
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
        var c = (Application.Current?.TryFindResource("SystemAccentColor") as Color?) 
                ?? Color.FromRgb(0xFF, 0xB9, 0x00);                          // fallback #FFB900
        var b = new SolidColorBrush(c); b.Freeze(); return b;
    }
    
    private static Pen CreateFrozenPen(Brush b, double thickness)
    {
        var p = new Pen(b, thickness); p.Freeze(); return p;
    }

    private static readonly Brush MarkerBrush = CreateFrozenAccentBrush();
    private static readonly Pen   MarkerPen   = CreateFrozenPen(MarkerBrush, 1.5);

    private static readonly Pen PreviewValidPen = CreateFrozenPreviewPen(MarkerBrush, false);
    private static readonly Pen PreviewInvalidPen = CreateFrozenPreviewPen(Brushes.Red, true);

    private static Pen CreateFrozenPreviewPen(Brush b, bool isInvalid)
    {
        var p = new Pen(b, 1.5);
        p.DashStyle = new DashStyle(new double[] { 4, 4 }, 0);
        if (isInvalid) p.Brush = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));
        p.Freeze();
        return p;
    }

    public CadViewportControl()
    {
        var rootGrid = new Grid();
        rootGrid.Background = Brushes.Transparent;
        
        _gridLayer = new CadRenderLayer();
        _modelLayer = new CadRenderLayer();
        _toolLayer = new CadRenderLayer();
        
        _gridVisual = new DrawingVisual();
        _gridLayer.AddVisual(_gridVisual);
        
        _modelVisual = new DrawingVisual();
        _modelLayer.AddVisual(_modelVisual);

        _toolVisual = new DrawingVisual();
        _toolLayer.AddVisual(_toolVisual);

        rootGrid.Children.Add(_gridLayer);
        rootGrid.Children.Add(_modelLayer); // Model, grid'in üstünde
        rootGrid.Children.Add(_toolLayer);  // Tool her şeyin üstünde
        
        this.Content = rootGrid;
        
        this.ClipToBounds = true;
        this.Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));

        this.PreviewMouseWheel += OnMouseWheel;
        this.MouseDown += OnMouseDown;
        this.MouseUp += OnMouseUp;
        this.MouseMove += OnMouseMove;
        this.MouseLeave += OnMouseLeave;
        this.PreviewKeyDown += OnPreviewKeyDown;
        this.SizeChanged += (s, e) => RequestRender();
        this.Focusable = true;
        
        _fpsTimer.Start();
        // Gerçek UI FPS sayacı (Ekranın donanım seviyesinde çizilme hızı)
        CompositionTarget.Rendering += OnCompositionTargetRendering;
    }
    
    private void OnCompositionTargetRendering(object? sender, EventArgs e)
    {
        _frameCount++;
        if (_fpsTimer.ElapsedMilliseconds >= 1000)
        {
            int currentFps = _frameCount;
            FpsUpdated?.Invoke(currentFps);
            _frameCount = 0;
            _fpsTimer.Restart();
        }
    }
    
    public void AttachDocument(CadDocument doc)
    {
        if (_document != null)
        {
            _document.Changed -= OnDocumentChanged;
        }
        
        _document = doc;
        _document.Changed += OnDocumentChanged;
        
        RenderModelBake();
        RequestRender();
    }

    private void OnDocumentChanged(object? sender, DocumentChangedEventArgs e)
    {
        if (e.Kind == DocumentChangeKind.GridChanged)
        {
            RequestRender();
            return;
        }

        // Gelişmiş versiyonlarda kirli-bölge (DirtyRegion) okunur. Şimdilik tam yeniden çizim.
        RenderModelBake();
        RequestRender();
    }

    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        Point mousePos = e.GetPosition(this);
        double factor = e.Delta > 0 ? 1.15 : 1 / 1.15;
        
        Transform.ZoomAt(mousePos, factor);
        
        if (ToolController != null)
        {
            _lastSnap = ToolController.PointerMove(mousePos, 25.0);
            RenderToolLayer(_lastSnap);
        }
        
        RequestRender();
        e.Handled = true;
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        // Klavye odağını viewporte çek — Delete tuşu çalışsın
        this.Focus();

        if (e.MiddleButton == MouseButtonState.Pressed)
        {
            _isPanning = true;
            _lastMousePos = e.GetPosition(this);
            this.CaptureMouse();
        }
        else if (e.RightButton == MouseButtonState.Pressed)
        {
            // Sağ tık → bağlama duyarlı radyal menü
            var screenPos = this.PointToScreen(e.GetPosition(this));
            var items = BuildRadialMenuItems(e.GetPosition(this));
            _radialMenu.ShowAt(screenPos, items);
            e.Handled = true;
        }
        else if (ToolController != null && _lastSnap != null)
        {
            ToolController.PointerDown(_lastSnap, e.ChangedButton);
        }
    }

    private SnapResult? _lastSnap;

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (ToolController != null)
        {
            bool handled = ToolController.KeyDown(e.Key);
            if (handled) e.Handled = true;
        }
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Middle && _isPanning)
        {
            _isPanning = false;
            this.ReleaseMouseCapture();
            return;
        }
        if (!_isPanning && ToolController != null && _lastSnap != null)
        {
            ToolController.PointerUp(_lastSnap, e.ChangedButton);
            RenderModelBake();
            RequestRender();
        }
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        Point currentPos = e.GetPosition(this);
        
        if (_isPanning)
        {
            Vector2D deltaWorld = new Vector2D(
                (currentPos.X - _lastMousePos.X) / Transform.Scale,
                (currentPos.Y - _lastMousePos.Y) / Transform.Scale
            );
            
            Transform.PanOffset = new Vector2D(
                Transform.PanOffset.X - deltaWorld.X,
                Transform.PanOffset.Y - deltaWorld.Y
            );
            
            _lastMousePos = currentPos;
            RequestRender(); // Her fare hareketinde Render
        }
        else if (ToolController != null)
        {
            _lastSnap = ToolController.PointerMove(currentPos, 25.0);
            // Hover hit-test (sadece fare sol butonu basılı değilken)
            if (e.LeftButton == MouseButtonState.Released && _document != null)
            {
                var world = Transform.ScreenToWorld(currentPos);
                var tolWorld = SnapEngine.ScreenToleranceToWorld(8.0, Transform.Scale);
                var box = BoundingBox.FromPoint(world, tolWorld);
                var buf = new List<Guid>(16);
                _document.QueryRegion(box, buf);
                Guid newHover = Guid.Empty;
                double bestSq = tolWorld * tolWorld;
                foreach (var id in buf)
                {
                    if (!_document.TryGetEntity(id, out var ent)) continue;
                    if (!_document.IsSelectable(id)) continue;
                    double dSq = EntityDistSq(ent, world);
                    if (dSq <= bestSq) { bestSq = dSq; newHover = id; }
                }
                if (newHover != _hoveredId)
                {
                    _hoveredId = newHover;
                    RenderModelBake();
                }
            }
            RenderToolLayer(_lastSnap);
        }
    }

    private void OnMouseLeave(object sender, MouseEventArgs e)
    {
        using var dc = _toolVisual.RenderOpen(); // clear snap marker
    }

    private void CizOk(DrawingContext dc, Point p1, Point p2, TrainService.Core.Enums.TravelDirection dir, double scale)
    {
        if (p1 == p2) return;
        var vec = dir == TrainService.Core.Enums.TravelDirection.Forward ? new Vector(p2.X - p1.X, p2.Y - p1.Y) : new Vector(p1.X - p2.X, p1.Y - p2.Y);
        vec.Normalize();
        var mid = new Point((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2);
        
        double s = 10.0 / scale; // ~10px ekran sabit
        var tip = new Point(mid.X + vec.X * s, mid.Y + vec.Y * s);
        var baseCenter = new Point(mid.X - vec.X * s, mid.Y - vec.Y * s);
        var ortho = new Vector(-vec.Y, vec.X);
        var left = new Point(baseCenter.X + ortho.X * (s * 0.8), baseCenter.Y + ortho.Y * (s * 0.8));
        var right = new Point(baseCenter.X - ortho.X * (s * 0.8), baseCenter.Y - ortho.Y * (s * 0.8));
        
        var sg = new StreamGeometry();
        using (var sgc = sg.Open())
        {
            sgc.BeginFigure(tip, true, true);
            sgc.LineTo(left, true, false);
            sgc.LineTo(right, true, false);
        }
        sg.Freeze();
        dc.DrawGeometry(CadColors.RouteArrowBrush, null, sg);
    }

    private void CizOkEkrana(DrawingContext dc, Point p1, Point p2, TrainService.Core.Enums.TravelDirection dir)
    {
        if (p1 == p2) return;
        var vec = dir == TrainService.Core.Enums.TravelDirection.Forward ? new Vector(p2.X - p1.X, p2.Y - p1.Y) : new Vector(p1.X - p2.X, p1.Y - p2.Y);
        vec.Normalize();
        var mid = new Point((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2);
        
        double s = 10.0;
        var tip = new Point(mid.X + vec.X * s, mid.Y + vec.Y * s);
        var baseCenter = new Point(mid.X - vec.X * s, mid.Y - vec.Y * s);
        var ortho = new Vector(-vec.Y, vec.X);
        var left = new Point(baseCenter.X + ortho.X * (s * 0.8), baseCenter.Y + ortho.Y * (s * 0.8));
        var right = new Point(baseCenter.X - ortho.X * (s * 0.8), baseCenter.Y - ortho.Y * (s * 0.8));
        
        var sg = new StreamGeometry();
        using (var sgc = sg.Open())
        {
            sgc.BeginFigure(tip, true, true);
            sgc.LineTo(left, true, false);
            sgc.LineTo(right, true, false);
        }
        sg.Freeze();
        dc.DrawGeometry(CadColors.RouteArrowBrush, null, sg);
    }

    private void RenderToolLayer(SnapResult r)
    {
        using var dc = _toolVisual.RenderOpen();
        
        // 1. Draw Preview Shape
        if (ToolController?.ActiveTool?.Preview is PreviewLine line)
        {
            Point p1 = Transform.WorldToScreen(line.From);
            Point p2 = Transform.WorldToScreen(line.To);
            Pen pen = line.IsValid ? PreviewValidPen : PreviewInvalidPen;
            dc.DrawLine(pen, p1, p2);
        }
        else if (ToolController?.ActiveTool?.Preview is PreviewRectangle rect)
        {
            Point p1 = Transform.WorldToScreen(rect.From);
            Point p2 = Transform.WorldToScreen(rect.To);
            var r2 = new Rect(p1, p2);
            if (rect.IsCrossing)
            {
                dc.DrawRectangle(CadColors.CrossingFill, CadColors.CrossingPen, r2);  // yeşil kesikli
            }
            else
            {
                dc.DrawRectangle(CadColors.WindowFill, CadColors.WindowPen, r2);      // mavi düz
            }
        }
        else if (ToolController?.ActiveTool?.Preview is PreviewRoute route)
        {
            foreach (var st in route.Steps)
            {
                if (_document != null && _document.TryGetEntity(st.SegmentId, out var se) && se is TrackSegment seg &&
                    _document.TryGetEntity(seg.StartNodeId, out var ea) && ea is TrackNode a &&
                    _document.TryGetEntity(seg.EndNodeId, out var eb) && eb is TrackNode b)
                {
                    var p1 = Transform.WorldToScreen(a.Position);
                    var p2 = Transform.WorldToScreen(b.Position);
                    dc.DrawLine(CadColors.RoutePreviewPen, p1, p2);
                    CizOkEkrana(dc, p1, p2, st.Direction);
                }
            }
            if (route.AdaySegmentId != Guid.Empty && _document != null &&
                _document.TryGetEntity(route.AdaySegmentId, out var ase) && ase is TrackSegment aseg &&
                _document.TryGetEntity(aseg.StartNodeId, out var aea) && aea is TrackNode aa &&
                _document.TryGetEntity(aseg.EndNodeId, out var aeb) && aeb is TrackNode ab)
            {
                var p1 = Transform.WorldToScreen(aa.Position);
                var p2 = Transform.WorldToScreen(ab.Position);
                var pen = route.AdayGecerli ? CadColors.RoutePreviewPen : CadColors.RouteInvalidPen;
                dc.DrawLine(pen, p1, p2);
            }
        }
        else if (ToolController?.ActiveTool?.Preview is PreviewSwitchPlace sp)
        {
            var entryPt = Transform.WorldToScreen(sp.EntryPos);
            var mainPt = Transform.WorldToScreen(sp.MainExitPos);
            var divPt = Transform.WorldToScreen(sp.DivergingExitPos);

            // Y-shaped preview: entry→main (green), entry→diverging (orange)
            dc.DrawLine(CadColors.SwitchMainPen, entryPt, mainPt);
            dc.DrawLine(CadColors.SwitchDivergingPen, entryPt, divPt);

            // 3 ghost port circles
            dc.DrawEllipse(CadColors.SwitchNodeFill, CadColors.SwitchNodePen, entryPt, 4, 4);
            dc.DrawEllipse(CadColors.SwitchNodeFill, CadColors.SwitchNodePen, mainPt, 4, 4);
            dc.DrawEllipse(CadColors.SwitchNodeFill, CadColors.SwitchNodePen, divPt, 4, 4);
        }
        else if (ToolController?.ActiveTool?.Preview is PreviewRampPlace rp)
        {
            var entryPt = Transform.WorldToScreen(rp.EntryPos);
            var exitPt = Transform.WorldToScreen(rp.ExitPos);

            // Line preview: entry→exit (orange)
            dc.DrawLine(CadColors.RampLinePen, entryPt, exitPt);

            // 2 ghost port circles
            dc.DrawEllipse(CadColors.RampNodeFill, CadColors.RampNodePen, entryPt, 4, 4);
            dc.DrawEllipse(CadColors.RampNodeFill, CadColors.RampNodePen, exitPt, 4, 4);
        }

        // 2. Draw Snap Marker
        if (r.Kind == SnapKind.None) return;
        Point p = Transform.WorldToScreen(r.Point);                          
        switch (r.Kind)
        {
            case SnapKind.Grid:
                dc.DrawEllipse(null, MarkerPen, p, 4.5, 4.5);
                dc.DrawEllipse(MarkerBrush, null, p, 1.0, 1.0);
                break;
            case SnapKind.Endpoint:
                dc.DrawRectangle(null, MarkerPen, new Rect(p.X - 4, p.Y - 4, 8, 8));
                dc.DrawRectangle(MarkerBrush, null, new Rect(p.X - 1, p.Y - 1, 2, 2));
                break;
            case SnapKind.OnSegment:
                dc.DrawLine(MarkerPen, new Point(p.X, p.Y - 5), new Point(p.X + 5, p.Y));
                dc.DrawLine(MarkerPen, new Point(p.X + 5, p.Y), new Point(p.X, p.Y + 5));
                dc.DrawLine(MarkerPen, new Point(p.X, p.Y + 5), new Point(p.X - 5, p.Y));
                dc.DrawLine(MarkerPen, new Point(p.X - 5, p.Y), new Point(p.X, p.Y - 5));
                dc.DrawEllipse(MarkerBrush, null, p, 1.0, 1.0);
                break;
        }
    }

    private bool _isRenderQueued;

    public void RequestRender()
    {
        if (_isRenderQueued) return; // Zaten render kuyruğunda bekliyorsa yeni istek atma (Debounce)
        _isRenderQueued = true;
        
        // Render işlemlerini WPF'in render önceliği kuyruğuna at (Saniyede max 60-144 kez çalışır, MouseMove şişmesini engeller)
        Dispatcher.InvokeAsync(() => 
        {
            _isRenderQueued = false;
            PerformRender();
        }, System.Windows.Threading.DispatcherPriority.Render);
    }

    private void PerformRender()
    {
        // 1. Grid dinamik olduğu için hep çizilecek
        RenderGrid();
        
        // 2. Model zaten Bake edildi, sadece MatrixTransform güncelleyerek GPU'ya kaydır diyeceğiz. (Kasmayı engeller)
        var mat = new Matrix();
        mat.Translate(-Transform.PanOffset.X, -Transform.PanOffset.Y);
        mat.Scale(Transform.Scale, Transform.Scale);
        
        _modelVisual.Transform = new MatrixTransform(mat);
    }

    private void RenderModelBake()
    {
        using var dc = _modelVisual.RenderOpen();
        if (_document == null || _document.Entities.Count == 0) return;

        var selectedIds = _selectionService?.SelectedIds;
        
        // Normal kalemler
        var normalLinePen = new Pen(Brushes.DodgerBlue, 2); normalLinePen.Freeze();
        var normalNodePen = new Pen(Brushes.Orange, 1); normalNodePen.Freeze();
        var normalNodeBrush = Brushes.Yellow;

        var streamGeometry = new StreamGeometry();
        using (var sgc = streamGeometry.Open())
        {
            foreach (var entity in _document.Entities)
            {
                if (!_document.IsVisible(entity.Id)) continue;
                if (entity is TrackSegment segment)
                {
                    if (_document.TryGetEntity(segment.StartNodeId, out var sn) && sn is TrackNode startNode &&
                        _document.TryGetEntity(segment.EndNodeId, out var en) && en is TrackNode endNode)
                    {
                        sgc.BeginFigure(new Point(startNode.Position.X, startNode.Position.Y), false, false);
                        sgc.LineTo(new Point(endNode.Position.X, endNode.Position.Y), true, false);
                    }
                }
            }
        }
        streamGeometry.Freeze();
        dc.DrawGeometry(null, normalLinePen, streamGeometry);
        
        // Düğümler — seçili/hover vurgusu uygulanır
        foreach (var entity in _document.Entities)
        {
            if (!_document.IsVisible(entity.Id)) continue;
            if (entity is TrackNode node)
            {
                bool isSelected = selectedIds?.Contains(node.Id) ?? false;
                bool isHovered  = node.Id == _hoveredId;
                bool isSwitch   = node.Role == NodeRole.SwitchNode;
                
                Pen pen = isSelected ? CadColors.SelectedPen
                        : isHovered  ? CadColors.HoverPen
                        : isSwitch   ? CadColors.SwitchMarkerPen
                        : normalNodePen;
                Brush brush = isSelected || isHovered ? Brushes.White
                            : isSwitch ? CadColors.SwitchMarkerFill
                            : normalNodeBrush;
                
                dc.DrawRectangle(brush, pen, new Rect(node.Position.X - 5, node.Position.Y - 5, 10, 10));
            }
        }
        
        // Switch/Makas prefab görseli — merkezde eşkenar dörtgen (diamond)
        foreach (var entity in _document.Entities)
        {
            if (!_document.IsVisible(entity.Id)) continue;
            if (entity is RailSwitch rs)
            {
                double cx = rs.Position.X, cy = rs.Position.Y, s = 7.0;
                var diamond = new StreamGeometry();
                using (var dgc = diamond.Open())
                {
                    dgc.BeginFigure(new Point(cx, cy - s), true, true);
                    dgc.LineTo(new Point(cx + s, cy), true, false);
                    dgc.LineTo(new Point(cx, cy + s), true, false);
                    dgc.LineTo(new Point(cx - s, cy), true, false);
                }
                diamond.Freeze();
                dc.DrawGeometry(CadColors.SwitchMarkerFill, CadColors.SwitchMarkerPen, diamond);
            }
        }

        // Ramp prefab görseli — merkezde yatay dikdörtgen
        foreach (var entity in _document.Entities)
        {
            if (!_document.IsVisible(entity.Id)) continue;
            if (entity is Ramp rmp)
            {
                double cx = rmp.Position.X, cy = rmp.Position.Y;
                double halfW = 10.0, halfH = 5.0;
                var rect = new Rect(cx - halfW, cy - halfH, halfW * 2, halfH * 2);
                dc.DrawRectangle(CadColors.RampMarkerFill, CadColors.RampMarkerPen, rect);
            }
        }

        // Segmentleri seçim/hover rengiyle çiz (seçiliyse üstüne beyaz çiz)
        foreach (var entity in _document.Entities)
        {
            if (!_document.IsVisible(entity.Id)) continue;
            if (entity is TrackSegment seg)
            {
                bool isSelected = selectedIds?.Contains(seg.Id) ?? false;
                bool isHovered  = seg.Id == _hoveredId;
                if (!isSelected && !isHovered) continue;

                if (_document.TryGetEntity(seg.StartNodeId, out var sn) && sn is TrackNode sNode &&
                    _document.TryGetEntity(seg.EndNodeId,   out var en) && en is TrackNode eNode)
                {
                    Pen vurgPen = isSelected ? CadColors.SelectedPen : CadColors.HoverPen;
                    dc.DrawLine(vurgPen,
                        new Point(sNode.Position.X, sNode.Position.Y),
                        new Point(eNode.Position.X, eNode.Position.Y));
                }
            }
        }
        // Rotasyonları ve ok çizimlerini yapalım
        foreach (var entity in _document.Entities)
        {
            if (!_document.IsVisible(entity.Id)) continue;
            if (entity is Route rota)
            {
                foreach (var st in rota.Steps)
                {
                    if (!_document.TryGetEntity(st.SegmentId, out var se) || se is not TrackSegment seg) continue;
                    if (!_document.TryGetEntity(seg.StartNodeId, out var ea) || ea is not TrackNode a) continue;
                    if (!_document.TryGetEntity(seg.EndNodeId, out var eb) || eb is not TrackNode b) continue;
                    var p1 = new Point(a.Position.X, a.Position.Y);
                    var p2 = new Point(b.Position.X, b.Position.Y);
                    
                    dc.DrawLine(CadColors.RoutePen, p1, p2);
                    CizOk(dc, p1, p2, st.Direction, Transform.Scale);
                }
            }
        }
    }

    private double EntityDistSq(CadEntity e, Vector2D p)
    {
        switch (e)
        {
            case TrackNode n:
            {
                double dx = p.X - n.Position.X, dy = p.Y - n.Position.Y;
                return dx * dx + dy * dy;
            }
            case TrackSegment s:
            {
                if (_document!.TryGetEntity(s.StartNodeId, out var sa) && sa is TrackNode a &&
                    _document.TryGetEntity(s.EndNodeId, out var sb) && sb is TrackNode b)
                    return TrainService.Core.Geometry.Vector2DMath.DistanceSquaredToSegment(p, a.Position, b.Position, out _);
                return double.MaxValue;
            }
            case Route r:
            {
                double minSq = double.MaxValue;
                foreach (var step in r.Steps)
                {
                    if (_document!.TryGetEntity(step.SegmentId, out var s) && s is TrackSegment seg &&
                        _document.TryGetEntity(seg.StartNodeId, out var sa) && sa is TrackNode a &&
                        _document.TryGetEntity(seg.EndNodeId, out var sb) && sb is TrackNode b)
                    {
                        double d = TrainService.Core.Geometry.Vector2DMath.DistanceSquaredToSegment(p, a.Position, b.Position, out _);
                        if (d < minSq) minSq = d;
                    }
                }
                return minSq;
            }
            default: return double.MaxValue;
        }
    }

    private void RenderGrid()
    {
        if (!_gridVisible)
        {
            _gridVisual.RenderOpen().Close(); // clear
            return;
        }

        using var dc = _gridVisual.RenderOpen();
        
        double baseGrid = _document?.GridSizeMm ?? 100.0;
        double currentGrid = baseGrid;
        
        while (currentGrid * Transform.Scale < 20.0)
            currentGrid *= 5.0;
            
        while (currentGrid * Transform.Scale > 200.0 && currentGrid > baseGrid)
            currentGrid /= 5.0;
        
        var gridPen = new Pen(new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), 1);
        gridPen.Freeze();
        
        double width = this.ActualWidth;
        double height = this.ActualHeight;
        
        Vector2D topLeftWorld = Transform.ScreenToWorld(new Point(0, 0));
        Vector2D bottomRightWorld = Transform.ScreenToWorld(new Point(width, height));
        
        double startX = Math.Floor(topLeftWorld.X / currentGrid) * currentGrid;
        double startY = Math.Floor(topLeftWorld.Y / currentGrid) * currentGrid;
        
        var gridGeometry = new StreamGeometry();
        using (var sgc = gridGeometry.Open())
        {
            for (double x = startX; x <= bottomRightWorld.X; x += currentGrid)
            {
                sgc.BeginFigure(Transform.WorldToScreen(new Vector2D(x, topLeftWorld.Y)), false, false);
                sgc.LineTo(Transform.WorldToScreen(new Vector2D(x, bottomRightWorld.Y)), true, false);
            }
            
            for (double y = startY; y <= bottomRightWorld.Y; y += currentGrid)
            {
                sgc.BeginFigure(Transform.WorldToScreen(new Vector2D(topLeftWorld.X, y)), false, false);
                sgc.LineTo(Transform.WorldToScreen(new Vector2D(bottomRightWorld.X, y)), true, false);
            }
        }
        gridGeometry.Freeze();
        dc.DrawGeometry(null, gridPen, gridGeometry);
        
        // Eksenler (0,0 Origin) - Kırmızı X, Yeşil Y
        var originScreen = Transform.WorldToScreen(new Vector2D(0, 0));
        var originPenX = new Pen(Brushes.Red, 2); originPenX.Freeze();
        var originPenY = new Pen(Brushes.LimeGreen, 2); originPenY.Freeze();
        
        var xEndScreen = Transform.WorldToScreen(new Vector2D(1000, 0));
        dc.DrawLine(originPenX, originScreen, xEndScreen);
        
        var yEndScreen = Transform.WorldToScreen(new Vector2D(0, 1000));
        dc.DrawLine(originPenY, originScreen, yEndScreen);
    }

    /// <summary>
    /// Builds context-sensitive radial menu items based on what is under the cursor.
    /// </summary>
    private IReadOnlyList<RadialMenuItem> BuildRadialMenuItems(Point screenPos)
    {
        var items = new List<RadialMenuItem>(8);

        // Hit-test: find entity under cursor
        Guid hitId = Guid.Empty;
        string? hitType = null;
        if (_document != null)
        {
            var world = Transform.ScreenToWorld(screenPos);
            var tolWorld = SnapEngine.ScreenToleranceToWorld(8.0, Transform.Scale);
            var box = BoundingBox.FromPoint(world, tolWorld);
            var buf = new List<Guid>(16);
            _document.QueryRegion(box, buf);
            double bestSq = tolWorld * tolWorld;
            foreach (var id in buf)
            {
                if (!_document.TryGetEntity(id, out var ent)) continue;
                if (!_document.IsSelectable(id)) continue;
                double dSq = EntityDistSq(ent, world);
                if (dSq <= bestSq) { bestSq = dSq; hitId = id; hitType = ent.GetType().Name; }
            }
        }

        if (hitId != Guid.Empty && hitType != null)
        {
            // Entity context menu
            items.Add(new RadialMenuItem("Seç", "🔍", () =>
            {
                if (_selectionService != null)
                {
                    _selectionService.Clear();
                    _selectionService.Add(hitId);
                }
            }));

            items.Add(new RadialMenuItem("Yakınlaştır", "🔎", () =>
            {
                if (_document != null)
                    ZoomToEntity(hitId, _document);
            }));

            items.Add(new RadialMenuItem("Sil", "🗑️", () =>
            {
                if (_document != null && CommandStack != null)
                {
                    var cmd = new TrainService.Cad.UndoRedo.DeleteEntitiesCommand(new[] { hitId });
                    CommandStack.Do(cmd, _document);
                    RenderModelBake();
                    RequestRender();
                }
            }));

            // Entity-type specific items
            if (hitType == nameof(TrackNode))
            {
                items.Add(new RadialMenuItem("Düğüm Özellikleri", "⚙️", () =>
                {
                    // Placeholder — v3.0.30+ özellik paneli
                }));
            }
            else if (hitType == nameof(TrackSegment))
            {
                items.Add(new RadialMenuItem("Ray Özellikleri", "📏", () =>
                {
                    // Placeholder — v3.0.30+ özellik paneli
                }));
            }
        }
        else
        {
            // Empty space context menu
            items.Add(new RadialMenuItem("Geri Al", "↩️", () =>
            {
                if (CommandStack?.CanUndo == true && _document != null)
                {
                    CommandStack.Undo(_document);
                    RenderModelBake(); RequestRender();
                }
            }));
            items.Add(new RadialMenuItem("Yenile", "↪️", () =>
            {
                if (CommandStack?.CanRedo == true && _document != null)
                {
                    CommandStack.Redo(_document);
                    RenderModelBake(); RequestRender();
                }
            }));
            items.Add(new RadialMenuItem("Seç (Pencere)", "🔲", () =>
            {
                if (ToolController != null)
                    ToolController.SetTool(new SelectTool());
            }));

            items.Add(new RadialMenuItem("Ray Çiz", "📐", () =>
            {
                if (ToolController != null)
                    ToolController.SetTool(new TrackTool());
            }));

            items.Add(new RadialMenuItem("Rota Çiz", "🗺️", () =>
            {
                if (ToolController != null)
                    ToolController.SetTool(new RouteTool());
            }));

            items.Add(new RadialMenuItem("Makas Yerleştir", "🔀", () =>
            {
                if (ToolController != null)
                    ToolController.SetTool(new SwitchTool());
            }));
        }

        return items;
    }

    /// <summary>
    /// Feature Tree'den cift tiklandiginda entity'e zoom yapar.
    /// Entity'nin bounding box'ini viewport'a sigdirir.
    /// </summary>
    /// <summary>
    /// Tüm entity'leri viewport'a sığdır (Zoom Extents).
    /// </summary>
    public void ZoomExtents()
    {
        if (_document == null || _document.Entities.Count == 0)
        {
            // Hiç entity yoksa, origin'e zoom yap
            Transform.Scale = 1.0;
            Transform.PanOffset = new Vector2D(0, 0);
            RenderModelBake();
            RequestRender();
            return;
        }

        double minX = double.MaxValue, minY = double.MaxValue;
        double maxX = double.MinValue, maxY = double.MinValue;
        bool hasAny = false;

        foreach (var entity in _document.Entities)
        {
            if (entity is TrackNode node)
            {
                minX = Math.Min(minX, node.Position.X);
                minY = Math.Min(minY, node.Position.Y);
                maxX = Math.Max(maxX, node.Position.X);
                maxY = Math.Max(maxY, node.Position.Y);
                hasAny = true;
            }
            else if (entity is RailSwitch sw)
            {
                minX = Math.Min(minX, sw.Position.X);
                minY = Math.Min(minY, sw.Position.Y);
                maxX = Math.Max(maxX, sw.Position.X);
                maxY = Math.Max(maxY, sw.Position.Y);
                hasAny = true;
            }
            else if (entity is Ramp rmp)
            {
                minX = Math.Min(minX, rmp.Position.X);
                minY = Math.Min(minY, rmp.Position.Y);
                maxX = Math.Max(maxX, rmp.Position.X);
                maxY = Math.Max(maxY, rmp.Position.Y);
                hasAny = true;
            }
            else if (entity is TrackSegment seg)
            {
                if (_document.TryGetEntity(seg.StartNodeId, out var sn) && sn is TrackNode a &&
                    _document.TryGetEntity(seg.EndNodeId, out var en) && en is TrackNode b)
                {
                    minX = Math.Min(minX, Math.Min(a.Position.X, b.Position.X));
                    minY = Math.Min(minY, Math.Min(a.Position.Y, b.Position.Y));
                    maxX = Math.Max(maxX, Math.Max(a.Position.X, b.Position.X));
                    maxY = Math.Max(maxY, Math.Max(a.Position.Y, b.Position.Y));
                    hasAny = true;
                }
            }
        }

        if (!hasAny)
        {
            Transform.Scale = 1.0;
            Transform.PanOffset = new Vector2D(0, 0);
            RequestRender();
            return;
        }

        double halfW = (maxX - minX) / 2 + 50;
        double halfH = (maxY - minY) / 2 + 50;
        double centerX = (minX + maxX) / 2;
        double centerY = (minY + maxY) / 2;

        double vpW = ActualWidth;
        double vpH = ActualHeight;
        if (vpW < 1 || vpH < 1) return;

        double margin = 0.85;
        double newScale = Math.Min(vpW * margin / (2 * halfW), vpH * margin / (2 * halfH));
        newScale = Math.Clamp(newScale, 0.01, 100.0);

        Transform.PanOffset = new Vector2D(
            centerX - vpW / (2 * newScale),
            centerY - vpH / (2 * newScale));
        Transform.Scale = newScale;

        RenderModelBake();
        RequestRender();
    }

    /// <summary>
    /// Pencere yakınlaştırma (MVP: merkezden 1.5x zoom-in).
    /// </summary>
    public void ZoomWindow()
    {
        Point center = new Point(ActualWidth / 2, ActualHeight / 2);
        Transform.ZoomAt(center, 1.5);

        if (ToolController != null && _lastSnap != null)
        {
            _lastSnap = ToolController.PointerMove(center, 25.0);
            RenderToolLayer(_lastSnap);
        }

        RequestRender();
    }

    /// <summary>
    /// Izgara görünürlüğünü değiştir.
    /// </summary>
    public void ToggleGrid()
    {
        _gridVisible = !_gridVisible;
        RequestRender();
    }

    public void ZoomToEntity(Guid entityId, CadDocument doc)
    {
        if (!doc.TryGetEntity(entityId, out var entity)) return;

        // Entity tipine gore bounding box hesapla
        var (center, halfSize) = GetEntityBounds(entity, doc);
        if (halfSize < 1) halfSize = 50; // minimum zoom seviyesi

        // Viewport boyutlari
        double vpWidth = ActualWidth;
        double vpHeight = ActualHeight;
        if (vpWidth < 1 || vpHeight < 1) return;

        // Entity'i viewport'un %80'ine sigdiracak scale'i hesapla
        double margin = 0.8;
        double scaleX = (vpWidth * margin) / (2 * halfSize);
        double scaleY = (vpHeight * margin) / (2 * halfSize);
        double newScale = Math.Min(scaleX, scaleY);
        newScale = Math.Clamp(newScale, 0.01, 100.0);

        // Viewport merkezini entity merkezine hizala
        double screenCenterX = vpWidth / 2.0;
        double screenCenterY = vpHeight / 2.0;
        Transform.PanOffset = new Vector2D(
            center.X - screenCenterX / newScale,
            center.Y - screenCenterY / newScale
        );
        Transform.Scale = newScale;

        RenderModelBake();
        RequestRender();
    }

    private (Vector2D center, double halfSize) GetEntityBounds(CadEntity entity, CadDocument doc)
    {
        if (entity is TrackNode node)
        {
            return (node.Position, 50);
        }
        else if (entity is TrackSegment seg)
        {
            if (doc.TryGetEntity(seg.StartNodeId, out var sa) && sa is TrackNode a &&
                doc.TryGetEntity(seg.EndNodeId, out var sb) && sb is TrackNode b)
            {
                var c = new Vector2D((a.Position.X + b.Position.X) / 2, (a.Position.Y + b.Position.Y) / 2);
                double half = Math.Max(Math.Abs(b.Position.X - a.Position.X), Math.Abs(b.Position.Y - a.Position.Y)) / 2 + 50;
                return (c, half);
            }
            return (default(Vector2D), 100);
        }
        else if (entity is Route route)
        {
            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;
            bool hasAny = false;
            foreach (var step in route.Steps)
            {
                if (doc.TryGetEntity(step.SegmentId, out var se) && se is TrackSegment rs &&
                    doc.TryGetEntity(rs.StartNodeId, out var ra) && ra is TrackNode rna &&
                    doc.TryGetEntity(rs.EndNodeId, out var rb) && rb is TrackNode rnb)
                {
                    minX = Math.Min(minX, Math.Min(rna.Position.X, rnb.Position.X));
                    minY = Math.Min(minY, Math.Min(rna.Position.Y, rnb.Position.Y));
                    maxX = Math.Max(maxX, Math.Max(rna.Position.X, rnb.Position.X));
                    maxY = Math.Max(maxY, Math.Max(rna.Position.Y, rnb.Position.Y));
                    hasAny = true;
                }
            }
            if (!hasAny) return (default(Vector2D), 100);
            var c = new Vector2D((minX + maxX) / 2, (minY + maxY) / 2);
            double half = Math.Max(maxX - minX, maxY - minY) / 2 + 50;
            return (c, half);
        }
        else if (entity is RailSwitch sw)
        {
            return (sw.Position, 100);
        }
        else if (entity is Ramp ramp)
        {
            return (ramp.Position, 100);
        }
        return (default(Vector2D), 100);
    }
}
