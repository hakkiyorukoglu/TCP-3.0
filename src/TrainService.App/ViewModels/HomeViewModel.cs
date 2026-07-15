using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using TrainService.Core.Abstractions;

namespace TrainService.App.ViewModels;

public partial class HomeViewModel : ObservableObject
{
    private readonly ILogBus _logBus;
    private readonly IMqttHub _mqttHub;

    public HomeViewModel(ILogBus logBus, IMqttHub mqttHub)
    {
        _logBus = logBus;
        _mqttHub = mqttHub;
    }

    [RelayCommand]
    private void TestLog()
    {
        _logBus.Info("System", "Sistem çalışmaya başladı.");
        _logBus.Success("Network", "Veritabanına bağlanıldı.");
        _logBus.Warn("Sensor", "Gecikme süresi 50ms'yi aştı.");
        _logBus.Error("Train_1", "Haberleşme koptu!");
    }

    [RelayCommand]
    private async Task TestMqttPublishAsync()
    {
        await _mqttHub.PublishAsync("trains/test", "{ \"status\": \"ok\", \"speed\": 85 }");
    }
}
