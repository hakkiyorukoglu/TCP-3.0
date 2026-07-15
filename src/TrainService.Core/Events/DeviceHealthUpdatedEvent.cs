using System;

namespace TrainService.Core.Events;

public enum DeviceHealthState
{
    Offline,
    PingOnly,
    Online
}

public record DeviceHealthUpdatedEvent(string IpAddress, DeviceHealthState State, DateTime LastSeen);
