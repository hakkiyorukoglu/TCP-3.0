using System;
using System.Linq;
using System.Collections.ObjectModel;
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
/// v3.0.29.2 — Sekmeli Çoklu Belge testleri.
/// T340–T347: Tab oluşturma, izolasyon, kirli bayrak, yeniden adlandırma, kapatma, Ctrl+Tab.
/// </summary>
public sealed class T340_T347_TabsTests
{
    // ============================================================
    // T340: AddTab_CreatesIsolatedSet — yeni sekme izole set üretir
    // ============================================================
    [Fact]
    public void T340_AddTab_CreatesIsolatedSet()
    {
        var vm = CreateDocumentTabsViewModel();

        vm.AddTabCommand.Execute(null);

        vm.Tabs.Should().HaveCount(1);
        vm.ActiveTab.Should().NotBeNull();
        vm.ActiveTab!.Document.Should().NotBeNull();
        vm.ActiveTab.CommandStack.Should().NotBeNull();
        vm.ActiveTab.SelectionService.Should().NotBeNull();
        vm.ActiveTab.SnapEngine.Should().NotBeNull();
        vm.ActiveTab.ClipboardService.Should().NotBeNull();
    }

    // ============================================================
    // T341: TabIsolation_DocA_DoesNotAffectDocB — sekme A'daki çizim B'ye sıçramaz
    // ============================================================
    [Fact]
    public void T341_TabIsolation_DocA_DoesNotAffectDocB()
    {
        var vm = CreateDocumentTabsViewModel();
        vm.AddTabCommand.Execute(null); // Tab 1
        vm.AddTabCommand.Execute(null); // Tab 2

        vm.Tabs.Should().HaveCount(2);
        var tab1 = vm.Tabs[0];
        var tab2 = vm.Tabs[1];

        // Tab 1'e entity ekle
        var node = new TrackNode { Id = Guid.NewGuid(), Position = new(10, 20), LayerId = tab1.Document.ActiveLayerId };
        tab1.Document.RestoreEntity(node);

        // Tab 2'de entity olmamalı
        tab2.Document.Entities.Should().BeEmpty("Tab 2 must be isolated from Tab 1");
    }

    // ============================================================
    // T342: TabIsolation_UndoStackA_DoesNotAffectStackB — undo yığınları izole
    // ============================================================
    [Fact]
    public void T342_TabIsolation_UndoStackA_DoesNotAffectStackB()
    {
        var vm = CreateDocumentTabsViewModel();
        vm.AddTabCommand.Execute(null); // Tab 1
        vm.AddTabCommand.Execute(null); // Tab 2

        var tab1 = vm.Tabs[0];
        var tab2 = vm.Tabs[1];

        // Tab 1'de entity ekle
        var node = new TrackNode { Id = Guid.NewGuid(), Position = new(10, 20), LayerId = tab1.Document.ActiveLayerId };
        tab1.Document.RestoreEntity(node);

        tab1.CommandStack.CanUndo.Should().BeFalse("RestoreEntity does not use CommandStack");
        tab2.CommandStack.CanUndo.Should().BeFalse("Tab 2 must be isolated");
    }

    // ============================================================
    // T343: IsDirty_ReflectedInTabHeader — kirli bayrak
    // ============================================================
    [Fact]
    public void T343_IsDirty_ReflectedInTabHeader()
    {
        var vm = CreateDocumentTabsViewModel();
        vm.AddTabCommand.Execute(null);

        var tab = vm.ActiveTab!;
        tab.IsDirty.Should().BeFalse("new tab should start clean");

        // Entity ekle → IsDirty true (RestoreEntity IsDirty=true)
        var node = new TrackNode { Id = Guid.NewGuid(), Position = new(10, 20), LayerId = tab.Document.ActiveLayerId };
        tab.Document.RestoreEntity(node);

        tab.IsDirty.Should().BeTrue("after adding entity, tab should be dirty");
    }

