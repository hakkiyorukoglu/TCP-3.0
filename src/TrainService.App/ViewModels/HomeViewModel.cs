using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TrainService.Core.Abstractions;

namespace TrainService.App.ViewModels;

public partial class HomeViewModel : ObservableObject
{
    private readonly ILogBus _logBus;

    public HomeViewModel(ILogBus logBus)
    {
        _logBus = logBus;
    }

    [RelayCommand]
    private void TestLog()
    {
        _logBus.Info("System", "Sistem çalışmaya başladı.");
        _logBus.Success("Network", "Veritabanına bağlanıldı.");
        _logBus.Warn("Sensor", "Gecikme süresi 50ms'yi aştı.");
        _logBus.Error("Train_1", "Haberleşme koptu!");
    }
}
