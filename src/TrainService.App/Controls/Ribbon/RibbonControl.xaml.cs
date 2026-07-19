using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using TrainService.App.ViewModels;
using WpfUi = Wpf.Ui.Controls;
using IconPacks = MahApps.Metro.IconPacks;

namespace TrainService.App.Controls.Ribbon;

public partial class RibbonControl : UserControl
{
    public static readonly DependencyProperty TabsProperty =
        DependencyProperty.Register(nameof(Tabs), typeof(List<RibbonTab>), typeof(RibbonControl),
            new PropertyMetadata(null, OnTabsChanged));

    public static readonly DependencyProperty QuickAccessItemsProperty =
        DependencyProperty.Register(nameof(QuickAccessItems), typeof(List<RibbonItem>), typeof(RibbonControl),
            new PropertyMetadata(null, OnQuickAccessChanged));

    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(nameof(ViewModel), typeof(object), typeof(RibbonControl),
            new PropertyMetadata(null, OnViewModelChanged));

    public List<RibbonTab> Tabs
    {
        get => (List<RibbonTab>)GetValue(TabsProperty);
        set => SetValue(TabsProperty, value);
    }
    public List<RibbonItem> QuickAccessItems
    {
        get => (List<RibbonItem>)GetValue(QuickAccessItemsProperty);
        set => SetValue(QuickAccessItemsProperty, value);
    }
    public object ViewModel
    {
        get => GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    private string _selectedTabId = "";
    private object? _vmCache;

    public RibbonControl() => InitializeComponent();

    private static void OnTabsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    { if (d is RibbonControl ctrl && ctrl._vmCache != null) ctrl.RebuildTabs(); }

