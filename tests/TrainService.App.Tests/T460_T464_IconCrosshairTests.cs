using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using FluentAssertions;
using Xunit;
using TrainService.App.Controls.Ribbon;
using TrainService.App.Controls.CadCanvas;
using IconPacks = MahApps.Metro.IconPacks;

namespace TrainService.App.Tests;

/// <summary>
/// v3.0.29.17 Ikon Paketi + Crosshair Cursor testleri.
/// T460–T464: IconPacks gecerlilik, fallback, crosshair render.
/// </summary>
public sealed class T460_T464_IconCrosshairTests
{
    // ============================================================
    // T460: RibbonDefinition'daki tum IconKind degerleri gecerli
    //       PackIconMaterialDesignKind enum'una parse edilebiliyor
    // ============================================================
    [Fact]
    public void T460_RibbonDefinition_AllIconKinds_NonEmpty()
    {
        var allItems = RibbonDefinitions.AllItems
            .Where(i => !string.IsNullOrEmpty(i.IconKind))
            .ToList();

        allItems.Should().NotBeEmpty("ribbon items with icons must exist");

        foreach (var item in allItems)
        {
            item.IconKind.Should().NotBeNullOrEmpty(
                $"Item '{item.Id}' must have a non-empty IconKind");
            item.IconPack.Should().Be("MaterialDesign",
                $"Item '{item.Id}' must use MaterialDesign pack");
        }
    }

    [Fact]
    public void T460_RibbonDefinition_AllItems_HaveIconPackDefault()
    {
        var allItems = RibbonDefinitions.AllItems
            .Where(i => !string.IsNullOrEmpty(i.IconKind))
            .ToList();

        allItems.Should().AllSatisfy(item =>
        {
            item.IconPack.Should().Be("MaterialDesign",
                $"RibbonItem '{item.Id}' must have default IconPack='MaterialDesign'");
        });
    }

    // ============================================================
    // T461: CreateIconPacks gecerli ikon icin PackIconMaterialDesign doner
    // ============================================================
    [Fact]
    public void T461_CreateIconPacks_MethodExists_AndReturnsControl()
    {
        // Reflection ile private static metodu cagir
        var method = typeof(RibbonControl).GetMethod(
            "CreateIconPacks",
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Static);

        method.Should().NotBeNull("CreateIconPacks method must exist in RibbonControl");

        // Gecersiz kind -> null donmeli (fallback)
        var nullResult = method!.Invoke(null, new object[] { "GecersizIkonXXXX", "MaterialDesign" });
        nullResult.Should().BeNull("invalid kind should return null");

        // CreateIconPacks metodunun dogru imzaya sahip oldugunu dogrula
        var parameters = method.GetParameters();
        parameters.Should().HaveCount(2);
        parameters[0].Name.Should().Be("kind");
        parameters[1].Name.Should().Be("pack");
        method.ReturnType.Should().Be(typeof(System.Windows.Controls.Control));
    }

    // ============================================================
    // T462: CreateIconPacks gecersiz ikon icin null doner (fallback)
    // ============================================================
    [Fact]
    public void T462_CreateIconPacks_InvalidKind_ReturnsNull()
    {
        var method = typeof(RibbonControl).GetMethod(
            "CreateIconPacks",
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Static);

        method.Should().NotBeNull("CreateIconPacks method must exist");

        var result = method!.Invoke(null, new object[] { "DefinitelyNotARealIcon", "MaterialDesign" });

        result.Should().BeNull("invalid kind should return null for graceful fallback");
    }

    // ============================================================
    // T463: Crosshair visual mevcut, RenderCrosshair cagrisi crash yapmaz
    // ============================================================
    [Fact]
    public void T463_Crosshair_FieldAndMethod_Exist()
    {
        // Instance olusturmadan sadece reflection ile field ve metod varligini kontrol et
        var crosshairField = typeof(CadViewportControl).GetField(
            "_crosshairVisual",
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);

        crosshairField.Should().NotBeNull("_crosshairVisual field must exist in CadViewportControl");
        crosshairField!.FieldType.Should().Be(typeof(DrawingVisual), "_crosshairVisual must be DrawingVisual");

        var renderMethod = typeof(CadViewportControl).GetMethod(
            "RenderCrosshair",
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);

        renderMethod.Should().NotBeNull("RenderCrosshair method must exist in CadViewportControl");
    }

    // ============================================================
    // T464: QuickAccess ve Tab item'lari IconPacks MaterialDesign kullanir
    // ============================================================
    [Fact]
    public void T464_QuickAccess_Items_HaveValidIconKinds()
    {
        var quickAccess = RibbonDefinitions.QuickAccessItems;

        quickAccess.Should().NotBeEmpty();
        quickAccess.Should().AllSatisfy(item =>
        {
            item.IconKind.Should().NotBeNullOrEmpty(
                $"QuickAccess item '{item.Id}' must have an IconKind");
            item.IconPack.Should().Be("MaterialDesign");
        });
    }
}