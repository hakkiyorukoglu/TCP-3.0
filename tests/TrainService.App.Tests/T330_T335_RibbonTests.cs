using System;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Xunit;
using TrainService.App.Controls.Ribbon;
using TrainService.App.ViewModels;
using TrainService.Core.Abstractions;
using TrainService.Core.Enums;
using TrainService.Cad;
using TrainService.Cad.Persistence;
using TrainService.Cad.Snapping;
using System.Threading.Tasks;

namespace TrainService.App.Tests;

/// <summary>
/// v3.0.29.1 Ribbon testleri.
/// T330-T335: RibbonDefinition, SetTool eşlemesi, ActiveToolName, kısayol çakışma, Delete, Clipboard.
/// </summary>
public sealed class T330_T335_RibbonTests
{
    // ============================================================
    // T330: SetTool eşlemesi — tüm ribbon araçları geçerli
    // ============================================================
    [Fact]
    public void T330_Ribbon_AllToolItemsHaveValidCommandParameter()
    {
        var toolItems = RibbonDefinitions.AllItems
            .Where(i => i.IsToggle);

        foreach (var item in toolItems)
        {
            item.CommandParameter.Should().NotBeNull(
                $"Toggle item '{item.Id}' must have a CommandParameter");
        }
    }

    // ============================================================
    // T331: ActiveToolName senkronu
    // ============================================================
    [Fact]
    public void T331_SetTool_UpdatesActiveToolName()
    {
        var vm = CreateEditorViewModel();

        vm.SetToolCommand.Execute("Track");

        vm.ActiveToolName.Should().Be("Track");

        vm.SetToolCommand.Execute("Hybrid");

        vm.ActiveToolName.Should().Be("Hybrid");
    }

    // ============================================================
    // T332: RibbonDefinition bütünlüğü
    // ============================================================
    [Fact]
    public void T332_RibbonDefinition_AllIdsAreUnique()
    {
        var allIds = RibbonDefinitions.AllItems.Select(i => i.Id).ToList();

        allIds.Distinct().Count().Should().Be(allIds.Count,
            "each RibbonItem must have a unique Id");
    }

    [Fact]
    public void T332_RibbonDefinition_HasCorrectTabCount()
    {
        RibbonDefinitions.Tabs.Should().HaveCount(4,
            "we must have Giriş, Çizim, Düzen, Görünüm tabs");
    }

    [Fact]
    public void T332_RibbonDefinition_HasCorrectQuickAccessCount()
    {
        RibbonDefinitions.QuickAccessItems.Should().HaveCount(3,
            "Quick Access must have Save, Undo, Redo");
    }

    [Fact]
    public void T332_RibbonDefinition_AllCommandNamesExistOnViewModel()
    {
        var vmType = typeof(EditorViewModel);
        var commandNames = RibbonDefinitions.AllItems
            .Where(i => i.CommandName != null)
            .Select(i => i.CommandName + "Command")
            .Distinct();

        foreach (var cmdProp in commandNames)
        {
            var prop = vmType.GetProperty(cmdProp, BindingFlags.Public | BindingFlags.Instance);
            prop.Should().NotBeNull($"EditorViewModel must have a property '{cmdProp}' for ribbon binding");
        }
    }

    // ============================================================
    // T333: Kısayol çakışma taraması
    // ============================================================
    [Fact]
    public void T333_NoDuplicateShortcuts()
    {
        var itemsWithShortcut = RibbonDefinitions.AllItems
            .Where(i => !string.IsNullOrWhiteSpace(i.ShortcutText))
            .ToList();

        // Group by shortcut; each non-empty shortcut must map to a single CommandName
        // (the same command may appear in multiple places, e.g. Undo in both Düzen tab and QuickAccess)
        var duplicates = itemsWithShortcut
            .GroupBy(i => i.ShortcutText)
            .Where(g => g.Count() > 1)
            .ToList();

        duplicates.Should().AllSatisfy(group =>
        {
            var cmdNames = group.Select(i => i.CommandName).Distinct().ToList();
            cmdNames.Should().HaveCount(1,
                $"shortcut '{group.Key}' is used by multiple distinct commands: {string.Join(", ", cmdNames)}");
        });
    }

    // ============================================================
    // T334: DeleteCommand undo/redo
    // ============================================================
    [Fact]
    public void T334_DeleteCommand_RemovesSelectedEntityAndCanUndo()
    {
        var vm = CreateEditorViewModel();
        var doc = vm.Document;
        var node = new TrainService.Core.Entities.TrackNode
        {
            Id = Guid.NewGuid(),
            Position = new(10, 20),
            LayerId = doc.ActiveLayerId
        };
        doc.RestoreEntity(node);
        vm.SelectionService.Add(node.Id);

        doc.Entities.Should().HaveCount(1);

        vm.DeleteCommand.Execute(null);

        doc.Entities.Should().BeEmpty("entity should be deleted");

        vm.UndoCommand.Execute(null);

        doc.Entities.Should().HaveCount(1, "entity should be restored after undo");
    }

    // ============================================================
    // T335: Clipboard Copy/Paste senkronu
    // ============================================================
    [Fact]
    public void T335_CopyThenPaste_DoublesEntityCount()
    {
        var vm = CreateEditorViewModel();
        var doc = vm.Document;
        var node = new TrainService.Core.Entities.TrackNode
        {
            Id = Guid.NewGuid(),
            Position = new(10, 20),
            LayerId = doc.ActiveLayerId
        };
        doc.RestoreEntity(node);
        vm.SelectionService.Add(node.Id);

        doc.Entities.Should().HaveCount(1);

        vm.CopyCommand.Execute(null);
        vm.ClipboardService.HasContent.Should().BeTrue();
        vm.ClipboardService.Count.Should().Be(1);

        vm.PasteCommand.Execute(null);

        doc.Entities.Should().HaveCount(2);
    }

    // ============================================================
    // Helper
    // ============================================================
    private static EditorViewModel CreateEditorViewModel()
    {
        var doc = new TrainService.Cad.CadDocument();
        var stack = new TrainService.Cad.UndoRedo.CommandStack();
        var selSvc = new TrainService.Cad.Selection.SelectionService();
        var snap = new SnapEngine(Enumerable.Empty<ISnapProvider>());
        var clip = new TrainService.Cad.Clipboard.ClipboardService();
        var log = new NullLogBus();
        var store = new NullCadDocumentStore();

        var terminal = new NullTerminalPanelViewModel();
        return new EditorViewModel(doc, stack, selSvc, snap, clip, log, terminal, store);
    }

    private sealed class NullLogBus : ILogBus
    {
        public System.Collections.Generic.IReadOnlyList<LogMessage> GetAllLogs() => Array.Empty<LogMessage>();
        public void Write(LogLevel level, string source, string message) { }
        public void Info(string source, string message) { }
        public void Success(string source, string message) { }
        public void Warn(string source, string message) { }
        public void Error(string source, string message) { }
        public event Action<LogMessage>? OnMessageReceived { add { } remove { } }
    }

    private sealed class NullTerminalPanelViewModel : TerminalPanelViewModel
    {
        public NullTerminalPanelViewModel() : base(new NullLogBus()) { }
    }

    private sealed class NullCadDocumentStore : ICadDocumentStore
    {
        public Task SaveDocumentAsync(Guid projectId, CadDocument document) => Task.CompletedTask;
        public Task LoadDocumentAsync(Guid projectId, CadDocument document) => Task.CompletedTask;
        public Task CreateSnapshotAsync(CadDocument document, string name) => Task.CompletedTask;
    }
}
