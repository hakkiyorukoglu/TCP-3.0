using System;
using System.Linq;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;
using TrainService.Core.Entities;
using TrainService.Core.Enums;
using TrainService.Core.Geometry;
using TrainService.Core.Topology;

namespace TrainService.Core.Tests;

public class T2xx_TrackGraphTests
{
    private static TrackNode N(double x = 0, double y = 0) => new() { Position = new Vector2D(x, y), LayerId = Guid.NewGuid() };
    private static TrackSegment S(TrackNode a, TrackNode b) => new()
    {
        StartNodeId = a.Id,
        EndNodeId = b.Id,
        LayerId = a.LayerId,
        LengthMm = (b.Position - a.Position).Length
    };

    [Fact]
    public void T214_Build_Komsuluk_DogruSayiVeReferans()
    {
        var a = N(0, 0); var b = N(100, 0);
        var seg = S(a, b);
        var g = TrackGraph.Build(new[] { a, b }, new[] { seg });

        g.GetAdjacentSegments(a.Id).Should().ContainSingle().Which.Should().Be(seg.Id);
        g.GetAdjacentSegments(b.Id).Should().ContainSingle().Which.Should().Be(seg.Id);
        g.Degree(a.Id).Should().Be(1);
        g.Nodes.Should().HaveCount(2);
        g.Segments.Should().ContainKey(seg.Id);
        // Bozuk sorgu guard:
        g.GetAdjacentSegments(Guid.NewGuid()).Should().BeEmpty("olmayan düğüm boş döner, exception atmaz");
    }

    [Fact]
    public void T215_FindBlocks_DuzHat_TekBlok()
    {
        // A-B-C-D: 3 segment, B ve C derece-2 (düz devam) → 1 blok
        var a = N(0, 0); var b = N(100, 0); var c = N(200, 0); var d = N(300, 0);
        var s1 = S(a, b); var s2 = S(b, c); var s3 = S(c, d);
        var g = TrackGraph.Build(new[] { a, b, c, d }, new[] { s1, s2, s3 });

        var bloklar = g.FindBlocks();
        bloklar.Should().HaveCount(1, "düz hatta makas yoksa tek blok");
        bloklar[0].SegmentIds.Should().BeEquivalentTo(new[] { s1.Id, s2.Id, s3.Id });
        bloklar[0].SegmentCount.Should().Be(3);
        // Blok sınırları uç düğümler (a ve d, derece-1):
        new[] { bloklar[0].StartNodeId, bloklar[0].EndNodeId }.Should().BeEquivalentTo(new[] { a.Id, d.Id });
    }

    [Fact]
    public void T216_FindBlocks_YMakasi_UcBlok()
    {
        // Merkez M (derece-3): M-A, M-B, M-C. Her kol ayrı blok.
        var m = N(0, 0); var a = N(-100, 0); var b = N(100, 50); var c = N(100, -50);
        var sa = S(m, a); var sb = S(m, b); var sc = S(m, c);
        var g = TrackGraph.Build(new[] { m, a, b, c }, new[] { sa, sb, sc });

        g.Degree(m.Id).Should().Be(3, "makas merkezi derece-3");
        var bloklar = g.FindBlocks();
        bloklar.Should().HaveCount(3, "3 kollu makasta her kol ayrı blok");
        bloklar.Should().OnlyContain(bl => bl.SegmentCount == 1);
        // Her segment tam bir blokta:
        bloklar.SelectMany(bl => bl.SegmentIds).Should().BeEquivalentTo(new[] { sa.Id, sb.Id, sc.Id });
    }

