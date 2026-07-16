using CommunityToolkit.Mvvm.ComponentModel;

namespace TrainService.App.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _applicationTitle = "TCP 3.0 - Train Control Platform";

    public TerminalPanelViewModel TerminalViewModel { get; }

    public MainWindowViewModel(TerminalPanelViewModel terminalViewModel, TrainService.Cad.CadDocument document)
    {
        TerminalViewModel = terminalViewModel;
        
        document.Changed += (s, e) =>
        {
            ApplicationTitle = document.IsDirty 
                ? "TCP 3.0 - Train Control Platform *" 
                : "TCP 3.0 - Train Control Platform";
        };
    }
}
