using System;
using System.Collections.Generic;
using System.Linq;
using TrainService.Core.Entities;
using TrainService.Core.Geometry;

namespace TrainService.Cad.UndoRedo;

/// <summary>
/// Panodan yapıştırılan entity'leri ekler. Undo ile kaldırır.
/// v3.0.29.1-fix: Paste artık CommandStack üzerinden undo/redo'lu.
/// ID remap, referans çözümleme ve +20 offset içerir.
/// </summary>
public sealed class PasteEntitiesCommand : ICadCommand
{
    private readonly List<CadEntity> _entities;

    /// <summary>Yapıştırılan entity'lerin yeni ID'leri.</summary>
    public IReadOnlyList<Guid> EklenenIds => _entities.Select(e => e.Id).ToList();

    public PasteEntitiesCommand(IReadOnlyList<CadEntity> clipboardEntities)
    {
        _entities = clipboardEntities.ToList();
        var idMap = new Dictionary<Guid, Guid>();

        // 1. Her entity'ye yeni ID ata
        foreach (var e in _entities)
        {
            var oldId = e.Id;
            var newId = Guid.NewGuid();
            idMap[oldId] = newId;
            var idProp = e.GetType().GetProperty("Id");
            if (idProp != null && idProp.CanWrite)
                idProp.SetValue(e, newId);
        }

        // 2. Referans property'lerini yeni ID'lere çevir + offset uygula
        foreach (var e in _entities)
        {
            switch (e)
            {
                case TrackNode node:
                    node.Position = new Vector2D(node.Position.X + 20, node.Position.Y + 20);
                    break;

                case TrackSegment seg:
                    if (idMap.TryGetValue(seg.StartNodeId, out var newStart))
                        seg.StartNodeId = newStart;
                    if (idMap.TryGetValue(seg.EndNodeId, out var newEnd))
                        seg.EndNodeId = newEnd;
                    break;

                case RailSwitch sw:
                    if (idMap.TryGetValue(sw.EntryNodeId, out var newEntry))
                        sw.EntryNodeId = newEntry;
                    if (idMap.TryGetValue(sw.MainExitNodeId, out var newMain))
                        sw.MainExitNodeId = newMain;
                    if (idMap.TryGetValue(sw.DivergingExitNodeId, out var newDiv))
                        sw.DivergingExitNodeId = newDiv;
                    break;

                case Ramp ramp:
                    if (idMap.TryGetValue(ramp.EntryNodeId, out var newRampEntry))
                        ramp.EntryNodeId = newRampEntry;
                    if (idMap.TryGetValue(ramp.ExitNodeId, out var newRampExit))
                        ramp.ExitNodeId = newRampExit;
                    if (idMap.TryGetValue(ramp.SegmentId, out var newRampSeg))
                        ramp.SegmentId = newRampSeg;
                    break;

                case Route route:
                    for (int i = 0; i < route.Steps.Count; i++)
                    {
                        var step = route.Steps[i];
                        if (idMap.TryGetValue(step.SegmentId, out var newSegId))
                            route.Steps[i] = step with { SegmentId = newSegId };
                    }
                    break;
            }
        }
    }

    public string Description => $"Yapıştır: {_entities.Count} nesne";

    public void Execute(CadDocument doc)
    {
        foreach (var e in _entities)
            doc.RestoreEntity(e);
    }

    public void Undo(CadDocument doc)
    {
        foreach (var e in _entities)
            doc.RemoveEntity(e.Id);
    }
}