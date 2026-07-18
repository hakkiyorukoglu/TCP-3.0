using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Controls.Primitives;
using TrainService.App.ViewModels;
using TrainService.Cad.FeatureTree;

namespace TrainService.App.Views.Pages;

public partial class EditorView : Page
{
    public EditorViewModel ViewModel { get; }

    public EditorView(EditorViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();

        this.PreviewKeyDown += (s, e) =>
        {
            if (Keyboard.FocusedElement is TextBoxBase) return;

            if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.None)
            {
                if (ViewModel.SetToolCommand.CanExecute("Select"))
                    ViewModel.SetToolCommand.Execute("Select");
                e.Handled = true;
            }
            else if (e.Key == Key.T && Keyboard.Modifiers == ModifierKeys.None)
            {
                if (ViewModel.SetToolCommand.CanExecute("Track"))
                    ViewModel.SetToolCommand.Execute("Track");
                e.Handled = true;
            }
            else if (e.Key == Key.R && Keyboard.Modifiers == ModifierKeys.None)
            {
                if (ViewModel.SetToolCommand.CanExecute("Route"))
                    ViewModel.SetToolCommand.Execute("Route");
                e.Handled = true;
            }
            else if (e.Key == Key.F8 && Keyboard.Modifiers == ModifierKeys.None)
            {
                if (ViewModel.SetToolCommand.CanExecute("Switch"))
                    ViewModel.SetToolCommand.Execute("Switch");
                e.Handled = true;
            }
            else if (Viewport.ToolController != null)
            {
                if (Viewport.ToolController.KeyDown(e.Key))
                    e.Handled = true;
            }
        };

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
            _ = ViewModel.InitializeAsync();
            Viewport.AttachDocument(ViewModel.Document);
            Viewport.AttachSelection(ViewModel.SelectionService);

            // Feature Tree ViewModel oluştur ve bağla
            var featureTreeVm = new FeatureTreeViewModel(ViewModel.Document, ViewModel.SelectionService);
            FeatureTreeCtrl.AttachViewModel(featureTreeVm);

            // ZoomToEntity olayını Viewport'a bağla
            featureTreeVm.ZoomRequested += (_, entityId) =>
            {
                Viewport.ZoomToEntity(entityId, ViewModel.Document);
            };

            var ctx = new TrainService.Cad.Tools.ToolContext(ViewModel.Document, ViewModel.CommandStack, ViewModel.SelectionService) { Clipboard = ViewModel.ClipboardService };
            var initialTool = new TrainService.Cad.Tools.SelectTool();
            Viewport.ToolController = new TrainService.App.Controls.CadCanvas.ToolController(ctx, ViewModel.SnapEngine, Viewport.Transform, initialTool) { Clipboard = ViewModel.ClipboardService };
            Viewport.CommandStack = ViewModel.CommandStack;

            Viewport.ToolController.LayerStatusChanged += (msg) =>
            {
                Dispatcher.Invoke(() => ViewModel.ActiveLayerStatusText = msg);
            };

            ViewModel.ToolChangeRequested += (toolName) =>
            {
                if (toolName == "Select")
                    Viewport.ToolController.SetTool(new TrainService.Cad.Tools.SelectTool());
                else if (toolName == "Track")
                    Viewport.ToolController.SetTool(new TrainService.Cad.Tools.TrackTool());
                else if (toolName == "Route")
                    Viewport.ToolController.SetTool(new TrainService.Cad.Tools.RouteTool());
                else if (toolName == "Switch")
                    Viewport.ToolController.SetTool(new TrainService.Cad.Tools.SwitchTool());
            };
        };
    }
}
