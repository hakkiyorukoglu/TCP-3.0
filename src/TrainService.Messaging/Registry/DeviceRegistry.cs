using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using TrainService.Core.Abstractions;

namespace TrainService.Messaging.Registry;

public class DeviceRegistry : IDeviceRegistry
{
    private readonly IMqttHub _mqttHub;
    private readonly ILogBus _logBus;
    private readonly ConcurrentDictionary<string, bool> _deviceStatusMap = new();

    public event Action<string, bool>? OnDeviceStatusChanged;

    public DeviceRegistry(IMqttHub mqttHub, ILogBus logBus)
    {
        _mqttHub = mqttHub;
        _logBus = logBus;
    }

    public void StartListening()
    {
        _mqttHub.OnMessageReceived += HandleMessage;
        
        Task.Run(async () => 
        {
            await _mqttHub.SubscribeAsync("restaurant/status/#");
            await _mqttHub.SubscribeAsync("restaurant/log/#");
            _logBus.Info("DeviceRegistry", "Cihaz LWT ve Log dinleyicileri başlatıldı.");
        });
    }

    private void HandleMessage(string topic, string payload)
    {
        if (topic.StartsWith("restaurant/status/"))
        {
            var deviceId = topic.Substring("restaurant/status/".Length);
            var isOnline = payload.Equals("online", StringComparison.OrdinalIgnoreCase);
            
            bool previousStatus = false;
            _deviceStatusMap.TryGetValue(deviceId, out previousStatus);

            if (previousStatus != isOnline || !_deviceStatusMap.ContainsKey(deviceId))
            {
                _deviceStatusMap.AddOrUpdate(deviceId, isOnline, (k, v) => isOnline);
                OnDeviceStatusChanged?.Invoke(deviceId, isOnline);
                
                if (isOnline)
                    _logBus.Success("DeviceRegistry", $"Cihaz bağlandı: {deviceId}");
                else
                    _logBus.Error("DeviceRegistry", $"Cihaz çevrimdışı (LWT koptu): {deviceId}");
            }
        }
        else if (topic.StartsWith("restaurant/log/"))
        {
            var deviceId = topic.Substring("restaurant/log/".Length);
            _logBus.Info($"HW-Log [{deviceId}]", payload);
        }
    }

    public bool IsDeviceOnline(string deviceId)
    {
        return _deviceStatusMap.TryGetValue(deviceId, out var isOnline) && isOnline;
    }
}
