using System;
using System.Linq;
using FluentAssertions;
using Xunit;
using TrainService.Cad;
using TrainService.Cad.UndoRedo;

namespace TrainService.App.Tests;

/// <summary>
/// v3.0.29.9 — Katman Yönetimi testleri.
/// T410–T417: Katman aktif/visibility/lock + araç LayerId doğrulama.
/// </summary>
public sealed class T410_T417_LayerTests
{
    // ============================================================
    // T410: CadDocument_ActiveLayer_DefaultZemin — Constructor'da aktif katman Zemin
    // ============================================================
    [Fact]
    public void T410_CadDocument_ActiveLayer_DefaultZemin()
    {
        var doc = new CadDocument();

        doc.ActiveLayerId.Should().Be(CadDocument.SabitKatmanlar.Zemin, "default active layer should be Zemin");
    }

    // ============================================================
    // T411: CadDocument_SetActiveLayer_ChangesActiveLayerId — SetActiveLayer çalışır
    // ============================================================
    [Fact]
    public void T411_CadDocument_SetActiveLayer_ChangesActiveLayerId()
    {
        var doc = new CadDocument();

        doc.SetActiveLayer(CadDocument.SabitKatmanlar.UstKat);

        doc.ActiveLayerId.Should().Be(CadDocument.SabitKatmanlar.UstKat);
    }

    // ============================================================
    // T412: CadDocument_LayerVisibility_HidesEntities — IsVisible=false → entity gizlenir
    // ============================================================
    [Fact]
    public void T412_CadDocument_LayerVisibility_HidesEntities()
    {
        var doc = new CadDocument();
        var layerId = CadDocument.SabitKatmanlar.Zemin;
        var node = new TrainService.Core.Entities.TrackNode { Position = new(0, 0), LayerId = layerId };
        doc.RestoreEntity(node);

        doc.IsVisible(node.Id).Should().BeTrue("entity on visible layer should be visible");

        doc.SetLayerVisibility(layerId, false);

        doc.IsVisible(node.Id).Should().BeFalse("entity on hidden layer should not be visible");
    }

    // ============================================================
    // T413: CadDocument_LayerLock_PreventsSelection — IsLocked=true → entity seçilemez
    // ============================================================
    [Fact]
    public void T413_CadDocument_LayerLock_PreventsSelection()
    {
        var doc = new CadDocument();
        var layerId = CadDocument.SabitKatmanlar.Zemin;
        var node = new TrainService.Core.Entities.TrackNode { Position = new(0, 0), LayerId = layerId };
        doc.RestoreEntity(node);

        doc.IsSelectable(node.Id).Should().BeTrue("entity on unlocked layer should be selectable");

        doc.SetLayerLock(layerId, true);

        doc.IsSelectable(node.Id).Should().BeFalse("entity on locked layer should not be selectable");
    }

    // ============================================================
    // T414: CadDocument_Layers_Count — 3 katman var
    // ============================================================
    [Fact]
    public void T414_CadDocument_Layers_Count()
    {
        var doc = new CadDocument();

        doc.Layers.Should().HaveCount(3, "document should have 3 default layers");
    }

    // ============================================================
    // T415: CadDocument_LayerNames — Katman isimleri doğru
    // ============================================================
    [Fact]
    public void T415_CadDocument_LayerNames()
    {
        var doc = new CadDocument();

        var names = doc.Layers.Select(l => l.Name).ToList();

        names.Should().Contain("Zemin");
        names.Should().Contain("Alt Kat");
        names.Should().Contain("Üst Kat");
    }

    // ============================================================
    // T416: CadDocument_SetActiveLayer_InvalidId_Ignored — Geçersiz ID ignore edilir
    // ============================================================
    [Fact]
    public void T416_CadDocument_SetActiveLayer_InvalidId_Ignored()
    {
        var doc = new CadDocument();
        var original = doc.ActiveLayerId;

        doc.SetActiveLayer(Guid.NewGuid());

        doc.ActiveLayerId.Should().Be(original, "invalid layer ID should be ignored");
    }

    // ============================================================
    // T417: CadDocument_LayerZHeight — Katman Z yükseklikleri doğru
    // ============================================================
    [Fact]
    public void T417_CadDocument_LayerZHeight()
    {
        var doc = new CadDocument();

        var zemin = doc.Layers.First(l => l.Id == CadDocument.SabitKatmanlar.Zemin);
        var altKat = doc.Layers.First(l => l.Id == CadDocument.SabitKatmanlar.AltKat);
        var ustKat = doc.Layers.First(l => l.Id == CadDocument.SabitKatmanlar.UstKat);

        zemin.ZHeightMm.Should().Be(0);
        altKat.ZHeightMm.Should().Be(-350);
        ustKat.ZHeightMm.Should().Be(400);
    }
}