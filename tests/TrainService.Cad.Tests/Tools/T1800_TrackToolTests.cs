using System;
using System.Linq;
using FluentAssertions;
using TrainService.Cad;
using TrainService.Cad.Snapping;
using TrainService.Cad.Tools;
using TrainService.Cad.UndoRedo;
using TrainService.Core.Entities;
using TrainService.Core.Geometry;
using Xunit;

namespace TrainService.Cad.Tests.Tools;

public class T1800_TrackToolTests
{
    private (CadDocument doc, CommandStack cmds, ToolContext ctx, TrackTool tool) Setup()
    {
        var doc = new CadDocument();
        var cmds = new CommandStack();
        var sel = new TrainService.Cad.Selection.SelectionService();
        var ctx = new ToolContext(doc, cmds, sel);
        var tool = new TrackTool();
        tool.Activate(ctx);
        return (doc, cmds, ctx, tool);
    }

    private SnapResult S(double x, double y) => new SnapResult(new Vector2D(x, y), SnapKind.None, null);

    [Fact]
    public void T1803_TrackTool_IlkTik_DokumanaYAZMAZ()
    {
        var (doc, cmds, ctx, tool) = Setup();
        
        tool.OnPointerDown(S(10, 10), ToolMouseButton.Left, ctx);
        
        doc.Entities.Should().BeEmpty();
        // CommandStack is empty (no property to check easily, but doc is empty is sufficient)
    }

    [Fact]
    public void T1804_TrackTool_IlkSegment_TekKomut_UcEntity()
    {
        var (doc, cmds, ctx, tool) = Setup();
        
        tool.OnPointerDown(S(10, 10), ToolMouseButton.Left, ctx);
        tool.OnPointerDown(S(20, 10), ToolMouseButton.Left, ctx);
        
        doc.Entities.Count.Should().Be(3); // 2 Node + 1 Segment
        doc.Entities.OfType<TrackNode>().Count().Should().Be(2);
        doc.Entities.OfType<TrackSegment>().Count().Should().Be(1);
    }

    [Fact]
    public void T1805_TrackTool_IlkSegment_Undo_HicYetimKalmaz()
    {
        var (doc, cmds, ctx, tool) = Setup();
        
        tool.OnPointerDown(S(10, 10), ToolMouseButton.Left, ctx);
        tool.OnPointerDown(S(20, 10), ToolMouseButton.Left, ctx);
        
        cmds.Undo(doc);
        
        doc.Entities.Should().BeEmpty();
    }

    [Fact]
    public void T1806_TrackTool_ZincirSegment_IkiEntity()
    {
        var (doc, cmds, ctx, tool) = Setup();
        
        tool.OnPointerDown(S(10, 10), ToolMouseButton.Left, ctx);
        tool.OnPointerDown(S(20, 10), ToolMouseButton.Left, ctx); // 1. segment -> 3 entity
        tool.OnPointerDown(S(30, 10), ToolMouseButton.Left, ctx); // 2. segment -> +2 entity
        
        doc.Entities.Count.Should().Be(5); // 3 Node, 2 Segment
        doc.Entities.OfType<TrackNode>().Count().Should().Be(3);
        doc.Entities.OfType<TrackSegment>().Count().Should().Be(2);
    }

    [Fact]
    public void T1807_TrackTool_ZincirUndoSirasi()
    {
        var (doc, cmds, ctx, tool) = Setup();
        
        tool.OnPointerDown(S(0, 0), ToolMouseButton.Left, ctx);
        tool.OnPointerDown(S(10, 0), ToolMouseButton.Left, ctx); // 1
        tool.OnPointerDown(S(20, 0), ToolMouseButton.Left, ctx); // 2
        tool.OnPointerDown(S(30, 0), ToolMouseButton.Left, ctx); // 3
        
        doc.Entities.Count.Should().Be(7); // 4 Node, 3 Segment
        
        cmds.Undo(doc); // Undo 3rd segment
        doc.Entities.Count.Should().Be(5); // 3 Node, 2 Segment
        
        cmds.Undo(doc); // Undo 2nd segment
        doc.Entities.Count.Should().Be(3); // 2 Node, 1 Segment
        
        cmds.Undo(doc); // Undo 1st segment
        doc.Entities.Should().BeEmpty();
    }

