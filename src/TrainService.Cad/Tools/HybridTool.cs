using System;
using System.Collections.Generic;
using System.Linq;
using TrainService.Cad.Snapping;
using TrainService.Cad.UndoRedo;
using TrainService.Core.Entities;
using TrainService.Core.Enums;
using TrainService.Core.Geometry;
using TrainService.Core.Topology;

namespace TrainService.Cad.Tools;

public sealed class HybridTool : ITool
{
    public string Name => "Hybrid";

    private enum State { Idle, Chaining }
    private State _state = State.Idle;
    private readonly List<CadEntity> _pendingNodes = new();
    private readonly List<CadEntity> _pendingSegments = new();
    private readonly List<RouteStep> _steps = new();
    private readonly List<Guid> _tiklananSegmentIds = new();
    private TrackNode? _chainTail;
    private Vector2D _cursor;
    private Guid _adayId;
    private bool _adayGecerli;

    public PreviewShape? Preview { get; private set; }

    public void Activate(ToolContext ctx) => Reset();
    public void Deactivate(ToolContext ctx) => Reset();

    private void Reset()
    {
        _state = State.Idle;
        _pendingNodes.Clear();
        _pendingSegments.Clear();
        _steps.Clear();
        _tiklananSegmentIds.Clear();
        _chainTail = null;
        _adayId = Guid.Empty;
        _adayGecerli = false;
        Preview = null;
    }

    private static TrackNode YeniNode(Vector2D pos, ToolContext ctx)
    {
        double z = 0;
        if (ctx.Document.TryGetLayer(ctx.Document.ActiveLayerId, out var layer))
            z = layer.ZHeightMm;
        return new TrackNode { Position = pos, Z = z, LayerId = ctx.Document.ActiveLayerId };
    }

    public void OnPointerMove(SnapResult snapped, ToolContext ctx)
    {
        _cursor = snapped.Point;

        if (snapped.Kind == SnapKind.OnSegment && snapped.TargetId is Guid segId
            && ctx.Document.TryGetEntity(segId, out var e) && e is TrackSegment
            && ctx.Document.IsSelectable(segId))
        {
            _adayId = segId;
            _adayGecerli = GecerliAdayMi(segId, ctx);
        }
        else
        {
            _adayId = Guid.Empty;
            _adayGecerli = false;
        }

        Preview = _state == State.Idle
            ? new PreviewHybrid(default, _cursor, false, Array.Empty<RouteStep>(), _adayId, _adayGecerli)
            : new PreviewHybrid(_chainTail!.Position, _cursor,
                (_cursor - _chainTail.Position).Length > 1e-6,
                _steps, _adayId, _adayGecerli);
    }

    private bool GecerliAdayMi(Guid segId, ToolContext ctx)
    {
        // Hiç tıklanmamış: her aday geçerli
        if (_tiklananSegmentIds.Count == 0) return true;
        // Aynı segment ikinci kez tıklanamaz
        if (_tiklananSegmentIds.Any(id => id == segId)) return false;
        // Son tıklanan segmente komşu mu?
        var graf = TrackGraph.Build(ctx.Document.Entities.OfType<TrackNode>(),
                                    ctx.Document.Entities.OfType<TrackSegment>());
        return graf.AreAdjacent(_tiklananSegmentIds[^1], segId);
    }

