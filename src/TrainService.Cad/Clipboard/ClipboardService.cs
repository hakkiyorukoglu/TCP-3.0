using System;
using System.Collections.Generic;
using System.Linq;
using TrainService.Core.Entities;

namespace TrainService.Cad.Clipboard;

public sealed class ClipboardService
{
    private IReadOnlyList<CadEntity> _icerik = Array.Empty<CadEntity>();

    public bool HasContent => _icerik.Count > 0;
    public int Count => _icerik.Count;

    public void Set(IEnumerable<CadEntity> entities) => _icerik = entities.Select(Klonla).ToList();
    public IReadOnlyList<CadEntity> Get() => _icerik.Select(Klonla).ToList();
    public void Clear() => _icerik = Array.Empty<CadEntity>();

    private static CadEntity Klonla(CadEntity e) => e switch
    {
        TrackNode n => new TrackNode {
            Id = n.Id, Position = n.Position, Z = n.Z, Role = n.Role, LayerId = n.LayerId },
        TrackSegment s => new TrackSegment {
            Id = s.Id, StartNodeId = s.StartNodeId, EndNodeId = s.EndNodeId,
            LengthMm = s.LengthMm, LayerId = s.LayerId },
        _ => throw new NotSupportedException($"Panoya kopyalanamayan tip: {e.GetType().Name}")
    };
}