    [Fact]
    public void T1808_TrackTool_SifirUzunluk_Reddi()
    {
        var (doc, cmds, ctx, tool) = Setup();
        
        tool.OnPointerDown(S(10, 10), ToolMouseButton.Left, ctx);
        tool.OnPointerDown(S(10, 10), ToolMouseButton.Left, ctx); // Same point
        
        doc.Entities.Should().BeEmpty(); // Still chaining, ignored
        
        tool.OnPointerMove(S(20, 10), ctx);
        tool.Preview.Should().NotBeNull();
    }

    [Fact]
    public void T1809_TrackTool_Esc_CommitliyeDokunmaz()
    {
        var (doc, cmds, ctx, tool) = Setup();
        
        tool.OnPointerDown(S(0, 0), ToolMouseButton.Left, ctx);
        tool.OnPointerDown(S(10, 0), ToolMouseButton.Left, ctx);
        tool.OnPointerDown(S(20, 0), ToolMouseButton.Left, ctx);
        
        tool.OnKeyDown(ToolKey.Escape, ctx);
        
        doc.Entities.Count.Should().Be(5); // 3 Node, 2 Segment
        tool.Preview.Should().BeNull(); // Idle state
    }

    [Fact]
    public void T1810_TrackTool_SagTik_ZinciriBitirir()
    {
        var (doc, cmds, ctx, tool) = Setup();
        
        tool.OnPointerDown(S(0, 0), ToolMouseButton.Left, ctx);
        tool.OnPointerDown(S(10, 0), ToolMouseButton.Left, ctx);
        
        tool.OnPointerDown(S(10, 0), ToolMouseButton.Right, ctx); // End chain
        
        tool.OnPointerDown(S(20, 0), ToolMouseButton.Left, ctx);
        tool.OnPointerDown(S(30, 0), ToolMouseButton.Left, ctx);
        
        // Two separate chains: 2 nodes, 1 seg each = 6 entities
        doc.Entities.Count.Should().Be(6); 
    }

    [Fact]
    public void T1811_TrackTool_Deactivate_SessizIptal()
    {
        var (doc, cmds, ctx, tool) = Setup();
        
        tool.OnPointerDown(S(10, 10), ToolMouseButton.Left, ctx);
        tool.Deactivate(ctx);
        
        doc.Entities.Should().BeEmpty();
        tool.Preview.Should().BeNull();
    }

    [Fact]
    public void T1812_TrackTool_Preview_IsValid()
    {
        var (doc, cmds, ctx, tool) = Setup();
        
        tool.Preview.Should().BeNull();
        
        tool.OnPointerDown(S(10, 10), ToolMouseButton.Left, ctx);
        
        tool.OnPointerMove(S(10, 10), ctx);
        tool.Preview.Should().BeOfType<PreviewLine>().Which.IsValid.Should().BeFalse();
        
        tool.OnPointerMove(S(20, 10), ctx);
        tool.Preview.Should().BeOfType<PreviewLine>().Which.IsValid.Should().BeTrue();
    }

    [Fact]
    public void T1813_TrackTool_Segment_LengthVeLayer()
    {
        var (doc, cmds, ctx, tool) = Setup();
        
        tool.OnPointerDown(S(0, 0), ToolMouseButton.Left, ctx);
        tool.OnPointerDown(S(0, 100), ToolMouseButton.Left, ctx); // length = 100
        
        var segment = doc.Entities.OfType<TrackSegment>().Single();
        segment.LengthMm.Should().BeApproximately(100.0, 1e-6);
        segment.LayerId.Should().Be(doc.ActiveLayerId);
    }

    [Fact]
    public void T1814_TrackTool_SadeceSnapNoktasiKullanir()
    {
        var (doc, cmds, ctx, tool) = Setup();
        
        tool.OnPointerDown(S(12.34, 56.78), ToolMouseButton.Left, ctx);
        tool.OnPointerDown(S(100, 200), ToolMouseButton.Left, ctx);
        
        var node1 = doc.Entities.OfType<TrackNode>().First(n => n.Position.X < 50);
        node1.Position.X.Should().BeApproximately(12.34, 1e-6);
        node1.Position.Y.Should().BeApproximately(56.78, 1e-6);
    }
}
