using Wpf.Ui.Controls;
using Wpf.Ui;
using TrainService.App.ViewModels;

namespace TrainService.App;

public partial class MainWindow : FluentWindow
{
    public MainWindowViewModel ViewModel { get; }

    public MainWindow(MainWindowViewModel viewModel, INavigationService navigationService, IPageService pageService, TrainService.Cad.CadDocument document)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();

        navigationService.SetPageService(pageService);
        navigationService.SetNavigationControl(RootNavigation);

        Closing += (s, e) =>
        {
            if (document.IsDirty)
            {
                var result = System.Windows.MessageBox.Show(
                    "Kaydedilmemiş değişiklikler var. Çıkmak istediğinize emin misiniz?", 
                    "Uyarı", 
                    System.Windows.MessageBoxButton.YesNo, 
                    System.Windows.MessageBoxImage.Warning);
                
                if (result == System.Windows.MessageBoxResult.No)
                {
                    e.Cancel = true;
                }
            }
        };
    }
}