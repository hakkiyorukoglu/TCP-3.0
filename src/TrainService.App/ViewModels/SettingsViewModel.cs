using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TrainService.Core.Abstractions;
using TrainService.Core.Models.Config;

namespace TrainService.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private string _mqttBrokerIp = string.Empty;

    [ObservableProperty]
    private int _mqttPort;

    public SettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        LoadSettings();
    }

    private void LoadSettings()
    {
        var config = _settingsService.GetMqttConfig();
        MqttBrokerIp = config.BrokerIp;
        MqttPort = config.Port;
    }

    [RelayCommand]
    private void SaveSettings()
    {
        var config = new MqttConfig
        {
            BrokerIp = MqttBrokerIp,
            Port = MqttPort
        };
        _settingsService.SaveMqttConfig(config);
    }
}
