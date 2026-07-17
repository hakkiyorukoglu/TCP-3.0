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

public class T270_SwitchToolTests
{
    private static ToolContext CreateCtx(CadDocument doc, CommandStack st, SelectionService sel)
        => new(doc, st, sel) { Clipboard = null! };

    private static SnapResult AnySnap(Vector2D pos) => new(pos, SnapKind.Grid, Guid.Empty);

    [Fact]
    public void T270_ToolAdi_Switch()
    {
        var tool = new SwitchTool();
        tool.Name.Should().Be("Switch");
    }

    [Fact]
    public void T271_PointerMove_PreviewSwitchPlaceOlusur()
    {
        var doc = new CadDocument();
        var tool = new SwitchTool();
        var ctx = CreateCtx(doc, new CommandStack(), new SelectionService());
        tool.Activate(ctx);

        var pos = new Vector2D(100, 200);
        tool.OnPointerMove(AnySnap(pos), ctx);

        var preview = tool.Preview.Should().BeOfType<PreviewSwitchPlace>().Subject;
        preview.Position.Should().Be(pos);
        preview.RotationDeg.Should().Be(0);
        preview.EntryPos.Should().Be(SwitchDefaults.EntryOffset(0) + pos);
        preview.MainExitPos.Should().Be(SwitchDefaults.MainExitOffset(0) + pos);
        preview.DivergingExitPos.Should().Be(SwitchDefaults.DivergingExitOffset(0) + pos);
    }

    [Fact]
    public void T272_Tikla_DortEntityOlusur()
    {
        var doc = new CadDocument();
        var st = new CommandStack();
        var sel = new SelectionService();
        var tool = new SwitchTool();
        var ctx = CreateCtx(doc, st, sel);
        tool.Activate(ctx);

        int ilkKume = doc.Entities.Count;
        var pos = new Vector2D(200, 300);
        tool.OnPointerMove(AnySnap(pos), ctx);
        tool.OnPointerDown(AnySnap(pos), ToolMouseButton.Left, ctx);

        doc.Entities.Count.Should().Be(ilkKume + 4);
        doc.Entities.OfType<RailSwitch>().Should().ContainSingle();
        doc.Entities.OfType<TrackNode>().Should().HaveCount(3);
    }

    [Fact]
    public void T273_PortNodePozisyonlari_SwitchDefaultsIleUyumlu()
    {
        var doc = new CadDocument();
        var st = new CommandStack();
        var sel = new SelectionService();
        var tool = new SwitchTool();
        var ctx = CreateCtx(doc, st, sel);
        tool.Activate(ctx);

        var pos = new Vector2D(0, 0);
        tool.OnPointerMove(AnySnap(pos), ctx);
        tool.OnPointerDown(AnySnap(pos), ToolMouseButton.Left, ctx);

        var nodes = doc.Entities.OfType<TrackNode>().ToList();
        nodes.Should().HaveCount(3);

        var entryOffset = SwitchDefaults.EntryOffset(0);
        var mainOffset  = SwitchDefaults.MainExitOffset(0);
        var divOffset   = SwitchDefaults.DivergingExitOffset(0);

        nodes.Should().Contain(n => n.Position == entryOffset);
        nodes.Should().Contain(n => n.Position == mainOffset);
        nodes.Should().Contain(n => n.Position == divOffset);
    }

    [Fact]
    public void T274_RailSwitch_Position_SnappedPointIleEslesir()
    {
        var doc = new CadDocument();
        var st = new CommandStack();
        var sel = new SelectionService();
        var tool = new SwitchTool();
        var ctx = CreateCtx(doc, st, sel);
        tool.Activate(ctx);

        var pos = new Vector2D(150, -80);
        tool.OnPointerMove(AnySnap(pos), ctx);
        tool.OnPointerDown(AnySnap(pos), ToolMouseButton.Left, ctx);

        var rs = doc.Entities.OfType<RailSwitch>().Single();
        rs.Position.Should().Be(pos);
        rs.RotationDeg.Should().Be(0);
        rs.State.Should().Be(SwitchState.Main);
        rs.LayerId.Should().Be(doc.ActiveLayerId);
    }

