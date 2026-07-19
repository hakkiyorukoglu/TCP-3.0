using System.Windows.Media;

namespace TrainService.App.Resources;

public static class CadColors
{
    public static readonly Brush WindowFill = Frozen(Color.FromArgb(50, 0, 128, 255));
    public static readonly Pen   WindowPen  = FrozenPen(Color.FromArgb(255, 0, 128, 255), 1, false);
    public static readonly Brush CrossingFill = Frozen(Color.FromArgb(50, 0, 200, 0));
    public static readonly Pen   CrossingPen  = FrozenPen(Color.FromArgb(255, 0, 200, 0), 1, true);
    public static readonly Pen HoverPen    = FrozenPen(Color.FromArgb(255, 0, 255, 255), 2, false);
    public static readonly Pen SelectedPen = FrozenPen(Colors.White, 2, true);
    public static readonly Pen   RoutePen        = FrozenPen(Color.FromArgb(140, 160, 32, 240), 5, false);
    public static readonly Brush RouteArrowBrush = Frozen(Color.FromArgb(220, 160, 32, 240));
    public static readonly Pen   RoutePreviewPen = FrozenPen(Color.FromArgb(140, 0, 200, 0), 5, false);
    public static readonly Pen   RouteInvalidPen = FrozenPen(Color.FromArgb(160, 220, 40, 40), 3, false);
    public static readonly Brush SwitchMarkerFill  = Frozen(Color.FromArgb(255, 220, 40, 220));
    public static readonly Pen   SwitchMarkerPen   = FrozenPen(Color.FromArgb(255, 255, 80, 255), 2, false);
    public static readonly Brush SwitchNodeFill      = Frozen(Color.FromArgb(80, 255, 200, 0));
    public static readonly Pen   SwitchNodePen       = FrozenPen(Color.FromArgb(255, 255, 200, 0), 2, false);
    public static readonly Pen   SwitchMainPen       = FrozenPen(Color.FromArgb(200, 0, 200, 0), 4, false);
    public static readonly Pen   SwitchDivergingPen  = FrozenPen(Color.FromArgb(200, 255, 128, 0), 4, false);
    public static readonly Brush RampMarkerFill  = Frozen(Color.FromArgb(255, 255, 140, 0));
    public static readonly Pen   RampMarkerPen   = FrozenPen(Color.FromArgb(255, 255, 180, 60), 2, false);
    public static readonly Brush RampNodeFill      = Frozen(Color.FromArgb(80, 255, 180, 60));
    public static readonly Pen   RampNodePen       = FrozenPen(Color.FromArgb(255, 255, 180, 60), 2, false);
    public static readonly Pen   RampLinePen       = FrozenPen(Color.FromArgb(200, 255, 140, 0), 4, false);

    // AutoCAD AutoSnap (fixalpha1) — ayrı metot ile FrozenPen çakışması önlenir
    public static readonly Pen SnapMarkerPen = SnapMarkerPenCreate();
    private static Pen SnapMarkerPenCreate() { var b = new SolidColorBrush(Color.FromArgb(255, 60, 255, 60)); b.Freeze(); var p = new Pen(b, 2.0); p.Freeze(); return p; }

    private static SolidColorBrush Frozen(Color c) { var b = new SolidColorBrush(c); b.Freeze(); return b; }
    private static Pen FrozenPen(Color c, double t, bool dashed) { var b = new SolidColorBrush(c); b.Freeze(); var p = new Pen(b, t); if(dashed){p.DashStyle=new DashStyle(new[]{4.0,3.0},0);p.DashCap=PenLineCap.Flat;}p.Freeze();return p; }
}