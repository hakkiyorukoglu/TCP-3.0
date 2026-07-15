using TrainService.Core.Models.Config;

namespace TrainService.Core.Abstractions;

public interface ISettingsService
{
    MqttConfig GetMqttConfig();
    void SaveMqttConfig(MqttConfig config);
}
