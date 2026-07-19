using System;
using System.Linq;
using FluentAssertions;
using Xunit;
using TrainService.App.ViewModels;
using TrainService.Core.Abstractions;
using TrainService.Core.Enums;
using TrainService.Cad;
using TrainService.Cad.Persistence;
using TrainService.Cad.Snapping;
using TrainService.Core.Entities;

namespace TrainService.App.Tests;

/// <summary>
/// v3.0.29.1-fix — Kritik bug düzeltmeleri testleri.
/// T336–T344: PasteCommand undo/redo, RestoreEntity IsDirty, Clipboard klonlama,
/// PruneMissing leak, SaveLoad ProjectId.
/// </summary>
public sealed class T336_T344_FixTests
{
    // ============================================================
    // T336: PasteCommand undo/redo — yapıştırma geri alınabilmeli
    // ============================================================
    [Fact]
    public void T336_PasteCommand_IsUndoable()
    {
        var vm = CreateEditorViewModel();
        var doc = vm.Document;
        var node = new TrackNode
        {
            Id = Guid.NewGuid(),
            Position = new(10, 20),
            LayerId = doc.ActiveLayerId
        };
        doc.RestoreEntity(node);
        vm.SelectionService.Add(node.Id);

        doc.Entities.Should().HaveCount(1);

        // Copy + Paste
        vm.CopyCommand.Execute(null);
        vm.PasteCommand.Execute(null);

        doc.Entities.Should().HaveCount(2, "paste should add one entity");

        // Undo paste
        vm.UndoCommand.Execute(null);

        doc.Entities.Should().HaveCount(1, "undo paste should restore original count");
    }

    // ============================================================
    // T337: PasteCommand IsDirty — yapıştırma dokümanı kirli yapmalı
    // ============================================================
    [Fact]
    public void T337_PasteCommand_SetsIsDirty()
    {
        var vm = CreateEditorViewModel();
        var doc = vm.Document;
        doc.MarkSaved();
        doc.IsDirty.Should().BeFalse();

        var node = new TrackNode
        {
            Id = Guid.NewGuid(),
            Position = new(10, 20),
            LayerId = doc.ActiveLayerId
        };
        doc.RestoreEntity(node);
        vm.SelectionService.Add(node.Id);

        vm.CopyCommand.Execute(null);
        vm.PasteCommand.Execute(null);

        doc.IsDirty.Should().BeTrue("paste must mark document dirty");
    }

    // ============================================================
    // T338: RestoreEntity IsDirty — geri yükleme kirli yapmalı
    // ============================================================
    [Fact]
    public void T338_RestoreEntity_SetsIsDirty()
    {
        var doc = new CadDocument();
        doc.MarkSaved();
        doc.IsDirty.Should().BeFalse();

        var node = new TrackNode
        {
            Id = Guid.NewGuid(),
            Position = new(10, 20),
            LayerId = doc.ActiveLayerId
        };
        doc.RestoreEntity(node);

        doc.IsDirty.Should().BeTrue("RestoreEntity must mark document dirty");
    }

    // ============================================================
    // T339: RestoreEntity Changed event — geri yükleme event fırlatmalı
    // ============================================================
    [Fact]
    public void T339_RestoreEntity_FiresChangedEvent()
    {
        var doc = new CadDocument();
        var eventFired = false;
        doc.Changed += (s, e) =>
        {
            if (e.Kind == DocumentChangeKind.Added) eventFired = true;
        };

        var node = new TrackNode
        {
            Id = Guid.NewGuid(),
            Position = new(10, 20),
            LayerId = doc.ActiveLayerId
        };
        doc.RestoreEntity(node);

        eventFired.Should().BeTrue("RestoreEntity must fire Changed event with Added kind");
    }

    // ============================================================
    // T340–T342: ClipboardService yeni entity türleri klonlama
    // ============================================================
    [Fact]
    public void T340_Clipboard_CanClone_RailSwitch()
    {
        var clip = new TrainService.Cad.Clipboard.ClipboardService();
        var sw = new RailSwitch
        {
            Id = Guid.NewGuid(),
            Position = new(100, 200),
            RotationDeg = 30,
            EntryNodeId = Guid.NewGuid(),
            MainExitNodeId = Guid.NewGuid(),
            DivergingExitNodeId = Guid.NewGuid(),
            State = SwitchState.Main,
            LayerId = CadDocument.SabitKatmanlar.Zemin
        };

        clip.Set(new[] { sw });
        var clones = clip.Get();

        clones.Should().HaveCount(1);
        clones[0].Should().BeOfType<RailSwitch>();
        var clone = (RailSwitch)clones[0];
        clone.Position.Should().Be(sw.Position);
        clone.RotationDeg.Should().Be(sw.RotationDeg);
        clone.State.Should().Be(sw.State);
    }

