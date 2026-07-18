using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using WpfUi = Wpf.Ui.Controls;

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

    public RibbonControl()
    {
        InitializeComponent();
    }

    private static void OnTabsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RibbonControl ctrl) ctrl.RebuildTabs();
    }

    private static void OnQuickAccessChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RibbonControl ctrl) ctrl.RebuildQuickAccess();
    }

    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RibbonControl ctrl) ctrl._vmCache = e.NewValue;
    }

    private void RebuildQuickAccess()
    {
        QuickAccessPanel.Children.Clear();
        if (QuickAccessItems == null) return;

        foreach (var item in QuickAccessItems)
        {
            var btn = CreateRibbonButton(item, isQuickAccess: true);
            QuickAccessPanel.Children.Add(btn);
        }
    }

    private void RebuildTabs()
    {
        TabHeaderPanel.Children.Clear();
        if (Tabs == null) return;

        foreach (var tab in Tabs)
        {
            var isSelected = _selectedTabId == tab.Id;
            var btn = new ToggleButton
            {
                Content = tab.Label,
                IsChecked = isSelected,
                Margin = new Thickness(2, 2, 0, 2),
                Padding = new Thickness(12, 2, 12, 2),
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC)),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
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

        // Update all toggle states
        for (int i = 0; i < TabHeaderPanel.Children.Count; i++)
        {
            if (TabHeaderPanel.Children[i] is ToggleButton tb && Tabs != null && i < Tabs.Count)
            {
                tb.IsChecked = Tabs[i].Id == tabId;
            }
        }

        RebuildTabContent();
    }

    private void RebuildTabContent()
    {
        TabContentPanel.Children.Clear();

        var selectedTab = Tabs?.FirstOrDefault(t => t.Id == _selectedTabId)
                          ?? Tabs?.FirstOrDefault();
        if (selectedTab == null) return;

        if (string.IsNullOrEmpty(_selectedTabId) && selectedTab != null)
        {
            _selectedTabId = selectedTab.Id;
        }

        foreach (var group in selectedTab.Groups)
        {
            var groupBorder = new Border
            {
                Margin = new Thickness(4, 0, 8, 0),
                Padding = new Thickness(4, 2, 4, 2),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x44)),
                BorderThickness = new Thickness(0, 0, 1, 0),
            };

            var stack = new StackPanel { Orientation = Orientation.Horizontal };

            foreach (var item in group.Items)
            {
                var itemPanel = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    Margin = new Thickness(4, 0, 4, 0),
                    Width = 48,
                };

                var btn = CreateRibbonButton(item, isQuickAccess: false);
                itemPanel.Children.Add(btn);

                if (!string.IsNullOrEmpty(item.Label))
                {
                    itemPanel.Children.Add(new TextBlock
                    {
                        Text = item.Label,
                        FontSize = 9,
                        Foreground = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA)),
                        TextAlignment = TextAlignment.Center,
                        TextWrapping = TextWrapping.NoWrap,
                    });
                }

                stack.Children.Add(itemPanel);
            }

            groupBorder.Child = stack;
            TabContentPanel.Children.Add(groupBorder);
        }
    }

    private WpfUi::Button CreateRibbonButton(RibbonItem item, bool isQuickAccess)
    {
        var btn = new WpfUi::Button
        {
            ToolTip = BuildToolTip(item),
            Icon = CreateIcon(item.IconSymbol),
            Appearance = isQuickAccess ? WpfUi::ControlAppearance.Secondary : WpfUi::ControlAppearance.Primary,
            IsEnabled = item.IsEnabled,
            Width = isQuickAccess ? 28 : 36,
            Height = isQuickAccess ? 24 : 32,
            Padding = new Thickness(isQuickAccess ? 2 : 1),
            FontSize = isQuickAccess ? 12 : 16,
            Cursor = Cursors.Hand,
            Margin = new Thickness(isQuickAccess ? 2 : 0, 0, isQuickAccess ? 2 : 0, 2),
        };

        if (!string.IsNullOrEmpty(item.CommandName))
        {
            btn.Command = ResolveCommand(item.CommandName);
            if (!string.IsNullOrEmpty(item.CommandParameter))
            {
                btn.CommandParameter = item.CommandParameter;
            }
        }

        return btn;
    }

    private static WpfUi::IconElement CreateIcon(string symbolName)
    {
        if (string.IsNullOrEmpty(symbolName))
            return new WpfUi::SymbolIcon();

        if (Enum.TryParse<WpfUi::SymbolRegular>(symbolName, out var symbol))
        {
            return new WpfUi::SymbolIcon { Symbol = symbol };
        }

        return new WpfUi::SymbolIcon();
    }

    private static string BuildToolTip(RibbonItem item)
    {
        var tip = item.Label;
        if (!string.IsNullOrEmpty(item.ShortcutText))
            tip += $" ({item.ShortcutText})";
        return tip;
    }

    private ICommand? ResolveCommand(string commandName)
    {
        if (_vmCache == null) return null;

        // CommunityToolkit.Mvvm generates properties ending with "Command"
        // e.g., method "SetTool" → property "SetToolCommand"
        var propName = commandName.EndsWith("Command") ? commandName : commandName + "Command";
        var prop = _vmCache.GetType().GetProperty(propName,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        if (prop?.GetValue(_vmCache) is ICommand cmd)
            return cmd;

        return null;
    }
}
