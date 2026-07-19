using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TrainService.Cad.FeatureTree;

namespace TrainService.App.Controls.FeatureTree;

/// <summary>
/// Feature Tree kontrolü — sol panelde hiyerarşik ağaç görünümü.
/// Çift tık → ZoomToEntity, seçim → SelectionService senkronizasyonu.
/// </summary>
public partial class FeatureTreeControl : UserControl
{
    private FeatureTreeViewModel? _viewModel;

    public FeatureTreeControl()
    {
        InitializeComponent();
    }

    /// <summary>
    /// ViewModel'i bağlar ve ağaç seçim olaylarını dinler.
    /// </summary>
    public void AttachViewModel(FeatureTreeViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    private void OnTreeViewSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (_viewModel == null) return;

        if (e.NewValue is FeatureTreeItem item && item.EntityId.HasValue)
        {
            _viewModel.OnTreeSelectionChanged(item.EntityId.Value);
        }
        else
        {
            _viewModel.OnTreeSelectionChanged(null);
        }
    }

    private void OnTreeViewDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (FeatureTreeView.SelectedItem is FeatureTreeItem item && item.DoubleClickCommand != null)
        {
            if (item.DoubleClickCommand.CanExecute(item.EntityId))
                item.DoubleClickCommand.Execute(item.EntityId);
        }
    }

    private void OnToggleVisibilityClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is FeatureTreeItem item)
        {
            item.ToggleVisibility();
            e.Handled = true;
        }
    }

    private void OnToggleLockClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is FeatureTreeItem item)
        {
            item.ToggleLock();
            e.Handled = true;
        }
    }
}
