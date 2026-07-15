using TrainService.Core.Models.Config;

namespace TrainService.Core.Abstractions;

public interface ISettingsService
{
    MqttConfig GetMqttConfig();
    DatabaseConfig GetDatabaseConfig();
    void SaveMqttConfig(MqttConfig config);
}
