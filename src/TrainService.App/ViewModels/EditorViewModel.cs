using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TrainService.Core.Abstractions;
using TrainService.Cad;
using TrainService.Cad.UndoRedo;
using TrainService.Cad.Selection;
using TrainService.Cad.Debug;
using TrainService.Cad.Persistence;

namespace TrainService.App.ViewModels;

public partial class EditorViewModel : ObservableObject
{
    private readonly ILogBus _logBus;
    
    private readonly ICadDocumentStore _store;
    private readonly System.Threading.SemaphoreSlim _saveSemaphore = new(1, 1);
    
    [ObservableProperty]
    private CadDocument _document;

    public CommandStack CommandStack { get; }
    public SelectionService SelectionService { get; }
    public TrainService.Cad.Snapping.SnapEngine SnapEngine { get; }

    [ObservableProperty]
    private string _cursorWorldPosition = "0.0, 0.0 mm";

    [ObservableProperty]
    private bool _isSnapEnabled = true;

    [ObservableProperty]
    private string _snapStatusText = " [GRID]";

    [ObservableProperty]
    private string _documentStatusText = "";

    public Action<string>? ToolChangeRequested;

    public EditorViewModel(
        CadDocument document, 
        CommandStack commandStack, 
        SelectionService selectionService,
        TrainService.Cad.Snapping.SnapEngine snapEngine,
        ILogBus logBus,
        ICadDocumentStore store)
    {
        _document = document;
        CommandStack = commandStack;
        SelectionService = selectionService;
        SnapEngine = snapEngine;
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
            await _store.LoadDocumentAsync(Guid.Empty, _document);
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
            await _store.SaveDocumentAsync(Guid.Empty, Document);
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
}
