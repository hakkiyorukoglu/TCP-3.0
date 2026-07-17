using System;
using System.Collections.Generic;
using System.Linq;
using TrainService.Cad.Snapping;
using TrainService.Cad.UndoRedo;
using TrainService.Core.Entities;
using TrainService.Core.Enums;
using TrainService.Core.Geometry;

namespace TrainService.Cad.Tools;

public sealed class SwitchTool : ITool
{
    public string Name => "Switch";

    private SwitchToolState _state = SwitchToolState.Idle;
    private Guid _selectedNodeId;
    private Guid _mainSegId;
    private Guid _divergingSegId;
    private Guid _adayNodeId;
    private Guid _adaySegId;
    private bool _adayGecerli;

    public PreviewShape? Preview { get; private set; }

    public void Activate(ToolContext ctx) => Reset();
    public void Deactivate(ToolContext ctx) => Reset();

    private void Reset()
    {
        _state = SwitchToolState.Idle;
        _selectedNodeId = Guid.Empty;
        _mainSegId = Guid.Empty;
        _divergingSegId = Guid.Empty;
        _adayNodeId = Guid.Empty;
        _adaySegId = Guid.Empty;
        _adayGecerli = false;
        Preview = null;
    }

    public void OnPointerMove(SnapResult snapped, ToolContext ctx)
    {
        if (_state == SwitchToolState.Idle)
        {
            // Node seçme aşaması: Endpoint snap kabul
            if (snapped.Kind == SnapKind.Endpoint && snapped.TargetId is Guid nodeId
                && ctx.Document.TryGetEntity(nodeId, out var e) && e is TrackNode node
                && ctx.Document.IsVisible(nodeId))
            {
                _adayNodeId = nodeId;
                _adayGecerli = KendiSegmentsayisi(nodeId, ctx) >= 2
                    && node.Role != NodeRole.SwitchNode;
            }
            else
            {
                _adayNodeId = Guid.Empty;
                _adayGecerli = false;
            }
        }
        else
        {
            // Segment seçme aşaması: OnSegment snap kabul
            if (snapped.Kind == SnapKind.OnSegment && snapped.TargetId is Guid segId
                && ctx.Document.TryGetEntity(segId, out var e) && e is TrackSegment
                && ctx.Document.IsSelectable(segId))
            {
                _adaySegId = segId;
                _adayGecerli = SegmenteBagliMi(segId, _selectedNodeId, ctx)
                    && segId != _mainSegId;
            }
            else
            {
                _adaySegId = Guid.Empty;
                _adayGecerli = false;
            }
        }

        Preview = new PreviewSwitch(
            _state == SwitchToolState.Idle ? _adayNodeId : _selectedNodeId,
            _mainSegId != Guid.Empty ? _mainSegId : null,
            _divergingSegId != Guid.Empty ? _divergingSegId : null,
            _state == SwitchToolState.Idle ? _adayNodeId : _adaySegId,
            _adayGecerli,
            _state);
    }

    public void OnPointerDown(SnapResult snapped, ToolMouseButton button, ToolContext ctx)
    {
        if (button != ToolMouseButton.Left) return;

        switch (_state)
        {
            case SwitchToolState.Idle:
            {
                // Endpoint snap ile node seç
                if (snapped.Kind == SnapKind.Endpoint && snapped.TargetId is Guid nid
                    && _adayNodeId == nid && _adayGecerli)
                {
                    _selectedNodeId = nid;
                    _state = SwitchToolState.NodeSelected;
                    _adaySegId = Guid.Empty;
                }
                break;
            }

            case SwitchToolState.NodeSelected:
            {
                // OnSegment snap ile Main segment seç
                if (snapped.Kind == SnapKind.OnSegment && snapped.TargetId is Guid sid
                    && _adaySegId == sid && _adayGecerli)
                {
                    _mainSegId = sid;
                    _state = SwitchToolState.MainSelected;
                    _adaySegId = Guid.Empty;
                }
                break;
            }

            case SwitchToolState.MainSelected:
            {
                // OnSegment snap ile Diverging segment seç
                if (snapped.Kind == SnapKind.OnSegment && snapped.TargetId is Guid sid2
                    && ctx.Document.TryGetEntity(sid2, out var e) && e is TrackSegment
                    && SegmenteBagliMi(sid2, _selectedNodeId, ctx) && sid2 != _mainSegId)
                {
                    _divergingSegId = sid2;
                    _state = SwitchToolState.DivergingSelected;
                    Commit(ctx); // auto-commit
                }
                break;
            }
        }

        // Preview'i güncelle (Commit içinde Reset çağrılır, o da Preview=null yapar)
        if (_state != SwitchToolState.Idle)
        {
            Preview = new PreviewSwitch(
                _selectedNodeId,
                _mainSegId != Guid.Empty ? _mainSegId : null,
                _divergingSegId != Guid.Empty ? _divergingSegId : null,
                Guid.Empty, false, _state);
        }
    }

    public void OnPointerUp(SnapResult s, ToolMouseButton b, ToolContext c) { }

    public void OnKeyDown(ToolKey key, ToolContext ctx)
    {
        switch (key)
        {
            case ToolKey.Enter:
                if (_state == SwitchToolState.DivergingSelected) Commit(ctx);
                break;
            case ToolKey.Escape:
                Reset();
                break;
        }
    }

    private void Commit(ToolContext ctx)
    {
        if (_selectedNodeId == Guid.Empty || _mainSegId == Guid.Empty || _divergingSegId == Guid.Empty)
        {
            Reset();
            return;
        }

        // Bayat entity guard'ı
        if (!ctx.Document.TryGetEntity(_selectedNodeId, out var nodeEntity) || nodeEntity is not TrackNode node)
        {
            Reset();
            return;
        }
        if (!ctx.Document.TryGetEntity(_mainSegId, out _) || !ctx.Document.TryGetEntity(_divergingSegId, out _))
        {
            Reset();
            return;
        }
        if (node.Role == NodeRole.SwitchNode)
        {
            Reset();
            return;
        }

        var commands = new List<ICadCommand>();

        // 1. RailSwitch entity'si oluştur
        var railSwitch = new RailSwitch
        {
            NodeId = _selectedNodeId,
            MainSegmentId = _mainSegId,
            DivergingSegmentId = _divergingSegId,
            State = SwitchState.Main,
            LayerId = ctx.Document.ActiveLayerId
        };
        commands.Add(new AddEntityCommand(railSwitch));

        // 2. Node rolünü güncelle
        commands.Add(new SetNodeRoleCommand(_selectedNodeId, NodeRole.SwitchNode));

        // Tek undo adımı
        var composite = new CompositeCadCommand("Makas Oluştur", commands);
        ctx.Commands.Do(composite, ctx.Document);

        // Yeni switch seçili gelsin
        ctx.Selection.Set(new[] { railSwitch.Id });

        Reset();
    }

    private static int KendiSegmentsayisi(Guid nodeId, ToolContext ctx)
    {
        return ctx.Document.Entities.OfType<TrackSegment>()
            .Count(s => s.StartNodeId == nodeId || s.EndNodeId == nodeId);
    }

    private static bool SegmenteBagliMi(Guid segId, Guid nodeId, ToolContext ctx)
    {
        if (!ctx.Document.TryGetEntity(segId, out var e) || e is not TrackSegment seg)
            return false;
        return seg.StartNodeId == nodeId || seg.EndNodeId == nodeId;
    }
}
