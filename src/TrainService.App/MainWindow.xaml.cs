using Wpf.Ui.Controls;
using Wpf.Ui;
using TrainService.App.ViewModels;

namespace TrainService.App;

public partial class MainWindow : FluentWindow
{
    public MainWindowViewModel ViewModel { get; }

    public MainWindow(MainWindowViewModel viewModel, INavigationService navigationService, IPageService pageService)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();

        navigationService.SetPageService(pageService);
        navigationService.SetNavigationControl(RootNavigation);
    }
}