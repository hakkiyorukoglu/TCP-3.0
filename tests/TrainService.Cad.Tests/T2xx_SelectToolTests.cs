using System;
using System.Collections.Generic;
using FluentAssertions;
using TrainService.Core.Entities;
using TrainService.Core.Geometry;
using TrainService.Cad;
using TrainService.Cad.Selection;
using TrainService.Cad.Snapping;
using TrainService.Cad.Tools;
using TrainService.Cad.UndoRedo;
using Xunit;

namespace TrainService.Cad.Tests;

public class T2xx_SelectToolTests
{
    private static (CadDocument doc, CommandStack st, SelectionService sel) Sahne()
    {
        var doc = new CadDocument();
        var st = new CommandStack();
        var sel = new SelectionService();
        sel.PruneMissing(doc);
        return (doc, st, sel);
    }

    private static SnapResult Snap(double x, double y) => new(new Vector2D(x, y), SnapKind.None, null);

    private static ToolContext Ctx(CadDocument d, CommandStack c, SelectionService s, bool add = false)
        => new(d, c, s) { ModifierAdd = add, ClickToleranceWorld = 20 };

    [Fact]
    public void T221_TekTik_EnYakinNodeSecer()
    {
        var (d, st, sel) = Sahne();
        var n = new TrackNode { Position = new Vector2D(100, 100), LayerId = d.ActiveLayerId };
        new AddEntityCommand(n).Execute(d);
        var t = new SelectTool();
        t.Activate(Ctx(d, st, sel));
        t.OnPointerDown(Snap(105, 100), ToolMouseButton.Left, Ctx(d, st, sel));  // 5 birim yakın
        t.OnPointerUp(Snap(105, 100), ToolMouseButton.Left, Ctx(d, st, sel));
        sel.SelectedIds.Should().ContainSingle().Which.Should().Be(n.Id);
    }

    [Fact]
    public void T222_BoslugaTik_SecimTemizler()
    {
        var (d, st, sel) = Sahne();
        var n = new TrackNode { Position = new Vector2D(0, 0), LayerId = d.ActiveLayerId };
        new AddEntityCommand(n).Execute(d);
        sel.Set(new[] { n.Id });
        var t = new SelectTool();
        t.Activate(Ctx(d, st, sel));
        t.OnPointerDown(Snap(9999, 9999), ToolMouseButton.Left, Ctx(d, st, sel));
        t.OnPointerUp(Snap(9999, 9999), ToolMouseButton.Left, Ctx(d, st, sel));
        sel.SelectedIds.Should().BeEmpty("boşluğa tık seçimi temizler");
    }

    [Fact]
    public void T223_Window_SoldanSaga_SadeceTamIcerenSecilir()   // mavi
    {
        var (d, st, sel) = Sahne();
        var ic = new TrackNode { Position = new Vector2D(50, 50), LayerId = d.ActiveLayerId };   // kutu içinde
        var dis = new TrackNode { Position = new Vector2D(500, 500), LayerId = d.ActiveLayerId }; // dışında
        new AddEntityCommand(ic).Execute(d);
        new AddEntityCommand(dis).Execute(d);
        var t = new SelectTool();
        t.Activate(Ctx(d, st, sel));
        // Soldan sağa: (0,0) → (100,100). x artıyor → window.
        t.OnPointerDown(Snap(0, 0), ToolMouseButton.Left, Ctx(d, st, sel));
        t.OnPointerMove(Snap(100, 100), Ctx(d, st, sel));
        (t.Preview as PreviewRectangle)!.IsCrossing.Should().BeFalse("soldan-sağ = window = mavi");
        t.OnPointerUp(Snap(100, 100), ToolMouseButton.Left, Ctx(d, st, sel));
        sel.SelectedIds.Should().Contain(ic.Id);
        sel.SelectedIds.Should().NotContain(dis.Id);
    }

