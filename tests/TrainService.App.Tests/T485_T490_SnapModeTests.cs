using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;
using TrainService.Core.Geometry;
using TrainService.Core.Entities;
using TrainService.Cad;
using TrainService.Cad.Snapping;
using TrainService.Cad.UndoRedo;
using TrainService.App.Controls.Ribbon;

namespace TrainService.App.Tests;

public sealed class T485_T490_SnapModeTests
{
    [Fact]
    public void T485_SnapKind_Midpoint_Exists()
    {
        var type = typeof(SnapKind);
        var names = Enum.GetNames(type);
        names.Should().Contain("Midpoint", "SnapKind.Midpoint must exist");
        var midpointVal = (int)Enum.Parse(type, "Midpoint");
        midpointVal.Should().Be(40, "Midpoint should be value 40");
    }

    [Fact]
    public void T486_SnapEngine_DisabledKinds_Blocks_Snap()
    {
        var doc = new CadDocument();
        var endpointProvider = new EndpointSnapProvider();
        var gridProvider = new GridSnapProvider();
        var engine = new SnapEngine(new ISnapProvider[] { endpointProvider, gridProvider });

        var node = new TrackNode
        {
            Position = new Vector2D(100, 100),
            LayerId = CadDocument.SabitKatmanlar.Zemin
        };
        new AddEntityCommand(node).Execute(doc);

        var result1 = engine.Resolve(new Vector2D(105, 100), 20, doc);
        result1.Kind.Should().Be(SnapKind.Endpoint);

        engine.DisabledKinds.Add(SnapKind.Endpoint);

        var result2 = engine.Resolve(new Vector2D(105, 100), 20, doc);
        result2.Kind.Should().NotBe(SnapKind.Endpoint, "endpoint disabled");
        result2.Kind.Should().Be(SnapKind.Grid, "should fall back to grid");
    }

    [Fact]
    public void T487_MidpointSnapProvider_Snaps_To_Midpoint()
    {
        var doc = new CadDocument();
        var layerId = CadDocument.SabitKatmanlar.Zemin;

        var n1 = new TrackNode { Position = new Vector2D(0, 0), LayerId = layerId };
        var n2 = new TrackNode { Position = new Vector2D(100, 0), LayerId = layerId };
        var seg = new TrackSegment { StartNodeId = n1.Id, EndNodeId = n2.Id, LayerId = layerId };

        foreach (var e in new CadEntity[] { n1, n2, seg })
            new AddEntityCommand(e).Execute(doc);

        var provider = new MidpointSnapProvider();
        var result = provider.TrySnap(new Vector2D(50, 0), 10, doc);

        result.Should().NotBeNull();
        result!.Kind.Should().Be(SnapKind.Midpoint);
        result.Point.X.Should().BeApproximately(50, 0.01);
        result.Point.Y.Should().BeApproximately(0, 0.01);
        result.TargetId.Should().Be(seg.Id);
    }

    [Fact]
    public void T487b_MidpointSnapProvider_NoSnap_When_Far()
    {
        var doc = new CadDocument();
        var layerId = CadDocument.SabitKatmanlar.Zemin;

        var n1 = new TrackNode { Position = new Vector2D(0, 0), LayerId = layerId };
        var n2 = new TrackNode { Position = new Vector2D(100, 0), LayerId = layerId };
        var seg = new TrackSegment { StartNodeId = n1.Id, EndNodeId = n2.Id, LayerId = layerId };

        new AddEntityCommand(n1).Execute(doc);
        new AddEntityCommand(n2).Execute(doc);
        new AddEntityCommand(seg).Execute(doc);

        var provider = new MidpointSnapProvider();
        var result = provider.TrySnap(new Vector2D(200, 200), 10, doc);
        result.Should().BeNull("cursor far from midpoint");
    }

    [Fact]
    public void T488_SnapEngine_Priority_Order()
    {
        var endpoint = new EndpointSnapProvider();
        var midpoint = new MidpointSnapProvider();
        var onSegment = new OnSegmentSnapProvider();
        var grid = new GridSnapProvider();

        endpoint.Priority.Should().Be(10);
        midpoint.Priority.Should().Be(15);
        onSegment.Priority.Should().Be(20);
        grid.Priority.Should().Be(100);

        endpoint.Priority.Should().BeLessThan(midpoint.Priority);
        midpoint.Priority.Should().BeLessThan(onSegment.Priority);
        onSegment.Priority.Should().BeLessThan(grid.Priority);
    }

    [Fact]
    public void T488b_SnapEngine_Endpoint_Wins_Over_Midpoint()
    {
        var doc = new CadDocument();
        var layerId = CadDocument.SabitKatmanlar.Zemin;

        var n1 = new TrackNode { Position = new Vector2D(0, 0), LayerId = layerId };
        var n2 = new TrackNode { Position = new Vector2D(100, 0), LayerId = layerId };
        var nExtra = new TrackNode { Position = new Vector2D(80, 0), LayerId = layerId };
        var seg = new TrackSegment { StartNodeId = n1.Id, EndNodeId = n2.Id, LayerId = layerId };

        foreach (var e in new CadEntity[] { n1, n2, nExtra, seg })
            new AddEntityCommand(e).Execute(doc);

        var engine = new SnapEngine(new ISnapProvider[]
        {
            new EndpointSnapProvider(),
            new MidpointSnapProvider(),
            new OnSegmentSnapProvider(),
            new GridSnapProvider()
        });

        var result = engine.Resolve(new Vector2D(80, 0), 15, doc);
        result.Kind.Should().Be(SnapKind.Endpoint,
            "endpoint (priority 10) wins over midpoint (priority 15)");
    }

    [Fact]
    public void T489_RibbonDefinition_SnapButtons_Exist()
    {
        var allItems = RibbonDefinitions.AllItems.ToList();

        var snapEndpoint = allItems.FirstOrDefault(i => i.Id == "SnapEndpoint");
        snapEndpoint.Should().NotBeNull();
        snapEndpoint!.IsToggle.Should().BeTrue();

        var snapMidpoint = allItems.FirstOrDefault(i => i.Id == "SnapMidpoint");
        snapMidpoint.Should().NotBeNull();
        snapMidpoint!.IsToggle.Should().BeTrue();

        var snapOnSegment = allItems.FirstOrDefault(i => i.Id == "SnapOnSegment");
        snapOnSegment.Should().NotBeNull();
        snapOnSegment!.IsToggle.Should().BeTrue();

        var snapGrid = allItems.FirstOrDefault(i => i.Id == "SnapGridSnp");
        snapGrid.Should().NotBeNull();
        snapGrid!.IsToggle.Should().BeTrue();
    }

    [Fact]
    public void T490_AllRibbonIds_Unique()
    {
        var allIds = RibbonDefinitions.AllItems.Select(i => i.Id).ToList();
        var distinctIds = allIds.Distinct().ToList();
        distinctIds.Count.Should().Be(allIds.Count, "all ribbon item IDs must be unique");
    }
}