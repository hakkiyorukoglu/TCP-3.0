using System;
using System.Collections.Generic;
using TrainService.Cad.Snapping;
using TrainService.Cad.UndoRedo;
using TrainService.Core.Entities;
using TrainService.Core.Enums;
using TrainService.Core.Geometry;

namespace TrainService.Cad.Tools;

/// <summary>
/// SwitchTool — 1 tıkla prefab Switch yerleştirme aracı.
/// Her sol tık, bulunulan pozisyona bir RailSwitch + 3 TrackNode (Entry/MainExit/DivergingExit)
/// oluşturur. Tümü tek CompositeCadCommand ile undo/redo yapılır.
/// Escape: preview'i gizler. Activate/Deactivate: preview sıfırlanır.
/// </summary>
public sealed class SwitchTool : ITool
{
    public string Name => "Switch";

    public PreviewShape? Preview { get; private set; }

    public void Activate(ToolContext ctx) { /* preview yok, move'da oluşur */ }

    public void Deactivate(ToolContext ctx) => Preview = null;

    public void OnPointerMove(SnapResult snapped, ToolContext ctx)
    {
        var pos = snapped.Point;
        double rot = 0; // İlk versiyonda rotation=0 sabit

        Preview = new PreviewSwitchPlace(
            pos, rot,
            SwitchDefaults.EntryOffset(rot) + pos,
            SwitchDefaults.MainExitOffset(rot) + pos,
            SwitchDefaults.DivergingExitOffset(rot) + pos
        );
    }

    public void OnPointerDown(SnapResult snapped, ToolMouseButton button, ToolContext ctx)
    {
        if (button != ToolMouseButton.Left) return;

        var pos = snapped.Point;
        double rot = 0;
        var activeLayer = ctx.Document.ActiveLayerId;

        // 1. Üç TrackNode oluştur (önce node'lar, sonra switch — undo sırası önemli)
        var entryNode = new TrackNode
        {
            Position = SwitchDefaults.EntryOffset(rot) + pos,
            Z = 0,
            Role = NodeRole.Plain,
            LayerId = activeLayer
        };
        var mainNode = new TrackNode
        {
            Position = SwitchDefaults.MainExitOffset(rot) + pos,
            Z = 0,
            Role = NodeRole.Plain,
            LayerId = activeLayer
        };
        var divNode = new TrackNode
        {
            Position = SwitchDefaults.DivergingExitOffset(rot) + pos,
            Z = 0,
            Role = NodeRole.Plain,
            LayerId = activeLayer
        };

        // 2. RailSwitch entity'si
        var railSwitch = new RailSwitch
        {
            Position = pos,
            RotationDeg = rot,
            EntryNodeId = entryNode.Id,
            MainExitNodeId = mainNode.Id,
            DivergingExitNodeId = divNode.Id,
            State = SwitchState.Main,
            LayerId = activeLayer
        };

        // 3. CompositeCadCommand — tek undo adımı
        var cmds = new List<ICadCommand>
        {
            new AddEntityCommand(entryNode),
            new AddEntityCommand(mainNode),
            new AddEntityCommand(divNode),
            new AddEntityCommand(railSwitch)
        };
        var composite = new CompositeCadCommand("Makas Yerleştir", cmds);
        ctx.Commands.Do(composite, ctx.Document);

        // 4. Yeni switch seçili gelsin
        ctx.Selection.Set(new[] { railSwitch.Id });
    }

    public void OnPointerUp(SnapResult snapped, ToolMouseButton button, ToolContext ctx) { }

    public void OnKeyDown(ToolKey key, ToolContext ctx)
    {
        if (key == ToolKey.Escape)
        {
            Preview = null;
        }
    }
}
