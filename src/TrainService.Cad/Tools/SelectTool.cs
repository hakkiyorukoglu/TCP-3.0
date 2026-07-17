using System;
using System.Collections.Generic;
using System.Linq;
using TrainService.Cad.Selection;
using TrainService.Cad.Snapping;
using TrainService.Cad.UndoRedo;
using TrainService.Core.Entities;
using TrainService.Core.Geometry;

namespace TrainService.Cad.Tools;

public sealed class SelectTool : ITool
{
    public string Name => "Select";

    private bool _pressed;
    private bool _dragging;
    private Vector2D _pressWorld;
    private Vector2D _cursorWorld;

    public PreviewShape? Preview { get; private set; }

    public void Activate(ToolContext ctx) => Reset();
    public void Deactivate(ToolContext ctx) => Reset();

    private void Reset()
    {
        _pressed = false;
        _dragging = false;
        Preview = null;
    }

    public void OnPointerDown(SnapResult snapped, ToolMouseButton button, ToolContext ctx)
    {
        if (button != ToolMouseButton.Left) return;
        _pressed = true;
        _dragging = false;
        _pressWorld = snapped.Point;
        _cursorWorld = snapped.Point;
    }

    public void OnPointerMove(SnapResult snapped, ToolContext ctx)
    {
        _cursorWorld = snapped.Point;
        if (!_pressed) { Preview = null; return; }

        double dx = _cursorWorld.X - _pressWorld.X, dy = _cursorWorld.Y - _pressWorld.Y;
        if ((dx * dx + dy * dy) > 1e-6) _dragging = true;

        if (_dragging)
        {
            bool crossing = _cursorWorld.X < _pressWorld.X;
            Preview = new PreviewRectangle(_pressWorld, _cursorWorld, crossing);
        }
    }

    public void OnPointerUp(SnapResult snapped, ToolMouseButton button, ToolContext ctx)
    {
        if (button != ToolMouseButton.Left || !_pressed) return;
        _pressed = false;
        _cursorWorld = snapped.Point;

        if (_dragging) MarqueeSelect(ctx);
        else           ClickSelect(ctx);

        _dragging = false;
        Preview = null;
    }

    private void MarqueeSelect(ToolContext ctx)
    {
        var box = BoxOf(_pressWorld, _cursorWorld);
        bool crossing = _cursorWorld.X < _pressWorld.X;

        var buf = new List<Guid>(128);
        ctx.Document.QueryRegion(box, buf);

        var secilen = new List<Guid>();
        foreach (var id in buf)
        {
            if (!ctx.Document.TryGetEntity(id, out var e)) continue;
            if (!ctx.Document.IsSelectable(id)) continue;
            var eb = EntityBounds(e, ctx.Document);
            if (eb == null) continue;
            bool hit = crossing ? box.IntersectsWith(eb.Value) : box.Contains(eb.Value);
            if (hit) secilen.Add(id);
        }

        if (ctx.ModifierAdd)
            foreach (var id in secilen) ctx.Selection.Add(id);
        else
            ctx.Selection.Set(secilen);
    }

    private void ClickSelect(ToolContext ctx)
    {
        var box = BoundingBox.FromPoint(_pressWorld, ctx.ClickToleranceWorld);
        var buf = new List<Guid>(32);
        ctx.Document.QueryRegion(box, buf);

        Guid enYakin = Guid.Empty;
        double enYakinSq = ctx.ClickToleranceWorld * ctx.ClickToleranceWorld;

        foreach (var id in buf)
        {
            if (!ctx.Document.TryGetEntity(id, out var e)) continue;
            if (!ctx.Document.IsSelectable(id)) continue;
            double dSq = MesafeKaresi(e, _pressWorld, ctx);
            if (dSq <= enYakinSq) { enYakinSq = dSq; enYakin = id; }
        }

        if (enYakin == Guid.Empty)
        {
            if (!ctx.ModifierAdd) ctx.Selection.Clear();
        }
        else if (ctx.ModifierAdd)
        {
            ctx.Selection.Toggle(enYakin);
        }
        else
        {
            ctx.Selection.Set(new[] { enYakin });
        }
    }

    private static double MesafeKaresi(CadEntity e, Vector2D p, ToolContext ctx)
    {
        switch (e)
        {
            case TrackNode n:
            {
                double dx = p.X - n.Position.X, dy = p.Y - n.Position.Y;
                return dx * dx + dy * dy;
            }
            case TrackSegment s:
            {
                if (ctx.Document.TryGetEntity(s.StartNodeId, out var sa) && sa is TrackNode a &&
                    ctx.Document.TryGetEntity(s.EndNodeId, out var sb) && sb is TrackNode b)
                    return Vector2DMath.DistanceSquaredToSegment(p, a.Position, b.Position, out _);
                return double.MaxValue;
            }
            default: return double.MaxValue;
        }
    }

    private static BoundingBox? EntityBounds(CadEntity e, CadDocument doc)
    {
        if (e.Bounds != null) return e.Bounds;
        // Segment için node pozisyonlarından hesapla
        if (e is TrackSegment seg &&
            doc.TryGetEntity(seg.StartNodeId, out var sa) && sa is TrackNode a &&
            doc.TryGetEntity(seg.EndNodeId, out var sb) && sb is TrackNode b)
        {
            return new BoundingBox(
                Math.Min(a.Position.X, b.Position.X), Math.Min(a.Position.Y, b.Position.Y),
                Math.Max(a.Position.X, b.Position.X), Math.Max(a.Position.Y, b.Position.Y));
        }
        return null;
    }

    private static BoundingBox BoxOf(Vector2D a, Vector2D b)
        => new(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Max(a.X, b.X), Math.Max(a.Y, b.Y));

    public void OnKeyDown(ToolKey key, ToolContext ctx)
    {
        switch (key)
        {
            case ToolKey.Delete:
                if (ctx.Selection.SelectedIds.Count > 0)
                {
                    ctx.Commands.Do(new DeleteEntitiesCommand(ctx.Selection.SelectedIds.ToList()), ctx.Document);
                    ctx.Selection.Clear();
                }
                break;

            case ToolKey.Copy:
                if (ctx.Selection.SelectedIds.Count > 0)
                    ctx.Clipboard.Set(SeciliEntities(ctx));
                break;

            case ToolKey.Cut:
                if (ctx.Selection.SelectedIds.Count > 0)
                {
                    ctx.Commands.Do(new CutEntitiesCommand(ctx.Selection.SelectedIds.ToList(), ctx.Clipboard), ctx.Document);
                    ctx.Selection.Clear();
                }
                break;

            case ToolKey.Paste:
                if (ctx.Clipboard != null && ctx.Clipboard.HasContent)
                {
                    var cmd = new PasteEntitiesCommand(ctx.Clipboard.Get());
                    ctx.Commands.Do(cmd, ctx.Document);
                    ctx.Selection.Set(cmd.EklenenIds);
                }
                break;
        }
    }

    private static List<CadEntity> SeciliEntities(ToolContext ctx)
    {
        var list = new List<CadEntity>();
        foreach (var id in ctx.Selection.SelectedIds)
            if (ctx.Document.TryGetEntity(id, out var e)) list.Add(e);
        return list;
    }
}
