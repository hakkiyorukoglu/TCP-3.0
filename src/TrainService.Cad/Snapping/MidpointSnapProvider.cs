using System;
using System.Collections.Generic;
using TrainService.Core.Entities;
using TrainService.Core.Geometry;

namespace TrainService.Cad.Snapping;

public sealed class MidpointSnapProvider : ISnapProvider
{
    public int Priority => 15;  // Endpoint(10) < Midpoint(15) < OnSegment(20)

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
            if (doc.TryGetEntity(id, out var e) && e is TrackSegment seg &&
                doc.TryGetEntity(seg.StartNodeId, out var sa) && sa is TrackNode a &&
                doc.TryGetEntity(seg.EndNodeId, out var sb) && sb is TrackNode b)
            {
                double midX = (a.Position.X + b.Position.X) / 2.0;
                double midY = (a.Position.Y + b.Position.Y) / 2.0;
                double dx = midX - worldCursor.X;
                double dy = midY - worldCursor.Y;
                double d2 = dx * dx + dy * dy;

                if (d2 <= minD)
                {
                    minD = d2;
                    best = new SnapResult(new Vector2D(midX, midY), SnapKind.Midpoint, seg.Id);
                }
            }
        }

        return best;
    }
}