using System;
using System.Collections.Generic;

namespace TrainService.Core.Topology;

/// <summary>
/// Mantıksal blok: iki sınır düğümü (uç veya makas) arasındaki kesintisiz segment dizisi.
/// Blok Sinyalizasyonun (Roadmap 6.4) ve simülasyon çakışma-önlemenin temel birimidir.
/// SegmentIds sıralıdır: bloğun bir ucundan diğerine yürüyüş sırasında.
/// </summary>
public sealed record TrackBlock(Guid Id, IReadOnlyList<Guid> SegmentIds, Guid StartNodeId, Guid EndNodeId)
{
    public int SegmentCount => SegmentIds.Count;
}
