using System;
using System.Linq;
using FluentAssertions;
using Xunit;
using TrainService.Core.Entities;
using TrainService.Core.Enums;
using TrainService.Core.Geometry;
using TrainService.Cad.Tools;
using TrainService.Cad.UndoRedo;
using TrainService.Cad.Selection;
using TrainService.Cad.Snapping;

namespace TrainService.Cad.Tests.Tools;

public class T280_SwitchToolTests
{
    private static (CadDocument doc, CommandStack st, SelectionService sel) Sahne()
    {
        var doc = new CadDocument();
        return (doc, new CommandStack(), new SelectionService());
    }

    private ToolContext CreateCtx(CadDocument doc, CommandStack st, SelectionService sel)
        => new(doc, st, sel) { Clipboard = null! };

    [Fact]
    public void T296_SwitchTool_PointerMove_PreviewOlusur()
    {
        var (doc, st, sel) = Sahne();
        var tool = new SwitchTool();
        var ctx = CreateCtx(doc, st, sel);
        tool.Activate(ctx);

        var pos = new Vector2D(200, 100);
        tool.OnPointerMove(new SnapResult(pos, SnapKind.Grid, null), ctx);

        var preview = tool.Preview.Should().BeOfType<PreviewSwitchPlace>().Subject;
        preview.Position.Should().Be(pos);
        preview.RotationDeg.Should().Be(0);
        // EntryOffset(0) = (-HalfLength, 0) = (-40, 0) → pos + (-40, 0) = (160, 100)
        preview.EntryPos.Should().Be(new Vector2D(160, 100));
        // MainExitOffset(0) = (HalfLength, 0) = (40, 0) → pos + (40, 0) = (240, 100)
        preview.MainExitPos.Should().Be(new Vector2D(240, 100));
        // DivergingExitOffset(0) = (HalfLength*cos30, HalfLength*sin30) = (40*0.866..., 40*0.5) ≈ (34.641, 20)
        preview.DivergingExitPos.X.Should().BeApproximately(200 + 40 * Math.Cos(30 * Math.PI / 180), 1e-10);
        preview.DivergingExitPos.Y.Should().BeApproximately(100 + 40 * Math.Sin(30 * Math.PI / 180), 1e-10);
    }

    [Fact]
    public void T297_SwitchTool_SolTik_EntitylerOlusur()
    {
        var (doc, st, sel) = Sahne();
        var tool = new SwitchTool();
        var ctx = CreateCtx(doc, st, sel);
        tool.Activate(ctx);

        tool.OnPointerMove(new SnapResult(new Vector2D(100, 50), SnapKind.Grid, null), ctx);
        tool.OnPointerDown(new SnapResult(new Vector2D(100, 50), SnapKind.Grid, null), ToolMouseButton.Left, ctx);

        // 3 node + 1 switch = 4 yeni entity
        doc.Entities.Count.Should().Be(4);
        doc.Entities.OfType<TrackNode>().Should().HaveCount(3);
        doc.Entities.OfType<RailSwitch>().Should().HaveCount(1);
    }

    [Fact]
    public void T298_SwitchTool_Commit_TekUndo()
    {
        var (doc, st, sel) = Sahne();
        var tool = new SwitchTool();
        var ctx = CreateCtx(doc, st, sel);
        tool.Activate(ctx);

        tool.OnPointerMove(new SnapResult(new Vector2D(100, 50), SnapKind.Grid, null), ctx);
        tool.OnPointerDown(new SnapResult(new Vector2D(100, 50), SnapKind.Grid, null), ToolMouseButton.Left, ctx);

        int ilkKume = doc.Entities.Count; // 4

        // Tek Ctrl+Z → hepsi gider
        st.Undo(doc);
        doc.Entities.Count.Should().Be(0);
        doc.Entities.OfType<RailSwitch>().Should().BeEmpty();
        doc.Entities.OfType<TrackNode>().Should().BeEmpty();
    }

    [Fact]
    public void T299_SwitchTool_Escape_PreviewGizlenir()
    {
        var (doc, st, sel) = Sahne();
        var tool = new SwitchTool();
        var ctx = CreateCtx(doc, st, sel);
        tool.Activate(ctx);

        tool.OnPointerMove(new SnapResult(new Vector2D(100, 50), SnapKind.Grid, null), ctx);
        tool.Preview.Should().NotBeNull();

        tool.OnKeyDown(ToolKey.Escape, ctx);
        tool.Preview.Should().BeNull();
    }

