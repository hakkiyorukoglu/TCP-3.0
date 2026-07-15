namespace TrainService.Core.Messaging.Contracts;

public record AckDto(string CmdId, bool Divert);
