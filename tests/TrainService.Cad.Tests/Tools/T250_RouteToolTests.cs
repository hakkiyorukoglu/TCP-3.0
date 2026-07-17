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

public class T250_RouteToolTests
{
    private static (CadDocument doc, CommandStack st, SelectionService sel) Sahne3Katman()
    {
        var doc = new CadDocument();
        // CadDocument constructor zaten SabitKatmanlar oluşturur. ActiveLayer = Zemin.
        return (doc, new CommandStack(), new SelectionService());
    }

    private static (CadDocument doc, CommandStack st, SelectionService sel, TrackSegment s1, TrackSegment s2, TrackSegment s3) SahneYol()
    {
        var (doc, st, sel) = Sahne3Katman();
        
        var nA = new TrackNode { Position = new Vector2D(0, 0), LayerId = doc.ActiveLayerId };
        var nB = new TrackNode { Position = new Vector2D(100, 0), LayerId = doc.ActiveLayerId };
        var nC = new TrackNode { Position = new Vector2D(200, 0), LayerId = doc.ActiveLayerId };
        var nD = new TrackNode { Position = new Vector2D(300, 0), LayerId = doc.ActiveLayerId };
        
        var s1 = new TrackSegment { StartNodeId = nA.Id, EndNodeId = nB.Id, LayerId = doc.ActiveLayerId };
        var s2 = new TrackSegment { StartNodeId = nC.Id, EndNodeId = nB.Id, LayerId = doc.ActiveLayerId }; // nC to nB
        var s3 = new TrackSegment { StartNodeId = nC.Id, EndNodeId = nD.Id, LayerId = doc.ActiveLayerId }; // nC to nD
        
        doc.AddEntity(nA); doc.AddEntity(nB); doc.AddEntity(nC); doc.AddEntity(nD);
        doc.AddEntity(s1); doc.AddEntity(s2); doc.AddEntity(s3);
        
        return (doc, st, sel, s1, s2, s3);
    }

    private static SnapResult SegSnap(TrackSegment s, Vector2D p) => new(p, SnapKind.OnSegment, s.Id);
    
    private ToolContext CreateCtx(CadDocument doc, CommandStack st, SelectionService sel)
        => new(doc, st, sel) { Clipboard = null! };

    [Fact]
    public void T250_IlkTik_AdimBaslar()
    {
        var (doc, st, sel, s1, _, _) = SahneYol();
        var tool = new RouteTool();
        var ctx = CreateCtx(doc, st, sel);
        tool.Activate(ctx);
        
        tool.OnPointerMove(SegSnap(s1, new Vector2D(50, 0)), ctx);
        tool.OnPointerDown(SegSnap(s1, new Vector2D(50, 0)), ToolMouseButton.Left, ctx);
        
        var preview = tool.Preview.Should().BeOfType<PreviewRoute>().Subject;
        preview.Steps.Should().HaveCount(1);
        preview.Steps[0].SegmentId.Should().Be(s1.Id);
    }

    [Fact]
    public void T251_KomsuTik_EklerVeYonKesinlesir()
    {
        var (doc, st, sel, s1, s2, _) = SahneYol();
        var tool = new RouteTool();
        var ctx = CreateCtx(doc, st, sel);
        tool.Activate(ctx);
        
        tool.OnPointerMove(SegSnap(s1, new Vector2D(50, 0)), ctx);
        tool.OnPointerDown(SegSnap(s1, new Vector2D(50, 0)), ToolMouseButton.Left, ctx);
        tool.OnPointerMove(SegSnap(s2, new Vector2D(150, 0)), ctx);
        tool.OnPointerDown(SegSnap(s2, new Vector2D(150, 0)), ToolMouseButton.Left, ctx);
        
        var preview = tool.Preview.Should().BeOfType<PreviewRoute>().Subject;
        preview.Steps.Should().HaveCount(2);
        
        // s1: A(0)->B(100). s2: C(200)->B(100). Common is B.
        // s1 exit should be B (EndNode), so s1 is Forward.
        // s2 entry should be B (EndNode), so s2 is Backward.
        preview.Steps[0].Direction.Should().Be(TravelDirection.Forward);
        preview.Steps[1].Direction.Should().Be(TravelDirection.Backward);
    }

    [Fact]
    public void T252_KomsuOlmayanTik_YokSayilir()
    {
        var (doc, st, sel, s1, _, s3) = SahneYol();
        var tool = new RouteTool();
        var ctx = CreateCtx(doc, st, sel);
        tool.Activate(ctx);
        
        tool.OnPointerMove(SegSnap(s1, new Vector2D(50, 0)), ctx);
        tool.OnPointerDown(SegSnap(s1, new Vector2D(50, 0)), ToolMouseButton.Left, ctx);
        
        tool.OnPointerMove(SegSnap(s3, new Vector2D(250, 0)), ctx);
        var preview = tool.Preview.Should().BeOfType<PreviewRoute>().Subject;
        preview.AdayGecerli.Should().BeFalse();
        
        tool.OnPointerDown(SegSnap(s3, new Vector2D(250, 0)), ToolMouseButton.Left, ctx);
        
        preview = tool.Preview.Should().BeOfType<PreviewRoute>().Subject;
        preview.Steps.Should().HaveCount(1);
    }

