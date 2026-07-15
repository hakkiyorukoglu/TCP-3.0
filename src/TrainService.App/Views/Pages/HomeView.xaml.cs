using System.Windows.Controls;
using TrainService.App.ViewModels;

namespace TrainService.App.Views.Pages;

public partial class HomeView : Page
{
    public HomeViewModel ViewModel { get; }

    public HomeView(HomeViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }
}
