using System;
using TrainService.Core.Geometry;

namespace TrainService.Cad.Snapping;

/// <param name="Point">Yakalanan dünya koordinatı (mm).</param>
/// <param name="Kind">Yakalama türü; None = snap yok, Point ham imleçtir.</param>
/// <param name="TargetId">Grid: daima null. Endpoint: NodeId. OnSegment: SegmentId. (v3.0.19)</param>
public sealed record SnapResult(Vector2D Point, SnapKind Kind, Guid? TargetId);