    [Fact]
    public void T253_AyniSegmentIkinciKez_YokSayilir()
    {
        var (doc, st, sel, s1, s2, _) = SahneYol();
        var tool = new RouteTool();
        var ctx = CreateCtx(doc, st, sel);
        tool.Activate(ctx);
        
        tool.OnPointerMove(SegSnap(s1, new Vector2D(50, 0)), ctx);
        tool.OnPointerDown(SegSnap(s1, new Vector2D(50, 0)), ToolMouseButton.Left, ctx);
        tool.OnPointerMove(SegSnap(s2, new Vector2D(150, 0)), ctx);
        tool.OnPointerDown(SegSnap(s2, new Vector2D(150, 0)), ToolMouseButton.Left, ctx);
        
        tool.OnPointerMove(SegSnap(s1, new Vector2D(50, 0)), ctx);
        var preview = tool.Preview.Should().BeOfType<PreviewRoute>().Subject;
        preview.AdayGecerli.Should().BeFalse();
        
        tool.OnPointerDown(SegSnap(s1, new Vector2D(50, 0)), ToolMouseButton.Left, ctx);
        
        preview = tool.Preview.Should().BeOfType<PreviewRoute>().Subject;
        preview.Steps.Should().HaveCount(2);
    }

    [Fact]
    public void T254_Enter_Commit_RotaEklenir_SecimGuncel()
    {
        var (doc, st, sel, s1, s2, _) = SahneYol();
        var tool = new RouteTool();
        var ctx = CreateCtx(doc, st, sel);
        tool.Activate(ctx);
        
        tool.OnPointerMove(SegSnap(s1, new Vector2D(50, 0)), ctx);
        tool.OnPointerDown(SegSnap(s1, new Vector2D(50, 0)), ToolMouseButton.Left, ctx);
        tool.OnPointerMove(SegSnap(s2, new Vector2D(150, 0)), ctx);
        tool.OnPointerDown(SegSnap(s2, new Vector2D(150, 0)), ToolMouseButton.Left, ctx);
        
        tool.OnKeyDown(ToolKey.Enter, ctx);
        
        var route = doc.Entities.OfType<Route>().Single();
        route.Steps.Should().HaveCount(2);
        route.LayerId.Should().Be(doc.ActiveLayerId);
        route.CachedBounds.Should().NotBeNull();
        sel.SelectedIds.Should().Contain(route.Id);
        tool.Preview.Should().BeNull();
        
        st.Undo(doc);
        doc.Entities.OfType<Route>().Should().BeEmpty();
    }

    [Fact]
    public void T255_Esc_Iptal_HicbirSeyEklenmez()
    {
        var (doc, st, sel, s1, s2, _) = SahneYol();
        var tool = new RouteTool();
        var ctx = CreateCtx(doc, st, sel);
        tool.Activate(ctx);
        
        tool.OnPointerMove(SegSnap(s1, new Vector2D(50, 0)), ctx);
        tool.OnPointerDown(SegSnap(s1, new Vector2D(50, 0)), ToolMouseButton.Left, ctx);
        tool.OnPointerMove(SegSnap(s2, new Vector2D(150, 0)), ctx);
        tool.OnPointerDown(SegSnap(s2, new Vector2D(150, 0)), ToolMouseButton.Left, ctx);
        
        tool.OnKeyDown(ToolKey.Escape, ctx);
        
        doc.Entities.OfType<Route>().Should().BeEmpty();
        tool.Preview.Should().BeNull();
    }

    [Fact]
    public void T256_Commit_BayatGraf_Reddeder()
    {
        var (doc, st, sel, s1, s2, _) = SahneYol();
        var tool = new RouteTool();
        var ctx = CreateCtx(doc, st, sel);
        tool.Activate(ctx);
        
        tool.OnPointerMove(SegSnap(s1, new Vector2D(50, 0)), ctx);
        tool.OnPointerDown(SegSnap(s1, new Vector2D(50, 0)), ToolMouseButton.Left, ctx);
        tool.OnPointerMove(SegSnap(s2, new Vector2D(150, 0)), ctx);
        tool.OnPointerDown(SegSnap(s2, new Vector2D(150, 0)), ToolMouseButton.Left, ctx);
        
        doc.RemoveEntity(s2.Id); // silindi
        
        tool.OnKeyDown(ToolKey.Enter, ctx);
        
        doc.Entities.OfType<Route>().Should().BeEmpty();
        tool.Preview.Should().BeNull();
    }

    [Fact]
    public void T257_OnSegmentDisiSnap_AdayUretmez()
    {
        var (doc, st, sel, s1, _, _) = SahneYol();
        var tool = new RouteTool();
        var ctx = CreateCtx(doc, st, sel);
        tool.Activate(ctx);
        
        tool.OnPointerMove(new SnapResult(new Vector2D(0, 0), SnapKind.Endpoint, s1.Id), ctx);
        
        var preview = tool.Preview.Should().BeOfType<PreviewRoute>().Subject;
        preview.AdaySegmentId.Should().Be(Guid.Empty);
        preview.AdayGecerli.Should().BeFalse();
        
        tool.OnPointerDown(new SnapResult(new Vector2D(0, 0), SnapKind.Endpoint, s1.Id), ToolMouseButton.Left, ctx);
        preview = tool.Preview.Should().BeOfType<PreviewRoute>().Subject;
        preview.Steps.Should().BeEmpty();
    }
}
