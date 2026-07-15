using System;
using System.Collections.Generic;
using TrainService.Core.Geometry;
using TrainService.Core.Enums;

namespace TrainService.Core.Entities;

public abstract class CadEntity
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid LayerId { get; set; }
    public bool IsSelected { get; set; }
}

public sealed class TrackNode : CadEntity
{
    public Vector2D Position { get; set; }
    public double Z { get; set; }
    public List<Guid> ConnectedSegments { get; } = new();
    public NodeRole Role { get; set; }
}

public sealed class TrackSegment : CadEntity
{
    public Guid StartNodeId { get; set; }
    public Guid EndNodeId { get; set; }
    public double LengthMm { get; set; }
}

public sealed record RouteStep(Guid SegmentId, TravelDirection Direction);

public sealed class Route : CadEntity
{
    public string Name { get; set; } = string.Empty;
    public List<RouteStep> Steps { get; } = new();
}

public sealed class RailSwitch : CadEntity
{
    public Guid NodeId { get; set; }
    public Guid MainSegmentId { get; set; }
    public Guid DivergingSegmentId { get; set; }
    public SwitchState State { get; set; }
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