    [Fact]
    public void T275_CompositeCommand_TekUndoIleDordunuSiler()
    {
        var doc = new CadDocument();
        var st = new CommandStack();
        var sel = new SelectionService();
        var tool = new SwitchTool();
        var ctx = CreateCtx(doc, st, sel);
        tool.Activate(ctx);

        int ilkKume = doc.Entities.Count;

        var pos = new Vector2D(50, 50);
        tool.OnPointerMove(AnySnap(pos), ctx);
        tool.OnPointerDown(AnySnap(pos), ToolMouseButton.Left, ctx);

        doc.Entities.Count.Should().Be(ilkKume + 4);

        // Composite description kontrolü
        st.PeekUndoDescription.Should().Be("Makas Yerleştir");

        // Tek Undo ile 4 entity de silinmeli
        st.Undo(doc);
        doc.Entities.Count.Should().Be(ilkKume);
        doc.Entities.OfType<RailSwitch>().Should().BeEmpty();
        doc.Entities.OfType<TrackNode>().Should().BeEmpty();
    }

    [Fact]
    public void T276_Undo_TumSwitchEntityleriSilinir()
    {
        var doc = new CadDocument();
        var st = new CommandStack();
        var sel = new SelectionService();
        var tool = new SwitchTool();
        var ctx = CreateCtx(doc, st, sel);
        tool.Activate(ctx);

        var pos = new Vector2D(100, 100);
        tool.OnPointerMove(AnySnap(pos), ctx);
        tool.OnPointerDown(AnySnap(pos), ToolMouseButton.Left, ctx);

        st.Undo(doc);

        doc.Entities.OfType<RailSwitch>().Should().BeEmpty();
        doc.Entities.OfType<TrackNode>().Should().BeEmpty();
    }

    [Fact]
    public void T277_Escape_PreviewTemizlenir()
    {
        var doc = new CadDocument();
        var tool = new SwitchTool();
        var ctx = CreateCtx(doc, new CommandStack(), new SelectionService());
        tool.Activate(ctx);

        tool.OnPointerMove(AnySnap(new Vector2D(50, 50)), ctx);
        tool.Preview.Should().NotBeNull();

        tool.OnKeyDown(ToolKey.Escape, ctx);
        tool.Preview.Should().BeNull();
    }

    [Fact]
    public void T278_Deactivate_PreviewTemizlenir()
    {
        var doc = new CadDocument();
        var tool = new SwitchTool();
        var ctx = CreateCtx(doc, new CommandStack(), new SelectionService());
        tool.Activate(ctx);

        tool.OnPointerMove(AnySnap(new Vector2D(50, 50)), ctx);
        tool.Preview.Should().NotBeNull();

        tool.Deactivate(ctx);
        tool.Preview.Should().BeNull();
    }

    [Fact]
    public void T279_ArdArdaYerlestirme_Reentrant()
    {
        var doc = new CadDocument();
        var st = new CommandStack();
        var sel = new SelectionService();
        var tool = new SwitchTool();
        var ctx = CreateCtx(doc, st, sel);
        tool.Activate(ctx);

        // 1. yerleştirme
        tool.OnPointerMove(AnySnap(new Vector2D(0, 0)), ctx);
        tool.OnPointerDown(AnySnap(new Vector2D(0, 0)), ToolMouseButton.Left, ctx);
        doc.Entities.OfType<RailSwitch>().Should().ContainSingle();
        doc.Entities.OfType<TrackNode>().Should().HaveCount(3);

        // Preview resetlenmiş ve tekrar ayarlanabilir
        tool.OnPointerMove(AnySnap(new Vector2D(200, 200)), ctx);
        tool.Preview.Should().NotBeNull();

        // 2. yerleştirme
        tool.OnPointerDown(AnySnap(new Vector2D(200, 200)), ToolMouseButton.Left, ctx);
        doc.Entities.OfType<RailSwitch>().Should().HaveCount(2);
        doc.Entities.OfType<TrackNode>().Should().HaveCount(6);
    }
}
