using System;
using System.Linq;
using FluentAssertions;
using Xunit;
using TrainService.Core.Entities;
using TrainService.Core.Geometry;
using TrainService.Cad.Tools;
using TrainService.Cad.UndoRedo;
using TrainService.Cad.Selection;
using TrainService.Cad.Snapping;
using TrainService.Core.Enums;

namespace TrainService.Cad.Tests.Tools;

public class T260_HybridToolTests
{
    private static (CadDocument doc, CommandStack st, SelectionService sel) Sahne3Katman()
    {
        var doc = new CadDocument();
        return (doc, new CommandStack(), new SelectionService());
    }

    /// <summary>Düz zincir: nA(0,0)─s1─nB(100,0)─s2─nC(200,0)─s3─nD(300,0)─s4─nE(400,0)</summary>
    private static (CadDocument doc, CommandStack st, SelectionService sel,
                    TrackSegment s1, TrackSegment s2, TrackSegment s3) SahneDuzZincir()
    {
        var (doc, st, sel) = Sahne3Katman();

        var nA = new TrackNode { Position = new Vector2D(0, 0), LayerId = doc.ActiveLayerId };
        var nB = new TrackNode { Position = new Vector2D(100, 0), LayerId = doc.ActiveLayerId };
        var nC = new TrackNode { Position = new Vector2D(200, 0), LayerId = doc.ActiveLayerId };
        var nD = new TrackNode { Position = new Vector2D(300, 0), LayerId = doc.ActiveLayerId };
        var nE = new TrackNode { Position = new Vector2D(400, 0), LayerId = doc.ActiveLayerId };

        var s1 = new TrackSegment { StartNodeId = nA.Id, EndNodeId = nB.Id, LayerId = doc.ActiveLayerId };
        var s2 = new TrackSegment { StartNodeId = nB.Id, EndNodeId = nC.Id, LayerId = doc.ActiveLayerId };
        var s3 = new TrackSegment { StartNodeId = nC.Id, EndNodeId = nD.Id, LayerId = doc.ActiveLayerId };
        var s4 = new TrackSegment { StartNodeId = nD.Id, EndNodeId = nE.Id, LayerId = doc.ActiveLayerId };

        doc.AddEntity(nA); doc.AddEntity(nB); doc.AddEntity(nC); doc.AddEntity(nD); doc.AddEntity(nE);
        doc.AddEntity(s1); doc.AddEntity(s2); doc.AddEntity(s3); doc.AddEntity(s4);

        return (doc, st, sel, s1, s2, s3);
    }

    private static SnapResult SegSnap(TrackSegment s, Vector2D p) => new(p, SnapKind.OnSegment, s.Id);

    private ToolContext CreateCtx(CadDocument doc, CommandStack st, SelectionService sel)
        => new(doc, st, sel) { Clipboard = null! };

    [Fact]
    public void T260_IlkTik_ChainingBaslar()
    {
        var (doc, st, sel, s1, _, _) = SahneDuzZincir();
        var tool = new HybridTool();
        var ctx = CreateCtx(doc, st, sel);
        tool.Activate(ctx);

        tool.OnPointerMove(SegSnap(s1, new Vector2D(50, 0)), ctx);
        tool.OnPointerDown(SegSnap(s1, new Vector2D(50, 0)), ToolMouseButton.Left, ctx);

        var preview = tool.Preview.Should().BeOfType<PreviewHybrid>().Subject;
        preview.Steps.Should().BeEmpty();
        // Chaining state: _chainTail = ilk node (50,0)
        preview.From.Should().Be(new Vector2D(50, 0));
        preview.To.Should().Be(new Vector2D(50, 0));
    }

    [Fact]
    public void T261_IkinciTik_SegmentVeStepEklenir()
    {
        var (doc, st, sel, s1, s2, _) = SahneDuzZincir();
        var tool = new HybridTool();
        var ctx = CreateCtx(doc, st, sel);
        tool.Activate(ctx);

        tool.OnPointerMove(SegSnap(s1, new Vector2D(50, 0)), ctx);
        tool.OnPointerDown(SegSnap(s1, new Vector2D(50, 0)), ToolMouseButton.Left, ctx);
        tool.OnPointerMove(SegSnap(s2, new Vector2D(150, 0)), ctx);
        tool.OnPointerDown(SegSnap(s2, new Vector2D(150, 0)), ToolMouseButton.Left, ctx);

        var preview = tool.Preview.Should().BeOfType<PreviewHybrid>().Subject;
        preview.Steps.Should().HaveCount(1);
        // _chainTail = ikinci node (150,0)
        preview.From.Should().Be(new Vector2D(150, 0));
        preview.To.Should().Be(new Vector2D(150, 0));
    }

