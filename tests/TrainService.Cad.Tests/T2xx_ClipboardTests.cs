using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using TrainService.Cad.Clipboard;
using TrainService.Cad.Selection;
using TrainService.Cad.Snapping;
using TrainService.Cad.Tools;
using TrainService.Cad.UndoRedo;
using TrainService.Core.Entities;
using TrainService.Core.Geometry;
using Xunit;

namespace TrainService.Cad.Tests;

public class T2xx_ClipboardTests
{
    private static (CadDocument doc, CommandStack st, SelectionService sel, ClipboardService cb) Sahne()
    {
        var doc = new CadDocument(); 
        return (doc, new CommandStack(), new SelectionService(), new ClipboardService());
    }

    private static ToolContext Ctx(CadDocument d, CommandStack c, SelectionService s, ClipboardService cb)
        => new(d, c, s) { Clipboard = cb, ClickToleranceWorld = 20 };

    private static TrackNode AddNode(CadDocument d, double x, double y)
    { 
        var n = new TrackNode{ Position=new Vector2D(x,y), LayerId=d.ActiveLayerId }; 
        new AddEntityCommand(n).Execute(d); 
        return n; 
    }

    [Fact]
    public void T231_Copy_PanoDolar_BelgeDegismez()
    {
        var (d,st,sel,cb) = Sahne();
        var n = AddNode(d,0,0); sel.Set(new[]{n.Id});
        int oncekiSayi = d.Entities.Count();
        
        var t = new SelectTool(); 
        t.OnKeyDown(ToolKey.Copy, Ctx(d,st,sel,cb));
        
        cb.HasContent.Should().BeTrue();
        cb.Count.Should().Be(1);
        d.Entities.Should().HaveCount(oncekiSayi, "copy belgeyi değiştirmemeli");
    }

    [Fact]
    public void T232_Cut_Siler_PanoyaYazar_UndoGeriGetirir()
    {
        var (d,st,sel,cb) = Sahne();
        var n = AddNode(d,10,10); sel.Set(new[]{n.Id});
        
        var t = new SelectTool(); 
        t.OnKeyDown(ToolKey.Cut, Ctx(d,st,sel,cb));
        
        d.TryGetEntity(n.Id, out _).Should().BeFalse("cut siler");
        cb.HasContent.Should().BeTrue("cut panoya yazar");
        
        st.Undo(d);
        d.TryGetEntity(n.Id, out _).Should().BeTrue("undo geri getirir");
        cb.HasContent.Should().BeTrue("undo panoyu geri ALMAZ (AutoCAD)");
    }

    [Fact]
    public void T233_Paste_YeniIdKlonlar_Offset_SecimGuncel()
    {
        var (d,st,sel,cb) = Sahne();
        var n = AddNode(d,100,100); sel.Set(new[]{n.Id});
        
        var t = new SelectTool();
        t.OnKeyDown(ToolKey.Copy, Ctx(d,st,sel,cb));
        t.OnKeyDown(ToolKey.Paste, Ctx(d,st,sel,cb));
        
        d.Entities.OfType<TrackNode>().Should().HaveCount(2, "orijinal + yapıştırılan");
        var yeni = d.Entities.OfType<TrackNode>().Single(x => x.Id != n.Id);
        
        yeni.Id.Should().NotBe(n.Id, "yeni Guid");
        yeni.Position.X.Should().BeApproximately(120, 1e-9, "+20 offset");
        yeni.Position.Y.Should().BeApproximately(120, 1e-9);
        yeni.LayerId.Should().Be(n.LayerId, "LayerId korunmalı (Sorun 2)");
        
        sel.SelectedIds.Should().Contain(yeni.Id, "yapıştırılan seçili gelir");
        sel.SelectedIds.Should().NotContain(n.Id, "orijinal artık seçili değil");
    }

