using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using TrainService.Core.Abstractions;
using TrainService.Core.Enums;

namespace TrainService.App.Services;

public class LogBus : ILogBus
{
    private const int MaxLogCount = 2000;
    private readonly ConcurrentQueue<LogMessage> _logs = new();
    private readonly IServiceScopeFactory _scopeFactory;

    public LogBus(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public event Action<LogMessage>? OnMessageReceived;

    public IReadOnlyList<LogMessage> GetAllLogs() => _logs.ToList();

    public void Write(LogLevel level, string source, string message)
    {
        var log = new LogMessage(DateTime.Now, level, source, message);
        
        _logs.Enqueue(log);
        if (_logs.Count > MaxLogCount)
        {
            _logs.TryDequeue(out _);
        }

        OnMessageReceived?.Invoke(log);

        // Veritabanına asenkron yaz
        System.Threading.Tasks.Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<ITcpRepository>();
                await repo.AddLogAsync(new TrainService.Core.Entities.EventLog
                {
                    Timestamp = log.Timestamp,
                    Level = level.ToString(),
                    Source = source,
                    Message = message
                });
            }
            catch { /* Hata göz ardı edilir (log-loop döngüsüne girmemesi için) */ }
        });
    }

    public void Info(string source, string message) => Write(LogLevel.Info, source, message);
    public void Success(string source, string message) => Write(LogLevel.Success, source, message);
    public void Warn(string source, string message) => Write(LogLevel.Warn, source, message);
    public void Error(string source, string message) => Write(LogLevel.Error, source, message);
}
