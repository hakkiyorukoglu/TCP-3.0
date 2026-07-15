using System;
using CommunityToolkit.Mvvm.ComponentModel;
using TrainService.Core.Entities;

namespace TrainService.App.Models;

public abstract partial class BaseNodeItem : ObservableObject
{
    [ObservableProperty]
    private double _x;

    [ObservableProperty]
    private double _y;
    
    public string Name { get; set; } = string.Empty;
}

public partial class SwitchNodeItem : BaseNodeItem
{
    public NetworkSwitch SwitchEntity { get; }
    
    public SwitchNodeItem(NetworkSwitch sw)
    {
        SwitchEntity = sw;
        Name = sw.Name;
    }
}

public partial class DeviceNodeItem : BaseNodeItem
{
    public Device DeviceEntity { get; }
    public string IpAddress => DeviceEntity.Ip;
    
    [ObservableProperty] private string _healthColor = "Gray"; // Gray, Green, Yellow, Red
    [ObservableProperty] private string _lastSeenText = "Son Görülme: Bilinmiyor";
    
    public DeviceNodeItem(Device device)
    {
        DeviceEntity = device;
        Name = device.Name;
    }
}

public partial class ConnectionLineItem : ObservableObject
{
    [ObservableProperty] private double _x1;
    [ObservableProperty] private double _y1;
    [ObservableProperty] private double _x2;
    [ObservableProperty] private double _y2;
}
