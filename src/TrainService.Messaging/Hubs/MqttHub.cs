using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TrainService.Core.Abstractions;
using MQTTnet;
using MQTTnet.Client;

namespace TrainService.Messaging.Hubs;

public class MqttHub : IMqttHub, IDisposable
{
    private readonly IMqttClient _mqttClient;
    private readonly ISettingsService _settingsService;
    private readonly ILogBus _logBus;
    private MqttClientOptions? _options;

    public event Action<string, string>? OnMessageReceived;

    public MqttHub(ISettingsService settingsService, ILogBus logBus)
    {
        _settingsService = settingsService;
        _logBus = logBus;

        var factory = new MqttFactory();
        _mqttClient = factory.CreateMqttClient();

        _mqttClient.ConnectedAsync += e =>
        {
            _logBus.Success("MqttHub", "MQTT Broker'a bağlanıldı.");
            return Task.CompletedTask;
        };

        _mqttClient.DisconnectedAsync += e =>
        {
            _logBus.Error("MqttHub", "MQTT Broker bağlantısı koptu!");
            return Task.CompletedTask;
        };

        _mqttClient.ApplicationMessageReceivedAsync += e =>
        {
            var topic = e.ApplicationMessage.Topic;
            
            string payload = string.Empty;
            if (e.ApplicationMessage.PayloadSegment.Array != null)
            {
                payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment.Array, e.ApplicationMessage.PayloadSegment.Offset, e.ApplicationMessage.PayloadSegment.Count);
            }
            else if (e.ApplicationMessage.Payload != null)
            {
                payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
            }

            OnMessageReceived?.Invoke(topic, payload);
            return Task.CompletedTask;
        };
    }

    public async Task ConnectAsync()
    {
        var config = _settingsService.GetMqttConfig();
        _options = new MqttClientOptionsBuilder()
            .WithTcpServer(config.BrokerIp, config.Port)
            .Build();

        _logBus.Info("MqttHub", $"Bağlanılıyor: {config.BrokerIp}:{config.Port}...");

        try
        {
            await _mqttClient.ConnectAsync(_options, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logBus.Error("MqttHub", $"Bağlantı hatası: {ex.Message}");
        }
    }

    public async Task DisconnectAsync()
    {
        if (_mqttClient.IsConnected)
        {
            await _mqttClient.DisconnectAsync();
        }
    }

    public async Task PublishAsync(string topic, string payload)
    {
        if (!_mqttClient.IsConnected)
        {
            _logBus.Warn("MqttHub", "Bağlantı yok, mesaj gönderilemedi.");
            return;
        }

        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .Build();

        await _mqttClient.PublishAsync(message, CancellationToken.None);
        _logBus.Info("MqttHub-Pub", $"[{topic}] {payload}");
    }

    public async Task SubscribeAsync(string topic)
    {
        if (_mqttClient.IsConnected)
        {
            var factory = new MqttFactory();
            var subOptions = factory.CreateSubscribeOptionsBuilder()
                .WithTopicFilter(f => f.WithTopic(topic))
                .Build();

            await _mqttClient.SubscribeAsync(subOptions, CancellationToken.None);
            _logBus.Info("MqttHub", $"Abone olundu: {topic}");
        }
    }

    public void Dispose()
    {
        _mqttClient?.Dispose();
    }
}
