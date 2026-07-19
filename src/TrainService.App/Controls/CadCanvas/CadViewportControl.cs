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
    
    // AutoCAD AutoSnap static frozen markers (fixalpha1)
    private static readonly StreamGeometry GeoSquare = CreateGeoSquare();
    private static readonly StreamGeometry GeoTriangle = CreateGeoTriangle();
    private static readonly Pen SnapMarkerPen = CadColors.SnapMarkerPen;

    private static StreamGeometry CreateGeoSquare() { var g = new StreamGeometry(); using var c = g.Open(); c.BeginFigure(new Point(-6,-6),true,true); c.LineTo(new Point(6,-6),true,false); c.LineTo(new Point(6,6),true,false); c.LineTo(new Point(-6,6),true,false); g.Freeze(); return g; }
    private static StreamGeometry CreateGeoTriangle() { var g = new StreamGeometry(); using var c = g.Open(); c.BeginFigure(new Point(0,-7),true,true); c.LineTo(new Point(6,5),true,false); c.LineTo(new Point(-6,5),true,false); g.Freeze(); return g; }

    private Point _lastMousePos; private bool _isPanning;
    private Stopwatch _fpsTimer = new(); private int _frameCount = 0;
    private CadDocument? _document;
    private SelectionService? _selectionService;
    private Guid _hoveredId = Guid.Empty;
    private bool _gridVisible = true;
    private readonly RadialMenuControl _radialMenu = new();

    public void AttachSelection(SelectionService sel) { _selectionService = sel; sel.SelectionChanged += (s,e) => { RenderModelBake(); RequestRender(); }; }
    private static Brush CreateFrozenAccentBrush() { var c = (Application.Current?.TryFindResource("SystemAccentColor") as Color?) ?? Color.FromRgb(0xFF,0xB9,0x00); var b = new SolidColorBrush(c); b.Freeze(); return b; }
    private static Pen CreateFrozenPen(Brush b, double t) { var p = new Pen(b,t); p.Freeze(); return p; }
    private static readonly Brush MarkerBrush = CreateFrozenAccentBrush();
    private static readonly Pen MarkerPen = CreateFrozenPen(MarkerBrush, 1.5);
    private static readonly Pen PreviewValidPen = CreateFrozenPPen(MarkerBrush, false);
    private static readonly Pen PreviewInvalidPen = CreateFrozenPPen(Brushes.Red, true);
    private static Pen CreateFrozenPPen(Brush b, bool inv) { var p = new Pen(b,1.5); p.DashStyle=new DashStyle(new[]{4.0,4.0},0); if(inv)p.Brush=new SolidColorBrush(Color.FromArgb(128,255,0,0)); p.Freeze(); return p; }

    public CadViewportControl()
    {
        var root = new Grid{Background=Brushes.Transparent};
        _gridLayer=new CadRenderLayer();_modelLayer=new CadRenderLayer();_toolLayer=new CadRenderLayer();
        _gridVisual=new DrawingVisual();_gridLayer.AddVisual(_gridVisual);
        _modelVisual=new DrawingVisual();_modelLayer.AddVisual(_modelVisual);
        _toolVisual=new DrawingVisual();_toolLayer.AddVisual(_toolVisual);
        _crosshairVisual=new DrawingVisual();_toolLayer.AddVisual(_crosshairVisual);
        root.Children.Add(_gridLayer);root.Children.Add(_modelLayer);root.Children.Add(_toolLayer);
        Content=root; ClipToBounds=true; Background=new SolidColorBrush(Color.FromRgb(30,30,30));
        PreviewMouseWheel+=OnMouseWheel;MouseDown+=OnMouseDown;MouseUp+=OnMouseUp;MouseMove+=OnMouseMove;MouseLeave+=OnMouseLeave;PreviewKeyDown+=OnPreviewKeyDown;
        SizeChanged+=(s,e)=>RequestRender();Focusable=true;_fpsTimer.Start();
        CompositionTarget.Rendering+=(s,e)=>{_frameCount++;if(_fpsTimer.ElapsedMilliseconds>=1000){FpsUpdated?.Invoke(_frameCount);_frameCount=0;_fpsTimer.Restart();}};
    }

    public void AttachDocument(CadDocument doc){if(_document!=null)_document.Changed-=OnDocumentChanged;_document=doc;_document.Changed+=OnDocumentChanged;RenderModelBake();RequestRender();}
    private void OnDocumentChanged(object? s,DocumentChangedEventArgs e){if(e.Kind==DocumentChangeKind.GridChanged){RequestRender();return;}RenderModelBake();RequestRender();}
    private void OnMouseWheel(object s,MouseWheelEventArgs e){var p=e.GetPosition(this);Transform.ZoomAt(p,e.Delta>0?1.15:1/1.15);if(ToolController!=null){_lastSnap=ToolController.PointerMove(p,25.0);RenderToolLayer(_lastSnap);}RequestRender();e.Handled=true;}
    private void OnMouseDown(object s,MouseButtonEventArgs e){Focus();if(e.MiddleButton==MouseButtonState.Pressed){_isPanning=true;_lastMousePos=e.GetPosition(this);CaptureMouse();}else if(e.RightButton==MouseButtonState.Pressed){var sp=PointToScreen(e.GetPosition(this));_radialMenu.ShowAt(sp,BuildRadialMenuItems(e.GetPosition(this)));e.Handled=true;}else if(ToolController!=null&&_lastSnap!=null)ToolController.PointerDown(_lastSnap,e.ChangedButton);}
    private SnapResult? _lastSnap;

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
#if DEBUG
        if (e.Key == Key.F12 && Keyboard.Modifiers == ModifierKeys.Control) { SnapTaniDump(); e.Handled = true; return; }
