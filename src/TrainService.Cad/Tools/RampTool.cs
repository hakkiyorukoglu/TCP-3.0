using System;
using System.Collections.Generic;
using TrainService.Cad.Snapping;
using TrainService.Cad.UndoRedo;
using TrainService.Core.Entities;
using TrainService.Core.Enums;
using TrainService.Core.Geometry;

namespace TrainService.Cad.Tools;

/// <summary>
/// RampTool — 1 tıkla prefab Ramp yerleştirme aracı.
/// Her sol tık, bulunulan pozisyona bir Ramp + 2 TrackNode (Entry/Exit)
/// oluşturur. Tümü tek CompositeCadCommand ile undo/redo yapılır.
/// Escape: preview'i gizler. Activate/Deactivate: preview sıfırlanır.
/// </summary>
public sealed class RampTool : ITool
{
    public string Name => "Ramp";

    public PreviewShape? Preview { get; private set; }

    public void Activate(ToolContext ctx) { /* preview yok, move'da oluşur */ }

    public void Deactivate(ToolContext ctx) => Preview = null;

    public void OnPointerMove(SnapResult snapped, ToolContext ctx)
    {
        var pos = snapped.Point;
        double rot = 0; // İlk versiyonda rotation=0 sabit

        Preview = new PreviewRampPlace(
            pos, rot,
            RampDefaults.EntryOffset(rot) + pos,
            RampDefaults.ExitOffset(rot) + pos
        );
    }

    public void OnPointerDown(SnapResult snapped, ToolMouseButton button, ToolContext ctx)
    {
        if (button != ToolMouseButton.Left) return;

        var pos = snapped.Point;
        double rot = 0;
        var activeLayer = ctx.Document.ActiveLayerId;

        // 1. İki TrackNode oluştur (önce node'lar, sonra ramp)
        var entryNode = new TrackNode
        {
            Position = RampDefaults.EntryOffset(rot) + pos,
            Z = 0,
            Role = NodeRole.Plain,
            LayerId = activeLayer
        };
        var exitNode = new TrackNode
        {
            Position = RampDefaults.ExitOffset(rot) + pos,
            Z = 0,
            Role = NodeRole.Plain,
            LayerId = activeLayer
        };

        // 2. Ramp entity'si
        var ramp = new Ramp
        {
            Position = pos,
            RotationDeg = rot,
            EntryNodeId = entryNode.Id,
            ExitNodeId = exitNode.Id,
            StartZ = RampDefaults.DefaultStartZ,
            EndZ = RampDefaults.DefaultEndZ,
            LengthMm = RampDefaults.LengthMm,
            LayerId = activeLayer
        };

        // 3. CompositeCadCommand — tek undo adımı
        var cmds = new List<ICadCommand>
        {
            new AddEntityCommand(entryNode),
            new AddEntityCommand(exitNode),
            new AddEntityCommand(ramp)
        };
        var composite = new CompositeCadCommand("Rampa Yerleştir", cmds);
        ctx.Commands.Do(composite, ctx.Document);

        // 4. Yeni ramp seçili gelsin
        ctx.Selection.Set(new[] { ramp.Id });
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
