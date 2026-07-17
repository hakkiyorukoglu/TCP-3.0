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

namespace TrainService.Cad.Tests;

public class T240_LayerTests
{
    private static (CadDocument doc, CommandStack st, SelectionService sel) Sahne3Katman()
    {
        var doc = new CadDocument();
        // CadDocument constructor zaten SabitKatmanlar (Zemin, AltKat, ÜstKat) oluşturur.
        // Testler mevcut varsayılan katmanları kullanır.
        // İsimler: "Zemin", "Alt Kat", "Üst Kat"
        return (doc, new CommandStack(), new SelectionService());
    }

    [Fact]
    public void T240_TrackTool_YeniNode_AktifKatmanZHeightAlir()
    {
        var (doc,st,sel) = Sahne3Katman();
        var alt = doc.Layers.Single(l => l.Name=="Alt Kat");
        doc.SetActiveLayer(alt.Id);
        var t = new TrackTool(); var ctx = new ToolContext(doc, st, sel);
        t.OnPointerDown(new SnapResult(new Vector2D(0,0), SnapKind.None, null), ToolMouseButton.Left, ctx);
        t.OnPointerDown(new SnapResult(new Vector2D(100,0), SnapKind.None, null), ToolMouseButton.Left, ctx);
        var node = doc.Entities.OfType<TrackNode>().First();
        node.Z.Should().BeApproximately(-350, 1e-9, "aktif katman (Alt) ZHeight'ı atanmalı");
        node.LayerId.Should().Be(alt.Id);
    }

    [Fact]
    public void T241_GizliKatman_Secilemez()
    {
        var (doc,st,sel) = Sahne3Katman();
        var zemin = doc.Layers.Single(l => l.Name=="Zemin");
        var n = new TrackNode{ Position=new Vector2D(50,50), LayerId=zemin.Id };
        new AddEntityCommand(n).Execute(doc);
        doc.SetLayerVisibility(zemin.Id, false);   // gizle
        var t = new SelectTool();
        var ctx = new ToolContext(doc,st,sel){ ClickToleranceWorld=20 };
        t.OnPointerDown(new SnapResult(new Vector2D(50,50), SnapKind.None, null), ToolMouseButton.Left, ctx);
        t.OnPointerUp(new SnapResult(new Vector2D(50,50), SnapKind.None, null), ToolMouseButton.Left, ctx);
        sel.SelectedIds.Should().BeEmpty("gizli katmandaki nesne seçilemez");
        doc.IsVisible(n.Id).Should().BeFalse();
        doc.IsSelectable(n.Id).Should().BeFalse();
    }

    [Fact]
    public void T242_KilitliKatman_Secilemez_AmaGorunur()
    {
        var (doc,st,sel) = Sahne3Katman();
        var zemin = doc.Layers.Single(l => l.Name=="Zemin");
        var n = new TrackNode{ Position=new Vector2D(50,50), LayerId=zemin.Id };
        new AddEntityCommand(n).Execute(doc);
        doc.SetLayerLock(zemin.Id, true);   // kilitle
        doc.IsVisible(n.Id).Should().BeTrue("kilitli ama görünür");
        doc.IsSelectable(n.Id).Should().BeFalse("kilitli seçilemez");
        var t = new SelectTool();
        var ctx = new ToolContext(doc,st,sel){ ClickToleranceWorld=20 };
        t.OnPointerDown(new SnapResult(new Vector2D(50,50), SnapKind.None, null), ToolMouseButton.Left, ctx);
        t.OnPointerUp(new SnapResult(new Vector2D(50,50), SnapKind.None, null), ToolMouseButton.Left, ctx);
        sel.SelectedIds.Should().BeEmpty("kilitli katman seçilemez");
    }

    [Fact]
    public void T243_GorunurlukToggle_EventTetikler()
    {
        var (doc,_,_) = Sahne3Katman();
        var zemin = doc.Layers.Single(l => l.Name=="Zemin");
        DocumentChangeKind? sonEvent = null;
        doc.Changed += (s,e) => sonEvent = e.Kind;
        doc.SetLayerVisibility(zemin.Id, false);
        sonEvent.Should().Be(DocumentChangeKind.LayerChanged, "görünürlük değişimi event tetikler");
    }