    [Fact]
    public void T262_UcTik_IkiSegmentIkiStep()
    {
        var (doc, st, sel, s1, s2, s3) = SahneDuzZincir();
        var tool = new HybridTool();
        var ctx = CreateCtx(doc, st, sel);
        tool.Activate(ctx);

        // 3 tık → 3 node, 2 segment, 2 step
        tool.OnPointerMove(SegSnap(s1, new Vector2D(50, 0)), ctx);
        tool.OnPointerDown(SegSnap(s1, new Vector2D(50, 0)), ToolMouseButton.Left, ctx);
        tool.OnPointerMove(SegSnap(s2, new Vector2D(150, 0)), ctx);
        tool.OnPointerDown(SegSnap(s2, new Vector2D(150, 0)), ToolMouseButton.Left, ctx);
        tool.OnPointerMove(SegSnap(s3, new Vector2D(250, 0)), ctx);
        tool.OnPointerDown(SegSnap(s3, new Vector2D(250, 0)), ToolMouseButton.Left, ctx);

        var preview = tool.Preview.Should().BeOfType<PreviewHybrid>().Subject;
        preview.Steps.Should().HaveCount(2);
    }

    [Fact]
    public void T263_Enter_Commit_TekUndo()
    {
        var (doc, st, sel, s1, s2, _) = SahneDuzZincir();
        var tool = new HybridTool();
        var ctx = CreateCtx(doc, st, sel);
        tool.Activate(ctx);

        int ilkKume = doc.Entities.Count; // 9 (5 node + 4 segment)

        tool.OnPointerMove(SegSnap(s1, new Vector2D(50, 0)), ctx);
        tool.OnPointerDown(SegSnap(s1, new Vector2D(50, 0)), ToolMouseButton.Left, ctx);
        tool.OnPointerMove(SegSnap(s2, new Vector2D(150, 0)), ctx);
        tool.OnPointerDown(SegSnap(s2, new Vector2D(150, 0)), ToolMouseButton.Left, ctx);

        tool.OnKeyDown(ToolKey.Enter, ctx);

        // 2 node + 1 segment + 1 route = 4 yeni entity
        doc.Entities.Count.Should().Be(ilkKume + 4);
        doc.Entities.OfType<Route>().Should().ContainSingle();
        tool.Preview.Should().BeNull();

        // Tek Ctrl+Z → hepsi gider
        st.Undo(doc);
        doc.Entities.Count.Should().Be(ilkKume);
        doc.Entities.OfType<Route>().Should().BeEmpty();
    }

    [Fact]
    public void T264_Esc_Iptal_HicbirSeyEklenmez()
    {
        var (doc, st, sel, s1, s2, _) = SahneDuzZincir();
        var tool = new HybridTool();
        var ctx = CreateCtx(doc, st, sel);
        tool.Activate(ctx);

        int ilkKume = doc.Entities.Count;

        tool.OnPointerMove(SegSnap(s1, new Vector2D(50, 0)), ctx);
        tool.OnPointerDown(SegSnap(s1, new Vector2D(50, 0)), ToolMouseButton.Left, ctx);
        tool.OnPointerMove(SegSnap(s2, new Vector2D(150, 0)), ctx);
        tool.OnPointerDown(SegSnap(s2, new Vector2D(150, 0)), ToolMouseButton.Left, ctx);

        tool.OnKeyDown(ToolKey.Escape, ctx);

        doc.Entities.Count.Should().Be(ilkKume);
        tool.Preview.Should().BeNull();
    }

    [Fact]
    public void T265_OnSegmentDisiSnap_Reddedilir()
    {
        var (doc, st, sel, s1, _, _) = SahneDuzZincir();
        var tool = new HybridTool();
        var ctx = CreateCtx(doc, st, sel);
        tool.Activate(ctx);

        tool.OnPointerMove(new SnapResult(new Vector2D(0, 0), SnapKind.Endpoint, s1.Id), ctx);

        var preview = tool.Preview.Should().BeOfType<PreviewHybrid>().Subject;
        preview.AdaySegmentId.Should().Be(Guid.Empty);
        preview.AdayGecerli.Should().BeFalse();

        tool.OnPointerDown(new SnapResult(new Vector2D(0, 0), SnapKind.Endpoint, s1.Id), ToolMouseButton.Left, ctx);
        preview = tool.Preview.Should().BeOfType<PreviewHybrid>().Subject;
        preview.Steps.Should().BeEmpty();
    }

