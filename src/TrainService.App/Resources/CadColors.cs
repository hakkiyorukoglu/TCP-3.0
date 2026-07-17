using System.Windows.Media;

namespace TrainService.App.Resources;

/// <summary>
/// Merkezi AutoCAD renk paleti. Tüm render kodu buradan beslenir — inline Color.FromArgb YASAK.
/// </summary>
public static class CadColors
{
    // --- Marquee: soldan-sağa = Window (mavi) ---
    public static readonly Brush WindowFill = Frozen(Color.FromArgb(50, 0, 128, 255));
    public static readonly Pen   WindowPen  = FrozenPen(Color.FromArgb(255, 0, 128, 255), 1, dashed: false);

    // --- Marquee: sağdan-sola = Crossing (yeşil) ---
    public static readonly Brush CrossingFill = Frozen(Color.FromArgb(50, 0, 200, 0));
    public static readonly Pen   CrossingPen  = FrozenPen(Color.FromArgb(255, 0, 200, 0), 1, dashed: true);

    // --- Nesne vurguları ---
    public static readonly Pen HoverPen    = FrozenPen(Color.FromArgb(255, 0, 255, 255), 2, dashed: false); // cyan
    public static readonly Pen SelectedPen = FrozenPen(Colors.White, 2, dashed: true);

    // --- Route ---
    public static readonly Pen   RoutePen        = FrozenPen(Color.FromArgb(140, 160, 32, 240), 5, dashed: false);  // mor, yarı saydam, kalın
    public static readonly Brush RouteArrowBrush = Frozen(Color.FromArgb(220, 160, 32, 240));
    public static readonly Pen   RoutePreviewPen = FrozenPen(Color.FromArgb(140, 0, 200, 0), 5, dashed: false);     // önizleme yeşil
    public static readonly Pen   RouteInvalidPen = FrozenPen(Color.FromArgb(160, 220, 40, 40), 3, dashed: false);   // geçersiz aday kırmızı

    // --- Yardımcılar ---
    private static SolidColorBrush Frozen(Color c)
    {
        var b = new SolidColorBrush(c);
        b.Freeze();
        return b;
    }

    private static Pen FrozenPen(Color c, double thickness, bool dashed)
    {
        var brush = new SolidColorBrush(c);
        brush.Freeze();
        var pen = new Pen(brush, thickness);
        if (dashed)
        {
            pen.DashStyle = new DashStyle(new double[] { 4, 3 }, 0);
            pen.DashCap   = PenLineCap.Flat;
        }
        pen.Freeze();
        return pen;
    }
}