    public void OnPointerDown(SnapResult snapped, ToolMouseButton button, ToolContext ctx)
    {
        if (button == ToolMouseButton.Right) { Commit(ctx); return; }
        if (button != ToolMouseButton.Left) return;
        if (_adayId == Guid.Empty || !_adayGecerli) return;
        if (!ctx.Document.TryGetEntity(_adayId, out var e) || e is not TrackSegment) return;

        var pos = snapped.Point;
        var node = YeniNode(pos, ctx);
        _pendingNodes.Add(node);
        _tiklananSegmentIds.Add(_adayId);

        if (_state == State.Idle)
        {
            _chainTail = node;
            _state = State.Chaining;
        }
        else if (_state == State.Chaining && _chainTail != null)
        {
            double dist = (pos - _chainTail.Position).Length;
            if (dist <= 1e-6) return;

            var segment = new TrackSegment
            {
                StartNodeId = _chainTail.Id,
                EndNodeId = node.Id,
                LengthMm = dist,
                LayerId = ctx.Document.ActiveLayerId
            };
            _pendingSegments.Add(segment);

            if (_steps.Count == 0)
            {
                _steps.Add(new RouteStep(segment.Id, TravelDirection.Forward));
            }
            else
            {
                var onceki = (TrackSegment)_pendingSegments[^2];
                var ortak = OrtakDugum(onceki, segment);

                if (_steps.Count == 1)
                {
                    _steps[0] = new RouteStep(onceki.Id,
                        onceki.EndNodeId == ortak
                            ? TravelDirection.Forward
                            : TravelDirection.Backward);
                }

                _steps.Add(new RouteStep(segment.Id,
                    segment.StartNodeId == ortak
                        ? TravelDirection.Forward
                        : TravelDirection.Backward));
            }

            _chainTail = node;
        }

        Preview = new PreviewHybrid(_chainTail!.Position, _cursor,
            (_cursor - _chainTail.Position).Length > 1e-6, _steps, Guid.Empty, false);
    }

    public void OnPointerUp(SnapResult s, ToolMouseButton b, ToolContext c) { }

    public void OnKeyDown(ToolKey key, ToolContext ctx)
    {
        switch (key)
        {
            case ToolKey.Enter: Commit(ctx); break;
            case ToolKey.Escape: Reset(); break;
        }
    }

    private void Commit(ToolContext ctx)
    {
        if (_pendingNodes.Count == 0) return;

        // Bayat graf kontrolü: tıklanan tüm sahne segmentleri hâlâ dokümanda mı?
        foreach (var id in _tiklananSegmentIds)
        {
            if (!ctx.Document.TryGetEntity(id, out _))
            {
                Reset();
                return;
            }
        }

        var commands = new List<ICadCommand>();
        foreach (var n in _pendingNodes) commands.Add(new AddEntityCommand(n));
        foreach (var s in _pendingSegments) commands.Add(new AddEntityCommand(s));

        if (_steps.Count > 0)
        {
            var rota = new Route { LayerId = ctx.Document.ActiveLayerId };
            rota.Steps.AddRange(_steps);
            rota.CachedBounds = HesaplaBounds(rota, ctx.Document);

            commands.Add(new AddEntityCommand(rota));

            var composite = new CompositeCadCommand("Hybrid Rota", commands);
            ctx.Commands.Do(composite, ctx.Document);
            ctx.Selection.Set(new[] { rota.Id });
        }
        else
        {
            var composite = new CompositeCadCommand("Hybrid Ray", commands);
            ctx.Commands.Do(composite, ctx.Document);
        }

        Reset();
    }

    private static Guid OrtakDugum(TrackSegment a, TrackSegment b)
    {
        if (a.StartNodeId == b.StartNodeId || a.StartNodeId == b.EndNodeId) return a.StartNodeId;
        if (a.EndNodeId == b.StartNodeId || a.EndNodeId == b.EndNodeId) return a.EndNodeId;
        return Guid.Empty;
    }

    private static BoundingBox HesaplaBounds(Route r, CadDocument doc)
    {
        double minX = double.MaxValue, minY = double.MaxValue,
               maxX = double.MinValue, maxY = double.MinValue;
        foreach (var st in r.Steps)
        {
            if (doc.TryGetEntity(st.SegmentId, out var e) && e is TrackSegment s
                && doc.TryGetEntity(s.StartNodeId, out var na) && na is TrackNode a
                && doc.TryGetEntity(s.EndNodeId, out var nb) && nb is TrackNode b)
            {
                minX = Math.Min(minX, Math.Min(a.Position.X, b.Position.X));
                maxX = Math.Max(maxX, Math.Max(a.Position.X, b.Position.X));
                minY = Math.Min(minY, Math.Min(a.Position.Y, b.Position.Y));
                maxY = Math.Max(maxY, Math.Max(a.Position.Y, b.Position.Y));
            }
        }
        return minX > maxX ? default : new BoundingBox(minX, minY, maxX, maxY);
    }
}