    [Fact]
    public void T266_KomsuOlmayanTik_YokSayilir()
    {
        var (doc, st, sel, s1, _, s3) = SahneDuzZincir();
        var tool = new HybridTool();
        var ctx = CreateCtx(doc, st, sel);
        tool.Activate(ctx);

        tool.OnPointerMove(SegSnap(s1, new Vector2D(50, 0)), ctx);
        tool.OnPointerDown(SegSnap(s1, new Vector2D(50, 0)), ToolMouseButton.Left, ctx);

        // s1 ve s3 komşu değil (s1=nA→nB, s3=nC→nD, ortak düğüm yok)
        tool.OnPointerMove(SegSnap(s3, new Vector2D(250, 0)), ctx);
        var preview = tool.Preview.Should().BeOfType<PreviewHybrid>().Subject;
        preview.AdayGecerli.Should().BeFalse();

        tool.OnPointerDown(SegSnap(s3, new Vector2D(250, 0)), ToolMouseButton.Left, ctx);

        preview = tool.Preview.Should().BeOfType<PreviewHybrid>().Subject;
        preview.Steps.Should().HaveCount(0);
    }

    [Fact]
    public void T267_BayatGraf_CommitleReddeder()
    {
        var (doc, st, sel, s1, s2, _) = SahneDuzZincir();
        var tool = new HybridTool();
        var ctx = CreateCtx(doc, st, sel);
        tool.Activate(ctx);

        tool.OnPointerMove(SegSnap(s1, new Vector2D(50, 0)), ctx);
        tool.OnPointerDown(SegSnap(s1, new Vector2D(50, 0)), ToolMouseButton.Left, ctx);
        tool.OnPointerMove(SegSnap(s2, new Vector2D(150, 0)), ctx);
        tool.OnPointerDown(SegSnap(s2, new Vector2D(150, 0)), ToolMouseButton.Left, ctx);

        // s2'yi dokümandan sil (tıklanan segment bayat)
        doc.RemoveEntity(s2.Id);

        tool.OnKeyDown(ToolKey.Enter, ctx);

        doc.Entities.OfType<Route>().Should().BeEmpty();
        tool.Preview.Should().BeNull();
    }

    [Fact]
    public void T268_PreviewHybrid_IkiBilesen()
    {
        var (doc, st, sel, s1, s2, _) = SahneDuzZincir();
        var tool = new HybridTool();
        var ctx = CreateCtx(doc, st, sel);
        tool.Activate(ctx);

        tool.OnPointerMove(SegSnap(s1, new Vector2D(50, 0)), ctx);
        tool.OnPointerDown(SegSnap(s1, new Vector2D(50, 0)), ToolMouseButton.Left, ctx);
        tool.OnPointerMove(SegSnap(s2, new Vector2D(150, 0)), ctx);
        tool.OnPointerDown(SegSnap(s2, new Vector2D(150, 0)), ToolMouseButton.Left, ctx);

        var preview = tool.Preview.Should().BeOfType<PreviewHybrid>().Subject;
        // Çizgi bileşeni: _chainTail (150,0) → _cursor (150,0)
        preview.From.Should().Be(new Vector2D(150, 0));
        preview.To.Should().Be(new Vector2D(150, 0));
        preview.SegmentGecerli.Should().BeFalse(); // _cursor == _chainTail
        // Rota bileşeni: Steps
        preview.Steps.Should().HaveCount(1);
    }

    [Fact]
    public void T269_DortSegmentRoute_YonlerDogru()
    {
        var (doc, st, sel, s1, s2, s3) = SahneDuzZincir();
        var tool = new HybridTool();
        var ctx = CreateCtx(doc, st, sel);
        tool.Activate(ctx);

        // s1→s2→s3 şeklinde 4 tık için yeterli segment yok; s1,s2,s3 üzerinde
        // 3 tık ile 2 segmentlik zincir → yönler Forward olmalı
        tool.OnPointerMove(SegSnap(s1, new Vector2D(50, 0)), ctx);
        tool.OnPointerDown(SegSnap(s1, new Vector2D(50, 0)), ToolMouseButton.Left, ctx);
        tool.OnPointerMove(SegSnap(s2, new Vector2D(150, 0)), ctx);
        tool.OnPointerDown(SegSnap(s2, new Vector2D(150, 0)), ToolMouseButton.Left, ctx);
        tool.OnPointerMove(SegSnap(s3, new Vector2D(250, 0)), ctx);
        tool.OnPointerDown(SegSnap(s3, new Vector2D(250, 0)), ToolMouseButton.Left, ctx);

        tool.OnKeyDown(ToolKey.Enter, ctx);

        var route = doc.Entities.OfType<Route>().Single();
        route.Steps.Should().HaveCount(2);
        foreach (var step in route.Steps)
            step.Direction.Should().Be(TravelDirection.Forward);

        // Tek Ctrl+Z ile hepsi gider
        st.Undo(doc);
        doc.Entities.OfType<Route>().Should().BeEmpty();
    }
}
