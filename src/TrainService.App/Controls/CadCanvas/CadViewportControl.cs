using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TrainService.Core.Geometry;
using TrainService.Cad;
using TrainService.Cad.Snapping;
using TrainService.Cad.Tools;

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
    
    public ViewportTransform Transform { get; } = new ViewportTransform();
    
    // FPS Bilgisini UI'a aktarmak için event
    public event Action<int>? FpsUpdated;
    
    private Point _lastMousePos;
    private bool _isPanning;
    
    // FPS Ölçümü için
    private Stopwatch _fpsTimer = new();
    private int _frameCount = 0;
    
    private CadDocument? _document;

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
        if (e.MiddleButton == MouseButtonState.Pressed)
        {
            _isPanning = true;
            _lastMousePos = e.GetPosition(this);
            this.CaptureMouse();
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
        if (e.MiddleButton == MouseButtonState.Released && _isPanning)
        {
            _isPanning = false;
            this.ReleaseMouseCapture();
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
            RenderToolLayer(_lastSnap);
        }
    }

    private void OnMouseLeave(object sender, MouseEventArgs e)
    {
        using var dc = _toolVisual.RenderOpen(); // clear snap marker
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
        
        var linePen = new Pen(Brushes.DodgerBlue, 2);
        linePen.Freeze();
        
        var nodePen = new Pen(Brushes.Orange, 1);
        nodePen.Freeze();
        var nodeBrush = Brushes.Yellow;

        var streamGeometry = new StreamGeometry();
        using (var sgc = streamGeometry.Open())
        {
            foreach (var entity in _document.Entities)
            {
                if (entity is TrainService.Core.Entities.TrackSegment segment)
                {
                    if (_document.TryGetEntity(segment.StartNodeId, out var sn) && sn is TrainService.Core.Entities.TrackNode startNode &&
                        _document.TryGetEntity(segment.EndNodeId, out var en) && en is TrainService.Core.Entities.TrackNode endNode)
                    {
                        sgc.BeginFigure(new Point(startNode.Position.X, startNode.Position.Y), false, false);
                        sgc.LineTo(new Point(endNode.Position.X, endNode.Position.Y), true, false);
                    }
                }
            }
        }
        streamGeometry.Freeze();
        
        dc.DrawGeometry(null, linePen, streamGeometry);
        
        // Düğümleri (TrackNode) kare olarak çiz
        foreach (var entity in _document.Entities)
        {
            if (entity is TrainService.Core.Entities.TrackNode node)
            {
                dc.DrawRectangle(nodeBrush, nodePen, new Rect(node.Position.X - 5, node.Position.Y - 5, 10, 10));
            }
        }
    }

    private void RenderGrid()
    {
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
}
