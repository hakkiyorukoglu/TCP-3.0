using System;
using System.Linq;
using FluentAssertions;
using Xunit;
using TrainService.Data;
using TrainService.Core.Entities;
using TrainService.Core.Enums;

namespace TrainService.Data.Tests;

public class CrudSmokeTests : IClassFixture<TempSqliteFixture>
{
    private readonly TempSqliteFixture _fx;

    public CrudSmokeTests(TempSqliteFixture fx)
    {
        _fx = fx;
    }

    [Fact]
    public void Crud_Project_AddReadUpdateDelete()
    {
        using var db = _fx.CreateContext();
        var p = new Project { Id = Guid.NewGuid(), Name = "crud_p" };
        db.Projects.Add(p); db.SaveChanges();
        db.Projects.Find(p.Id).Should().NotBeNull();
        p.Name = "updated"; db.SaveChanges(); db.Projects.Find(p.Id)!.Name.Should().Be("updated");
        db.Projects.Remove(p); db.SaveChanges();
        db.Projects.Find(p.Id).Should().BeNull();
    }

    [Fact]
    public void Crud_Layer_AddReadUpdateDelete()
    {
        using var db = _fx.CreateContext();
        var p = new Project { Name = "crud_l" }; db.Projects.Add(p); db.SaveChanges();
        var l = new Layer { Id = Guid.NewGuid(), ProjectId = p.Id, Name = "layer" };
        db.Layers.Add(l); db.SaveChanges();
        db.Layers.Find(l.Id).Should().NotBeNull();
        l.Name = "l2"; db.SaveChanges(); db.Layers.Find(l.Id)!.Name.Should().Be("l2");
        db.Layers.Remove(l); db.SaveChanges();
        db.Layers.Find(l.Id).Should().BeNull();
    }

    [Fact]
    public void Crud_TrackNode_AddReadUpdateDelete()
    {
        using var db = _fx.CreateContext();
        var p = new Project { Name = "crud_tn" }; db.Projects.Add(p); db.SaveChanges();
        var l = new Layer { ProjectId = p.Id, Name = "l" }; db.Layers.Add(l); db.SaveChanges();

        var n = new TrackNode { Id = Guid.NewGuid(), ProjectId = p.Id, LayerId = l.Id, Position = new TrainService.Core.Geometry.Vector2D(1, 2) };
        db.TrackNodes.Add(n); db.SaveChanges();
        db.TrackNodes.Find(n.Id).Should().NotBeNull();
        n.Position = new TrainService.Core.Geometry.Vector2D(99, 99); db.SaveChanges(); db.TrackNodes.Find(n.Id)!.Position.X.Should().Be(99);
        db.TrackNodes.Remove(n); db.SaveChanges();
        db.TrackNodes.Find(n.Id).Should().BeNull();
    }

    [Fact]
    public void Crud_TrackSegment_AddReadUpdateDelete()
    {
        using var db = _fx.CreateContext();
        var p = new Project { Name = "crud_ts" }; db.Projects.Add(p); db.SaveChanges();
        var l = new Layer { ProjectId = p.Id, Name = "l" }; db.Layers.Add(l); db.SaveChanges();
        
        var t = new TrackSegment { Id = Guid.NewGuid(), ProjectId = p.Id, LayerId = l.Id, LengthMm = 5000 };
        db.TrackSegments.Add(t); db.SaveChanges();
        db.TrackSegments.Find(t.Id).Should().NotBeNull();
        t.LengthMm = 99; db.SaveChanges(); db.TrackSegments.Find(t.Id)!.LengthMm.Should().Be(99);
        db.TrackSegments.Remove(t); db.SaveChanges();
        db.TrackSegments.Find(t.Id).Should().BeNull();
    }

    [Fact]
    public void Crud_Route_AddReadUpdateDelete()
    {
        using var db = _fx.CreateContext();
        var p = new Project { Name = "crud_r" }; db.Projects.Add(p); db.SaveChanges();
        
        var r = new Route { Id = Guid.NewGuid(), ProjectId = p.Id, Name = "rt" };
        db.Routes.Add(r); db.SaveChanges();
        db.Routes.Find(r.Id).Should().NotBeNull();
        r.Name = "rt2"; db.SaveChanges(); db.Routes.Find(r.Id)!.Name.Should().Be("rt2");
        db.Routes.Remove(r); db.SaveChanges();
        db.Routes.Find(r.Id).Should().BeNull();
    }

