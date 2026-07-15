using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using TrainService.Core.Abstractions;
using TrainService.Core.Models.Config;

namespace TrainService.App.Services;

public class SettingsService : ISettingsService
{
    private readonly string _settingsFilePath = "appsettings.json";
    private readonly ILogBus _logBus;

    public SettingsService(ILogBus logBus)
    {
        _logBus = logBus;
    }

    public MqttConfig GetMqttConfig()
    {
        try
        {
            if (!File.Exists(_settingsFilePath))
                return new MqttConfig();

            var json = File.ReadAllText(_settingsFilePath);
            var doc = JsonNode.Parse(json);
            
            if (doc != null && doc["MqttSettings"] != null)
            {
                return doc["MqttSettings"].Deserialize<MqttConfig>() ?? new MqttConfig();
            }
        }
        catch (Exception ex)
        {
            _logBus.Error("SettingsService", $"Ayarlar okunamadı: {ex.Message}");
        }

        return new MqttConfig();
    }

    public void SaveMqttConfig(MqttConfig config)
    {
        try
        {
            JsonNode root;
            if (File.Exists(_settingsFilePath))
            {
                var json = File.ReadAllText(_settingsFilePath);
                root = JsonNode.Parse(json) ?? new JsonObject();
            }
            else
            {
                root = new JsonObject();
            }

            root["MqttSettings"] = JsonSerializer.SerializeToNode(config);

            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(_settingsFilePath, root.ToJsonString(options));

            _logBus.Success("SettingsService", "Mqtt ayarları başarıyla kaydedildi.");
        }
        catch (Exception ex)
        {
            _logBus.Error("SettingsService", $"Ayarlar kaydedilemedi: {ex.Message}");
        }
    }
}
