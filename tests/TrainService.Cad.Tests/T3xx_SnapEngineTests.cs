using System;
using System.Linq;
using FluentAssertions;
using TrainService.Cad;
using TrainService.Cad.Snapping;
using TrainService.Core.Entities;
using TrainService.Core.Geometry;
using Xunit;

namespace TrainService.Cad.Tests;

public class T3xx_SnapEngineTests
{
    private CadDocument CreateDocWithSegment(out TrackNode nA, out TrackNode nB, out TrackSegment seg)
    {
        var doc = new CadDocument();
        nA = new TrackNode { Id = Guid.NewGuid(), Position = new Vector2D(100, 100), LayerId = doc.ActiveLayerId };
        nB = new TrackNode { Id = Guid.NewGuid(), Position = new Vector2D(300, 100), LayerId = doc.ActiveLayerId };
        seg = new TrackSegment { Id = Guid.NewGuid(), StartNodeId = nA.Id, EndNodeId = nB.Id, LayerId = doc.ActiveLayerId };
        
        // Entity ekleme işlemi SpatialHash'e de düşer
        doc.RestoreEntity(nA);
        doc.RestoreEntity(nB);
        doc.RestoreEntity(seg);
        return doc;
    }

    [Fact]
    public void T309_SnapEngine_OncelikZinciri()
    {
        // 3 provider da var
        var engine = new SnapEngine(new ISnapProvider[] {
            new EndpointSnapProvider(),
            new OnSegmentSnapProvider(),
            new GridSnapProvider()
        });

        var doc = CreateDocWithSegment(out var nA, out _, out var seg);

        // nA noktasına (100, 100) çok yakın bir nokta: (101, 100).
        // Hem OnSegment (101,100'e snapler), hem Endpoint (100,100'e snapler), hem Grid (100,100'e snapler) geçerli.
        // Ama Endpoint (Priority=10), OnSegment (20) ve Grid (100)'den daha önceliklidir.
        var r = engine.Resolve(new Vector2D(101, 100), 5.0, doc);
        
        r.Should().NotBeNull();
        r!.Kind.Should().Be(SnapKind.Endpoint, "Aynı tolerans içindeyse yüksek öncelikli olan kazanmalı");
        r.TargetId.Should().Be(nA.Id);
        r.Point.Should().Be(new Vector2D(100, 100));
        
        // Eğer Endpoint için toleransın DIŞINDA ama OnSegment İÇİNDE isek
        // (150, 100)'e tıklarsak nA (100,100)'e uzaklık 50. Tolerans 5.0 ise Endpoint bulamaz.
        var r2 = engine.Resolve(new Vector2D(150, 101), 5.0, doc);
        r2.Should().NotBeNull();
        r2!.Kind.Should().Be(SnapKind.OnSegment);
        r2.TargetId.Should().Be(seg.Id);
        r2.Point.Should().Be(new Vector2D(150, 100)); // Dik izdüşüm
    }

    [Fact]
    public void T310_OnSegment_Clamp()
    {
        var engine = new SnapEngine(new ISnapProvider[] { new OnSegmentSnapProvider() });
        var doc = CreateDocWithSegment(out var nA, out _, out _);

        // Segment (100,100)-(300,100). 
        // (80, 100) noktası segmentin DIŞINDA. İzdüşüm (100,100)'e clamp'lenir.
        // Eğer toleransımız (mesela 25) içindeyse (80'den 100'e 20 birim), o uca snaplenir.
        var r = engine.Resolve(new Vector2D(80, 100), 25.0, doc);
        
        r.Should().NotBeNull();
        r!.Kind.Should().Be(SnapKind.OnSegment);
        r.Point.Should().Be(new Vector2D(100, 100), "Clamp edilmiş uç noktaya dönmeli");
    }

}
