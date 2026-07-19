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
    public DocumentTabsViewModel TabsViewModel { get; }

    public EditorView(DocumentTabsViewModel tabsVm, EditorViewModel viewModel)
    {
        TabsViewModel = tabsVm;
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();

        // ActiveTab değişiminde yeniden bağla
        tabsVm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(DocumentTabsViewModel.ActiveTab))
                ReattachActiveTab();
        };

        this.PreviewKeyDown += (s, e) =>
        {
            if (Keyboard.FocusedElement is TextBoxBase) return;

            // Tool shortcuts (no modifiers)
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
            else if (e.Key == Key.H && Keyboard.Modifiers == ModifierKeys.None)
            {
                if (ViewModel.SetToolCommand.CanExecute("Hybrid"))
                    ViewModel.SetToolCommand.Execute("Hybrid");
                e.Handled = true;
            }
            else if (e.Key == Key.F8 && Keyboard.Modifiers == ModifierKeys.None)
            {
                if (ViewModel.SetToolCommand.CanExecute("Switch"))
                    ViewModel.SetToolCommand.Execute("Switch");
                e.Handled = true;
            }
            else if (e.Key == Key.Delete && Keyboard.Modifiers == ModifierKeys.None)
            {
                if (ViewModel.DeleteCommand.CanExecute(null))
                    ViewModel.DeleteCommand.Execute(null);
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
            // İlk sekme oluştur ve bağla
            if (TabsViewModel.Tabs.Count == 0)
                TabsViewModel.AddTabCommand.Execute(null);

            ReattachActiveTab();

            _ = ViewModel.InitializeAsync();

            // Ribbon → tool mapping
            ViewModel.ToolChangeRequested += (toolName) =>
            {
                if (Viewport.ToolController == null) return;
                if (toolName == "Select")
                    Viewport.ToolController.SetTool(new TrainService.Cad.Tools.SelectTool());
                else if (toolName == "Track")
                    Viewport.ToolController.SetTool(new TrainService.Cad.Tools.TrackTool());
                else if (toolName == "Route")
                    Viewport.ToolController.SetTool(new TrainService.Cad.Tools.RouteTool());
                else if (toolName == "Switch")
                    Viewport.ToolController.SetTool(new TrainService.Cad.Tools.SwitchTool());
                else if (toolName == "Hybrid")
                    Viewport.ToolController.SetTool(new TrainService.Cad.Tools.HybridTool());
                else if (toolName == "Ramp")
                    Viewport.ToolController.SetTool(new TrainService.Cad.Tools.RampTool());
            };

            // Zoom events
            ViewModel.ZoomExtentsRequested += () =>
            {
                Dispatcher.Invoke(() => Viewport.ZoomExtents());
            };

            ViewModel.ZoomWindowRequested += () =>
            {
                Dispatcher.Invoke(() => Viewport.ZoomWindow());
            };

            ViewModel.ToggleGridRequested += () =>
            {
                Dispatcher.Invoke(() => Viewport.ToggleGrid());
            };
        };
    }

    /// <summary>
    /// Aktif sekmenin dokümanını Viewport, FeatureTree ve ToolController'a bağlar.
    /// </summary>
    private void ReattachActiveTab()
    {
        var tab = TabsViewModel.ActiveTab;
        if (tab == null) return;

        // 1. Önceki event handler'ları temizle (memory leak önleme)
        if (Viewport.ToolController != null)
            Viewport.ToolController.LayerStatusChanged -= OnLayerStatusChanged;

        // 2. Viewport yeniden bağla
        Viewport.AttachDocument(tab.Document);
        Viewport.AttachSelection(tab.SelectionService);

        // 3. ToolController yeniden oluştur
        var currentTool = new TrainService.Cad.Tools.SelectTool();
        var ctx = new TrainService.Cad.Tools.ToolContext(
            tab.Document, tab.CommandStack, tab.SelectionService)
        {
            Clipboard = tab.ClipboardService
        };
        Viewport.ToolController = new TrainService.App.Controls.CadCanvas.ToolController(
            ctx, tab.SnapEngine, Viewport.Transform, currentTool)
        {
            Clipboard = tab.ClipboardService
        };
        Viewport.CommandStack = tab.CommandStack;

        Viewport.ToolController.LayerStatusChanged += OnLayerStatusChanged;

        // 4. FeatureTree yeniden bağla
        var featureTreeVm = new FeatureTreeViewModel(tab.Document, tab.SelectionService);
        FeatureTreeCtrl.AttachViewModel(featureTreeVm);

        featureTreeVm.ZoomRequested += (_, entityId) =>
        {
            Viewport.ZoomToEntity(entityId, tab.Document);
        };

        // 5. Ribbon proxy — aktif sekme EditorViewModel'e bildirilir
        ViewModel.ActiveTab = tab;
    }

    private void OnLayerStatusChanged(string msg)
    {
        Dispatcher.Invoke(() => ViewModel.ActiveLayerStatusText = msg);
    }
}
