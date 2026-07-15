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
    private readonly IPingService _pingService;
    private readonly IDispatchService _dispatchService;
    private readonly TrainService.Messaging.Mocks.MockStationClient _mockStation;

    public TrainManager(IMqttHub mqttHub, ILogBus logBus, IServiceScopeFactory scopeFactory, 
        IDeviceRegistry deviceRegistry, IPingService pingService, IDispatchService dispatchService, 
        TrainService.Messaging.Mocks.MockStationClient mockStation)
    {
        _mqttHub = mqttHub;
        _logBus = logBus;
        _scopeFactory = scopeFactory;
        _deviceRegistry = deviceRegistry;
        _pingService = pingService;
        _dispatchService = dispatchService;
        _mockStation = mockStation;
    }

    public void Initialize()
    {
        _deviceRegistry.StartListening();
        _pingService.StartPinging();
        _dispatchService.StartListening();
        _mockStation.StartListening();
        _mqttHub.OnMessageReceived += OnMqttMessageReceived;
        
        _mqttHub.ConnectAsync().ContinueWith(async t => 
        {
            if (t.IsCompletedSuccessfully)
            {
                _logBus.Success("TrainManager", "Gömülü broker'a loopback bağlantısı kuruldu.");
                
                // TEST: 2 saniye sonra test komutu gönder
                await System.Threading.Tasks.Task.Delay(2000);
                var testCmd = new TrainService.Core.Messaging.Contracts.CommandDto(Guid.NewGuid().ToString(), "Train-1", "Station-5", "ROUTE_TO_STATION");
                await _dispatchService.SendCommandAndWaitAckAsync(testCmd, 3000);
            }
            else
            {
                _logBus.Error("TrainManager", "Loopback MQTT bağlantısı başarısız oldu.");
            }
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
