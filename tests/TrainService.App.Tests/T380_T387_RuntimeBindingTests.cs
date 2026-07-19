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
/// v3.0.29.6 — Gerçek çalışma zamanı entegrasyonu testleri.
/// T380–T387: DocumentTabsViewModel + EditorTabModel runtime davranışı.
/// </summary>
public sealed class T380_T387_RuntimeBindingTests
{
    // ============================================================
    // T380: DocumentTabsViewModel_AddTab_CreatesActiveTab — AddTab ActiveTab oluşturur
    // ============================================================
    [Fact]
    public void T380_DocumentTabsViewModel_AddTab_CreatesActiveTab()
    {
        var tabsVm = CreateDocumentTabsViewModel();
        tabsVm.AddTabCommand.Execute(null);

        tabsVm.ActiveTab.Should().NotBeNull();
        tabsVm.ActiveTab!.DisplayName.Should().Be("Yeni Proje");
    }

    // ============================================================
    // T381: ActiveTab_Null_PropertiesSafe — Null ActiveTab güvenli davranır
    // ============================================================
    [Fact]
    public void T381_ActiveTab_Null_PropertiesSafe()
    {
        var tabsVm = CreateDocumentTabsViewModel();

        tabsVm.ActiveTab.Should().BeNull();

        // Null ActiveTab ile property erişimi güvenli
        tabsVm.Tabs.Should().BeEmpty();
    }

    // ============================================================
    // T382: DocumentTabsViewModel_AddTab_SetsActiveTab — AddTab ActiveTab'i ayarlar
    // ============================================================
    [Fact]
    public void T382_DocumentTabsViewModel_AddTab_SetsActiveTab()
    {
        var tabsVm = CreateDocumentTabsViewModel();

        tabsVm.ActiveTab.Should().BeNull();

        tabsVm.AddTabCommand.Execute(null);

        tabsVm.ActiveTab.Should().NotBeNull();
        tabsVm.ActiveTab!.DisplayName.Should().Be("Yeni Proje");
    }

    // ============================================================
    // T383: DocumentTabsViewModel_AddMultipleTabs_ActiveTabIsLast — Çoklu sekmede son eklenen aktif
    // ============================================================
    [Fact]
    public void T383_DocumentTabsViewModel_AddMultipleTabs_ActiveTabIsLast()
    {
        var tabsVm = CreateDocumentTabsViewModel();
        tabsVm.AddTabCommand.Execute(null); // Tab 1
        tabsVm.AddTabCommand.Execute(null); // Tab 2

        tabsVm.Tabs.Should().HaveCount(2);
        tabsVm.ActiveTab.Should().Be(tabsVm.Tabs[1], "last added tab should be active");
    }

    // ============================================================
    // T384: DocumentTabsViewModel_CloseTab_SwitchesActiveTab — CloseTab aktif sekmeyi değiştirir
    // ============================================================
    [Fact]
    public void T384_DocumentTabsViewModel_CloseTab_SwitchesActiveTab()
    {
        var tabsVm = CreateDocumentTabsViewModel();
        tabsVm.AddTabCommand.Execute(null); // Tab 1
        tabsVm.AddTabCommand.Execute(null); // Tab 2

        var tab1 = tabsVm.Tabs[0];
        var tab2 = tabsVm.Tabs[1];

        tabsVm.ActiveTab = tab2;

        // Tab 2'yi kapat
        tabsVm.CloseTabCommand.Execute(tab2);

        // Aktif sekme Tab 1 olmalı
        tabsVm.ActiveTab.Should().Be(tab1, "closing active tab should switch to previous");
    }

    // ============================================================
    // T385: EditorTabModel_HasAllRequiredServices — Her sekme tüm servislere sahip
    // ============================================================
    [Fact]
    public void T385_EditorTabModel_HasAllRequiredServices()
    {
        var tabsVm = CreateDocumentTabsViewModel();
        tabsVm.AddTabCommand.Execute(null);

        var tab = tabsVm.ActiveTab!;

        tab.Document.Should().NotBeNull();
        tab.CommandStack.Should().NotBeNull();
        tab.SelectionService.Should().NotBeNull();
        tab.SnapEngine.Should().NotBeNull();
        tab.ClipboardService.Should().NotBeNull();
        tab.ProjectId.Should().NotBe(Guid.Empty);
    }

    // ============================================================
    // T386: DocumentTabsViewModel_Tabs_CollectionChanged — Tabs koleksiyon değişimi
    // ============================================================
    [Fact]
    public void T386_DocumentTabsViewModel_Tabs_CollectionChanged()
    {
        var tabsVm = CreateDocumentTabsViewModel();

        var collectionChangedCount = 0;
        tabsVm.Tabs.CollectionChanged += (s, e) => collectionChangedCount++;

        tabsVm.AddTabCommand.Execute(null);
        tabsVm.AddTabCommand.Execute(null);

        collectionChangedCount.Should().Be(2, "AddTab should fire CollectionChanged twice");
    }

    // ============================================================
    // T387: EditorTabModel_ProjectId_IsValid — ProjectId geçerli
    // ============================================================
    [Fact]
    public void T387_EditorTabModel_ProjectId_IsValid()
    {
        var tabsVm = CreateDocumentTabsViewModel();
        tabsVm.AddTabCommand.Execute(null);

        var tab = tabsVm.ActiveTab!;
        tab.ProjectId.Should().NotBe(Guid.Empty);
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