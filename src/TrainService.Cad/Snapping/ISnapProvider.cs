using TrainService.Core.Geometry;

namespace TrainService.Cad.Snapping;

public interface ISnapProvider
{
    /// <summary>KÜÇÜK sayı = YÜKSEK öncelik. Rezerve plan:
    /// Endpoint=10, OnSegment=20, Grid=100. Aralıklar bilinçli geniştir.</summary>
    int Priority { get; }

    /// <summary>Yakalayamazsa null döner ("zincirdeki sıradaki denesin").
    /// ASLA exception fırlatmaz; geçersiz durumda null döner.
    /// worldTolerance: mm cinsinden yakalama yarıçapı (ekran-px'ten çevrilmiş).</summary>
    SnapResult? TrySnap(Vector2D worldCursor, double worldTolerance, CadDocument doc);
}
