using System;
using TrainService.Core.Abstractions;

namespace TrainService.App.Services;

public class TrainManager : ITrainManager, IDisposable
{
    private readonly IMqttHub _mqttHub;
    private readonly ILogBus _logBus;

    public TrainManager(IMqttHub mqttHub, ILogBus logBus)
    {
        _mqttHub = mqttHub;
        _logBus = logBus;
    }

    public void Initialize()
    {
        _mqttHub.OnMessageReceived += OnMqttMessageReceived;
        
        _mqttHub.ConnectAsync().ContinueWith(t => 
        {
            _mqttHub.SubscribeAsync("trains/#");
        });

        _logBus.Info("TrainManager", "Başlatıldı ve dinliyor.");
    }

    private void OnMqttMessageReceived(string topic, string payload)
    {
        _logBus.Success("TrainManager-Sub", $"[{topic}] {payload}");
    }

    public void Dispose()
    {
        _mqttHub.OnMessageReceived -= OnMqttMessageReceived;
    }
}
