using System;
using TrainService.Core.Geometry;

namespace TrainService.Cad;

/// <summary>
/// Switch prefab geometri sabitleri ve yardımcı offset hesaplamaları.
/// Rotation=0 varsayılan yön: Entry aşağıda (-Y), MainExit yukarıda (+Y),
/// DivergingExit sağ-üstte (25° açıyla).
/// </summary>
public static class SwitchDefaults
{
    /// <summary>Toplam switch boyu (mm).</summary>
    public const double LengthMm = 80.0;

    /// <summary>Sapak açısı (derece).</summary>
    public const double DivergingAngleDeg = 25.0;

    /// <summary>Merkezden portlara yarım boy (mm).</summary>
    public const double HalfLength = LengthMm / 2;

    /// <summary>Giriş portunun merkeze göre ofseti (Rotation=0 için aşağı).</summary>
    public static Vector2D EntryOffset(double rotDeg) =>
        Rotate(new Vector2D(0, -HalfLength), rotDeg);

    /// <summary>Ana hat çıkış portunun merkeze göre ofseti (Rotation=0 için yukarı).</summary>
    public static Vector2D MainExitOffset(double rotDeg) =>
        Rotate(new Vector2D(0, HalfLength), rotDeg);

    /// <summary>Sapak çıkış portunun merkeze göre ofseti (Rotation=0 için sağ-üst).</summary>
    public static Vector2D DivergingExitOffset(double rotDeg)
    {
        double rad = DivergingAngleDeg * Math.PI / 180;
        return Rotate(new Vector2D(HalfLength * Math.Sin(rad), HalfLength * Math.Cos(rad)), rotDeg);
    }

    /// <summary>2D vektörü derece cinsinden döndürür.</summary>
    private static Vector2D Rotate(Vector2D v, double deg)
    {
        double rad = deg * Math.PI / 180;
        double cos = Math.Cos(rad);
        double sin = Math.Sin(rad);
        return new Vector2D(v.X * cos - v.Y * sin, v.X * sin + v.Y * cos);
    }
}