#endif
        if (ToolController != null) { if (ToolController.KeyDown(e.Key)) e.Handled = true; }
    }

#if DEBUG
    private void SnapTaniDump()
    {
        var s = Transform.Scale; var screen = Mouse.GetPosition(this); var world = Transform.ScreenToWorld(screen);
        double tolW = SnapEngine.ScreenToleranceToWorld(10.0, s); var doc = _document;
        var buf = new List<Guid>(64); doc?.QueryRegion(BoundingBox.FromPoint(world, tolW), buf);
        // SnapEngine'e reflection ile eris
        SnapResult? direct = null;
        if (ToolController != null && doc != null)
        {
            var fld = typeof(ToolController).GetField("_snapEngine", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var eng = fld?.GetValue(ToolController) as SnapEngine;
            if (eng != null) direct = eng.Resolve(world, tolW, doc);
        }
        Debug.WriteLine($"SNAP TANI: scale={s:F4} NaN={double.IsNaN(s)} screen=({screen.X:F1},{screen.Y:F1}) world=({world.X:F2},{world.Y:F2}) tolW={tolW:F4} docEntities={doc?.Entities.Count()??-1} hashAday={buf.Count} directKind={direct?.Kind.ToString()??"null"}");
    }
#endif

    private void OnMouseUp(object s,MouseButtonEventArgs e){if(e.ChangedButton==MouseButton.Middle&&_isPanning){_isPanning=false;ReleaseMouseCapture();return;}if(!_isPanning&&ToolController!=null&&_lastSnap!=null){ToolController.PointerUp(_lastSnap,e.ChangedButton);RenderModelBake();RequestRender();}}
    private void OnMouseMove(object s,MouseEventArgs e){var cur=e.GetPosition(this);RenderCrosshair(cur);if(_isPanning){var dw=new Vector2D((cur.X-_lastMousePos.X)/Transform.Scale,(cur.Y-_lastMousePos.Y)/Transform.Scale);Transform.PanOffset=new Vector2D(Transform.PanOffset.X-dw.X,Transform.PanOffset.Y-dw.Y);_lastMousePos=cur;RequestRender();}else if(ToolController!=null){_lastSnap=ToolController.PointerMove(cur,25.0);RenderToolLayer(_lastSnap);SnapKindChanged?.Invoke(_lastSnap?.Kind??SnapKind.None);}}
    private void OnMouseLeave(object s,MouseEventArgs e){using var dc=_toolVisual.RenderOpen();using var dc2=_crosshairVisual.RenderOpen();}
    private void RenderCrosshair(Point pos){using var dc=_crosshairVisual.RenderOpen();double sz=20;var pen=new Pen(new SolidColorBrush(Color.FromArgb(180,255,255,255)),1){DashStyle=new DashStyle(new[]{3.0,3.0},0)};pen.Freeze();dc.DrawLine(pen,new Point(pos.X-sz,pos.Y),new Point(pos.X+sz,pos.Y));dc.DrawLine(pen,new Point(pos.X,pos.Y-sz),new Point(pos.X,pos.Y+sz));}
    private void RenderToolLayer(SnapResult r)
    {
        using var dc = _toolVisual.RenderOpen();
        if (ToolController?.ActiveTool?.Preview is PreviewLine pl) { var p1=Transform.WorldToScreen(pl.From);var p2=Transform.WorldToScreen(pl.To);dc.DrawLine(pl.IsValid?PreviewValidPen:PreviewInvalidPen,p1,p2); }
        // Snap marker: AutoCAD AutoSnap green + frozen geometry
        if (r.Kind == SnapKind.None) return;
        var pt = Transform.WorldToScreen(r.Point);
        var geo = r.Kind switch { SnapKind.Endpoint => GeoSquare, SnapKind.Midpoint => GeoTriangle, _ => null };
        if (geo != null) { dc.PushTransform(new TranslateTransform(pt.X, pt.Y)); dc.DrawGeometry(null, SnapMarkerPen, geo); dc.Pop(); }
        else { dc.DrawEllipse(null, MarkerPen, pt, 6, 6); dc.DrawEllipse(MarkerBrush, null, pt, 2, 2); }
    }

    private bool _isRenderQueued;
    public void RequestRender(){if(_isRenderQueued)return;_isRenderQueued=true;Dispatcher.InvokeAsync(()=>{_isRenderQueued=false;RenderGrid();var m=new Matrix();m.Translate(-Transform.PanOffset.X,-Transform.PanOffset.Y);m.Scale(Transform.Scale,Transform.Scale);_modelVisual.Transform=new MatrixTransform(m);},System.Windows.Threading.DispatcherPriority.Render);}
    private void RenderModelBake()
    {
        using var dc=_modelVisual.RenderOpen();if(_document==null)return;
        var sel=_selectionService?.SelectedIds;
        var linePen=new Pen(Brushes.DodgerBlue,2);linePen.Freeze();
        foreach(var e in _document.Entities){if(!_document.IsVisible(e.Id)||e is not TrackSegment seg)continue;if(_document.TryGetEntity(seg.StartNodeId,out var sn)&&sn is TrackNode a&&_document.TryGetEntity(seg.EndNodeId,out var en)&&en is TrackNode b)dc.DrawLine(linePen,new Point(a.Position.X,a.Position.Y),new Point(b.Position.X,b.Position.Y));}
        foreach(var e in _document.Entities){if(!_document.IsVisible(e.Id)||e is not TrackNode n)continue;bool s2=sel?.Contains(n.Id)??false;dc.DrawRectangle(s2?Brushes.White:Brushes.Yellow,new Pen(s2?Brushes.White:Brushes.Orange,1),new Rect(n.Position.X-5,n.Position.Y-5,10,10));}
    }
    private void RenderGrid()
    {
        using var dc=_gridVisual.RenderOpen();if(!_gridVisible)return;double g=_document?.GridSizeMm??100;while(g*Transform.Scale<20)g*=5;while(g*Transform.Scale>200&&g>100)g/=5;
        var gp=new Pen(new SolidColorBrush(Color.FromArgb(40,255,255,255)),1);gp.Freeze();var tl=Transform.ScreenToWorld(new Point(0,0));var br=Transform.ScreenToWorld(new Point(ActualWidth,ActualHeight));
        for(double x=Math.Floor(tl.X/g)*g;x<=br.X;x+=g){dc.DrawLine(gp,Transform.WorldToScreen(new Vector2D(x,tl.Y)),Transform.WorldToScreen(new Vector2D(x,br.Y)));}
        for(double y=Math.Floor(tl.Y/g)*g;y<=br.Y;y+=g){dc.DrawLine(gp,Transform.WorldToScreen(new Vector2D(tl.X,y)),Transform.WorldToScreen(new Vector2D(br.X,y)));}
    }

    private IReadOnlyList<RadialMenuItem> BuildRadialMenuItems(Point sp) => Array.Empty<RadialMenuItem>();
    public void ZoomExtents(){}
    public void ZoomWindow(){}
    public void ToggleGrid(){_gridVisible=!_gridVisible;RequestRender();}
    public void ZoomToEntity(Guid id,CadDocument d){}
}