using System;
using System.Collections.Generic;
using TrainService.Core.Entities;
using TrainService.Core.Geometry;

namespace TrainService.Cad.Snapping;

public sealed class OnSegmentSnapProvider : ISnapProvider
{
    public int Priority => 20;

    [ThreadStatic] private static List<Guid>? _buffer;

    public SnapResult? TrySnap(Vector2D worldCursor, double worldTolerance, CadDocument doc)
    {
        _buffer ??= new List<Guid>(32);
        
        var box = new BoundingBox(
            worldCursor.X - worldTolerance, 
            worldCursor.Y - worldTolerance, 
            worldCursor.X + worldTolerance, 
            worldCursor.Y + worldTolerance);
            
        doc.QueryRegion(box, _buffer);

        double minD = worldTolerance * worldTolerance;
        SnapResult? best = null;

        foreach (var id in _buffer)
        {
            if (doc.TryGetEntity(id, out var e) && e is TrackSegment segment)
            {
                if (doc.TryGetEntity(segment.StartNodeId, out var eA) && eA is TrackNode nA &&
                    doc.TryGetEntity(segment.EndNodeId, out var eB) && eB is TrackNode nB)
                {
                    var d2 = Vector2DMath.DistanceSquaredToSegment(worldCursor, nA.Position, nB.Position, out var proj);
                    if (d2 <= minD)
                    {
                        minD = d2;
                        best = new SnapResult(proj, SnapKind.OnSegment, segment.Id);
                    }
                }
            }
        }

        return best;
    }
}
