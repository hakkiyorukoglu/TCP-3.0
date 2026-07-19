using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;
using TrainService.App.ViewModels;
using TrainService.App.Models;

namespace TrainService.App.Tests;

/// <summary>
/// v3.0.29.8 — Ribbon Proxy + Memory Leak Düzeltmesi testleri.
/// T400–T407: ActiveTab property ve komut yönlendirmesi.
/// </summary>
public sealed class T400_T407_RibbonProxyTests
{
    // ============================================================
    // T400: EditorViewModel_ActiveTab_SetsDocument — ActiveTab değişince Document güncellenir
    // ============================================================
    [Fact]
    public void T400_EditorViewModel_ActiveTab_SetsDocument()
    {
        var vm = CreateEditorViewModel();
        var tab = CreateEditorTabModel();

        vm.ActiveTab = tab;

        vm.ActiveTab.Should().NotBeNull();
        vm.Document.Should().Be(tab.Document, "Document should update when ActiveTab changes");
    }

    // ============================================================
    // T401: EditorViewModel_ActiveTab_Null_Set — Null ActiveTab güvenli set edilir
    // ============================================================
    [Fact]
    public void T401_EditorViewModel_ActiveTab_Null_Set()
    {
        var vm = CreateEditorViewModel();
        
        vm.ActiveTab = null;

        vm.ActiveTab.Should().BeNull();
    }

    // ============================================================
    // T402: EditorViewModel_CanUndo_NullActiveTab — Null ActiveTab'da CanUndo fallback
    // ============================================================
    [Fact]
    public void T402_EditorViewModel_CanUndo_NullActiveTab()
    {
        var vm = CreateEditorViewModel();

        vm.ActiveTab.Should().BeNull();

        // Fallback: null ActiveTab → constructor'dan gelen CommandStack kullanılır
        vm.UndoCommand.CanExecute(null).Should().Be(vm.CommandStack.CanUndo);
    }

    // ============================================================
    // T403: EditorViewModel_CanRedo_NullActiveTab — Null ActiveTab'da CanRedo fallback
    // ============================================================
    [Fact]
    public void T403_EditorViewModel_CanRedo_NullActiveTab()
    {
        var vm = CreateEditorViewModel();

        vm.ActiveTab.Should().BeNull();

        // Fallback: null ActiveTab → constructor'dan gelen CommandStack kullanılır
        vm.RedoCommand.CanExecute(null).Should().Be(vm.CommandStack.CanRedo);
    }

    // ============================================================
    // T404: EditorViewModel_ActiveTab_HasServices — ActiveTab tüm servislere sahip
    // ============================================================
    [Fact]
    public void T404_EditorViewModel_ActiveTab_HasServices()
    {
        var vm = CreateEditorViewModel();
        var tab = CreateEditorTabModel();
        vm.ActiveTab = tab;

        vm.ActiveTab!.Document.Should().NotBeNull();
        vm.ActiveTab.CommandStack.Should().NotBeNull();
        vm.ActiveTab.SelectionService.Should().NotBeNull();
        vm.ActiveTab.SnapEngine.Should().NotBeNull();
        vm.ActiveTab.ClipboardService.Should().NotBeNull();
    }

    // ============================================================
    // T405: EditorViewModel_ActiveTab_ChangesDocument — ActiveTab değişimi Document'i değiştirir
    // ============================================================
    [Fact]
    public void T405_EditorViewModel_ActiveTab_ChangesDocument()
    {
        var vm = CreateEditorViewModel();
        var tab1 = CreateEditorTabModel();
        var tab2 = CreateEditorTabModel();

        vm.ActiveTab = tab1;
        vm.Document.Should().Be(tab1.Document);

        vm.ActiveTab = tab2;
        vm.Document.Should().Be(tab2.Document, "Document should change when ActiveTab changes");
    }

    // ============================================================
    // T406: EditorViewModel_ActiveTab_ProjectId_Matches — ActiveTab ProjectId doğru
    // ============================================================
    [Fact]
    public void T406_EditorViewModel_ActiveTab_ProjectId_Matches()
    {
        var vm = CreateEditorViewModel();
        var tab = CreateEditorTabModel();
        vm.ActiveTab = tab;

        vm.ActiveTab!.ProjectId.Should().NotBe(Guid.Empty);
    }

    // ============================================================
    // T407: EditorViewModel_ActiveTab_IsDirty_InitiallyFalse — Yeni sekme kirli değil
    // ============================================================
    [Fact]
    public void T407_EditorViewModel_ActiveTab_IsDirty_InitiallyFalse()
    {
        var vm = CreateEditorViewModel();
        var tab = CreateEditorTabModel();
        vm.ActiveTab = tab;

        vm.ActiveTab!.IsDirty.Should().BeFalse("new tab should not be dirty initially");
    }

    // ============================================================
    // Helpers
    // ============================================================
    private static EditorViewModel CreateEditorViewModel()
    {
        var doc = new TrainService.Cad.CadDocument();
        var stack = new TrainService.Cad.UndoRedo.CommandStack();
        var sel = new TrainService.Cad.Selection.SelectionService();
        var snap = new TrainService.Cad.Snapping.SnapEngine(new List<TrainService.Cad.Snapping.ISnapProvider>());
        var clip = new TrainService.Cad.Clipboard.ClipboardService();
        var log = new NullLogBus();
        var terminal = new NullTerminalPanelViewModel();
        var store = new NullCadDocumentStore();
        return new EditorViewModel(doc, stack, sel, snap, clip, log, terminal, store);
    }

    private static EditorTabModel CreateEditorTabModel()
    {
        var doc = new TrainService.Cad.CadDocument();
        var stack = new TrainService.Cad.UndoRedo.CommandStack();
        var sel = new TrainService.Cad.Selection.SelectionService();
        var snap = new TrainService.Cad.Snapping.SnapEngine(new List<TrainService.Cad.Snapping.ISnapProvider>());
        var clip = new TrainService.Cad.Clipboard.ClipboardService();
        return new EditorTabModel(Guid.NewGuid(), doc, stack, sel, snap, clip);
    }

    private sealed class NullLogBus : TrainService.Core.Abstractions.ILogBus
    {
        public System.Collections.Generic.IReadOnlyList<TrainService.Core.Abstractions.LogMessage> GetAllLogs() => Array.Empty<TrainService.Core.Abstractions.LogMessage>();
        public void Write(TrainService.Core.Enums.LogLevel level, string source, string message) { }
        public void Info(string source, string message) { }
        public void Success(string source, string message) { }
        public void Warn(string source, string message) { }
        public void Error(string source, string message) { }
        public event Action<TrainService.Core.Abstractions.LogMessage>? OnMessageReceived { add { } remove { } }
    }

    private sealed class NullTerminalPanelViewModel : TerminalPanelViewModel
    {
        public NullTerminalPanelViewModel() : base(new NullLogBus()) { }
    }

    private sealed class NullCadDocumentStore : TrainService.Cad.Persistence.ICadDocumentStore
    {
        public Task SaveDocumentAsync(Guid projectId, TrainService.Cad.CadDocument document) => Task.CompletedTask;
        public Task LoadDocumentAsync(Guid projectId, TrainService.Cad.CadDocument document) => Task.CompletedTask;
        public Task CreateSnapshotAsync(TrainService.Cad.CadDocument document, string name) => Task.CompletedTask;
    }
}