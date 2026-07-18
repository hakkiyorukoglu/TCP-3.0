using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using TrainService.Data;
using TrainService.Core.Entities;
using TrainService.Core.Enums;
using TrainService.Cad;

namespace TrainService.Data.Tests;

public class T560_RailSwitch_StoreRoundTrip : IClassFixture<TempSqliteFixture>
{
    private readonly TempSqliteFixture _fx;

    public T560_RailSwitch_StoreRoundTrip(TempSqliteFixture fx)
    {
        _fx = fx;
    }

    [Fact]
    public async Task T560_RailSwitch_SaveLoad()
    {
        var (store, projectId) = await TestHelpers.YeniStoreVeProjeAsync(_fx, "SwitchRoundTrip");
        var doc = TestHelpers.DokumaniKur(katman: 1, node: 5, segment: 4, gridSize: 50.0);

        var nodes = doc.Entities.OfType<TrackNode>().ToList();
        var sw = new RailSwitch
        {
            LayerId = doc.ActiveLayerId,
            Position = new TrainService.Core.Geometry.Vector2D(1234.5, 6789.0),
            RotationDeg = 45.0,
            EntryNodeId = nodes[0].Id,
            MainExitNodeId = nodes[1].Id,
            DivergingExitNodeId = nodes[2].Id,
            State = SwitchState.Diverging,
            BoundServoDeviceId = Guid.Parse("A1B2C3D4-E5F6-7890-ABCD-EF1234567890")
        };
        doc.AddEntity(sw);

        await store.SaveDocumentAsync(projectId, doc);

        var loadedDoc = await store.LoadDocumentAsync(projectId);

        loadedDoc.Should().NotBeNull();
        var switches = loadedDoc!.Entities.OfType<RailSwitch>().ToList();
        switches.Should().HaveCount(1);

        var loaded = switches[0];
        loaded.Id.Should().Be(sw.Id);
        loaded.LayerId.Should().Be(sw.LayerId);
        loaded.Position.X.Should().Be(1234.5);
        loaded.Position.Y.Should().Be(6789.0);
        loaded.RotationDeg.Should().Be(45.0);
        loaded.EntryNodeId.Should().Be(nodes[0].Id);
        loaded.MainExitNodeId.Should().Be(nodes[1].Id);
        loaded.DivergingExitNodeId.Should().Be(nodes[2].Id);
        loaded.State.Should().Be(SwitchState.Diverging);
        loaded.BoundServoDeviceId.Should().Be(Guid.Parse("A1B2C3D4-E5F6-7890-ABCD-EF1234567890"));
    }
}
