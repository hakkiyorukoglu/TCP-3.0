using System.Collections.Generic;
using System.Linq;
using TrainService.Core.Geometry;

namespace TrainService.Cad.Snapping;

/// <summary>Snap zinciri yürütücüsü. SICAK YOL (MouseMove başına 1 çağrı):
/// bu sınıfta LINQ, liste tahsisi, sıralama, string işlemi YASAKTIR.
/// Provider seti ctor'da sabitlenir; çalışma anında provider ekleme yoktur.</summary>
public sealed class SnapEngine
{
    private readonly ISnapProvider[] _providers;

    public bool IsEnabled { get; set; } = true;

    public SnapEngine(IEnumerable<ISnapProvider> providers)
    {
        _providers = providers.OrderBy(p => p.Priority).ToArray();  // LINQ sadece burada (ctor, 1 kez)
    }

    public SnapResult Resolve(Vector2D cursor, double worldTolerance, CadDocument doc)
    {
        if (!IsEnabled)
            return new SnapResult(cursor, SnapKind.None, null);

        for (int i = 0; i < _providers.Length; i++)                 // foreach değil: enumerator tahsisi yok
            if (_providers[i].TrySnap(cursor, worldTolerance, doc) is { } hit)
                return hit;

        return new SnapResult(cursor, SnapKind.None, null);
    }

    /// <summary>Ekran-piksel toleransını dünya (mm) toleransına çevirir.
    /// scale = piksel/mm. Guard: scale <= 0 -> 0 (snap fiilen devre dışı, çökme yok).</summary>
    public static double ScreenToleranceToWorld(double tolerancePx, double scale)
        => scale <= 0 ? 0 : tolerancePx / scale;
}
