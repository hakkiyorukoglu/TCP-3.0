using CommunityToolkit.Mvvm.ComponentModel;

namespace TrainService.App.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _applicationTitle = "TCP 3.0 - Train Control Platform";

    public TerminalPanelViewModel TerminalViewModel { get; }

    public MainWindowViewModel(TerminalPanelViewModel terminalViewModel)
    {
        TerminalViewModel = terminalViewModel;
    }
}