    // ============================================================
    // T344: RenameTab_UpdatesDisplayName — sekme adı değişir
    // ============================================================
    [Fact]
    public void T344_RenameTab_UpdatesDisplayName()
    {
        var vm = CreateDocumentTabsViewModel();
        vm.AddTabCommand.Execute(null);

        var tab = vm.ActiveTab!;
        tab.DisplayName.Should().Be("Yeni Proje");

        vm.RenameTabCommand.Execute("ProjeA");

        tab.DisplayName.Should().Be("ProjeA");
    }

    // ============================================================
    // T345: CloseTab_LastTab_CreatesNewEmpty — son sekme kapanınca yeni boş
    // ============================================================
    [Fact]
    public void T345_CloseTab_LastTab_CreatesNewEmpty()
    {
        var vm = CreateDocumentTabsViewModel();
        vm.AddTabCommand.Execute(null); // Tek sekme

        vm.Tabs.Should().HaveCount(1);
        var tab = vm.ActiveTab!;

        vm.CloseTabCommand.Execute(tab);

        vm.Tabs.Should().HaveCount(1, "last closed tab should auto-create new empty tab");
        vm.ActiveTab.Should().NotBe(tab, "new tab should be a different instance");
    }

    // ============================================================
    // T346: CloseTab_Dirty_ReturnsFalseWhenCancelled — kirli sekme vazgeç
    // ============================================================
    [Fact]
    public void T346_CloseTab_Dirty_ReturnsFalseWhenCancelled()
    {
        var vm = CreateDocumentTabsViewModel();
        vm.AddTabCommand.Execute(null);

        var tab = vm.ActiveTab!;
        var node = new TrackNode { Id = Guid.NewGuid(), Position = new(10, 20), LayerId = tab.Document.ActiveLayerId };
        tab.Document.RestoreEntity(node); // Kirli yap

        tab.IsDirty.Should().BeTrue();

        // CloseTab vazgeç durumunda sekme kalır
        // Test: CloseTab çağrıldığında IsDirty true → false döner (vazgeç)
        var result = vm.TryCloseTab(tab); // Public sync overload for testing

        result.Should().BeFalse("closing dirty tab without save should return false (cancelled)");
        vm.Tabs.Should().Contain(tab, "tab should remain when close is cancelled");
    }

    // ============================================================
    // T347: ActiveTab_SwitchesCorrectly — aktif sekme değişimi
    // ============================================================
    [Fact]
    public void T347_ActiveTab_SwitchesCorrectly()
    {
        var vm = CreateDocumentTabsViewModel();
        vm.AddTabCommand.Execute(null); // Tab 1
        vm.AddTabCommand.Execute(null); // Tab 2
        vm.AddTabCommand.Execute(null); // Tab 3

        vm.Tabs.Should().HaveCount(3);

        var tab1 = vm.Tabs[0];
        var tab2 = vm.Tabs[1];
        var tab3 = vm.Tabs[2];

        vm.ActiveTab = tab1;
        vm.ActiveTab.Should().Be(tab1);

        vm.ActiveTab = tab2;
        vm.ActiveTab.Should().Be(tab2);

        vm.ActiveTab = tab3;
        vm.ActiveTab.Should().Be(tab3);
    }

    // ============================================================
    // Helpers
    // ============================================================
    private static DocumentTabsViewModel CreateDocumentTabsViewModel()
    {
        var log = new NullLogBus();
        return new DocumentTabsViewModel(log);
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

    private sealed class NullCadDocumentStore : ICadDocumentStore
    {
        public System.Threading.Tasks.Task SaveDocumentAsync(Guid projectId, CadDocument document) => System.Threading.Tasks.Task.CompletedTask;
        public System.Threading.Tasks.Task LoadDocumentAsync(Guid projectId, CadDocument document) => System.Threading.Tasks.Task.CompletedTask;
        public System.Threading.Tasks.Task CreateSnapshotAsync(CadDocument document, string name) => System.Threading.Tasks.Task.CompletedTask;
    }
}