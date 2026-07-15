using System.Windows;
using System.Windows.Controls;
using TrainService.App.ViewModels;

namespace TrainService.App.Views.Pages;

public partial class EditorView : Page
{
    public EditorViewModel ViewModel { get; }

    public EditorView(EditorViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();

        ViewModel.OnMapLoaded += (nodes, segments) =>
        {
            Application.Current.Dispatcher.Invoke(() => 
            {
                Viewer.RenderMap(nodes, segments);
            });
        };

        this.Loaded += async (s, e) => 
        {
            await ViewModel.LoadSampleMapAsync();
        };
    }
}
