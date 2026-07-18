using System;
using System.Linq;
using FluentAssertions;
using Xunit;
using TrainService.Cad;
using TrainService.Cad.FeatureTree;
using TrainService.Cad.Selection;
using TrainService.Core.Entities;
using TrainService.Core.Geometry;

namespace TrainService.Cad.Tests;

/// <summary>
/// v3.0.28 Feature Tree testleri.
/// T310-T319: FeatureTreeItem, BuildFeatureTree, ViewModel senkronizasyonu.
/// </summary>
public sealed class T310_FeatureTreeTests
{
    // ============================================================
    // T310: FeatureTreeItem — temel özellikler
    // ============================================================
    [Fact]
    public void T310_FeatureTreeItem_DefaultValues()
    {
        var item = new FeatureTreeItem();
        item.Name.Should().BeEmpty();
        item.EntityId.Should().BeNull();
        item.EntityType.Should().Be("Group");
        item.IsVisible.Should().BeTrue();
        item.IsLocked.Should().BeFalse();
        item.IsSelected.Should().BeFalse();
        item.IsExpanded.Should().BeTrue();
        item.Children.Should().NotBeNull();
        item.Children.Count.Should().Be(0);
    }

    [Fact]
    public void T311_FeatureTreeItem_PropertyChanged_Fires()
    {
        var item = new FeatureTreeItem();
        using var monitor = item.Monitor();

        item.Name = "TestItem";
        item.IsVisible = false;
        item.IsLocked = true;
        item.IsSelected = true;
        item.IsExpanded = false;

        monitor.Should().RaisePropertyChangeFor(x => x.Name);
        monitor.Should().RaisePropertyChangeFor(x => x.IsVisible);
        monitor.Should().RaisePropertyChangeFor(x => x.IsLocked);
        monitor.Should().RaisePropertyChangeFor(x => x.IsSelected);
        monitor.Should().RaisePropertyChangeFor(x => x.IsExpanded);
    }

    [Fact]
    public void T312_FeatureTreeItem_Children_Hierarchy()
    {
        var parent = new FeatureTreeItem { Name = "Parent" };
        var child = new FeatureTreeItem { Name = "Child", EntityId = Guid.NewGuid() };
        parent.Children.Add(child);
        child.Parent = parent;

        parent.Children.Should().Contain(child);
        child.Parent.Should().Be(parent);
    }

    // ============================================================
    // T313: BuildFeatureTree — 5 grup + içerik
    // ============================================================
    [Fact]
    public void T313_BuildFeatureTree_ReturnsFiveGroups()
    {
        var doc = new CadDocument();
        var roots = doc.BuildFeatureTree();

        roots.Should().HaveCount(5);
        roots[0].Name.Should().Be("Katmanlar");
        roots[1].Name.Should().Be("Raylar");
        roots[2].Name.Should().Be("Hatlar");
        roots[3].Name.Should().Be("Makaslar");
        roots[4].Name.Should().Be("Rampalar");
    }

    [Fact]
    public void T314_BuildFeatureTree_Katmanlar_ContainsThreeLayers()
    {
        var doc = new CadDocument();
        var roots = doc.BuildFeatureTree();

        var katmanGroup = roots[0];
        katmanGroup.Children.Should().HaveCount(3);
        katmanGroup.Children[0].Name.Should().Be("Zemin");
        katmanGroup.Children[1].Name.Should().Be("Alt Kat");
        katmanGroup.Children[2].Name.Should().Be("Üst Kat");
    }

    [Fact]
    public void T315_BuildFeatureTree_Raylar_ContainsSegments()
    {
        var doc = new CadDocument();
        var n1 = new TrackNode { Position = new Vector2D(0, 0), LayerId = CadDocument.SabitKatmanlar.Zemin };
        var n2 = new TrackNode { Position = new Vector2D(100, 0), LayerId = CadDocument.SabitKatmanlar.Zemin };
        doc.AddEntity(n1);
        doc.AddEntity(n2);
        var seg = new TrackSegment { StartNodeId = n1.Id, EndNodeId = n2.Id, LayerId = CadDocument.SabitKatmanlar.Zemin };
        doc.AddEntity(seg);

        var roots = doc.BuildFeatureTree();
        var rayGroup = roots[1];
        rayGroup.Children.Should().HaveCount(1);
        rayGroup.Children[0].EntityType.Should().Be("TrackSegment");
        rayGroup.Children[0].EntityId.Should().Be(seg.Id);
    }

