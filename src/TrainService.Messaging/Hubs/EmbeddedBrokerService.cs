using System;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Server;
using TrainService.Core.Abstractions;

namespace TrainService.Messaging.Hubs;

public class EmbeddedBrokerService : IEmbeddedBrokerService
{
    private readonly ILogBus _logBus;
    private MqttServer? _mqttServer;

    public EmbeddedBrokerService(ILogBus logBus)
    {
        _logBus = logBus;
    }

    public async Task StartAsync(int port = 1883)
    {
        try
        {
            if (_mqttServer != null) return;

            var factory = new MqttFactory();
            var options = new MqttServerOptionsBuilder()
                .WithDefaultEndpoint()
                .WithDefaultEndpointPort(port)
                .Build();

            _mqttServer = factory.CreateMqttServer(options);

            _mqttServer.ClientConnectedAsync += e =>
            {
                _logBus.Info("Broker", $"Yeni cihaz bağlandı: {e.ClientId}");
                return Task.CompletedTask;
            };

            _mqttServer.ClientDisconnectedAsync += e =>
            {
                _logBus.Warn("Broker", $"Cihaz bağlantısı koptu: {e.ClientId} - Neden: {e.DisconnectType}");
                return Task.CompletedTask;
            };

            await _mqttServer.StartAsync();
            _logBus.Success("Broker", $"Embedded Broker başlatıldı. Port: {port}");
        }
        catch (Exception ex)
        {
            _logBus.Error("Broker", $"Broker başlatılamadı (Port: {port} dolu olabilir): {ex.Message}");
        }
    }

    public async Task StopAsync()
    {
        if (_mqttServer != null)
        {
            await _mqttServer.StopAsync();
            _mqttServer.Dispose();
            _mqttServer = null;
            _logBus.Info("Broker", "Embedded Broker durduruldu.");
        }
    }
}
