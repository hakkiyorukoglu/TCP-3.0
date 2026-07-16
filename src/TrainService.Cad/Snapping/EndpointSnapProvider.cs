using System;
using System.Collections.Generic;
using TrainService.Core.Entities;
using TrainService.Core.Geometry;

namespace TrainService.Cad.Snapping;

public sealed class EndpointSnapProvider : ISnapProvider
{
    public int Priority => 10;
    
    // Tahsis kirliliğini önlemek için ortak buffer
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
            if (doc.TryGetEntity(id, out var e) && e is TrackNode node)
            {
                var dx = node.Position.X - worldCursor.X;
                var dy = node.Position.Y - worldCursor.Y;
                var d2 = dx * dx + dy * dy;
                
                if (d2 <= minD)
                {
                    minD = d2;
                    best = new SnapResult(node.Position, SnapKind.Endpoint, node.Id);
                }
            }
        }

        return best;
    }
}
