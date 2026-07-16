using System;

namespace TrainService.Core.Entities;

public sealed class EventLog
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime Ts { get; init; } = DateTime.UtcNow;
    public int Level { get; init; }
    public string Source { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}
