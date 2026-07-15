using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using TrainService.Core.Abstractions;

namespace TrainService.App.ViewModels;

public partial class TerminalPanelViewModel : ObservableObject, IDisposable
{
    private readonly ILogBus _logBus;

    public ObservableCollection<LogMessage> RecentLogs { get; } = new();

    public TerminalPanelViewModel(ILogBus logBus)
    {
        _logBus = logBus;
        _logBus.OnMessageReceived += OnLogReceived;
    }

    private void OnLogReceived(LogMessage log)
    {
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            RecentLogs.Add(log);
            if (RecentLogs.Count > 4)
            {
                RecentLogs.RemoveAt(0);
            }
        });
    }

    public void Dispose()
    {
        _logBus.OnMessageReceived -= OnLogReceived;
    }
}
