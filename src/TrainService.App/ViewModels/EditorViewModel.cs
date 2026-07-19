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
using TrainService.Cad.Snapping;
using TrainService.App.Controls.Ribbon;
using TrainService.App.Models;

namespace TrainService.App.ViewModels;

public partial class EditorViewModel : ObservableObject
{
    private readonly ILogBus _logBus;
    private readonly Guid _projectId;
    private readonly ICadDocumentStore _store;
    private readonly System.Threading.SemaphoreSlim _saveSemaphore = new(1, 1);

    [ObservableProperty] private CadDocument _document;
    public CommandStack CommandStack { get; }
    public SelectionService SelectionService { get; }
    public SnapEngine SnapEngine { get; }
    public TrainService.Cad.Clipboard.ClipboardService ClipboardService { get; }

    [ObservableProperty] private string _activeToolName = "";
    [ObservableProperty] private string _activeSelectionMode = "Crossing";
    [ObservableProperty] private string _cursorWorldPosition = "0.0, 0.0 mm";
    [ObservableProperty] private bool _isSnapEnabled = true;
    [ObservableProperty] private string _snapStatusText = " [GRID]";
    [ObservableProperty] private string _snapStatusColor = "#FFFF00";
    [ObservableProperty] private string _documentStatusText = "";
    [ObservableProperty] private string _activeLayerStatusText = "Katman: Zemin";
    [ObservableProperty] private double _zoomScale = 1.0;
    [ObservableProperty] private EditorTabModel? _activeTab;
    [ObservableProperty] private Guid _activeLayerId;

    public string ZoomPercentText => $"{ZoomScale * 100:F0}%";

    partial void OnActiveTabChanged(EditorTabModel? value)
    {
        if (value == null) return;
        Document = value.Document;
        ActiveLayerId = value.Document.ActiveLayerId;
    }
    partial void OnActiveLayerIdChanged(Guid value) => ActiveTab?.Document.SetActiveLayer(value);

    public string ActiveLayerName => ActiveTab?.Document.Layers.FirstOrDefault(l => l.Id == ActiveLayerId)?.Name ?? "Zemin";
    public TerminalPanelViewModel TerminalViewModel { get; }
    public Action<string>? ToolChangeRequested;
    public Action? ZoomExtentsRequested;
    public Action? ZoomWindowRequested;
    public Action? ToggleGridRequested;
    public Action<string>? SelectionModeChanged;
    public List<RibbonTab> RibbonTabs => RibbonDefinitions.Tabs;
    public List<RibbonItem> RibbonQuickAccess => RibbonDefinitions.QuickAccessItems;

    public EditorViewModel(CadDocument document, CommandStack commandStack, SelectionService selectionService,
        SnapEngine snapEngine, TrainService.Cad.Clipboard.ClipboardService clipboardService,
        ILogBus logBus, TerminalPanelViewModel terminalViewModel, ICadDocumentStore store, Guid? projectId = null)
    {
        _projectId = projectId ?? Guid.NewGuid();
        TerminalViewModel = terminalViewModel;
        Document = document; CommandStack = commandStack;
        SelectionService = selectionService; SnapEngine = snapEngine; ClipboardService = clipboardService;
        _logBus = logBus; _store = store;
        CommandStack.StackChanged += (s, e) => { UndoCommand.NotifyCanExecuteChanged(); RedoCommand.NotifyCanExecuteChanged(); };
        Document.Changed += (s, e) => { SaveCommand.NotifyCanExecuteChanged(); UpdateDocumentStatus(); };
        SelectionService.PruneMissing(Document);
        UpdateDocumentStatus();
    }

    public async Task InitializeAsync()
    {
        try { await _store.LoadDocumentAsync(_projectId, Document); SelectionService.PruneMissing(Document); UpdateDocumentStatus(); _logBus.Info("Editor", "Proje veritabanından yüklendi."); }
        catch (Exception ex) { _logBus.Error("Editor", $"Proje yüklenirken hata: {ex.Message}"); }
    }

    private void UpdateDocumentStatus() => DocumentStatusText = Document.IsDirty ? "Kayıtlı Değil (*)" : "Kaydedildi";
    private bool CanSave() => Document.IsDirty;

