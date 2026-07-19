using System.Collections.Generic;
using System.Linq;
using TrainService.Core.Geometry;

namespace TrainService.Cad.Snapping;

public sealed class SnapEngine
{
    private readonly ISnapProvider[] _providers;

    public bool IsEnabled { get; set; } = true;

    /// <summary>Devre disi birakilan snap turleri. Bos = tumu aktif.</summary>
    public HashSet<SnapKind> DisabledKinds { get; } = new();

    public SnapEngine(IEnumerable<ISnapProvider> providers)
    {
        _providers = providers.OrderBy(p => p.Priority).ToArray();
    }

    public SnapResult Resolve(Vector2D cursor, double worldTolerance, CadDocument doc)
    {
        if (!IsEnabled)
            return new SnapResult(cursor, SnapKind.None, null);

        for (int i = 0; i < _providers.Length; i++)
        {
            var hit = _providers[i].TrySnap(cursor, worldTolerance, doc);
            if (hit != null && !DisabledKinds.Contains(hit.Kind))
                return hit;
        }

        return new SnapResult(cursor, SnapKind.None, null);
    }

    public static double ScreenToleranceToWorld(double tolerancePx, double scale)
        => scale <= 0 ? 0 : tolerancePx / scale;
}