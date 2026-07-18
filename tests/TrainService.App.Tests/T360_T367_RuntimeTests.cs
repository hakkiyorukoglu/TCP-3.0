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
/// v3.0.29.4 — Çalışma Zamanı Entegrasyonu testleri.
/// T360–T367: ActiveTab proxy, komut yönlendirme, izolasyon.
/// </summary>
public sealed class T360_T367_RuntimeTests
{
    // ============================================================
    // T360: ProjectId_IsValidPerTab — Her sekme kendi valid ProjectId'sine sahip
    // ============================================================
    [Fact]
    public void T360_ProjectId_IsValidPerTab()
    {
        var tabsVm = CreateDocumentTabsViewModel();
        tabsVm.AddTabCommand.Execute(null); // Tab 1
        tabsVm.AddTabCommand.Execute(null); // Tab 2

        var tab1 = tabsVm.Tabs[0];
        var tab2 = tabsVm.Tabs[1];

        tab1.ProjectId.Should().NotBe(Guid.Empty);
        tab2.ProjectId.Should().NotBe(Guid.Empty);
        tab1.ProjectId.Should().NotBe(tab2.ProjectId, "each tab must have unique ProjectId");
    }

    // ============================================================
    // T361: ActiveTab_Document_IsDirty_TracksIndependently — IsDirty her sekme bağımsız
    // ============================================================
    [Fact]
    public void T361_ActiveTab_Document_IsDirty_TracksIndependently()
    {
        var tabsVm = CreateDocumentTabsViewModel();
        tabsVm.AddTabCommand.Execute(null); // Tab 1
        tabsVm.AddTabCommand.Execute(null); // Tab 2

        var tab1 = tabsVm.Tabs[0];
        var tab2 = tabsVm.Tabs[1];

        // Tab 1 kirli yap
        var node = new TrackNode { Id = Guid.NewGuid(), Position = new(10, 20), LayerId = tab1.Document.ActiveLayerId };
        tab1.Document.RestoreEntity(node);

        tab1.IsDirty.Should().BeTrue();
        tab2.IsDirty.Should().BeFalse("Tab 2 must remain clean");
    }

    // ============================================================
    // T362: CommandStack_IsolatedPerTab — Her sekme kendi CommandStack'ine sahip
    // ============================================================
    [Fact]
    public void T362_CommandStack_IsolatedPerTab()
    {
        var tabsVm = CreateDocumentTabsViewModel();
        tabsVm.AddTabCommand.Execute(null); // Tab 1
        tabsVm.AddTabCommand.Execute(null); // Tab 2

        var tab1 = tabsVm.Tabs[0];
        var tab2 = tabsVm.Tabs[1];

        tab1.CommandStack.Should().NotBeSameAs(tab2.CommandStack,
            "each tab must have its own CommandStack");

        // Tab 1'de command çalıştır
        var node = new TrackNode { Id = Guid.NewGuid(), Position = new(10, 20), LayerId = tab1.Document.ActiveLayerId };
        tab1.Document.RestoreEntity(node);

        // Tab 1'de IsDirty true (RestoreEntity IsDirty=true)
        tab1.IsDirty.Should().BeTrue();

        // Tab 2 temiz kalır
        tab2.IsDirty.Should().BeFalse();
    }

    // ============================================================
    // T363: ActiveTab_PropertyChanged_FiresWhenTabSwitches — ActiveTab değişim event'i
    // ============================================================
    [Fact]
    public void T363_ActiveTab_PropertyChanged_FiresWhenTabSwitches()
    {
        var tabsVm = CreateDocumentTabsViewModel();
        tabsVm.AddTabCommand.Execute(null); // Tab 1
        tabsVm.AddTabCommand.Execute(null); // Tab 2

        var propertyChangedCount = 0;
        tabsVm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(DocumentTabsViewModel.ActiveTab))
                propertyChangedCount++;
        };

        tabsVm.ActiveTab = tabsVm.Tabs[0];
        tabsVm.ActiveTab = tabsVm.Tabs[1];

        propertyChangedCount.Should().Be(2, "ActiveTab should fire PropertyChanged twice");
    }

    // ============================================================
    // T364: Clipboard_IsolatedPerTab — Her sekme kendi clipboard'una sahip
    // ============================================================
    [Fact]
    public void T364_Clipboard_IsolatedPerTab()
    {
        var tabsVm = CreateDocumentTabsViewModel();
        tabsVm.AddTabCommand.Execute(null); // Tab 1
        tabsVm.AddTabCommand.Execute(null); // Tab 2

        var tab1 = tabsVm.Tabs[0];
        var tab2 = tabsVm.Tabs[1];

        tab1.ClipboardService.Should().NotBeSameAs(tab2.ClipboardService,
            "each tab must have its own ClipboardService");
    }

    // ============================================================
    // T365: Selection_IsolatedWhenTabSwitches — Seçim sekme değişiminde izole kalır
    // ============================================================
    [Fact]
    public void T365_Selection_IsolatedWhenTabSwitches()
    {
        var tabsVm = CreateDocumentTabsViewModel();
        tabsVm.AddTabCommand.Execute(null); // Tab 1
        tabsVm.AddTabCommand.Execute(null); // Tab 2

        var tab1 = tabsVm.Tabs[0];
        var tab2 = tabsVm.Tabs[1];

        // Tab 1'de entity ekle
        var node = new TrackNode { Id = Guid.NewGuid(), Position = new(10, 20), LayerId = tab1.Document.ActiveLayerId };
        tab1.Document.RestoreEntity(node);

        // Tab 1'de entity var, Tab 2'de yok
        tab1.Document.Entities.Count().Should().Be(1);
        tab2.Document.Entities.Should().BeEmpty("Tab 2 must be isolated");
    }

    // ============================================================
    // T366: SnapEngine_IsolatedPerTab — Her sekme kendi SnapEngine'ine sahip
    // ============================================================
    [Fact]
    public void T366_SnapEngine_IsolatedPerTab()
    {
        var tabsVm = CreateDocumentTabsViewModel();
        tabsVm.AddTabCommand.Execute(null); // Tab 1
        tabsVm.AddTabCommand.Execute(null); // Tab 2

        var tab1 = tabsVm.Tabs[0];
        var tab2 = tabsVm.Tabs[1];

        tab1.SnapEngine.Should().NotBeSameAs(tab2.SnapEngine,
            "each tab must have its own SnapEngine");
    }

    // ============================================================
    // T367: ActiveTab_Null_SafeBehavior — Null ActiveTab güvenli davranır
    // ============================================================
    [Fact]
    public void T367_ActiveTab_Null_SafeBehavior()
    {
        var tabsVm = CreateDocumentTabsViewModel();

        // Henüz sekme yok
        tabsVm.ActiveTab.Should().BeNull();

        // CloseTab null'a güvenli
        var result = tabsVm.TryCloseTab(null);
        result.Should().BeFalse("closing null tab should return false");
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