    [Fact]
    public void T316_BuildFeatureTree_Hatlar_ContainsRoutes()
    {
        var doc = new CadDocument();
        var route = new Route { Name = "TestRoute", LayerId = CadDocument.SabitKatmanlar.Zemin };
        doc.AddEntity(route);

        var roots = doc.BuildFeatureTree();
        var hatGroup = roots[2];
        hatGroup.Children.Should().HaveCount(1);
        hatGroup.Children[0].EntityType.Should().Be("Route");
        hatGroup.Children[0].EntityId.Should().Be(route.Id);
    }

    [Fact]
    public void T317_BuildFeatureTree_Makaslar_ContainsSwitches()
    {
        var doc = new CadDocument();
        var sw = new RailSwitch { Position = new Vector2D(50, 50), LayerId = CadDocument.SabitKatmanlar.Zemin };
        doc.AddEntity(sw);

        var roots = doc.BuildFeatureTree();
        var makasGroup = roots[3];
        makasGroup.Children.Should().HaveCount(1);
        makasGroup.Children[0].EntityType.Should().Be("RailSwitch");
        makasGroup.Children[0].EntityId.Should().Be(sw.Id);
    }

    [Fact]
    public void T318_BuildFeatureTree_Rampalar_ContainsRamps()
    {
        var doc = new CadDocument();
        var ramp = new Ramp { Position = new Vector2D(30, 30), LayerId = CadDocument.SabitKatmanlar.Zemin };
        doc.AddEntity(ramp);

        var roots = doc.BuildFeatureTree();
        var rampaGroup = roots[4];
        rampaGroup.Children.Should().HaveCount(1);
        rampaGroup.Children[0].EntityType.Should().Be("Ramp");
        rampaGroup.Children[0].EntityId.Should().Be(ramp.Id);
    }

    // ============================================================
    // T319: FeatureTreeViewModel — SelectionService senkronizasyonu
    // ============================================================
    [Fact]
    public void T319_FeatureTreeViewModel_SelectionSync_CanvasToTree()
    {
        var doc = new CadDocument();
        var sel = new SelectionService();
        var vm = new FeatureTreeViewModel(doc, sel);

        // Boş dokümanda 5 grup olmalı
        vm.Roots.Should().HaveCount(5);

        // Entity ekle
        var sw = new RailSwitch { Position = new Vector2D(10, 10), LayerId = CadDocument.SabitKatmanlar.Zemin };
        doc.AddEntity(sw);

        // SelectionService üzerinden seç
        sel.Set(new[] { sw.Id });

        // Ağaçta ilgili item seçili olmalı
        var makasGroup = vm.Roots[3];
        var makasItem = makasGroup.Children.FirstOrDefault(c => c.EntityId == sw.Id);
        makasItem.Should().NotBeNull();
        makasItem!.IsSelected.Should().BeTrue();
    }

    [Fact]
    public void T319b_FeatureTreeViewModel_SelectionSync_TreeToCanvas()
    {
        var doc = new CadDocument();
        var sel = new SelectionService();
        var vm = new FeatureTreeViewModel(doc, sel);

        var sw = new RailSwitch { Position = new Vector2D(10, 10), LayerId = CadDocument.SabitKatmanlar.Zemin };
        doc.AddEntity(sw);

        // Ağaçtan seç
        vm.OnTreeSelectionChanged(sw.Id);

        // SelectionService güncellenmiş olmalı
        sel.SelectedIds.Should().Contain(sw.Id);
    }

    [Fact]
    public void T319c_FeatureTreeViewModel_RebuildTree_OnDocumentChange()
    {
        var doc = new CadDocument();
        var sel = new SelectionService();
        var vm = new FeatureTreeViewModel(doc, sel);

        // Başlangıçta Raylar boş
        vm.Roots[1].Children.Should().HaveCount(0);

        // Segment ekle → ağaç otomatik yenilenmeli
        var n1 = new TrackNode { Position = new Vector2D(0, 0), LayerId = CadDocument.SabitKatmanlar.Zemin };
        var n2 = new TrackNode { Position = new Vector2D(100, 0), LayerId = CadDocument.SabitKatmanlar.Zemin };
        doc.AddEntity(n1);
        doc.AddEntity(n2);
        var seg = new TrackSegment { StartNodeId = n1.Id, EndNodeId = n2.Id, LayerId = CadDocument.SabitKatmanlar.Zemin };
        doc.AddEntity(seg);

        vm.Roots[1].Children.Should().HaveCount(1);
    }
}
