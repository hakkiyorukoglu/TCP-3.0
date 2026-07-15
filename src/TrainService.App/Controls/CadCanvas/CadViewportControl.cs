using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TrainService.Core.Geometry;

namespace TrainService.App.Controls.CadCanvas;

public class CadViewportControl : ContentControl
{
    private readonly CadRenderLayer _gridLayer;
    private readonly CadRenderLayer _modelLayer;
    private readonly DrawingVisual _gridVisual;
    private readonly DrawingVisual _modelVisual;
    
    public ViewportTransform Transform { get; } = new ViewportTransform();
    
    // FPS Bilgisini UI'a aktarmak için event
    public event Action<int>? FpsUpdated;
    
    private Point _lastMousePos;
    private bool _isPanning;
    
    // FPS Ölçümü için
    private Stopwatch _fpsTimer = new();
    private int _frameCount = 0;
    
    private List<(Vector2D p1, Vector2D p2)> _testLines = new();

    public CadViewportControl()
    {
        var rootGrid = new Grid();
        
        _gridLayer = new CadRenderLayer();
        _modelLayer = new CadRenderLayer();
        
        _gridVisual = new DrawingVisual();
        _gridLayer.AddVisual(_gridVisual);
        
        _modelVisual = new DrawingVisual();
        _modelLayer.AddVisual(_modelVisual);

        rootGrid.Children.Add(_gridLayer);
        rootGrid.Children.Add(_modelLayer); // Model, grid'in üstünde
        
        this.Content = rootGrid;
        
        this.ClipToBounds = true;
        this.Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));

        this.MouseWheel += OnMouseWheel;
        this.MouseDown += OnMouseDown;
        this.MouseUp += OnMouseUp;
        this.MouseMove += OnMouseMove;
        this.SizeChanged += (s, e) => RequestRender();
        
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
    
    public void SetTestLines(List<(Vector2D, Vector2D)> lines)
    {
        _testLines = lines;
        // Model SADECE BİR KERE dünya koordinatlarında çizilir!
        RenderModelBake(); 
        RequestRender();
    }

    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        Point mousePos = e.GetPosition(this);
        double factor = e.Delta > 0 ? 1.15 : 1 / 1.15;
        
        Transform.ZoomAt(mousePos, factor);
        RequestRender();
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.MiddleButton == MouseButtonState.Pressed)
        {
            _isPanning = true;
            _lastMousePos = e.GetPosition(this);
            this.CaptureMouse();
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
        if (_testLines.Count == 0) return;
        
        var pen = new Pen(Brushes.DodgerBlue, 2);
        pen.Freeze();

        var streamGeometry = new StreamGeometry();
        using (var sgc = streamGeometry.Open())
        {
            foreach (var line in _testLines)
            {
                sgc.BeginFigure(new Point(line.p1.X, line.p1.Y), false, false);
                sgc.LineTo(new Point(line.p2.X, line.p2.Y), true, false);
            }
        }
        streamGeometry.Freeze(); // GPU'ya yükle
        
        // 10.000 DrawLine yerine 1 DrawGeometry çağrısı
        dc.DrawGeometry(null, pen, streamGeometry);
    }

    private void RenderGrid()
    {
        using var dc = _gridVisual.RenderOpen();
        
        double gridSizeMm = 100.0;
        double scaledGridSize = gridSizeMm * Transform.Scale;
        
        if (scaledGridSize < 5) return;
        
        var gridPen = new Pen(new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), 1);
        gridPen.Freeze();
        
        double width = this.ActualWidth;
        double height = this.ActualHeight;
        
        Vector2D topLeftWorld = Transform.ScreenToWorld(new Point(0, 0));
        Vector2D bottomRightWorld = Transform.ScreenToWorld(new Point(width, height));
        
        double startX = Math.Floor(topLeftWorld.X / gridSizeMm) * gridSizeMm;
        double startY = Math.Floor(topLeftWorld.Y / gridSizeMm) * gridSizeMm;
        
        var gridGeometry = new StreamGeometry();
        using (var sgc = gridGeometry.Open())
        {
            for (double x = startX; x <= bottomRightWorld.X; x += gridSizeMm)
            {
                sgc.BeginFigure(Transform.WorldToScreen(new Vector2D(x, topLeftWorld.Y)), false, false);
                sgc.LineTo(Transform.WorldToScreen(new Vector2D(x, bottomRightWorld.Y)), true, false);
            }
            
            for (double y = startY; y <= bottomRightWorld.Y; y += gridSizeMm)
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