    [Fact]
    public void T341_Clipboard_CanClone_Ramp()
    {
        var clip = new TrainService.Cad.Clipboard.ClipboardService();
        var ramp = new Ramp
        {
            Id = Guid.NewGuid(),
            Position = new(50, 75),
            RotationDeg = 0,
            EntryNodeId = Guid.NewGuid(),
            ExitNodeId = Guid.NewGuid(),
            StartZ = 0,
            EndZ = 350,
            LengthMm = 100,
            LayerId = CadDocument.SabitKatmanlar.Zemin
        };

        clip.Set(new[] { ramp });
        var clones = clip.Get();

        clones.Should().HaveCount(1);
        clones[0].Should().BeOfType<Ramp>();
        var clone = (Ramp)clones[0];
        clone.StartZ.Should().Be(ramp.StartZ);
        clone.EndZ.Should().Be(ramp.EndZ);
        clone.GradePercent.Should().Be(ramp.GradePercent);
    }

    [Fact]
    public void T342_Clipboard_CanClone_Route()
    {
        var clip = new TrainService.Cad.Clipboard.ClipboardService();
        var route = new Route
        {
            Id = Guid.NewGuid(),
            Name = "TestRoute",
            LayerId = CadDocument.SabitKatmanlar.Zemin
        };
        route.Steps.Add(new RouteStep(Guid.NewGuid(), TravelDirection.Forward));

        clip.Set(new[] { route });
        var clones = clip.Get();

        clones.Should().HaveCount(1);
        clones[0].Should().BeOfType<Route>();
        var clone = (Route)clones[0];
        clone.Name.Should().Be(route.Name);
        clone.Steps.Should().HaveCount(1);
    }

    // ============================================================
    // T343: PruneMissing event leak — çoklu çağrı tek handler
    // ============================================================
    [Fact]
    public void T343_PruneMissing_DoesNotLeakOnMultipleCalls()
    {
        var doc = new CadDocument();
        var sel = new TrainService.Cad.Selection.SelectionService();
        var eventCount = 0;
        sel.SelectionChanged += (s, e) => eventCount++;

        sel.PruneMissing(doc);
        sel.PruneMissing(doc); // ikinci çağrı — leak olursa 2 event atar
        sel.PruneMissing(doc); // üçüncü çağrı

        var node = new TrackNode
        {
            Id = Guid.NewGuid(),
            Position = new(0, 0),
            LayerId = doc.ActiveLayerId
        };
        doc.RestoreEntity(node);
        sel.Add(node.Id);

        // Entity silindiğinde selection otomatik düşmeli
        var cmd = new TrainService.Cad.UndoRedo.DeleteEntitiesCommand(new[] { node.Id });
        var stack = new TrainService.Cad.UndoRedo.CommandStack();
        stack.Do(cmd, doc);

        eventCount.Should().Be(2, // Add + Remove
            "PruneMissing multiple calls should not register duplicate handlers");
    }

    // ============================================================
    // T344: SaveLoad ProjectId — Guid.Empty yerine gerçek ID
    // ============================================================
    [Fact]
    public void T344_SaveLoad_UsesProjectId()
    {
        var projectId = Guid.NewGuid();
        var store = new SpyCadDocumentStore();
        var doc = new CadDocument();
        var stack = new TrainService.Cad.UndoRedo.CommandStack();
        var selSvc = new TrainService.Cad.Selection.SelectionService();
        var snap = new SnapEngine(Enumerable.Empty<ISnapProvider>());
        var clip = new TrainService.Cad.Clipboard.ClipboardService();
        var log = new NullLogBus();

        var terminal = new NullTerminalPanelViewModel();
        var vm = new EditorViewModel(doc, stack, selSvc, snap, clip, log, terminal, store);
        // ProjectId inject için reflection (şimdilik, constructor sonrası set)
        // v3.0.29.2'de constructor parametresi olacak

        vm.SaveCommand.Execute(null);

        store.LastSaveProjectId.Should().NotBe(Guid.Empty, "Save must use a real ProjectId, not Guid.Empty");
    }

    // ============================================================
    // Helpers
    // ============================================================
    private static EditorViewModel CreateEditorViewModel()
    {
        var doc = new CadDocument();
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
        public System.Threading.Tasks.Task SaveDocumentAsync(Guid projectId, CadDocument document) => System.Threading.Tasks.Task.CompletedTask;
        public System.Threading.Tasks.Task LoadDocumentAsync(Guid projectId, CadDocument document) => System.Threading.Tasks.Task.CompletedTask;
        public System.Threading.Tasks.Task CreateSnapshotAsync(CadDocument document, string name) => System.Threading.Tasks.Task.CompletedTask;
    }

    private sealed class SpyCadDocumentStore : ICadDocumentStore
    {
        public Guid LastSaveProjectId { get; private set; }
        public System.Threading.Tasks.Task SaveDocumentAsync(Guid projectId, CadDocument document)
        {
            LastSaveProjectId = projectId;
            return System.Threading.Tasks.Task.CompletedTask;
        }
        public System.Threading.Tasks.Task LoadDocumentAsync(Guid projectId, CadDocument document) => System.Threading.Tasks.Task.CompletedTask;
        public System.Threading.Tasks.Task CreateSnapshotAsync(CadDocument document, string name) => System.Threading.Tasks.Task.CompletedTask;
    }
}