    [Fact]
    public void Crud_RouteStep_AddReadUpdateDelete()
    {
        using var db = _fx.CreateContext();
        var p = new Project { Name = "crud_rs" }; db.Projects.Add(p); db.SaveChanges();
        var r = new Route { Id = Guid.NewGuid(), ProjectId = p.Id, Name = "rt" }; db.Routes.Add(r); db.SaveChanges();
        
        r.Steps.Add(new RouteStep(Guid.NewGuid(), TravelDirection.Forward));
        db.SaveChanges();
        
        db.Routes.Find(r.Id)!.Steps.Count.Should().Be(1);
        r.Steps[0] = new RouteStep(Guid.NewGuid(), TravelDirection.Backward);
        db.SaveChanges();
        db.Routes.Find(r.Id)!.Steps[0].Direction.Should().Be(TravelDirection.Backward);
        
        r.Steps.Clear();
        db.SaveChanges();
        db.Routes.Find(r.Id)!.Steps.Count.Should().Be(0);
    }

    [Fact]
    public void Crud_Switch_AddReadUpdateDelete()
    {
        using var db = _fx.CreateContext();
        var p = new Project { Name = "crud_sw" }; db.Projects.Add(p); db.SaveChanges();
        var l = new Layer { ProjectId = p.Id, Name = "l" }; db.Layers.Add(l); db.SaveChanges();

        var s = new RailSwitch { Id = Guid.NewGuid(), ProjectId = p.Id, LayerId = l.Id, State = SwitchState.Main };
        db.Switches.Add(s); db.SaveChanges();
        db.Switches.Find(s.Id).Should().NotBeNull();
        s.State = SwitchState.Diverging; db.SaveChanges(); db.Switches.Find(s.Id)!.State.Should().Be(SwitchState.Diverging);
        db.Switches.Remove(s); db.SaveChanges();
        db.Switches.Find(s.Id).Should().BeNull();
    }

    [Fact]
    public void Crud_Ramp_AddReadUpdateDelete()
    {
        using var db = _fx.CreateContext();
        var p = new Project { Name = "crud_rmp" }; db.Projects.Add(p); db.SaveChanges();
        var l = new Layer { ProjectId = p.Id, Name = "l" }; db.Layers.Add(l); db.SaveChanges();

        var r = new Ramp { Id = Guid.NewGuid(), ProjectId = p.Id, LayerId = l.Id, StartZ = 0 };
        db.Ramps.Add(r); db.SaveChanges();
        db.Ramps.Find(r.Id).Should().NotBeNull();
        r.StartZ = 99; db.SaveChanges(); db.Ramps.Find(r.Id)!.StartZ.Should().Be(99);
        db.Ramps.Remove(r); db.SaveChanges();
        db.Ramps.Find(r.Id).Should().BeNull();
    }

    [Fact]
    public void Crud_Station_AddReadUpdateDelete()
    {
        using var db = _fx.CreateContext();
        var p = new Project { Name = "crud_st" }; db.Projects.Add(p); db.SaveChanges();
        var l = new Layer { ProjectId = p.Id, Name = "l" }; db.Layers.Add(l); db.SaveChanges();

        var st = new Station { Id = Guid.NewGuid(), ProjectId = p.Id, LayerId = l.Id, Name = "st1" };
        db.Stations.Add(st); db.SaveChanges();
        db.Stations.Find(st.Id).Should().NotBeNull();
        st.Name = "st2"; db.SaveChanges(); db.Stations.Find(st.Id)!.Name.Should().Be("st2");
        db.Stations.Remove(st); db.SaveChanges();
        db.Stations.Find(st.Id).Should().BeNull();
    }

    [Fact]
    public void Crud_Train_AddReadUpdateDelete()
    {
        using var db = _fx.CreateContext();
        var p = new Project { Name = "crud_tr" }; db.Projects.Add(p); db.SaveChanges();

        var t = new Train { Id = Guid.NewGuid(), ProjectId = p.Id, Name = "tr1" };
        db.Trains.Add(t); db.SaveChanges();
        db.Trains.Find(t.Id).Should().NotBeNull();
        t.Name = "tr2"; db.SaveChanges(); db.Trains.Find(t.Id)!.Name.Should().Be("tr2");
        db.Trains.Remove(t); db.SaveChanges();
        db.Trains.Find(t.Id).Should().BeNull();
    }
}


