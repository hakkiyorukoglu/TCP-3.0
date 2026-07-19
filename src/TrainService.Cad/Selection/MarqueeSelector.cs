using System;
using System.Collections.Generic;
using TrainService.Core.Entities;
using TrainService.Core.Geometry;

namespace TrainService.Cad.Selection;

/// <summary>
/// Stateless selection engine for marquee and fence selection.
/// Pure functions — no state, no side effects.
/// </summary>
public static class MarqueeSelector
{
    /// <summary>
    /// Window mode: only entities whose bounding box is fully contained in the selection box.
    /// </summary>
    public static List<Guid> WindowSelect(CadDocument doc, BoundingBox box)
    {
        var buffer = new List<Guid>(128);
        doc.QueryRegion(box, buffer);

        var result = new List<Guid>();
        foreach (var id in buffer)
        {
            if (!IsSelectable(doc, id)) continue;
            var eb = GetEntityBounds(doc, id);
            if (eb == null) continue;
            if (box.Contains(eb.Value))
                result.Add(id);
        }
        return result;
    }

    /// <summary>
    /// Crossing mode: entities intersecting the selection box.
    /// </summary>
    public static List<Guid> CrossingSelect(CadDocument doc, BoundingBox box)
    {
        var buffer = new List<Guid>(128);
        doc.QueryRegion(box, buffer);

        var result = new List<Guid>();
        foreach (var id in buffer)
        {
            if (!IsSelectable(doc, id)) continue;
            var eb = GetEntityBounds(doc, id);
            if (eb == null) continue;
            if (box.IntersectsWith(eb.Value))
                result.Add(id);
        }
        return result;
    }

    /// <summary>
    /// Fence mode: entities whose center point falls inside the polygon.
    /// </summary>
    public static List<Guid> FenceSelect(CadDocument doc, IReadOnlyList<Vector2D> polygon)
    {
        if (polygon.Count < 3) return new List<Guid>();

        // Compute bounding box of polygon for spatial query
        var minX = double.MaxValue; var minY = double.MaxValue;
        var maxX = double.MinValue; var maxY = double.MinValue;
        foreach (var p in polygon)
        {
            if (p.X < minX) minX = p.X;
            if (p.Y < minY) minY = p.Y;
            if (p.X > maxX) maxX = p.X;
            if (p.Y > maxY) maxY = p.Y;
        }
        var searchBox = new BoundingBox(minX, minY, maxX, maxY);

        var buffer = new List<Guid>(128);
        doc.QueryRegion(searchBox, buffer);

        var result = new List<Guid>();
        foreach (var id in buffer)
        {
            if (!IsSelectable(doc, id)) continue;

            // Use entity's center point for point-in-polygon test
            var center = GetEntityCenter(doc, id);
            if (center == null) continue;

            if (IsPointInPolygon(center.Value, polygon))
                result.Add(id);
        }
        return result;
    }

    /// <summary>
    /// Ray casting algorithm — determines if a point is inside a polygon.
    /// </summary>
    public static bool IsPointInPolygon(Vector2D point, IReadOnlyList<Vector2D> polygon)
    {
        if (polygon.Count < 3) return false;

        bool inside = false;
        int j = polygon.Count - 1;

        for (int i = 0; i < polygon.Count; i++)
        {
            var pi = polygon[i];
            var pj = polygon[j];

            // Check if ray from point to +X crosses this edge
            if ((pi.Y > point.Y) != (pj.Y > point.Y))
            {
                double intersectX = (pj.X - pi.X) * (point.Y - pi.Y) / (pj.Y - pi.Y) + pi.X;
                if (point.X < intersectX)
                    inside = !inside;
            }
            j = i;
        }

        return inside;
    }

    private static bool IsSelectable(CadDocument doc, Guid id)
    {
        if (!doc.TryGetEntity(id, out _)) return false;
        return doc.IsSelectable(id);
    }

    private static BoundingBox? GetEntityBounds(CadDocument doc, Guid id)
    {
        if (!doc.TryGetEntity(id, out var e)) return null;

        if (e.Bounds != null) return e.Bounds;

        if (e is TrackSegment seg &&
            doc.TryGetEntity(seg.StartNodeId, out var sa) && sa is TrackNode a &&
            doc.TryGetEntity(seg.EndNodeId, out var sb) && sb is TrackNode b)
        {
            return new BoundingBox(
                Math.Min(a.Position.X, b.Position.X),
                Math.Min(a.Position.Y, b.Position.Y),
                Math.Max(a.Position.X, b.Position.X),
                Math.Max(a.Position.Y, b.Position.Y));
        }
        return null;
    }

    private static Vector2D? GetEntityCenter(CadDocument doc, Guid id)
    {
        if (!doc.TryGetEntity(id, out var e)) return null;

        switch (e)
        {
            case TrackNode n:
                return n.Position;

            case TrackSegment seg:
                if (doc.TryGetEntity(seg.StartNodeId, out var sa) && sa is TrackNode a &&
                    doc.TryGetEntity(seg.EndNodeId, out var sb) && sb is TrackNode b)
                {
                    return new Vector2D(
                        (a.Position.X + b.Position.X) / 2.0,
                        (a.Position.Y + b.Position.Y) / 2.0);
                }
                return null;

            case RailSwitch sw:
                return sw.Position;

            case Ramp r:
                return r.Position;

            case Route route:
                // Route: use first step's segment center or fallback to zero
                if (route.Steps.Count > 0 &&
                    doc.TryGetEntity(route.Steps[0].SegmentId, out var firstSeg) && firstSeg is TrackSegment ts &&
                    doc.TryGetEntity(ts.StartNodeId, out var na) && na is TrackNode ndA &&
                    doc.TryGetEntity(ts.EndNodeId, out var nb) && nb is TrackNode ndB)
                {
                    return new Vector2D(
                        (ndA.Position.X + ndB.Position.X) / 2.0,
                        (ndA.Position.Y + ndB.Position.Y) / 2.0);
                }
                return null;

            default:
                return null;
        }
    }
}