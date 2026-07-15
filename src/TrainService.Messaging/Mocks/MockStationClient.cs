using System;
using System.Text.Json;
using System.Threading.Tasks;
using TrainService.Core.Abstractions;
using TrainService.Core.Messaging.Contracts;

namespace TrainService.Messaging.Mocks;

public class MockStationClient
{
    private readonly IMqttHub _mqttHub;
    private readonly ILogBus _logBus;
    private readonly string _stationId = "Station-5";

    public MockStationClient(IMqttHub mqttHub, ILogBus logBus)
    {
        _mqttHub = mqttHub;
        _logBus = logBus;
    }

    public void StartListening()
    {
        _mqttHub.OnMessageReceived += HandleMessage;
        Task.Run(async () => await _mqttHub.SubscribeAsync("restaurant/commands"));
    }

    private async void HandleMessage(string topic, string payload)
    {
        if (topic == "restaurant/commands")
        {
            try
            {
                var cmd = JsonSerializer.Deserialize<CommandDto>(payload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (cmd != null && cmd.TargetStationId == _stationId)
                {
                    _logBus.Info($"MockStation [{_stationId}]", $"Emir alındı: '{cmd.Action}'. 500ms içinde ACK gönderiliyor...");
                    
                    // İşlem süresini (Makas değiştirme vs) simüle et
                    await Task.Delay(500);

                    var ack = new AckDto(cmd.CmdId, Divert: true);
                    var ackPayload = JsonSerializer.Serialize(ack);
                    
                    await _mqttHub.PublishAsync($"restaurant/ack/{_stationId}", ackPayload);
                }
            }
            catch 
            {
                // Parse error yutulur (Gerçek sistemde loglanabilir)
            }
        }
    }
}
