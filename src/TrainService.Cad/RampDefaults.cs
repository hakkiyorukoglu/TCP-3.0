using System;
using TrainService.Core.Geometry;

namespace TrainService.Cad;

/// <summary>
/// Ramp prefab geometri sabitleri ve yardımcı offset hesaplamaları.
/// Rotation=0 varsayılan yön: Entry solda (-X), Exit sağda (+X).
/// </summary>
public static class RampDefaults
{
    /// <summary>Toplam rampa boyu (mm).</summary>
    public const double LengthMm = 100.0;

    /// <summary>Maksimum eğim (% olarak).</summary>
    public const double MaxGradePercent = 15.0;

    /// <summary>Varsayılan başlangıç Z yüksekliği (mm).</summary>
    public const double DefaultStartZ = 0.0;

    /// <summary>Varsayılan bitiş Z yüksekliği (mm).</summary>
    public const double DefaultEndZ = 350.0;

    /// <summary>Merkezden portlara yarım boy (mm).</summary>
    public const double HalfLength = LengthMm / 2;

    /// <summary>Giriş portunun merkeze göre ofseti (Rotation=0 için sol).</summary>
    public static Vector2D EntryOffset(double rotDeg) =>
        Rotate(new Vector2D(-HalfLength, 0), rotDeg);

    /// <summary>Çıkış portunun merkeze göre ofseti (Rotation=0 için sağ).</summary>
    public static Vector2D ExitOffset(double rotDeg) =>
        Rotate(new Vector2D(HalfLength, 0), rotDeg);

    /// <summary>2D vektörü derece cinsinden döndürür.</summary>
    private static Vector2D Rotate(Vector2D v, double deg)
    {
        double rad = deg * Math.PI / 180;
        double cos = Math.Cos(rad);
        double sin = Math.Sin(rad);
        return new Vector2D(v.X * cos - v.Y * sin, v.X * sin + v.Y * cos);
    }
}
