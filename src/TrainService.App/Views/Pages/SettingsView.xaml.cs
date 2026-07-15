using System.Windows.Controls;
using TrainService.App.ViewModels;

namespace TrainService.App.Views.Pages;

public partial class SettingsView : Page
{
    public SettingsViewModel ViewModel { get; }

    public SettingsView(SettingsViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }
}
