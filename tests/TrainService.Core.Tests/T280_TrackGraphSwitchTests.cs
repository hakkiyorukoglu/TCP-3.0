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

public class T280_TrackGraphSwitchTests
{
    private static TrackNode N(double x = 0, double y = 0) => new() { Position = new Vector2D(x, y), LayerId = Guid.NewGuid() };
    private static TrackSegment S(TrackNode a, TrackNode b) => new()
    {
        StartNodeId = a.Id,
        EndNodeId = b.Id,
        LayerId = a.LayerId,
        LengthMm = (b.Position - a.Position).Length
    };
    private static RailSwitch MakeSwitch(TrackNode entry, TrackNode main, TrackNode div, SwitchState state = SwitchState.Main)
        => new()
        {
            Position = entry.Position,
            RotationDeg = 0,
            EntryNodeId = entry.Id,
            MainExitNodeId = main.Id,
            DivergingExitNodeId = div.Id,
            State = state,
            LayerId = entry.LayerId
        };

    /// <summary>
    /// Switch senaryosu: entry(0,0) ── main(80,0), diverging(80,37)
    /// entry → main segment ve entry → diverging segment
    /// </summary>
    private static (TrackGraph g, RailSwitch sw, TrackNode entry, TrackNode main, TrackNode div, TrackSegment sMain, TrackSegment sDiv) SwitchScene()
    {
        var entry = N(0, 0);
        var main = N(80, 0);
        var div = N(80, 37);
        var sMain = S(entry, main);
        var sDiv = S(entry, div);
        var sw = MakeSwitch(entry, main, div);
        var g = TrackGraph.Build(new[] { entry, main, div }, new[] { sMain, sDiv }, new[] { sw });
        return (g, sw, entry, main, div, sMain, sDiv);
    }

    [Fact]
    public void T280_Build_WithSwitches_SwitchPortsMapped()
    {
        var (g, sw, entry, main, div, _, _) = SwitchScene();

        g.IsSwitchPort(entry.Id).Should().BeTrue("entry bir switch portudur");
        g.IsSwitchPort(main.Id).Should().BeTrue("main bir switch portudur");
        g.IsSwitchPort(div.Id).Should().BeTrue("diverging bir switch portudur");
        g.Switches.Should().ContainKey(sw.Id);
    }

    [Fact]
    public void T281_IsSwitchPort_NonSwitchNode_ReturnsFalse()
    {
        var plain = N(200, 200);
        var g = TrackGraph.Build(new[] { plain }, Array.Empty<TrackSegment>());

        g.IsSwitchPort(plain.Id).Should().BeFalse("sıradan düğüm switch portu değildir");
    }

    [Fact]
    public void T282_GetSwitchState_MainState_ReturnsMain()
    {
        var (g, _, entry, main, div, _, _) = SwitchScene();
        // switch State = Main (varsayılan)

        var state = g.GetSwitchState(entry.Id);
        state.Should().Be(SwitchState.Main, "switch State=Main iken Entry port Main durumundadır");

        state = g.GetSwitchState(main.Id);
        state.Should().Be(SwitchState.Main, "MainExit port Main durumundadır");

        state = g.GetSwitchState(div.Id);
        state.Should().Be(SwitchState.Diverging, "DivergingExit port her zaman Diverging durumundadır");
    }

    [Fact]
    public void T283_GetSwitchState_DivergingState_ReturnsDiverging()
    {
        // Switch State = Diverging
        var entry = N(0, 0);
        var main = N(80, 0);
        var div = N(80, 37);
        var sMain = S(entry, main);
        var sDiv = S(entry, div);
        var sw = MakeSwitch(entry, main, div, SwitchState.Diverging);
        var g = TrackGraph.Build(new[] { entry, main, div }, new[] { sMain, sDiv }, new[] { sw });

        g.GetSwitchState(entry.Id).Should().Be(SwitchState.Diverging, "switch Diverging iken Entry Diverging'dir");
        g.GetSwitchState(main.Id).Should().Be(SwitchState.Main, "MainExit her zaman Main'dir");
        g.GetSwitchState(div.Id).Should().Be(SwitchState.Diverging, "DivergingExit her zaman Diverging'dir");
    }

    [Fact]
    public void T284_GetSwitchForPort_EntryNode_ReturnsSwitch()
    {
        var (g, sw, entry, _, _, _, _) = SwitchScene();

        var result = g.GetSwitchForPort(entry.Id);
        result.Should().NotBeNull();
        result!.Id.Should().Be(sw.Id);
        result.EntryNodeId.Should().Be(entry.Id);
    }

    [Fact]
    public void T285_GetSwitchForPort_PlainNode_ReturnsNull()
    {
        var plain = N(200, 200);
        var g = TrackGraph.Build(new[] { plain }, Array.Empty<TrackSegment>());

        g.GetSwitchForPort(plain.Id).Should().BeNull("sıradan düğümün switch'i yoktur");
    }

    [Fact]
    public void T286_AreAdjacent_CrossSwitch_Works()
    {
        var (g, _, _, main, _, sMain, sDiv) = SwitchScene();

        // Entry → Main segment ile Entry → Diverging segment ortak düğüm paylaşır (entry)
        g.AreAdjacent(sMain.Id, sDiv.Id).Should().BeTrue("entry ortak düğüm → komşu");
    }
}
