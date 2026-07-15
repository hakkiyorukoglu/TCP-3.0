using System;
using System.Collections.Generic;
using TrainService.Core.Enums;

namespace TrainService.Core.Abstractions;

public record LogMessage(DateTime Timestamp, LogLevel Level, string Source, string Message);

public interface ILogBus
{
    IReadOnlyList<LogMessage> GetAllLogs();
    void Write(LogLevel level, string source, string message);
    void Info(string source, string message);
    void Success(string source, string message);
    void Warn(string source, string message);
    void Error(string source, string message);
    
    event Action<LogMessage>? OnMessageReceived;
}