    [Fact]
    public void T300_SwitchTool_SagTik_YokSayilir()
    {
        var (doc, st, sel) = Sahne();
        var tool = new SwitchTool();
        var ctx = CreateCtx(doc, st, sel);
        tool.Activate(ctx);

        tool.OnPointerMove(new SnapResult(new Vector2D(100, 50), SnapKind.Grid, null), ctx);
        tool.OnPointerDown(new SnapResult(new Vector2D(100, 50), SnapKind.Grid, null), ToolMouseButton.Right, ctx);

        // Sağ tık hiçbir entity oluşturmaz
        doc.Entities.Count.Should().Be(0);
    }

    [Fact]
    public void T301_SwitchTool_TrackNodePozisyonlari()
    {
        var (doc, st, sel) = Sahne();
        var tool = new SwitchTool();
        var ctx = CreateCtx(doc, st, sel);
        tool.Activate(ctx);

        var pos = new Vector2D(200, 100);
        tool.OnPointerMove(new SnapResult(pos, SnapKind.Grid, null), ctx);
        tool.OnPointerDown(new SnapResult(pos, SnapKind.Grid, null), ToolMouseButton.Left, ctx);

        var nodes = doc.Entities.OfType<TrackNode>().ToList();
        nodes.Should().HaveCount(3);

        // Entry node solda (-X)
        nodes.Any(n => n.Position == new Vector2D(160, 100)).Should().BeTrue("Entry node solda olmalı");
        // MainExit node sağda (+X, düz)
        nodes.Any(n => n.Position == new Vector2D(240, 100)).Should().BeTrue("MainExit node sağda olmalı");
        // DivergingExit node sağ-üstte (+X, +Y, 30°)
        nodes.Any(n =>
            Math.Abs(n.Position.X - (200 + 40 * Math.Cos(30 * Math.PI / 180))) < 1e-10 &&
            Math.Abs(n.Position.Y - (100 + 40 * Math.Sin(30 * Math.PI / 180))) < 1e-10)
            .Should().BeTrue("DivergingExit node sağ-üstte olmalı (30°)");
    }

    [Fact]
    public void T302_SwitchTool_RailSwitchProperties()
    {
        var (doc, st, sel) = Sahne();
        var tool = new SwitchTool();
        var ctx = CreateCtx(doc, st, sel);
        tool.Activate(ctx);

        tool.OnPointerMove(new SnapResult(new Vector2D(100, 50), SnapKind.Grid, null), ctx);
        tool.OnPointerDown(new SnapResult(new Vector2D(100, 50), SnapKind.Grid, null), ToolMouseButton.Left, ctx);

        var sw = doc.Entities.OfType<RailSwitch>().Single();
        sw.Position.Should().Be(new Vector2D(100, 50));
        sw.RotationDeg.Should().Be(0);
        sw.State.Should().Be(SwitchState.Main);
        // EntryNodeId, MainExitNodeId, DivergingExitNodeId üç farklı TrackNode'a işaret eder
        sw.EntryNodeId.Should().NotBeEmpty();
        sw.MainExitNodeId.Should().NotBeEmpty();
        sw.DivergingExitNodeId.Should().NotBeEmpty();
        var ids = new[] { sw.EntryNodeId, sw.MainExitNodeId, sw.DivergingExitNodeId };
        ids.Distinct().Should().HaveCount(3, "Üç port farklı node'lara bağlanmalı");
    }

    [Fact]
    public void T303_SwitchTool_ActivateDeactivate_PreviewSifirlanir()
    {
        var (doc, st, sel) = Sahne();
        var tool = new SwitchTool();
        var ctx = CreateCtx(doc, st, sel);

        tool.Activate(ctx);
        tool.Preview.Should().BeNull();

        tool.OnPointerMove(new SnapResult(new Vector2D(100, 50), SnapKind.Grid, null), ctx);
        tool.Preview.Should().NotBeNull();

        tool.Deactivate(ctx);
        tool.Preview.Should().BeNull();

        // Activate tekrar çağrılınca preview hala null
        tool.Activate(ctx);
        tool.Preview.Should().BeNull();
    }

    [Fact]
    public void T304_SwitchTool_Selection_SonSwitchSecili()
    {
        var (doc, st, sel) = Sahne();
        var tool = new SwitchTool();
        var ctx = CreateCtx(doc, st, sel);
        tool.Activate(ctx);

        tool.OnPointerMove(new SnapResult(new Vector2D(100, 50), SnapKind.Grid, null), ctx);
        tool.OnPointerDown(new SnapResult(new Vector2D(100, 50), SnapKind.Grid, null), ToolMouseButton.Left, ctx);

        var sw = doc.Entities.OfType<RailSwitch>().Single();
        sel.SelectedIds.Should().ContainSingle().Which.Should().Be(sw.Id);
    }
}
