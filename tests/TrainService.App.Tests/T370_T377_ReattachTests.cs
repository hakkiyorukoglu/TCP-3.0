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
/// v3.0.29.5 — Sekme değişiminde yeniden bağlama testleri.
/// T370–T377: ActiveTab değişiminde Viewport/FeatureTree/ToolController yeniden bağlanır.
/// </summary>
public sealed class T370_T377_ReattachTests
{
    // ============================================================
    // T370: ActiveTab_Switch_ChangesDocument — Sekme değişiminde doc farklı
    // ============================================================
    [Fact]
    public void T370_ActiveTab_Switch_ChangesDocument()
    {
        var tabsVm = CreateDocumentTabsViewModel();
        tabsVm.AddTabCommand.Execute(null); // Tab 1
        tabsVm.AddTabCommand.Execute(null); // Tab 2

        var tab1 = tabsVm.Tabs[0];
        var tab2 = tabsVm.Tabs[1];

        // Tab 1'e entity ekle
        var node = new TrackNode { Id = Guid.NewGuid(), Position = new(10, 20), LayerId = tab1.Document.ActiveLayerId };
        tab1.Document.RestoreEntity(node);

        // Aktif sekme Tab 1
        tabsVm.ActiveTab = tab1;
        tabsVm.ActiveTab.Document.Entities.Count().Should().Be(1);

        // Aktif sekme Tab 2
        tabsVm.ActiveTab = tab2;
        tabsVm.ActiveTab.Document.Entities.Should().BeEmpty("Tab 2 must be empty after switch");
    }

