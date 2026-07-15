using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using TrainService.Core.Abstractions;
using TrainService.Cad.Abstractions;

namespace TrainService.App.Services;

public class TrainManager : ITrainManager, IDisposable
{
    private readonly IMqttHub _mqttHub;
    private readonly ILogBus _logBus;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDeviceRegistry _deviceRegistry;

    public TrainManager(IMqttHub mqttHub, ILogBus logBus, IServiceScopeFactory scopeFactory, IDeviceRegistry deviceRegistry)
    {
        _mqttHub = mqttHub;
        _logBus = logBus;
        _scopeFactory = scopeFactory;
        _deviceRegistry = deviceRegistry;
    }

    public void Initialize()
    {
        _deviceRegistry.StartListening();
        _mqttHub.OnMessageReceived += OnMqttMessageReceived;
        
        _mqttHub.ConnectAsync().ContinueWith(t => 
        {
            _mqttHub.SubscribeAsync("trains/#");
        });

        _logBus.Info("TrainManager", "Başlatıldı ve dinliyor.");

        Task.Run(async () => {
            try {
                using var scope = _scopeFactory.CreateScope();
                var cadParser = scope.ServiceProvider.GetRequiredService<ICadParser>();
                var result = await cadParser.ParseAsync("sample_map.json");
                
                _logBus.Success("CadParser", $"CAD dosyası başarıyla yüklendi: {result.Nodes.Count} Node, {result.Segments.Count} Segment bulundu.");
            }
            catch (Exception ex) {
                _logBus.Error("CadParser", $"Harita yüklenemedi: {ex.Message}");
            }
        });
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
