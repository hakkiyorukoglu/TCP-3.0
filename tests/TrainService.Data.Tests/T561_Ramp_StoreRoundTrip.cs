using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using TrainService.Data;
using TrainService.Core.Entities;
using TrainService.Cad;

namespace TrainService.Data.Tests;

public class T561_Ramp_StoreRoundTrip : IClassFixture<TempSqliteFixture>
{
    private readonly TempSqliteFixture _fx;

    public T561_Ramp_StoreRoundTrip(TempSqliteFixture fx)
    {
        _fx = fx;
    }

    [Fact]
    public async Task T561_Ramp_SaveLoad()
    {
        var (store, projectId) = await TestHelpers.YeniStoreVeProjeAsync(_fx, "RampRoundTrip");
        var doc = TestHelpers.DokumaniKur(katman: 1, node: 5, segment: 4, gridSize: 50.0);

        var nodes = doc.Entities.OfType<TrackNode>().ToList();
        var segments = doc.Entities.OfType<TrackSegment>().ToList();
        var rmp = new Ramp
        {
            LayerId = doc.ActiveLayerId,
            SegmentId = segments[0].Id,
            Position = new TrainService.Core.Geometry.Vector2D(111.1, 222.2),
            RotationDeg = 90.0,
            EntryNodeId = nodes[0].Id,
            ExitNodeId = nodes[1].Id,
            StartZ = 10.5,
            EndZ = 25.3,
            LengthMm = 5000.0
        };
        doc.AddEntity(rmp);

        await store.SaveDocumentAsync(projectId, doc);

        var loadedDoc = await store.LoadDocumentAsync(projectId);

        loadedDoc.Should().NotBeNull();
        var ramps = loadedDoc!.Entities.OfType<Ramp>().ToList();
        ramps.Should().HaveCount(1);

        var loaded = ramps[0];
        loaded.Id.Should().Be(rmp.Id);
        loaded.LayerId.Should().Be(rmp.LayerId);
        loaded.SegmentId.Should().Be(segments[0].Id);
        loaded.Position.X.Should().Be(111.1);
        loaded.Position.Y.Should().Be(222.2);
        loaded.RotationDeg.Should().Be(90.0);
        loaded.EntryNodeId.Should().Be(nodes[0].Id);
        loaded.ExitNodeId.Should().Be(nodes[1].Id);
        loaded.StartZ.Should().Be(10.5);
        loaded.EndZ.Should().Be(25.3);
        loaded.LengthMm.Should().Be(5000.0);
    }
}