    [Fact]
    public void T224_Crossing_SagdanSola_DegenSecilir()   // yeşil
    {
        var (d, st, sel) = Sahne();
        // Segment kutuya sadece DEĞİYOR (bir ucu içeride bir ucu dışarıda)
        var a = new TrackNode { Position = new Vector2D(50, 50), LayerId = d.ActiveLayerId };
        var b = new TrackNode { Position = new Vector2D(500, 50), LayerId = d.ActiveLayerId };
        var s = new TrackSegment { StartNodeId = a.Id, EndNodeId = b.Id, LayerId = d.ActiveLayerId, LengthMm = 450 };
        foreach (var e in new CadEntity[] { a, b, s }) new AddEntityCommand(e).Execute(d);
        var t = new SelectTool();
        t.Activate(Ctx(d, st, sel));
        // Sağdan sola: (100,100) → (0,0). x azalıyor → crossing.
        t.OnPointerDown(Snap(100, 100), ToolMouseButton.Left, Ctx(d, st, sel));
        t.OnPointerMove(Snap(0, 0), Ctx(d, st, sel));
        (t.Preview as PreviewRectangle)!.IsCrossing.Should().BeTrue("sağdan-sol = crossing = yeşil");
        t.OnPointerUp(Snap(0, 0), ToolMouseButton.Left, Ctx(d, st, sel));
        sel.SelectedIds.Should().Contain(s.Id, "kutuya değen segment crossing'de seçilir");
    }

    [Fact]
    public void T225_Window_DegenAmaTamIcermeyen_SECILMEZ()   // window vs crossing farkının kanıtı
    {
        var (d, st, sel) = Sahne();
        var a = new TrackNode { Position = new Vector2D(50, 50), LayerId = d.ActiveLayerId };
        var b = new TrackNode { Position = new Vector2D(500, 50), LayerId = d.ActiveLayerId };
        var s = new TrackSegment { StartNodeId = a.Id, EndNodeId = b.Id, LayerId = d.ActiveLayerId, LengthMm = 450 };
        foreach (var e in new CadEntity[] { a, b, s }) new AddEntityCommand(e).Execute(d);
        var t = new SelectTool();
        t.Activate(Ctx(d, st, sel));
        // Soldan sağa (window) ama segment tam içermiyor (b dışarıda) → SEÇİLMEMELİ
        t.OnPointerDown(Snap(0, 0), ToolMouseButton.Left, Ctx(d, st, sel));
        t.OnPointerMove(Snap(100, 100), Ctx(d, st, sel));
        t.OnPointerUp(Snap(100, 100), ToolMouseButton.Left, Ctx(d, st, sel));
        sel.SelectedIds.Should().NotContain(s.Id, "window'da tam içermeyen segment seçilmez");
    }

    [Fact]
    public void T226_Shift_MevcutSecimeEkler()
    {
        var (d, st, sel) = Sahne();
        var n1 = new TrackNode { Position = new Vector2D(0, 0), LayerId = d.ActiveLayerId };
        var n2 = new TrackNode { Position = new Vector2D(100, 100), LayerId = d.ActiveLayerId };
        new AddEntityCommand(n1).Execute(d);
        new AddEntityCommand(n2).Execute(d);
        sel.Set(new[] { n1.Id });
        var t = new SelectTool();
        t.Activate(Ctx(d, st, sel, add: true));
        t.OnPointerDown(Snap(100, 100), ToolMouseButton.Left, Ctx(d, st, sel, add: true));
        t.OnPointerUp(Snap(100, 100), ToolMouseButton.Left, Ctx(d, st, sel, add: true));
        sel.SelectedIds.Should().BeEquivalentTo(new[] { n1.Id, n2.Id }, "Shift ile eklenir, öncekini silmez");
    }

    [Fact]
    public void T227_BoundingBox_ContainsVeIntersects()
    {
        var buyuk = new BoundingBox(0, 0, 100, 100);
        var icte = new BoundingBox(10, 10, 20, 20);
        var degen = new BoundingBox(90, 90, 150, 150);
        var uzak = new BoundingBox(200, 200, 300, 300);
        buyuk.Contains(icte).Should().BeTrue();
        buyuk.Contains(degen).Should().BeFalse("kısmen dışarıda");
        buyuk.IntersectsWith(degen).Should().BeTrue("kesişiyor");
        buyuk.IntersectsWith(uzak).Should().BeFalse();
    }

    [Fact]
    public void T228_Delete_SeciliyiSiler_UndoGeriGetirir()
    {
        var (d, st, sel) = Sahne();
        var n = new TrackNode { Position = new Vector2D(0, 0), LayerId = d.ActiveLayerId };
        st.Do(new AddEntityCommand(n), d);
        st.Do(new DeleteEntitiesCommand(new[] { n.Id }), d);
        d.TryGetEntity(n.Id, out _).Should().BeFalse("silindi");
        st.Undo(d);
        d.TryGetEntity(n.Id, out _).Should().BeTrue("undo geri getirdi");
    }
}
