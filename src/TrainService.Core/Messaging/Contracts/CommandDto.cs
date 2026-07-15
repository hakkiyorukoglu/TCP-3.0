namespace TrainService.Core.Messaging.Contracts;

public record CommandDto(string CmdId, string TrainId, string TargetStationId, string Action);
