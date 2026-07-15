using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using TrainService.Core.Abstractions;
using TrainService.Core.Enums;

namespace TrainService.App.ViewModels;

public partial class InfoViewModel : ObservableObject, IDisposable
{
    private readonly ILogBus _logBus;

    [ObservableProperty]
    private LogLevel? _selectedFilter;

    public ObservableCollection<LogMessage> Logs { get; } = new();
    
    public Array FilterOptions => new LogLevel?[] { null, LogLevel.Info, LogLevel.Success, LogLevel.Warn, LogLevel.Error };

    public InfoViewModel(ILogBus logBus)
    {
        _logBus = logBus;
        _logBus.OnMessageReceived += OnLogReceived;
        RefreshLogs();
    }

    partial void OnSelectedFilterChanged(LogLevel? value)
    {
        RefreshLogs();
    }

    private void RefreshLogs()
    {
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            Logs.Clear();
            var all = _logBus.GetAllLogs();
            if (SelectedFilter.HasValue)
            {
                all = all.Where(x => x.Level == SelectedFilter.Value).ToList();
            }

            foreach (var l in all)
            {
                Logs.Add(l);
            }
        });
    }

    private void OnLogReceived(LogMessage log)
    {
        if (SelectedFilter.HasValue && log.Level != SelectedFilter.Value)
            return;

        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            Logs.Add(log);
        });
    }

    public void Dispose()
    {
        _logBus.OnMessageReceived -= OnLogReceived;
    }
}
