using System;
using TrainService.Core.Geometry;

namespace TrainService.Cad.Snapping;

public sealed class GridSnapProvider : ISnapProvider
{
    public int Priority => 100;   // her zaman son çare

    public SnapResult? TrySnap(Vector2D c, double worldTolerance, CadDocument doc)
    {
        double g = doc.GridSizeMm;
        if (g <= 0 || double.IsNaN(g) || double.IsInfinity(g))
            return null;                                            // bozuk ayar zinciri kırmaz

        // ★ AwayFromZero ZORUNLU: varsayılan ToEven, orta noktaları çift hücrelere savurur
        //   ve pozitif/negatif tarafta ASİMETRİK davranır. Bkz. T1702/T1703.
        double x = Math.Round(c.X / g, MidpointRounding.AwayFromZero) * g;
        double y = Math.Round(c.Y / g, MidpointRounding.AwayFromZero) * g;

        return new SnapResult(new Vector2D(x, y), SnapKind.Grid, null);
        // Grid, worldTolerance'ı YOK SAYAR: grid her zaman yakalar (maks. sapma = g*√2/2).
        // Parametre imzada kalır; Endpoint/OnSegment (v3.0.19) gerçek yarıçap kontrolü yapacak.
    }
}
