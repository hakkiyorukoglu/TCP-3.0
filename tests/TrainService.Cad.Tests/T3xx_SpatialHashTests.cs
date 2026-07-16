using System;
using System.Collections.Generic;
using FluentAssertions;
using TrainService.Cad;
using TrainService.Cad.Spatial;
using TrainService.Core.Entities;
using TrainService.Core.Geometry;
using Xunit;

namespace TrainService.Cad.Tests;

public class T3xx_SpatialHashTests
{
    private static BoundingBox BBox(double minx, double miny, double maxx, double maxy) => new BoundingBox(minx, miny, maxx, maxy);

    [Fact]
    public void T305_SpatialHash_EkleSilSorgula()
    {
        var h = new SpatialHash(1000);
        var id1 = Guid.NewGuid(); var id2 = Guid.NewGuid();
        h.Add(id1, BBox(0,0, 100,100));       // hücre (0,0)
        h.Add(id2, BBox(5000,5000, 5100,5100)); // uzak hücre
        var buf = new List<Guid>();
        h.Query(BBox(-10,-10, 200,200), buf);
        buf.Should().Contain(id1);
        buf.Should().NotContain(id2, "uzak hücredeki entity sorguya girmemeli");

        h.Remove(id1); buf.Clear(); h.Query(BBox(-10,-10, 200,200), buf);
        buf.Should().NotContain(id1, "silinen entity artık dönmemeli");
    }

    [Fact]
    public void T306_SpatialHash_CokHucreyeYayilanEntity()
    {
        // Büyük bir segment birden çok hücreyi kesiyorsa TÜM o hücrelerden bulunmalı
        var h = new SpatialHash(1000);
        var id = Guid.NewGuid();
        h.Add(id, BBox(0,0, 3500,0));   // 4 hücreye yayılır (x: 0..3500)
        var buf = new List<Guid>();
        h.Query(BBox(3000,-10, 3100,10), buf);   // en sağ uçtaki hücreden sorgu
        buf.Should().Contain(id, "çok hücreye yayılan entity her hücreden bulunmalı");
    }

    [Fact]
    public void T307_SpatialHash_QueryTahsisYok()
    {
        var h = new SpatialHash(1000);
        for (int i = 0; i < 100; i++) h.Add(Guid.NewGuid(), BBox(i*10, 0, i*10+5, 5));
        var buf = new List<Guid>(256);
        h.Query(BBox(0,0, 1000,1000), buf);   // ısınma
        long once = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++) { buf.Clear(); h.Query(BBox(0,0, 1000,1000), buf); }
        long sonra = GC.GetAllocatedBytesForCurrentThread();
        (sonra - once).Should().BeLessThan(50_000, "Query hot-path'te kayda değer tahsis yapmamalı");
    }

    [Fact]
    public void T308_CadDocument_QueryRegion_IndexSenkron()
    {
        var doc = new CadDocument();
        var stack = new TrainService.Cad.UndoRedo.CommandStack();
        var n = new TrackNode { Position = new Vector2D(500,500), LayerId = doc.ActiveLayerId };
        stack.Do(new TrainService.Cad.UndoRedo.AddEntityCommand(n), doc);
        var buf = new List<Guid>();
        doc.QueryRegion(BBox(400,400, 600,600), buf);
        buf.Should().Contain(n.Id, "eklenen node bölgesel sorguda bulunmalı");

        stack.Undo(doc);   // ★ undo → index de senkron güncellenmeli (stale index tuzağı)
        buf.Clear(); doc.QueryRegion(BBox(400,400, 600,600), buf);
        buf.Should().NotContain(n.Id, "undo sonrası index'te kalmamalı (senkron kanıtı)");
    }
}
