using System.Windows.Controls;
using TrainService.App.ViewModels;

namespace TrainService.App.Views.Pages;

public partial class ElectronicsView : Page
{
    public ElectronicsViewModel ViewModel { get; }

    public ElectronicsView(ElectronicsViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }
}
