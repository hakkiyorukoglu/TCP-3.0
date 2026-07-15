using System.Windows.Controls;
using TrainService.App.ViewModels;

namespace TrainService.App.Views.Pages;

public partial class KitchenView : Page
{
    public KitchenViewModel ViewModel { get; }

    public KitchenView(KitchenViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }
}
