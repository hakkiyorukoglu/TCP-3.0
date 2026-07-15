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


        // Fare hareket ettikçe Viewport'tan dünya koordinatını alıp doğrudan Label'a yazalım
        Viewport.MouseMove += (s, e) =>
        {
            var currentPos = e.GetPosition(Viewport);
            var worldPos = Viewport.Transform.ScreenToWorld(currentPos);
            LblCoordinates.Text = $"X: {worldPos.X:F1} Y: {worldPos.Y:F1} mm";
        };
        
        // FPS güncellendiğinde doğrudan Label'a yazalım
        Viewport.FpsUpdated += (fps) =>
        {
            Dispatcher.Invoke(() => 
            {
                LblFps.Text = $"FPS: {fps}";
                // FPS'e göre renk değiştir (Opsiyonel görselleştirme)
                LblFps.Foreground = fps >= 55 ? System.Windows.Media.Brushes.LimeGreen : 
                                    fps >= 30 ? System.Windows.Media.Brushes.Orange : 
                                                System.Windows.Media.Brushes.Red;
            });
        };

        this.Loaded += (s, e) => 
        {
            Viewport.AttachDocument(ViewModel.Document);
        };
    }
}