    [RelayCommand(CanExecute = nameof(CanSave))] private async Task Save() { if (!await _saveSemaphore.WaitAsync(0)) return; try { await _store.SaveDocumentAsync(_projectId, Document); _logBus.Success("Editor", "Proje veritabanına kaydedildi."); } catch (Exception ex) { _logBus.Error("Editor", $"Kayıt hatası: {ex.Message}"); } finally { _saveSemaphore.Release(); } }
    [RelayCommand] private void SetTool(string toolName) { ActiveToolName = toolName; ToolChangeRequested?.Invoke(toolName); _logBus.Info("Editor", $"Araç seçildi: {toolName}"); }
    [RelayCommand] private void SetSelectionMode(string mode) { ActiveSelectionMode = mode; SelectionModeChanged?.Invoke(mode); _logBus.Info("Editor", $"Seçim modu: {mode}"); }
    private bool CanUndo() => ActiveTab?.CommandStack.CanUndo ?? CommandStack.CanUndo;
    [RelayCommand(CanExecute = nameof(CanUndo))] private void Undo() { var stack = ActiveTab?.CommandStack ?? CommandStack; var doc = ActiveTab?.Document ?? Document; var desc = stack.PeekUndoDescription; stack.Undo(doc); _logBus.Info("Editor", $"Geri alındı: {desc}"); }
    private bool CanRedo() => ActiveTab?.CommandStack.CanRedo ?? CommandStack.CanRedo;
    [RelayCommand(CanExecute = nameof(CanRedo))] private void Redo() { var stack = ActiveTab?.CommandStack ?? CommandStack; var doc = ActiveTab?.Document ?? Document; var desc = stack.PeekRedoDescription; stack.Redo(doc); _logBus.Success("Editor", $"Tekrar yapıldı: {desc}"); }
    [RelayCommand] private void DebugAddLine() { var cmd = new DebugAddLineCommand(Document.ActiveLayerId); CommandStack.Do(cmd, Document); _logBus.Success("Editor", $"Komut: {cmd.Description}"); }
    [RelayCommand] private void ToggleSnap() { IsSnapEnabled = !IsSnapEnabled; if (SnapEngine != null) SnapEngine.IsEnabled = IsSnapEnabled; SnapStatusText = IsSnapEnabled ? " [GRID]" : " [OFF]"; _logBus.Info("Snap", IsSnapEnabled ? "Snap AÇIK" : "Snap KAPALI"); }
    [RelayCommand] private void Delete() { var sel = ActiveTab?.SelectionService ?? SelectionService; var doc = ActiveTab?.Document ?? Document; var stack = ActiveTab?.CommandStack ?? CommandStack; var selectedIds = sel.SelectedIds.ToList(); if (selectedIds.Count == 0) return; var cmd = new DeleteEntitiesCommand(selectedIds); stack.Do(cmd, doc); _logBus.Success("Editor", $"Silindi: {selectedIds.Count} nesne"); }
    [RelayCommand] private void Copy() { var sel = ActiveTab?.SelectionService ?? SelectionService; var doc = ActiveTab?.Document ?? Document; var clip = ActiveTab?.ClipboardService ?? ClipboardService; var selected = doc.Entities.Where(e => sel.SelectedIds.Contains(e.Id)).ToList(); if (selected.Count == 0) return; clip.Set(selected); _logBus.Info("Editor", $"Kopyalandı: {selected.Count} nesne"); }
    [RelayCommand] private void Cut() { var sel = ActiveTab?.SelectionService ?? SelectionService; var doc = ActiveTab?.Document ?? Document; var stack = ActiveTab?.CommandStack ?? CommandStack; var clip = ActiveTab?.ClipboardService ?? ClipboardService; var selected = doc.Entities.Where(e => sel.SelectedIds.Contains(e.Id)).ToList(); if (selected.Count == 0) return; clip.Set(selected); var ids = selected.Select(e => e.Id).ToList(); var cmd = new DeleteEntitiesCommand(ids); stack.Do(cmd, doc); _logBus.Success("Editor", $"Kesildi: {selected.Count} nesne"); }
    [RelayCommand] private void Paste() { var stack = ActiveTab?.CommandStack ?? CommandStack; var doc = ActiveTab?.Document ?? Document; var clip = ActiveTab?.ClipboardService ?? ClipboardService; if (!clip.HasContent) return; var clones = clip.Get(); var cmd = new PasteEntitiesCommand(clones); stack.Do(cmd, doc); _logBus.Success("Editor", $"Yapıştırıldı: {clones.Count} nesne"); }
    [RelayCommand] private void ZoomExtents() { ZoomExtentsRequested?.Invoke(); _logBus.Info("Editor", "Zoom Extents"); }
    [RelayCommand] private void ZoomWindow() { ZoomWindowRequested?.Invoke(); _logBus.Info("Editor", "Zoom Window"); }
    [RelayCommand] private void ToggleGrid() { ToggleGridRequested?.Invoke(); _logBus.Info("Editor", "Izgara değiştirildi"); }
    [RelayCommand] private void SetActiveLayer(string layerId) { if (Guid.TryParse(layerId, out var id)) { ActiveLayerId = id; _logBus.Info("Editor", $"Aktif katman: {ActiveLayerName}"); } }

    // v3.0.29.22 — Snap toggle komutları (log'lu)
    [RelayCommand] private void ToggleEndpointSnap() { if (SnapEngine == null) return; bool wasOff = SnapEngine.DisabledKinds.Contains(SnapKind.Endpoint); if (wasOff) SnapEngine.DisabledKinds.Remove(SnapKind.Endpoint); else SnapEngine.DisabledKinds.Add(SnapKind.Endpoint); _logBus.Info("Snap", wasOff ? "Endpoint Snap: AÇILDI" : "Endpoint Snap: KAPATILDI"); }
    [RelayCommand] private void ToggleMidpointSnap() { if (SnapEngine == null) return; bool wasOff = SnapEngine.DisabledKinds.Contains(SnapKind.Midpoint); if (wasOff) SnapEngine.DisabledKinds.Remove(SnapKind.Midpoint); else SnapEngine.DisabledKinds.Add(SnapKind.Midpoint); _logBus.Info("Snap", wasOff ? "Midpoint Snap: AÇILDI" : "Midpoint Snap: KAPATILDI"); }
    [RelayCommand] private void ToggleOnSegmentSnap() { if (SnapEngine == null) return; bool wasOff = SnapEngine.DisabledKinds.Contains(SnapKind.OnSegment); if (wasOff) SnapEngine.DisabledKinds.Remove(SnapKind.OnSegment); else SnapEngine.DisabledKinds.Add(SnapKind.OnSegment); _logBus.Info("Snap", wasOff ? "OnSegment Snap: AÇILDI" : "OnSegment Snap: KAPATILDI"); }
    [RelayCommand] private void ToggleGridSnap() { if (SnapEngine == null) return; bool wasOff = SnapEngine.DisabledKinds.Contains(SnapKind.Grid); if (wasOff) SnapEngine.DisabledKinds.Remove(SnapKind.Grid); else SnapEngine.DisabledKinds.Add(SnapKind.Grid); _logBus.Info("Snap", wasOff ? "Grid Snap: AÇILDI" : "Grid Snap: KAPATILDI"); }
}