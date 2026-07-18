using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TrainService.Core.Abstractions;

namespace TrainService.Cad.FeatureTree;

/// <summary>
/// Feature Tree'deki her bir düğümü temsil eder.
/// Grup başlıkları (EntityId=null) veya entity referansları (EntityId=Guid) olabilir.
/// </summary>
public sealed class FeatureTreeItem : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private string _icon = string.Empty;
    private bool _isVisible = true;
    private bool _isLocked;
    private bool _isSelected;
    private bool _isExpanded = true;

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string Icon
    {
        get => _icon;
        set => SetProperty(ref _icon, value);
    }

    public Guid? EntityId { get; set; }

    public string EntityType { get; set; } = "Group";

    public ObservableCollection<FeatureTreeItem> Children { get; } = new();

    public FeatureTreeItem? Parent { get; set; }

    public bool IsVisible
    {
        get => _isVisible;
        set => SetProperty(ref _isVisible, value);
    }

    public bool IsLocked
    {
        get => _isLocked;
        set => SetProperty(ref _isLocked, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    public ICommand? DoubleClickCommand { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
