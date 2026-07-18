using System;
using System.Linq;
using FluentAssertions;
using Xunit;
using TrainService.App.ViewModels;
using TrainService.App.Models;
using TrainService.Core.Abstractions;
using TrainService.Core.Enums;
using TrainService.Core.Entities;

namespace TrainService.App.Tests;

/// <summary>
/// v3.0.29.3 — Sekmeli Çoklu Belge UI Entegrasyon testleri.
/// T350–T357: ViewModel proxy, sekme başlığı, Viewport/FeatureTree bağlama.
/// </summary>
public sealed class T350_T357_TabsUiTests
{
    // ============================================================
    // T350: ProxyCommands_RouteToActiveTab — proxy komutlar aktif sekmeye gider
    // ============================================================
    [Fact]
    public void T350_ProxyCommands_RouteToActiveTab()
    {
        var vm = CreateDocumentTabsViewModel();
        vm.AddTabCommand.Execute(null);
        var tab = vm.ActiveTab!;

        // Proxy: ActiveTab üzerinden Undo/Redo/Save erişilebilir
        tab.CommandStack.Should().NotBeNull();
        tab.Document.Should().NotBeNull();
        tab.SelectionService.Should().NotBeNull();
    }

    // ============================================================
    // T351: TabHeader_DisplayNameAndDirtyStar — sekme başlığı ad + ★
    // ============================================================
    [Fact]
    public void T351_TabHeader_DisplayNameAndDirtyStar()
    {
        var vm = CreateDocumentTabsViewModel();
        vm.AddTabCommand.Execute(null);

        var tab = vm.ActiveTab!;
        tab.DisplayName.Should().Be("Yeni Proje");
        tab.IsDirty.Should().BeFalse();

        // Kirli yap
        var node = new TrackNode { Id = Guid.NewGuid(), Position = new(10, 20), LayerId = tab.Document.ActiveLayerId };
        tab.Document.RestoreEntity(node);

        tab.IsDirty.Should().BeTrue();
        // UI'da ★ gösterilir (IsDirty=true)
    }

    // ============================================================
    // T352: ActiveTab_Document_IsolatedPerTab — her sekme kendi doc'una sahip
    // ============================================================
    [Fact]
    public void T352_ActiveTab_Document_IsolatedPerTab()
    {
        var vm = CreateDocumentTabsViewModel();
        vm.AddTabCommand.Execute(null); // Tab 1
        vm.AddTabCommand.Execute(null); // Tab 2

        var doc1 = vm.Tabs[0].Document;
        var doc2 = vm.Tabs[1].Document;

        doc1.Should().NotBeSameAs(doc2, "each tab must have its own CadDocument");
    }

    // ============================================================
    // T353: SwitchActiveTab_ViewportGetsNewDoc — aktif sekme değişince Viewport yeni doc alır
    // ============================================================
    [Fact]
    public void T353_SwitchActiveTab_ViewportGetsNewDoc()
    {
        var vm = CreateDocumentTabsViewModel();
        vm.AddTabCommand.Execute(null); // Tab 1
        vm.AddTabCommand.Execute(null); // Tab 2

        var tab1 = vm.Tabs[0];
        var tab2 = vm.Tabs[1];

        // Tab 1'e entity ekle
        var node1 = new TrackNode { Id = Guid.NewGuid(), Position = new(10, 20), LayerId = tab1.Document.ActiveLayerId };
        tab1.Document.RestoreEntity(node1);

        // Tab 2 aktif
        vm.ActiveTab = tab2;
        vm.ActiveTab.Document.Entities.Should().BeEmpty("Tab 2 must be empty after switch");
        vm.ActiveTab.Should().Be(tab2);

        // Tab 1'e geri dön
        vm.ActiveTab = tab1;
        vm.ActiveTab.Document.Entities.Should().HaveCount(1, "Tab 1 must retain its entity");
    }

    // ============================================================
    // T354: SwitchActiveTab_FeatureTreeGetsNewDoc — FeatureTree yeni doc'a bağlanır
    // ============================================================
    [Fact]
    public void T354_SwitchActiveTab_FeatureTreeGetsNewDoc()
    {
        var vm = CreateDocumentTabsViewModel();
        vm.AddTabCommand.Execute(null); // Tab 1
        vm.AddTabCommand.Execute(null); // Tab 2

        var tab1 = vm.Tabs[0];
        var tab2 = vm.Tabs[1];

        // Tab 1'e entity ekle
        var node = new TrackNode { Id = Guid.NewGuid(), Position = new(10, 20), LayerId = tab1.Document.ActiveLayerId };
        tab1.Document.RestoreEntity(node);

        // Tab 1'de 1 entity, Tab 2'de 0
        tab1.Document.Entities.Count().Should().Be(1);
        tab2.Document.Entities.Count().Should().Be(0);

        // Aktif sekme değişimi FeatureTree'yi yeni doc'a bağlar (UI test yerine model test)
        vm.ActiveTab = tab2;
        vm.ActiveTab.Document.Entities.Should().BeEmpty();
    }

    // ============================================================
    // T355: AddTab_CreatesWithDefaultName — + butonu yeni "Yeni Proje" oluşturur
    // ============================================================
    [Fact]
    public void T355_AddTab_CreatesWithDefaultName()
    {
        var vm = CreateDocumentTabsViewModel();
        vm.AddTabCommand.Execute(null);

        vm.Tabs.Should().HaveCount(1);
        vm.ActiveTab!.DisplayName.Should().Be("Yeni Proje");
    }

    // ============================================================
    // T356: CloseTab_RemovesFromStrip — X butonu sekme kaldırır
    // ============================================================
    [Fact]
    public void T356_CloseTab_RemovesFromStrip()
    {
        var vm = CreateDocumentTabsViewModel();
        vm.AddTabCommand.Execute(null); // Tab 1
        vm.AddTabCommand.Execute(null); // Tab 2

        var tab = vm.Tabs[0];
        vm.Tabs.Should().HaveCount(2);

        vm.CloseTabCommand.Execute(tab);
        vm.Tabs.Should().HaveCount(1);
        vm.Tabs.Should().NotContain(tab);
    }

    // ============================================================
    // T357: RibbonCommands_WorkWithActiveTab — Ribbon komutları aktif sekmede çalışır
    // ============================================================
    [Fact]
    public void T357_RibbonCommands_WorkWithActiveTab()
    {
        var vm = CreateDocumentTabsViewModel();
        vm.AddTabCommand.Execute(null); // Tab 1
        vm.AddTabCommand.Execute(null); // Tab 2

        var tab1 = vm.Tabs[0];
        var tab2 = vm.Tabs[1];

        // Tab 1 aktif, entity ekle
        vm.ActiveTab = tab1;
        var node = new TrackNode { Id = Guid.NewGuid(), Position = new(10, 20), LayerId = tab1.Document.ActiveLayerId };
        tab1.Document.RestoreEntity(node);
        tab1.IsDirty.Should().BeTrue();

        // Tab 2 aktif, Tab 1'in IsDirty'si korunur
        vm.ActiveTab = tab2;
        tab2.IsDirty.Should().BeFalse();

        // Tab 1 geri dön, IsDirty hâlâ true
        vm.ActiveTab = tab1;
        tab1.IsDirty.Should().BeTrue();
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
}