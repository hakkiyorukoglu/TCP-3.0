using System.Windows.Controls;
using TrainService.App.ViewModels;

namespace TrainService.App.Views.Pages;

public partial class InfoView : Page
{
    public InfoViewModel ViewModel { get; }

    public InfoView(InfoViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }
}