    [Fact]
    public void T217_ValidateRoute_DogruVeKopukVeTers()
    {
        var a = N(0, 0); var b = N(100, 0); var c = N(200, 0);
        var s1 = S(a, b); var s2 = S(b, c);           // uç-uca bağlı (b ortak)
        var uzak = N(999, 999); var s3 = S(uzak, N(999, 1099));  // kopuk
        var g = TrackGraph.Build(new[] { a, b, c, uzak }, new[] { s1, s2, s3 });

        // Doğru rota: s1(Forward: a->b) sonra s2(Forward: b->c). b'de birleşiyor.
        var dogru = new Route(); dogru.Steps.Add(new RouteStep(s1.Id, TravelDirection.Forward));
        dogru.Steps.Add(new RouteStep(s2.Id, TravelDirection.Forward));
        g.ValidateRoute(dogru).Should().BeTrue("uç-uca bağlı rota geçerli");

        // Kopuk rota: s1 sonra s3 (hiç bağlı değil)
        var kopuk = new Route(); kopuk.Steps.Add(new RouteStep(s1.Id, TravelDirection.Forward));
        kopuk.Steps.Add(new RouteStep(s3.Id, TravelDirection.Forward));
        g.ValidateRoute(kopuk).Should().BeFalse("bağlı olmayan segmentler geçersiz");

        // Ters yön: s1(Forward: a->b, çıkış b) sonra s2(Backward: c->b, giriş c). b≠c → kopuk.
        var ters = new Route(); ters.Steps.Add(new RouteStep(s1.Id, TravelDirection.Forward));
        ters.Steps.Add(new RouteStep(s2.Id, TravelDirection.Backward));
        g.ValidateRoute(ters).Should().BeFalse("ters yönde giriş/çıkış uyuşmuyor");
    }

    [Fact]
    public void T218_AreAdjacent_OrtakDugum()
    {
        var a = N(0, 0); var b = N(100, 0); var c = N(200, 0);
        var s1 = S(a, b); var s2 = S(b, c); var s3 = S(a, c);  // s3 a-c arası (s1,s2 ile b'de değil a/c'de komşu)
        var g = TrackGraph.Build(new[] { a, b, c }, new[] { s1, s2, s3 });
        g.AreAdjacent(s1.Id, s2.Id).Should().BeTrue("b düğümünü paylaşıyorlar");
        g.AreAdjacent(s1.Id, s3.Id).Should().BeTrue("a düğümünü paylaşıyorlar");
        g.AreAdjacent(s1.Id, s1.Id).Should().BeFalse("kendisiyle komşu değil");
    }

    [Fact]
    public void T219_FindBlocks_KapaliHalka_SonsuzDonguYok()
    {
        // A-B-C-A üçgeni: tüm düğümler derece-2. Sonsuz döngüye girmemeli.
        var a = N(0, 0); var b = N(100, 0); var c = N(50, 100);
        var s1 = S(a, b); var s2 = S(b, c); var s3 = S(c, a);
        var g = TrackGraph.Build(new[] { a, b, c }, new[] { s1, s2, s3 });
        var bloklar = g.FindBlocks();   // çökmeden dönmeli
        bloklar.SelectMany(bl => bl.SegmentIds).Distinct().Should().HaveCount(3, "3 segment de bir bloğa atanmalı");
    }

    [Fact]
    public void T220_FindBlocks_HerSegmentTamBirBlokta()
    {
        // Karışık: düz hat + makas
        var a = N(0, 0); var b = N(100, 0); var c = N(200, 0); var d = N(200, 100); var e = N(300, 0);
        var s1 = S(a, b); var s2 = S(b, c); var s3 = S(c, d); var s4 = S(c, e);  // c derece-3 (makas)
        var g = TrackGraph.Build(new[] { a, b, c, d, e }, new[] { s1, s2, s3, s4 });
        var bloklar = g.FindBlocks();
        var tumSeg = bloklar.SelectMany(bl => bl.SegmentIds).ToList();
        tumSeg.Should().HaveCount(4, "her segment tam bir kez");
        tumSeg.Distinct().Should().HaveCount(4, "hiçbir segment iki blokta olamaz");
    }
}
