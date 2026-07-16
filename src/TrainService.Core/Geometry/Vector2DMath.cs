using System;

namespace TrainService.Core.Geometry;

public static class Vector2DMath
{
    /// <summary>Noktanın [a,b] doğru parçasına en yakın izdüşümünü ve MESAFE KARESİNİ döndürür.
    /// Karekök YOK (karşılaştırma karesel yapılır — hot-path performansı). Uç noktalara clamp'lenir.</summary>
    public static double DistanceSquaredToSegment(Vector2D p, Vector2D a, Vector2D b, out Vector2D projection)
    {
        var ab = new Vector2D(b.X - a.X, b.Y - a.Y);
        double abLenSq = ab.X * ab.X + ab.Y * ab.Y;
        if (abLenSq < 1e-18)                          // ★ sıfır uzunluklu segment (dejenere): a döner, NaN YOK
        {
            projection = a;
            double dx0 = p.X - a.X, dy0 = p.Y - a.Y;
            return dx0 * dx0 + dy0 * dy0;
        }
        double t = ((p.X - a.X) * ab.X + (p.Y - a.Y) * ab.Y) / abLenSq;
        t = t < 0 ? 0 : (t > 1 ? 1 : t);              // clamp [0,1] — köşe izdüşümü uç noktaya oturur
        projection = new Vector2D(a.X + t * ab.X, a.Y + t * ab.Y);
        double dx = p.X - projection.X, dy = p.Y - projection.Y;
        return dx * dx + dy * dy;
    }
}