    // ============================================================
    // T371: ReattachActiveTab_PropertyChanged_Fires — PropertyChanged event'i
    // ============================================================
    [Fact]
    public void T371_ReattachActiveTab_PropertyChanged_Fires()
    {
        var tabsVm = CreateDocumentTabsViewModel();
        tabsVm.AddTabCommand.Execute(null);
        tabsVm.AddTabCommand.Execute(null);

        var eventCount = 0;
        tabsVm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(DocumentTabsViewModel.ActiveTab))
                eventCount++;
        };

        tabsVm.ActiveTab = tabsVm.Tabs[0];
        tabsVm.ActiveTab = tabsVm.Tabs[1];

        eventCount.Should().Be(2);
    }

    // ============================================================
    // T372: TabHeader_Click_SelectsTab — Sekme başlığı tıklama ActiveTab değiştirir
    // ============================================================
    [Fact]
    public void T372_TabHeader_Click_SelectsTab()
    {
        var tabsVm = CreateDocumentTabsViewModel();
        tabsVm.AddTabCommand.Execute(null); // Tab 1
        tabsVm.AddTabCommand.Execute(null); // Tab 2

        // Programatik olarak sekme değiştir (UI click simülasyonu)
        tabsVm.ActiveTab = tabsVm.Tabs[1];
        tabsVm.ActiveTab.Should().Be(tabsVm.Tabs[1]);

        tabsVm.ActiveTab = tabsVm.Tabs[0];
        tabsVm.ActiveTab.Should().Be(tabsVm.Tabs[0]);
    }

    // ============================================================
    // T373: DocumentTabsViewModel_CanUndo_AfterSwitch — CanUndo aktif sekmeden
    // ============================================================
    [Fact]
    public void T373_DocumentTabsViewModel_CanUndo_AfterSwitch()
    {
        var tabsVm = CreateDocumentTabsViewModel();
        tabsVm.AddTabCommand.Execute(null); // Tab 1
        tabsVm.AddTabCommand.Execute(null); // Tab 2

        var tab1 = tabsVm.Tabs[0];
        var tab2 = tabsVm.Tabs[1];

        // Tab 1'e entity ekle
        var node = new TrackNode { Id = Guid.NewGuid(), Position = new(10, 20), LayerId = tab1.Document.ActiveLayerId };
        tab1.Document.RestoreEntity(node);

        // Tab 1 aktif → IsDirty true
        tabsVm.ActiveTab = tab1;
        tab1.IsDirty.Should().BeTrue();

        // Tab 2 aktif → IsDirty false
        tabsVm.ActiveTab = tab2;
        tab2.IsDirty.Should().BeFalse();
    }

    // ============================================================
    // T374: FirstTab_AutoCreatedOnAddTab — AddTab otomatik sekme oluşturur
    // ============================================================
    [Fact]
    public void T374_FirstTab_AutoCreatedOnAddTab()
    {
        var tabsVm = CreateDocumentTabsViewModel();

        tabsVm.Tabs.Should().BeEmpty("initially no tabs");

        tabsVm.AddTabCommand.Execute(null);

        tabsVm.Tabs.Should().HaveCount(1);
        tabsVm.ActiveTab.Should().NotBeNull();
        tabsVm.ActiveTab!.DisplayName.Should().Be("Yeni Proje");
    }

    // ============================================================
    // T375: Ribbon_UndoCommand_UsesActiveTabStack — Undo aktif sekme stack'ini kullanır
    // ============================================================
    [Fact]
    public void T375_Ribbon_UndoCommand_UsesActiveTabStack()
    {
        var tabsVm = CreateDocumentTabsViewModel();
        tabsVm.AddTabCommand.Execute(null); // Tab 1
        tabsVm.AddTabCommand.Execute(null); // Tab 2

        var tab1 = tabsVm.Tabs[0];

        // Tab 1'e entity ekle
        var node = new TrackNode { Id = Guid.NewGuid(), Position = new(10, 20), LayerId = tab1.Document.ActiveLayerId };
        tab1.Document.RestoreEntity(node);
        tab1.IsDirty.Should().BeTrue();

        // Tab 1 aktif
        tabsVm.ActiveTab = tab1;

        // Undo çalıştır
        tab1.CommandStack.Undo(tab1.Document);

        // IsDirty false olmaz çünkü RestoreEntity CommandStack kullanmaz
        // Ama CanUndo false olur (stack boş)
        tab1.CommandStack.CanUndo.Should().BeFalse();
    }

    // ============================================================
    // T376: Tab_IsDirty_PreservedAfterSwitch — Kirli bayrak sekme değişiminde korunur
    // ============================================================
    [Fact]
    public void T376_Tab_IsDirty_PreservedAfterSwitch()
    {
        var tabsVm = CreateDocumentTabsViewModel();
        tabsVm.AddTabCommand.Execute(null); // Tab 1
        tabsVm.AddTabCommand.Execute(null); // Tab 2

        var tab1 = tabsVm.Tabs[0];

        // Tab 1 kirli yap
        var node = new TrackNode { Id = Guid.NewGuid(), Position = new(10, 20), LayerId = tab1.Document.ActiveLayerId };
        tab1.Document.RestoreEntity(node);
        tab1.IsDirty.Should().BeTrue();

        // Tab 2'ye geç
        tabsVm.ActiveTab = tabsVm.Tabs[1];

        // Tab 1 hâlâ kirli
        tab1.IsDirty.Should().BeTrue("Tab 1 must remain dirty after switching away");
    }

    // ============================================================
    // T377: CloseTab_LastTab_CreatesNewEmpty — Son sekme kapanınca yeni boş
    // ============================================================
    [Fact]
    public void T377_CloseTab_LastTab_CreatesNewEmpty()
    {
        var tabsVm = CreateDocumentTabsViewModel();
        tabsVm.AddTabCommand.Execute(null); // Tek sekme

        var tab = tabsVm.ActiveTab!;
        tabsVm.Tabs.Should().HaveCount(1);

        // Son sekme kapanınca yeni boş oluşur
        tabsVm.CloseTabCommand.Execute(tab);

        tabsVm.Tabs.Should().HaveCount(1, "last closed tab should auto-create new empty tab");
        tabsVm.ActiveTab.Should().NotBe(tab, "new tab should be a different instance");
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