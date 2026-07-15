using System;

namespace TrainService.Core.Events;

public record PingStatusUpdatedMessage(string IpAddress, bool IsSuccess);

public record MqttStatusUpdatedMessage(string DeviceId, bool IsOnline);