    [Fact]
    public void T244_AyniDegere_Set_EventTetiklemez()
    {
        var (doc,_,_) = Sahne3Katman();
        var zemin = doc.Layers.Single(l => l.Name=="Zemin");   // zaten IsVisible=true
        int eventSayisi = 0; doc.Changed += (s,e) => eventSayisi++;
        doc.SetLayerVisibility(zemin.Id, true);   // aynı değer
        eventSayisi.Should().Be(0, "değişiklik yoksa event yok (gereksiz render önlenir)");
    }

    [Fact]
    public void T245_TryGetLayer_O1Erisim()
    {
        var (doc,_,_) = Sahne3Katman();
        var zemin = doc.Layers.Single(l => l.Name=="Zemin");
        doc.TryGetLayer(zemin.Id, out var l).Should().BeTrue();
        l.Name.Should().Be("Zemin");
        doc.TryGetLayer(Guid.NewGuid(), out _).Should().BeFalse("olmayan katman false");
    }

    [Fact]
    public void T246_MarqueeSecim_GizliKatmaniAtlar()
    {
        var (doc,st,sel) = Sahne3Katman();
        var zemin = doc.Layers.Single(l => l.Name=="Zemin");
        var ust = doc.Layers.Single(l => l.Name=="Üst Kat");
        var gorunur = new TrackNode{ Position=new Vector2D(50,50), LayerId=zemin.Id };
        var gizli   = new TrackNode{ Position=new Vector2D(60,60), LayerId=ust.Id };
        new AddEntityCommand(gorunur).Execute(doc); new AddEntityCommand(gizli).Execute(doc);
        doc.SetLayerVisibility(ust.Id, false);   // üst katman gizli
        var t = new SelectTool();
        var ctx = new ToolContext(doc,st,sel){ ClickToleranceWorld=20 };
        // Soldan sağa window, ikisini de kapsayan kutu:
        t.OnPointerDown(new SnapResult(new Vector2D(0,0), SnapKind.None, null), ToolMouseButton.Left, ctx);
        t.OnPointerMove(new SnapResult(new Vector2D(100,100), SnapKind.None, null), ctx);
        t.OnPointerUp(new SnapResult(new Vector2D(100,100), SnapKind.None, null), ToolMouseButton.Left, ctx);
        sel.SelectedIds.Should().Contain(gorunur.Id);
        sel.SelectedIds.Should().NotContain(gizli.Id, "gizli katman marquee'de de atlanır");
    }

    [Fact]
    public void T247_KatmaniOlmayanEntity_GorunurKalir()
    {
        var (doc,_,_) = Sahne3Katman();
        // LayerId'si HİÇBİR katmana denk gelmeyen bir node:
        var yetim = new TrackNode{ Position=new Vector2D(0,0), LayerId=Guid.NewGuid() };
        new AddEntityCommand(yetim).Execute(doc);
        doc.IsVisible(yetim.Id).Should().BeTrue("katmanı bulunamayan entity gizlenmez, çizilir (regresyon koruması)");
        doc.IsSelectable(yetim.Id).Should().BeTrue("katmansız entity seçilebilir kalır");
    }

    [Fact]
    public void T248_YuklenenKatmanlar_EntityLayerId_ileEslesir()
    {
        var (doc,_,_) = Sahne3Katman();
        var zemin = doc.Layers.Single(l => l.Name=="Zemin");
        var n = new TrackNode{ Position=new Vector2D(0,0), LayerId=zemin.Id };
        new AddEntityCommand(n).Execute(doc);
        doc.TryGetLayer(n.Id == Guid.Empty ? Guid.Empty : n.LayerId, out _).Should().BeTrue();
        doc.IsVisible(n.Id).Should().BeTrue("normal entity görünür");
    }
}
