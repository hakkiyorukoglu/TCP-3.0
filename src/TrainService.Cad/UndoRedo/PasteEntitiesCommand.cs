using System;
using System.Collections.Generic;
using System.Linq;
using TrainService.Core.Entities;

namespace TrainService.Cad.UndoRedo;

public sealed class PasteEntitiesCommand : ICadCommand
{
    private readonly IReadOnlyList<CadEntity> _kaynak;
    private readonly List<CadEntity> _eklenen = new();
    private const double Offset = 20.0;

    public PasteEntitiesCommand(IReadOnlyList<CadEntity> kaynak) => _kaynak = kaynak;
    public string Description => $"Yapıştır: {_kaynak.Count} nesne";

    public void Execute(CadDocument doc)
    {
        _eklenen.Clear();
        var idMap = new Dictionary<Guid, Guid>();
        var yeniNodeById = new Dictionary<Guid, TrackNode>();

        foreach (var n in _kaynak.OfType<TrackNode>())
        {
            var yeni = new TrackNode {
                Position = new TrainService.Core.Geometry.Vector2D(n.Position.X + Offset, n.Position.Y + Offset),
                Z = n.Z, Role = n.Role, LayerId = n.LayerId };
            idMap[n.Id] = yeni.Id;
            yeniNodeById[yeni.Id] = yeni;
            _eklenen.Add(yeni);
        }

        foreach (var s in _kaynak.OfType<TrackSegment>())
        {
            if (!idMap.TryGetValue(s.StartNodeId, out var yeniStart)) continue;
            if (!idMap.TryGetValue(s.EndNodeId,   out var yeniEnd))   continue;
            
            var a = yeniNodeById[yeniStart].Position;
            var b = yeniNodeById[yeniEnd].Position;
            var yeni = new TrackSegment {
                StartNodeId = yeniStart, EndNodeId = yeniEnd,
                LengthMm = (b - a).Length, LayerId = s.LayerId };
            _eklenen.Add(yeni);
        }

        foreach (var e in _eklenen) doc.AddEntity(e);
    }

    public void Undo(CadDocument doc)
    {
        foreach (var e in _eklenen) doc.RemoveEntity(e.Id);
        _eklenen.Clear();
    }

    public IReadOnlyList<Guid> EklenenIds => _eklenen.Select(e => e.Id).ToList();
}