    [Fact]
    public void T234_Paste_Undo_EklenenlerSilinir()
    {
        var (d,st,sel,cb) = Sahne();
        var n = AddNode(d,0,0); sel.Set(new[]{n.Id});
        
        var t = new SelectTool();
        t.OnKeyDown(ToolKey.Copy, Ctx(d,st,sel,cb));
        t.OnKeyDown(ToolKey.Paste, Ctx(d,st,sel,cb));
        
        d.Entities.OfType<TrackNode>().Should().HaveCount(2);
        
        st.Undo(d);
        d.Entities.OfType<TrackNode>().Should().HaveCount(1, "paste undo yapıştırılanı siler");
        d.TryGetEntity(n.Id, out _).Should().BeTrue("orijinale dokunulmamalı");
    }

    [Fact]
    public void T235_Paste_SegmentReferanslari_YeniIdyeCevrilir_LengthDogru()
    {
        var (d,st,sel,cb) = Sahne();
        var a = AddNode(d,0,0); 
        var b = AddNode(d,300,400);   // uzunluk 500
        
        var s = new TrackSegment{ StartNodeId=a.Id, EndNodeId=b.Id, LengthMm=500, LayerId=d.ActiveLayerId };
        new AddEntityCommand(s).Execute(d);
        
        sel.Set(new[]{a.Id, b.Id, s.Id});
        
        var t = new SelectTool();
        t.OnKeyDown(ToolKey.Copy, Ctx(d,st,sel,cb));
        t.OnKeyDown(ToolKey.Paste, Ctx(d,st,sel,cb));

        var yeniSeg = d.Entities.OfType<TrackSegment>().Single(x => x.Id != s.Id);
        
        // Yeni segmentin node'ları YENİ node'lar olmalı (eski a/b DEĞİL):
        yeniSeg.StartNodeId.Should().NotBe(a.Id);
        yeniSeg.EndNodeId.Should().NotBe(b.Id);
        
        // Ve o yeni node'lar belgede var olmalı (çözümlenebilir):
        d.TryGetEntity(yeniSeg.StartNodeId, out _).Should().BeTrue("segment yeni node'a bağlı");
        d.TryGetEntity(yeniSeg.EndNodeId, out _).Should().BeTrue();
        yeniSeg.LengthMm.Should().BeApproximately(500, 1e-6, "offset paralel öteleme, uzunluk korunur (Sorun 1)");
    }

    [Fact]
    public void T236_BosSecimde_CopyCut_PanoDegismez()
    {
        var (d,st,sel,cb) = Sahne();
        AddNode(d,0,0);   // var ama SEÇİLİ DEĞİL
        
        var t = new SelectTool();
        t.OnKeyDown(ToolKey.Copy, Ctx(d,st,sel,cb));
        cb.HasContent.Should().BeFalse("boş seçimde copy pano doldurmaz");
        
        t.OnKeyDown(ToolKey.Cut, Ctx(d,st,sel,cb));
        cb.HasContent.Should().BeFalse("boş seçimde cut pano doldurmaz");
        d.Entities.Should().HaveCount(1, "cut hiçbir şeyi silmedi");
    }

    [Fact]
    public void T237_Copy_SonrasiKaynakDegisse_PanoSabit()
    {
        var (d,st,sel,cb) = Sahne();
        var n = AddNode(d,0,0); sel.Set(new[]{n.Id});
        
        var t = new SelectTool();
        t.OnKeyDown(ToolKey.Copy, Ctx(d,st,sel,cb));
        
        // Copy'den SONRA orijinali taşı (mutable):
        n.Position = new Vector2D(999,999);
        
        // Paste → panodaki ESKİ konum (0,0)+20 gelmeli, 999 DEĞİL:
        t.OnKeyDown(ToolKey.Paste, Ctx(d,st,sel,cb));
        
        var yeni = d.Entities.OfType<TrackNode>().Single(x => x.Id != n.Id);
        yeni.Position.X.Should().BeApproximately(20, 1e-9, "pano snapshot aldı, kaynak değişimi etkilemez");
    }
}
