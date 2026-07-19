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

    // Fence state
    private SelectionMode _mode = SelectionMode.Crossing;
    private List<Vector2D> _fencePoints = new();
    private bool _isFencing;

    public PreviewShape? Preview { get; private set; }

    public void SetMode(SelectionMode mode)
    {
        _mode = mode;
        if (_isFencing && mode != SelectionMode.Fence)
        {
            _fencePoints.Clear();
            _isFencing = false;
            Preview = null;
        }
    }

    public void Activate(ToolContext ctx) => Reset();
    public void Deactivate(ToolContext ctx) => Reset();

    private void Reset()
    {
        _pressed = false;
        _dragging = false;
        _isFencing = false;
        _fencePoints.Clear();
        Preview = null;
    }

    public void OnPointerDown(SnapResult snapped, ToolMouseButton button, ToolContext ctx)
    {
        if (_mode == SelectionMode.Fence)
        {
            if (button == ToolMouseButton.Left)
            {
                _fencePoints.Add(snapped.Point);
                _isFencing = true;
                Preview = new PreviewFence(_fencePoints.ToList(), false);
            }
            else if (button == ToolMouseButton.Right)
            {
                CommitFence(ctx);
            }
            return;
        }

        if (button != ToolMouseButton.Left) return;
        _pressed = true;
        _dragging = false;
        _pressWorld = snapped.Point;
        _cursorWorld = snapped.Point;
    }

    public void OnPointerMove(SnapResult snapped, ToolContext ctx)
    {
        if (_mode == SelectionMode.Fence) return;

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
        if (_mode == SelectionMode.Fence) return;

        if (button != ToolMouseButton.Left || !_pressed) return;
        _pressed = false;
        _cursorWorld = snapped.Point;

        if (_dragging) MarqueeSelect(ctx);
        else ClickSelect(ctx);

        _dragging = false;
        Preview = null;
    }

    public void OnKeyDown(ToolKey key, ToolContext ctx)
    {
        if (_mode == SelectionMode.Fence && _isFencing)
        {
            switch (key)
            {
                case ToolKey.Enter:
                    CommitFence(ctx);
                    return;
                case ToolKey.Escape:
                    CancelFence(ctx);
                    return;
            }
        }

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

            case ToolKey.Escape:
                _pressed = false;
                _dragging = false;
                Preview = null;
                break;
        }
    }

    private void CommitFence(ToolContext ctx)
    {
        if (_fencePoints.Count < 3)
        {
            CancelFence(ctx);
            return;
        }

        _isFencing = false;
        Preview = new PreviewFence(_fencePoints.ToList(), true);

        var selected = MarqueeSelector.FenceSelect(ctx.Document, _fencePoints);

        if (ctx.ModifierAdd)
            foreach (var id in selected) ctx.Selection.Add(id);
        else
            ctx.Selection.Set(selected);

        _fencePoints.Clear();
        Preview = null;
    }

    private void CancelFence(ToolContext ctx)
    {
        _isFencing = false;
        _fencePoints.Clear();
        Preview = null;
    }

    private void MarqueeSelect(ToolContext ctx)
    {
        var box = BoxOf(_pressWorld, _cursorWorld);
        bool rightToLeft = _cursorWorld.X < _pressWorld.X; // sağdan-sola = crossing yönü

        List<Guid> selected;
        if (_mode == SelectionMode.Window)
        {
            // Window mode: her zaman tamamen içeren
            selected = MarqueeSelector.WindowSelect(ctx.Document, box);
        }
        else if (rightToLeft)
        {
            // Default (Crossing) + sağdan-sola: kesişen
            selected = MarqueeSelector.CrossingSelect(ctx.Document, box);
        }
        else
        {
            // Default (Crossing) + soldan-sağa: tamamen içeren (window davranışı)
            selected = MarqueeSelector.WindowSelect(ctx.Document, box);
        }

        if (ctx.ModifierAdd)
            foreach (var id in selected) ctx.Selection.Add(id);
        else
            ctx.Selection.Set(selected);
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

    private static List<CadEntity> SeciliEntities(ToolContext ctx)
    {
        var list = new List<CadEntity>();
        foreach (var id in ctx.Selection.SelectedIds)
            if (ctx.Document.TryGetEntity(id, out var e)) list.Add(e);
        return list;
    }

    private static BoundingBox BoxOf(Vector2D a, Vector2D b)
        => new(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Max(a.X, b.X), Math.Max(a.Y, b.Y));
}