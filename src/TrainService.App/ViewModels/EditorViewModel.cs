using System;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TrainService.Core.Abstractions;
using TrainService.Cad;
using TrainService.Cad.UndoRedo;
using TrainService.Cad.Selection;
using TrainService.Cad.Debug;
using TrainService.Cad.Persistence;
using TrainService.App.Controls.Ribbon;

namespace TrainService.App.ViewModels;

public partial class EditorViewModel : ObservableObject
{
    private readonly ILogBus _logBus;
    private readonly Guid _projectId;
    
    private readonly ICadDocumentStore _store;
    private readonly System.Threading.SemaphoreSlim _saveSemaphore = new(1, 1);
    
    [ObservableProperty]
    private CadDocument _document;

    public CommandStack CommandStack { get; }
    public SelectionService SelectionService { get; }
    public TrainService.Cad.Snapping.SnapEngine SnapEngine { get; }
    public TrainService.Cad.Clipboard.ClipboardService ClipboardService { get; }

    [ObservableProperty]
    private string _activeToolName = "";

    [ObservableProperty]
    private string _cursorWorldPosition = "0.0, 0.0 mm";

    [ObservableProperty]
    private bool _isSnapEnabled = true;

    [ObservableProperty]
    private string _snapStatusText = " [GRID]";

    [ObservableProperty]
    private string _documentStatusText = "";

    [ObservableProperty]
    private string _activeLayerStatusText = "Katman: Zemin";

    public Action<string>? ToolChangeRequested;
    public Action? ZoomExtentsRequested;
    public Action? ZoomWindowRequested;
    public Action? ToggleGridRequested;

    public List<RibbonTab> RibbonTabs => RibbonDefinitions.Tabs;
    public List<RibbonItem> RibbonQuickAccess => RibbonDefinitions.QuickAccessItems;

    public EditorViewModel(
        CadDocument document, 
        CommandStack commandStack, 
        SelectionService selectionService,
        TrainService.Cad.Snapping.SnapEngine snapEngine,
        TrainService.Cad.Clipboard.ClipboardService clipboardService,
        ILogBus logBus,
        ICadDocumentStore store,
        Guid? projectId = null)
    {
        _projectId = projectId ?? Guid.NewGuid();
        _document = document;
        CommandStack = commandStack;
        SelectionService = selectionService;
        SnapEngine = snapEngine;
        ClipboardService = clipboardService;
        _logBus = logBus;
        _store = store;

        CommandStack.StackChanged += (s, e) =>
        {
            UndoCommand.NotifyCanExecuteChanged();
            RedoCommand.NotifyCanExecuteChanged();
        };

        Document.Changed += (s, e) =>
        {
            SaveCommand.NotifyCanExecuteChanged();
            UpdateDocumentStatus();
        };
        
        SelectionService.PruneMissing(Document);
        UpdateDocumentStatus();
    }

    public async Task InitializeAsync()
    {
        try
        {
            await _store.LoadDocumentAsync(_projectId, _document);
            SelectionService.PruneMissing(_document);
            UpdateDocumentStatus();
            _logBus.Info("Editor", "Proje veritabanından yüklendi.");
        }
        catch (Exception ex)
        {
            _logBus.Error("Editor", $"Proje yüklenirken hata: {ex.Message}");
        }
    }

    private void UpdateDocumentStatus()
    {
        DocumentStatusText = Document.IsDirty ? "Kayıtlı Değil (*)" : "Kaydedildi";
    }

    private bool CanSave() => Document.IsDirty;

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task Save()
    {
        if (!await _saveSemaphore.WaitAsync(0)) return; // prevent double clicks
        try
        {
            await _store.SaveDocumentAsync(_projectId, Document);
            _logBus.Success("Editor", "Proje veritabanına kaydedildi.");
        }
        catch (Exception ex)
        {
            _logBus.Error("Editor", $"Kayıt hatası: {ex.Message}");
        }
        finally
        {
            _saveSemaphore.Release();
        }
    }

    [RelayCommand]
    private void SetTool(string toolName)
    {
        ActiveToolName = toolName;
        ToolChangeRequested?.Invoke(toolName);
        _logBus.Info("Editor", $"Araç seçildi: {toolName}");
    }

    private bool CanUndo() => CommandStack.CanUndo;
    
    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void Undo()
    {
        var desc = CommandStack.PeekUndoDescription;
        CommandStack.Undo(Document);
        _logBus.Info("Editor", $"Geri alındı: {desc}");
    }

    private bool CanRedo() => CommandStack.CanRedo;
    
    [RelayCommand(CanExecute = nameof(CanRedo))]
    private void Redo()
    {
        var desc = CommandStack.PeekRedoDescription;
        CommandStack.Redo(Document);
        _logBus.Success("Editor", $"Tekrar yapıldı: {desc}");
    }

    [RelayCommand]
    private void DebugAddLine()
    {
        var cmd = new DebugAddLineCommand(Document.ActiveLayerId);
        CommandStack.Do(cmd, Document);
        _logBus.Success("Editor", $"Komut: {cmd.Description}");
    }

    [RelayCommand]
    private void ToggleSnap()
    {
        IsSnapEnabled = !IsSnapEnabled;
        if (SnapEngine != null)
        {
            SnapEngine.IsEnabled = IsSnapEnabled;
        }
        SnapStatusText = IsSnapEnabled ? " [GRID]" : " [OFF]";
    }

    [RelayCommand]
    private void Delete()
    {
        var selectedIds = SelectionService.SelectedIds.ToList();
        if (selectedIds.Count == 0) return;

        var cmd = new DeleteEntitiesCommand(selectedIds);
        CommandStack.Do(cmd, Document);
        _logBus.Success("Editor", $"Silindi: {selectedIds.Count} nesne");
    }

    [RelayCommand]
    private void Copy()
    {
        var selected = Document.Entities
            .Where(e => SelectionService.SelectedIds.Contains(e.Id))
            .ToList();

        if (selected.Count == 0) return;

        ClipboardService.Set(selected);
        _logBus.Info("Editor", $"Kopyalandı: {selected.Count} nesne");
    }

    [RelayCommand]
    private void Cut()
    {
        var selected = Document.Entities
            .Where(e => SelectionService.SelectedIds.Contains(e.Id))
            .ToList();

        if (selected.Count == 0) return;

        ClipboardService.Set(selected);

        var ids = selected.Select(e => e.Id).ToList();
        var cmd = new DeleteEntitiesCommand(ids);
        CommandStack.Do(cmd, Document);
        _logBus.Success("Editor", $"Kesildi: {selected.Count} nesne");
    }

    [RelayCommand]
    private void Paste()
    {
        if (!ClipboardService.HasContent) return;

        var clones = ClipboardService.Get();
        var cmd = new PasteEntitiesCommand(clones);
        CommandStack.Do(cmd, Document);

        _logBus.Success("Editor", $"Yapıştırıldı: {clones.Count} nesne");
    }

    [RelayCommand]
    private void ZoomExtents()
    {
        ZoomExtentsRequested?.Invoke();
        _logBus.Info("Editor", "Zoom Extents");
    }

    [RelayCommand]
    private void ZoomWindow()
    {
        ZoomWindowRequested?.Invoke();
        _logBus.Info("Editor", "Zoom Window");
    }

    [RelayCommand]
    private void ToggleGrid()
    {
        ToggleGridRequested?.Invoke();
        _logBus.Info("Editor", "Izgara değiştirildi");
    }
}