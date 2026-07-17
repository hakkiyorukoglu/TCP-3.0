using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using TrainService.Data;
using TrainService.Core.Entities;
using TrainService.Cad;
using TrainService.Core.Enums;

namespace TrainService.Data.Tests;

public class T550_Route_SaveLoad_RoundTrip : IClassFixture<TempSqliteFixture>
{
    private readonly TempSqliteFixture _fx;

    public T550_Route_SaveLoad_RoundTrip(TempSqliteFixture fx)
    {
        _fx = fx;
    }

    [Fact]
    public async Task T550_Route_SaveLoad()
    {
        var (store, projectId) = await TestHelpers.YeniStoreVeProjeAsync(_fx, "RouteProje");
        var doc = TestHelpers.DokumaniKur(katman: 1, node: 5, segment: 4, gridSize: 50.0);   // zincir: n1-n2-n3-n4-n5
        
        var segments = doc.Entities.OfType<TrackSegment>().ToList();
        var route = new Route { LayerId = doc.ActiveLayerId, Name = "Test Route" };
        route.Steps.Add(new RouteStep(segments[0].Id, TravelDirection.Forward));
        route.Steps.Add(new RouteStep(segments[1].Id, TravelDirection.Backward));
        doc.AddEntity(route);

        await store.SaveDocumentAsync(projectId, doc);
        
        var loadedDoc = await store.LoadDocumentAsync(projectId);
        
        loadedDoc.Should().NotBeNull();
        var routes = loadedDoc!.Entities.OfType<Route>().ToList();
        routes.Should().HaveCount(1);
        
        var loadedRoute = routes[0];
        loadedRoute.Id.Should().Be(route.Id);
        loadedRoute.LayerId.Should().Be(route.LayerId);
        loadedRoute.Name.Should().Be("Test Route");
        loadedRoute.Steps.Should().HaveCount(2);
        loadedRoute.Steps[0].SegmentId.Should().Be(segments[0].Id);
        loadedRoute.Steps[0].Direction.Should().Be(TravelDirection.Forward);
        loadedRoute.Steps[1].SegmentId.Should().Be(segments[1].Id);
        loadedRoute.Steps[1].Direction.Should().Be(TravelDirection.Backward);
        
        loadedRoute.CachedBounds.Should().NotBeNull("Yükleme sırasında route bounds yeniden hesaplanmalı");
    }
}
