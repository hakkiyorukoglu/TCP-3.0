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
    private static (CadDocument doc, CommandStack st, SelectionService sel,
                    TrackNode nA, TrackNode nB, TrackNode nC, TrackNode nD,
                    TrackSegment s1, TrackSegment s2, TrackSegment s3) SahneKavsak()
    {
        var doc = new CadDocument();
        var st = new CommandStack();
        var sel = new SelectionService();

        var nA = new TrackNode { Position = new Vector2D(0, 0), LayerId = doc.ActiveLayerId };
        var nB = new TrackNode { Position = new Vector2D(100, 0), LayerId = doc.ActiveLayerId };
        var nC = new TrackNode { Position = new Vector2D(200, 0), LayerId = doc.ActiveLayerId };
        var nD = new TrackNode { Position = new Vector2D(100, 100), LayerId = doc.ActiveLayerId };

        var s1 = new TrackSegment { StartNodeId = nA.Id, EndNodeId = nB.Id, LayerId = doc.ActiveLayerId };
        var s2 = new TrackSegment { StartNodeId = nB.Id, EndNodeId = nC.Id, LayerId = doc.ActiveLayerId };
        var s3 = new TrackSegment { StartNodeId = nB.Id, EndNodeId = nD.Id, LayerId = doc.ActiveLayerId };

        doc.AddEntity(nA); doc.AddEntity(nB); doc.AddEntity(nC); doc.AddEntity(nD);
        doc.AddEntity(s1); doc.AddEntity(s2); doc.AddEntity(s3);

        return (doc, st, sel, nA, nB, nC, nD, s1, s2, s3);
    }

    private static SnapResult EndSnap(TrackNode n) => new(n.Position, SnapKind.Endpoint, n.Id);
    private static SnapResult SegSnap(TrackSegment s, Vector2D p) => new(p, SnapKind.OnSegment, s.Id);

    private ToolContext CreateCtx(CadDocument doc, CommandStack st, SelectionService sel)
        => new(doc, st, sel) { Clipboard = null! };

    [Fact]
    public void T270_NodeSec_MakasBaslar()
    {
        var (doc, st, sel, nA, nB, nC, nD, s1, s2, s3) = SahneKavsak();
        var tool = new SwitchTool();
        var ctx = CreateCtx(doc, st, sel);
        tool.Activate(ctx);

        // nB'ye doğru hareket et (Endpoint snap)
        tool.OnPointerMove(EndSnap(nB), ctx);

        var preview = tool.Preview.Should().BeOfType<PreviewSwitch>().Subject;
        preview.NodeId.Should().Be(nB.Id);
        preview.AdayGecerli.Should().BeTrue();

        // Tıkla
        tool.OnPointerDown(EndSnap(nB), ToolMouseButton.Left, ctx);

        preview = tool.Preview.Should().BeOfType<PreviewSwitch>().Subject;
        preview.MachineState.Should().Be(SwitchToolState.NodeSelected);
        preview.NodeId.Should().Be(nB.Id);
    }

    [Fact]
    public void T271_MainSegmentSec_StateGecer()
    {
        var (doc, st, sel, nA, nB, nC, nD, s1, s2, s3) = SahneKavsak();
        var tool = new SwitchTool();
        var ctx = CreateCtx(doc, st, sel);
        tool.Activate(ctx);

        // nB seç
        tool.OnPointerMove(EndSnap(nB), ctx);
        tool.OnPointerDown(EndSnap(nB), ToolMouseButton.Left, ctx);

        // s2'ye OnSegment tık
        tool.OnPointerMove(SegSnap(s2, new Vector2D(150, 0)), ctx);
        tool.OnPointerDown(SegSnap(s2, new Vector2D(150, 0)), ToolMouseButton.Left, ctx);

        var preview = tool.Preview.Should().BeOfType<PreviewSwitch>().Subject;
        preview.MachineState.Should().Be(SwitchToolState.MainSelected);
        preview.MainSegmentId.Should().Be(s2.Id);
    }

    [Fact]
    public void T272_DivergingSec_AutoCommit()
    {
        var (doc, st, sel, nA, nB, nC, nD, s1, s2, s3) = SahneKavsak();
        var tool = new SwitchTool();
        var ctx = CreateCtx(doc, st, sel);
        tool.Activate(ctx);

        // nB seç
        tool.OnPointerMove(EndSnap(nB), ctx);
        tool.OnPointerDown(EndSnap(nB), ToolMouseButton.Left, ctx);

        // Main = s2
        tool.OnPointerMove(SegSnap(s2, new Vector2D(150, 0)), ctx);
        tool.OnPointerDown(SegSnap(s2, new Vector2D(150, 0)), ToolMouseButton.Left, ctx);

        // Diverging = s3 → auto-commit
        tool.OnPointerMove(SegSnap(s3, new Vector2D(100, 50)), ctx);
        tool.OnPointerDown(SegSnap(s3, new Vector2D(100, 50)), ToolMouseButton.Left, ctx);

        // RailSwitch entity oluşmuş olmalı
        var railSwitch = doc.Entities.OfType<RailSwitch>().Single();
        railSwitch.NodeId.Should().Be(nB.Id);
        railSwitch.MainSegmentId.Should().Be(s2.Id);
        railSwitch.DivergingSegmentId.Should().Be(s3.Id);
        railSwitch.State.Should().Be(SwitchState.Main);

        // Preview null (Reset çağrıldı)
        tool.Preview.Should().BeNull();
    }

    [Fact]
    public void T273_Commit_RailSwitchDogruAlanlar()
    {
        var (doc, st, sel, nA, nB, nC, nD, s1, s2, s3) = SahneKavsak();
        var tool = new SwitchTool();
        var ctx = CreateCtx(doc, st, sel);
        tool.Activate(ctx);

        // 3 tık
        tool.OnPointerMove(EndSnap(nB), ctx);
        tool.OnPointerDown(EndSnap(nB), ToolMouseButton.Left, ctx);
        tool.OnPointerMove(SegSnap(s2, new Vector2D(150, 0)), ctx);
        tool.OnPointerDown(SegSnap(s2, new Vector2D(150, 0)), ToolMouseButton.Left, ctx);
        tool.OnPointerMove(SegSnap(s3, new Vector2D(100, 50)), ctx);
        tool.OnPointerDown(SegSnap(s3, new Vector2D(100, 50)), ToolMouseButton.Left, ctx);

        var sw = doc.Entities.OfType<RailSwitch>().Single();
        sw.NodeId.Should().Be(nB.Id);
        sw.MainSegmentId.Should().Be(s2.Id);
        sw.DivergingSegmentId.Should().Be(s3.Id);
        sw.State.Should().Be(SwitchState.Main);
        sw.LayerId.Should().Be(doc.ActiveLayerId);
    }

    [Fact]
    public void T274_Commit_NodeRoleSwitchNode()
    {
        var (doc, st, sel, nA, nB, nC, nD, s1, s2, s3) = SahneKavsak();
        var tool = new SwitchTool();
        var ctx = CreateCtx(doc, st, sel);
        tool.Activate(ctx);

        // 3 tık
        tool.OnPointerMove(EndSnap(nB), ctx);
        tool.OnPointerDown(EndSnap(nB), ToolMouseButton.Left, ctx);
        tool.OnPointerMove(SegSnap(s2, new Vector2D(150, 0)), ctx);
        tool.OnPointerDown(SegSnap(s2, new Vector2D(150, 0)), ToolMouseButton.Left, ctx);
        tool.OnPointerMove(SegSnap(s3, new Vector2D(100, 50)), ctx);
        tool.OnPointerDown(SegSnap(s3, new Vector2D(100, 50)), ToolMouseButton.Left, ctx);

        // Node rolü güncellenmiş olmalı
        nB.Role.Should().Be(NodeRole.SwitchNode);
        // Diğer düğümler etkilenmemeli
        nA.Role.Should().Be(NodeRole.Plain);
        nC.Role.Should().Be(NodeRole.Plain);
        nD.Role.Should().Be(NodeRole.Plain);
    }

    [Fact]
    public void T275_Undo_MakasGeriAlinir()
    {
        var (doc, st, sel, nA, nB, nC, nD, s1, s2, s3) = SahneKavsak();
        var tool = new SwitchTool();
        var ctx = CreateCtx(doc, st, sel);
        tool.Activate(ctx);

        // 3 tık → commit
        tool.OnPointerMove(EndSnap(nB), ctx);
        tool.OnPointerDown(EndSnap(nB), ToolMouseButton.Left, ctx);
        tool.OnPointerMove(SegSnap(s2, new Vector2D(150, 0)), ctx);
        tool.OnPointerDown(SegSnap(s2, new Vector2D(150, 0)), ToolMouseButton.Left, ctx);
        tool.OnPointerMove(SegSnap(s3, new Vector2D(100, 50)), ctx);
        tool.OnPointerDown(SegSnap(s3, new Vector2D(100, 50)), ToolMouseButton.Left, ctx);

        // Undo
        st.Undo(doc);

        // RailSwitch silinmiş olmalı
        doc.Entities.OfType<RailSwitch>().Should().BeEmpty();
        // NodeRole geri dönmüş olmalı
        nB.Role.Should().Be(NodeRole.Plain);
    }

    [Fact]
    public void T276_Esc_Iptal_StateSifirlanir()
    {
        var (doc, st, sel, nA, nB, nC, nD, s1, s2, s3) = SahneKavsak();
        var tool = new SwitchTool();
        var ctx = CreateCtx(doc, st, sel);
        tool.Activate(ctx);

        // nB seç
        tool.OnPointerMove(EndSnap(nB), ctx);
        tool.OnPointerDown(EndSnap(nB), ToolMouseButton.Left, ctx);

        // Esc
        tool.OnKeyDown(ToolKey.Escape, ctx);

        // State sıfırlanmış olmalı
        tool.Preview.Should().BeNull();
        doc.Entities.OfType<RailSwitch>().Should().BeEmpty();
        nB.Role.Should().Be(NodeRole.Plain);
    }

    [Fact]
    public void T277_AyniSegmentMainDiverging_Reddedilir()
    {
        var (doc, st, sel, nA, nB, nC, nD, s1, s2, s3) = SahneKavsak();
        var tool = new SwitchTool();
        var ctx = CreateCtx(doc, st, sel);
        tool.Activate(ctx);

        // nB seç
        tool.OnPointerMove(EndSnap(nB), ctx);
        tool.OnPointerDown(EndSnap(nB), ToolMouseButton.Left, ctx);

        // Main = s2
        tool.OnPointerMove(SegSnap(s2, new Vector2D(150, 0)), ctx);
        tool.OnPointerDown(SegSnap(s2, new Vector2D(150, 0)), ToolMouseButton.Left, ctx);

        // Aynı segmenti (s2) Diverging olarak dene → red
        tool.OnPointerMove(SegSnap(s2, new Vector2D(150, 0)), ctx);
        var preview = tool.Preview.Should().BeOfType<PreviewSwitch>().Subject;
        preview.AdayGecerli.Should().BeFalse();

        tool.OnPointerDown(SegSnap(s2, new Vector2D(150, 0)), ToolMouseButton.Left, ctx);

        // State hala MainSelected (değişmedi)
        preview = tool.Preview.Should().BeOfType<PreviewSwitch>().Subject;
        preview.MachineState.Should().Be(SwitchToolState.MainSelected);

        // Henüz RailSwitch oluşmamış
        doc.Entities.OfType<RailSwitch>().Should().BeEmpty();
    }

    [Fact]
    public void T278_AzBaglantiliNode_Reddedilir()
    {
        var (doc, st, sel, nA, nB, nC, nD, s1, s2, s3) = SahneKavsak();
        var tool = new SwitchTool();
        var ctx = CreateCtx(doc, st, sel);
        tool.Activate(ctx);

        // nA'nın sadece 1 bağlantısı var (s1)
        tool.OnPointerMove(EndSnap(nA), ctx);

        var preview = tool.Preview.Should().BeOfType<PreviewSwitch>().Subject;
        preview.AdayGecerli.Should().BeFalse();

        // Tık yok sayılır
        tool.OnPointerDown(EndSnap(nA), ToolMouseButton.Left, ctx);

        preview = tool.Preview.Should().BeOfType<PreviewSwitch>().Subject;
        preview.MachineState.Should().Be(SwitchToolState.Idle);
    }

    [Fact]
    public void T279_ZatenSwitchNode_Reddedilir()
    {
        var (doc, st, sel, nA, nB, nC, nD, s1, s2, s3) = SahneKavsak();
        // nB'yi önceden SwitchNode yap
        nB.Role = NodeRole.SwitchNode;

        var tool = new SwitchTool();
        var ctx = CreateCtx(doc, st, sel);
        tool.Activate(ctx);

        // nB'ye tıkla
        tool.OnPointerMove(EndSnap(nB), ctx);
        var preview = tool.Preview.Should().BeOfType<PreviewSwitch>().Subject;
        preview.AdayGecerli.Should().BeFalse();

        tool.OnPointerDown(EndSnap(nB), ToolMouseButton.Left, ctx);

        // State değişmemeli
        preview = tool.Preview.Should().BeOfType<PreviewSwitch>().Subject;
        preview.MachineState.Should().Be(SwitchToolState.Idle);
    }
}
