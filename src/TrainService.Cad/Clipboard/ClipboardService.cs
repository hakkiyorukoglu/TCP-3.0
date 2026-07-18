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

    private static Route KlonlaRoute(Route rt)
    {
        var clone = new Route
        {
            Id = rt.Id,
            Name = rt.Name,
            CachedBounds = rt.CachedBounds,
            LayerId = rt.LayerId
        };
        foreach (var step in rt.Steps)
            clone.Steps.Add(step with { });
        return clone;
    }

    private static CadEntity Klonla(CadEntity e) => e switch
    {
        TrackNode n => new TrackNode {
            Id = n.Id, Position = n.Position, Z = n.Z, Role = n.Role, LayerId = n.LayerId },
        TrackSegment s => new TrackSegment {
            Id = s.Id, StartNodeId = s.StartNodeId, EndNodeId = s.EndNodeId,
            LengthMm = s.LengthMm, LayerId = s.LayerId },
        RailSwitch sw => new RailSwitch {
            Id = sw.Id, Position = sw.Position, RotationDeg = sw.RotationDeg,
            EntryNodeId = sw.EntryNodeId, MainExitNodeId = sw.MainExitNodeId,
            DivergingExitNodeId = sw.DivergingExitNodeId, State = sw.State,
            BoundServoDeviceId = sw.BoundServoDeviceId, LayerId = sw.LayerId },
        Ramp r => new Ramp {
            Id = r.Id, SegmentId = r.SegmentId, Position = r.Position, RotationDeg = r.RotationDeg,
            EntryNodeId = r.EntryNodeId, ExitNodeId = r.ExitNodeId,
            StartZ = r.StartZ, EndZ = r.EndZ, LengthMm = r.LengthMm, LayerId = r.LayerId },
        Route rt => KlonlaRoute(rt),
        _ => throw new NotSupportedException($"Panoya kopyalanamayan tip: {e.GetType().Name}")
    };
}
