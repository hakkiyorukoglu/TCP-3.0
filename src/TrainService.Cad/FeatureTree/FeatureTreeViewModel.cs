using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using TrainService.Cad.Selection;
using TrainService.Core.Abstractions;

namespace TrainService.Cad.FeatureTree;

/// <summary>
/// Feature Tree ViewModel — CadDocument'dan ağaç yapısını üretir,
/// SelectionService ile çift yönlü senkronizasyon sağlar.
/// </summary>
public sealed class FeatureTreeViewModel : INotifyPropertyChanged
{
    private readonly CadDocument _document;
    private readonly SelectionService _selectionService;
    private bool _isSyncing;

    public FeatureTreeViewModel(CadDocument document, SelectionService selectionService)
    {
        _document = document;
        _selectionService = selectionService;

        _selectionService.SelectionChanged += OnSelectionChanged;
        _document.Changed += OnDocumentChanged;

        ZoomToEntityCommand = new RelayCommand<Guid?>(ZoomToEntity);
        RebuildTree();
    }

    public ObservableCollection<FeatureTreeItem> Roots { get; } = new();

    public ICommand ZoomToEntityCommand { get; }

    /// <summary>
    /// CadDocument değişikliklerinde ağacı yeniden oluşturur.
    /// </summary>
    public void RebuildTree()
    {
        Roots.Clear();
        foreach (var root in _document.BuildFeatureTree())
        {
            Roots.Add(root);
        }
    }

    /// <summary>
    /// Tuval → Ağaç senkronizasyonu.
    /// </summary>
    private void OnSelectionChanged(object? sender, EventArgs e)
    {
        if (_isSyncing) return;
        _isSyncing = true;

        var selectedIds = _selectionService.SelectedIds;
        UpdateSelectionState(Roots, selectedIds);

        _isSyncing = false;
    }

    private static void UpdateSelectionState(ObservableCollection<FeatureTreeItem> items, IReadOnlySet<Guid> selectedIds)
    {
        foreach (var item in items)
        {
            if (item.EntityId.HasValue)
                item.IsSelected = selectedIds.Contains(item.EntityId.Value);
            if (item.Children.Count > 0)
                UpdateSelectionState(item.Children, selectedIds);
        }
    }

    /// <summary>
    /// Ağaç → Tuval senkronizasyonu (FeatureTreeControl'dan çağrılır).
    /// </summary>
    public void OnTreeSelectionChanged(Guid? entityId)
    {
        if (_isSyncing) return;
        _isSyncing = true;

        if (entityId.HasValue)
            _selectionService.Set(new[] { entityId.Value });
        else
            _selectionService.Clear();

        _isSyncing = false;
    }

    /// <summary>
    /// Çift tık → ZoomToEntity.
    /// </summary>
    private void ZoomToEntity(Guid? entityId)
    {
        if (!entityId.HasValue) return;
        ZoomRequested?.Invoke(this, entityId.Value);
    }

    /// <summary>
    /// Viewport'un ZoomToEntity çağırması için event.
    /// </summary>
    public event EventHandler<Guid>? ZoomRequested;

    private void OnDocumentChanged(object? sender, DocumentChangedEventArgs e)
    {
        RebuildTree();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// Basit ICommand implementasyonu — WPF bağımlılığı yok (CommandManager kullanılmaz).
/// TrainService.Core.Abstraities.ICommand kullanır, System.Windows.Input.ICommand DEĞİL.
/// </summary>
public sealed class RelayCommand<T> : Core.Abstractions.ICommand
{
    private readonly Action<T?> _execute;
    private readonly Func<T?, bool>? _canExecute;

    public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public void NotifyCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

    public bool CanExecute(object? parameter) => _canExecute?.Invoke((T?)parameter) ?? true;

    public void Execute(object? parameter) => _execute((T?)parameter);
}