    private static void OnQuickAccessChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    { if (d is RibbonControl ctrl && ctrl._vmCache != null) ctrl.RebuildQuickAccess(); }

    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RibbonControl ctrl)
        {
            ctrl._vmCache = e.NewValue;
            if (ctrl.QuickAccessItems != null) ctrl.RebuildQuickAccess();
            if (ctrl.Tabs != null) ctrl.RebuildTabs();
        }
    }

    private void RebuildQuickAccess()
    {
        QuickAccessPanel.Children.Clear();
        if (QuickAccessItems == null) return;
        foreach (var item in QuickAccessItems)
        {
            if (item.Id == "Save" && _vmCache is EditorViewModel vm)
            {
                var saveBtn = CreateRibbonButton(item, isQuickAccess: true);
                vm.PropertyChanged += (s, e) =>
                { if (e.PropertyName == nameof(EditorViewModel.DocumentStatusText)) saveBtn.Appearance = vm.Document.IsDirty ? WpfUi::ControlAppearance.Caution : WpfUi::ControlAppearance.Secondary; };
                QuickAccessPanel.Children.Add(saveBtn);
                continue;
            }
            QuickAccessPanel.Children.Add(CreateRibbonButton(item, isQuickAccess: true));
        }
    }

    private void RebuildTabs()
    {
        TabHeaderPanel.Children.Clear();
        if (Tabs == null) return;
        foreach (var tab in Tabs)
        {
            var btn = new ToggleButton
            {
                Content = tab.Label, IsChecked = _selectedTabId == tab.Id,
                Margin = new Thickness(2, 2, 0, 2), Padding = new Thickness(12, 2, 12, 2),
                FontSize = 12, FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC)),
                Background = Brushes.Transparent, BorderThickness = new Thickness(0), Cursor = Cursors.Hand,
            };
            btn.Click += (s, e) => SelectTab(tab.Id);
            TabHeaderPanel.Children.Add(btn);
        }
        RebuildTabContent();
    }

    private void SelectTab(string tabId)
    {
        if (_selectedTabId == tabId) return;
        _selectedTabId = tabId;
        for (int i = 0; i < TabHeaderPanel.Children.Count; i++)
            if (TabHeaderPanel.Children[i] is ToggleButton tb && Tabs != null && i < Tabs.Count)
                tb.IsChecked = Tabs[i].Id == tabId;
        RebuildTabContent();
    }

    private void RebuildTabContent()
    {
        TabContentPanel.Children.Clear();
        var selectedTab = Tabs?.FirstOrDefault(t => t.Id == _selectedTabId) ?? Tabs?.FirstOrDefault();
        if (selectedTab == null) return;
        if (string.IsNullOrEmpty(_selectedTabId)) _selectedTabId = selectedTab.Id;

        foreach (var group in selectedTab.Groups)
        {
            var gb = new Border { Margin = new Thickness(4, 0, 8, 0), Padding = new Thickness(4, 2, 4, 2), BorderBrush = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x44)), BorderThickness = new Thickness(0, 0, 1, 0) };
            var stack = new StackPanel { Orientation = Orientation.Horizontal };

            foreach (var item in group.Items)
            {
                if (item.Id == "LayerSelector" && _vmCache is EditorViewModel vm)
                {
                    var cb = new ComboBox { Width = 120, Margin = new Thickness(4), ItemsSource = vm.Document.Layers, DisplayMemberPath = "Name", SelectedValuePath = "Id", SelectedValue = vm.ActiveLayerId, Background = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)), Foreground = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC)), BorderBrush = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55)) };
                    cb.SelectionChanged += (s, e) => { if (cb.SelectedValue is Guid id) vm.ActiveLayerId = id; };
                    stack.Children.Add(cb); continue;
                }

                var ip = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(4, 0, 4, 0), Width = 48 };
                ip.Children.Add(CreateRibbonButton(item, false));
                if (!string.IsNullOrEmpty(item.Label))
                    ip.Children.Add(new TextBlock { Text = item.Label, FontSize = 9, Foreground = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA)), TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.NoWrap });
                stack.Children.Add(ip);
            }
            gb.Child = stack;
            TabContentPanel.Children.Add(gb);
        }
    }

    private WpfUi::Button CreateRibbonButton(RibbonItem item, bool isQuickAccess)
    {
        var btn = new WpfUi::Button
        {
            ToolTip = BuildToolTip(item),
            Appearance = isQuickAccess ? WpfUi::ControlAppearance.Secondary : WpfUi::ControlAppearance.Primary,
            IsEnabled = item.IsEnabled,
            Width = isQuickAccess ? 28 : 36, Height = isQuickAccess ? 24 : 32,
            Padding = new Thickness(isQuickAccess ? 2 : 1), FontSize = isQuickAccess ? 12 : 16,
            Cursor = Cursors.Hand, Margin = new Thickness(isQuickAccess ? 2 : 0, 0, isQuickAccess ? 2 : 0, 2),
        };

        // İkon oluşturmayı dene, başarısız olursa label kullan
        if (!string.IsNullOrEmpty(item.IconKind))
        {
            var icon = CreateIconPacks(item.IconKind, item.IconPack);
            if (icon != null)
            {
                icon.Width = isQuickAccess ? 14 : 18;
                icon.Height = isQuickAccess ? 14 : 18;
                icon.Foreground = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC));
                btn.Content = icon;
            }
        }

        // İkon YOKSA label göster (buton boş kalmasın)
        if (btn.Content == null && !string.IsNullOrEmpty(item.Label))
            btn.Content = item.Label;

        if (!string.IsNullOrEmpty(item.CommandName))
        {
            btn.Command = ResolveCommand(item.CommandName);
            if (!string.IsNullOrEmpty(item.CommandParameter))
                btn.CommandParameter = item.CommandParameter;
        }

        return btn;
    }

    private static Control? CreateIconPacks(string kind, string pack)
    {
        if (string.IsNullOrEmpty(kind)) return null;
        try
        {
            return new IconPacks.PackIconMaterialDesign { Kind = Enum.Parse<IconPacks.PackIconMaterialDesignKind>(kind) };
        }
        catch { return null; }
    }

    private static string BuildToolTip(RibbonItem item)
    {
        var tip = item.Label;
        if (!string.IsNullOrEmpty(item.ShortcutText)) tip += $" ({item.ShortcutText})";
        return tip;
    }

    private ICommand? ResolveCommand(string commandName)
    {
        if (_vmCache == null) return null;
        var propName = commandName.EndsWith("Command") ? commandName : commandName + "Command";
        var prop = _vmCache.GetType().GetProperty(propName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        return prop?.GetValue(_vmCache) as ICommand;
    }
}