using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TrainService.App.Models;
using TrainService.App.ViewModels;

namespace TrainService.App.Controls.DocumentTabs;

/// <summary>
/// Sekme şeridi kontrolü. + butonu, sekme başlıkları, kapatma butonu içerir.
/// </summary>
public partial class DocumentTabsControl : UserControl
{
    public DocumentTabsControl()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Sekme başlığına tıklama — ActiveTab değişimi.
    /// </summary>
    private void OnTabClicked(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.DataContext is EditorTabModel tab)
        {
            if (DataContext is DocumentTabsViewModel vm)
            {
                vm.ActiveTab = tab;
                e.Handled = true;
            }
        }
    }
}
