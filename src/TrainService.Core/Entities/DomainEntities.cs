using System;
using System.Collections.Generic;
using TrainService.Core.Geometry;
using TrainService.Core.Enums;

namespace TrainService.Core.Entities;

public abstract class CadEntity
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public Guid LayerId { get; set; }
    public bool IsSelected { get; set; }
    public virtual BoundingBox? Bounds => null;
}

public sealed class TrackNode : CadEntity
{
    public Vector2D Position { get; set; }
    public double Z { get; set; }
    public List<Guid> ConnectedSegments { get; } = new();
    public NodeRole Role { get; set; }
    
    public override BoundingBox? Bounds => new BoundingBox(Position.X, Position.Y, Position.X, Position.Y);
}

public sealed class TrackSegment : CadEntity
{
    public Guid StartNodeId { get; set; }
    public Guid EndNodeId { get; set; }
    public double LengthMm { get; set; }
}

public sealed record RouteStep(Guid SegmentId, TravelDirection Direction, SwitchState? SwitchState = null);

public sealed class Route : CadEntity
{
    public string Name { get; set; } = string.Empty;
    public List<RouteStep> Steps { get; } = new();
    public BoundingBox CachedBounds { get; set; }
    public override BoundingBox? Bounds => CachedBounds;
}

public sealed class RailSwitch : CadEntity
{
    public Vector2D Position { get; set; }
    public double RotationDeg { get; set; }
    public Guid EntryNodeId { get; set; }
    public Guid MainExitNodeId { get; set; }
    public Guid DivergingExitNodeId { get; set; }
    public SwitchState State { get; set; }
    public Guid? BoundServoDeviceId { get; set; }
}

public sealed class Ramp : CadEntity
{
    public Guid SegmentId { get; set; }
    public Vector2D Position { get; set; }
    public double RotationDeg { get; set; }
    public Guid EntryNodeId { get; set; }
    public Guid ExitNodeId { get; set; }
    public double StartZ { get; set; }
    public double EndZ { get; set; }
    public double LengthMm { get; set; }
    
    public double GradePercent =>
        LengthMm <= 0 || double.IsNaN(LengthMm)
            ? 0
            : (EndZ - StartZ) / LengthMm * 100.0;
}

public sealed class Station : CadEntity
{
    public int TableNo { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid EntrySwitchId { get; set; }
}

public sealed class Train : CadEntity
{
    public string Name { get; set; } = string.Empty;
    public string NfcTagId { get; set; } = string.Empty;
    public double MaxSpeedMmS { get; set; }
}

public sealed class Device : CadEntity
{
    public string Name { get; set; } = string.Empty;
    public DeviceKind Kind { get; set; }
    public string Ip { get; set; } = string.Empty;
    public string Mac { get; set; } = string.Empty;
}

public sealed class NetworkSwitch : CadEntity
{
    public string Name { get; set; } = string.Empty;
    public int PortCount { get; set; } = 5;
}

public sealed class SwitchPort : CadEntity
{
    public Guid NetworkSwitchId { get; set; }
    public int PortNo { get; set; }
    public PortRole Role { get; set; }
    public Guid? ConnectedDeviceId { get; set; }
    public Guid? CascadeSwitchId { get; set; }
}
