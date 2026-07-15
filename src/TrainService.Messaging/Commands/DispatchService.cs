using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TrainService.Core.Abstractions;
using TrainService.Core.Messaging.Contracts;

namespace TrainService.Messaging.Commands;

public class DispatchService : IDispatchService
{
    private readonly IMqttHub _mqttHub;
    private readonly ILogBus _logBus;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<bool>> _pendingCommands = new();

    public DispatchService(IMqttHub mqttHub, ILogBus logBus)
    {
        _mqttHub = mqttHub;
        _logBus = logBus;
    }

    public void StartListening()
    {
        _mqttHub.OnMessageReceived += HandleMessage;
        Task.Run(async () => await _mqttHub.SubscribeAsync("restaurant/ack/#"));
    }

    private void HandleMessage(string topic, string payload)
    {
        if (topic.StartsWith("restaurant/ack/"))
        {
            try
            {
                var ack = JsonSerializer.Deserialize<AckDto>(payload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (ack != null && _pendingCommands.TryRemove(ack.CmdId, out var tcs))
                {
                    // ACK alındı, bekleyen non-blocking asenkron akışı serbest bırak (Superpowers Rule)
                    tcs.TrySetResult(ack.Divert);
                    _logBus.Success("DispatchService", $"[BAŞARILI] Onay (ACK) Alındı. CmdId: {ack.CmdId}");
                }
            }
            catch (Exception ex)
            {
                _logBus.Error("DispatchService", $"ACK Parse Hatası: {ex.Message}");
            }
        }
    }

    public async Task<bool> SendCommandAndWaitAckAsync(CommandDto command, int timeoutMs = 3000, CancellationToken cancellationToken = default)
    {
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeoutMs);

        // İptal (Timeout) durumunda TaskCompletionSource'u serbest bırakıp iptal hatası fırlatmasını sağla
        using var reg = timeoutCts.Token.Register(() => 
        {
            if (_pendingCommands.TryRemove(command.CmdId, out var pendingTcs))
            {
                pendingTcs.TrySetCanceled();
            }
        });

        _pendingCommands.TryAdd(command.CmdId, tcs);

        var payload = JsonSerializer.Serialize(command);
        await _mqttHub.PublishAsync("restaurant/commands", payload);
        _logBus.Info("DispatchService", $"[BEKLİYOR] Komut Yayınlandı. ACK bekleniyor: {payload}");

        try
        {
            return await tcs.Task;
        }
        catch (TaskCanceledException)
        {
            _logBus.Error("DispatchService", $"[HATA] Komut Zaman Aşımı! Cihazdan {timeoutMs}ms içinde yanıt gelmedi. CmdId: {command.CmdId}");
            return false;
        }
    }
}
