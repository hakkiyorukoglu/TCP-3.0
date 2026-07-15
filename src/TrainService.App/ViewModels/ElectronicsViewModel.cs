using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using TrainService.Core.Entities;
using TrainService.Core.Enums;
using TrainService.Data;

namespace TrainService.App.ViewModels;

public partial class ElectronicsViewModel : ObservableObject
{
    private readonly TrainDbContext _dbContext;

    [ObservableProperty]
    private ObservableCollection<NetworkSwitch> _switches = new();

    [ObservableProperty]
    private ObservableCollection<Device> _devices = new();

    [ObservableProperty]
    private NetworkSwitch? _selectedSwitch;

    [ObservableProperty]
    private Device? _selectedDevice;

    [ObservableProperty]
    private bool _isDrawerOpen;

    // Form alanları (Add Panel)
    [ObservableProperty] private string _newEntityName = string.Empty;
    [ObservableProperty] private string _newEntityIp = string.Empty;
    [ObservableProperty] private string _newEntityMac = string.Empty;
    [ObservableProperty] private int _newEntityTypeIndex = 0; // 0=Switch, 1=Device(PC), 2=Device(Station), 3=Device(Train)
    [ObservableProperty] private int _newSwitchPortCount = 5;

    public ElectronicsViewModel(TrainDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task LoadDataAsync()
    {
        var switchesList = await _dbContext.NetworkSwitches.ToListAsync();
        var devicesList = await _dbContext.Devices.ToListAsync();

        Switches = new ObservableCollection<NetworkSwitch>(switchesList);
        Devices = new ObservableCollection<Device>(devicesList);
    }

    [RelayCommand]
    private void OpenAddDrawer()
    {
        NewEntityName = "";
        NewEntityIp = "";
        NewEntityMac = "";
        NewSwitchPortCount = 5;
        NewEntityTypeIndex = 0;
        IsDrawerOpen = true;
    }

    [RelayCommand]
    private async Task SaveNewEntityAsync()
    {
        if (string.IsNullOrWhiteSpace(NewEntityName)) return;

        if (NewEntityTypeIndex == 0) // Switch
        {
            var sw = new NetworkSwitch 
            { 
                Name = NewEntityName, 
                PortCount = NewSwitchPortCount,
                LayerId = Guid.Empty // Default
            };
            _dbContext.NetworkSwitches.Add(sw);
            Switches.Add(sw);
        }
        else // Device
        {
            var kind = NewEntityTypeIndex switch
            {
                1 => DeviceKind.PC,
                2 => DeviceKind.StationEsp32,
                3 => DeviceKind.TrainEsp32,
                _ => DeviceKind.PC
            };

            var dev = new Device
            {
                Name = NewEntityName,
                Ip = NewEntityIp,
                Mac = NewEntityMac,
                Kind = kind,
                LayerId = Guid.Empty
            };
            _dbContext.Devices.Add(dev);
            Devices.Add(dev);
        }

        await _dbContext.SaveChangesAsync();
        IsDrawerOpen = false;
    }

    [RelayCommand]
    private async Task DeleteSwitchAsync(NetworkSwitch sw)
    {
        if (sw == null) return;
        _dbContext.NetworkSwitches.Remove(sw);
        Switches.Remove(sw);
        await _dbContext.SaveChangesAsync();
    }

    [RelayCommand]
    private async Task DeleteDeviceAsync(Device dev)
    {
        if (dev == null) return;
        _dbContext.Devices.Remove(dev);
        Devices.Remove(dev);
        await _dbContext.SaveChangesAsync();
    }
}
