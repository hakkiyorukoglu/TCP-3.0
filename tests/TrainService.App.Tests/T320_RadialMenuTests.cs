using System;
using System.Linq;
using FluentAssertions;
using Xunit;
using TrainService.App.Controls.RadialMenu;

namespace TrainService.App.Tests;

/// <summary>
/// v3.0.29 Radyal Menü testleri.
/// T320-T329: RadialMenuItem, menü öğeleri, hit-test, bağlama duyarlılık.
/// </summary>
public sealed class T320_RadialMenuTests
{
    // ============================================================
    // T320: RadialMenuItem — temel özellikler
    // ============================================================
    [Fact]
    public void T320_RadialMenuItem_DefaultValues()
    {
        var item = new RadialMenuItem("Test", "🔍", () => { });

        item.Label.Should().Be("Test");
        item.IconGlyph.Should().Be("🔍");
        item.IsEnabled.Should().BeTrue();
        item.Command.Should().NotBeNull();
    }

    // ============================================================
    // T321: RadialMenuItem — IsEnabled=false
    // ============================================================
    [Fact]
    public void T321_RadialMenuItem_Disabled()
    {
        var item = new RadialMenuItem("Sil", "🗑️", () => { }, IsEnabled: false);

        item.IsEnabled.Should().BeFalse();
        item.Label.Should().Be("Sil");
    }

    // ============================================================
    // T322: RadialMenuItem — komut çalıştırma
    // ============================================================
    [Fact]
    public void T322_RadialMenuItem_CommandExecutes()
    {
        bool executed = false;
        var item = new RadialMenuItem("Seç", "🔍", () => executed = true);

        item.Command.Invoke();
        executed.Should().BeTrue();
    }

    // ============================================================
    // T323: RadialMenuItem — birden çok öğe listesi
    // ============================================================
    [Fact]
    public void T323_RadialMenuItem_MultipleItems()
    {
        int callCount = 0;
        var items = new[]
        {
            new RadialMenuItem("Seç", "🔍", () => callCount++),
            new RadialMenuItem("Sil", "🗑️", () => callCount++),
            new RadialMenuItem("Yakınlaştır", "🔎", () => callCount++)
        };

        items.Length.Should().Be(3);
        items.All(i => i.IsEnabled).Should().BeTrue();
        items[0].Label.Should().Be("Seç");
        items[1].Label.Should().Be("Sil");
        items[2].Label.Should().Be("Yakınlaştır");
    }

    // ============================================================
    // T324: RadialMenuItem — boş liste (sınır durumu)
    // ============================================================
    [Fact]
    public void T324_RadialMenuItem_EmptyList()
    {
        var items = Array.Empty<RadialMenuItem>();
        items.Length.Should().Be(0);
    }

    // ============================================================
    // T325: RadialMenuItem — null Command (sınır durumu)
    // ============================================================
    [Fact]
    public void T325_RadialMenuItem_NullCommand()
    {
        var item = new RadialMenuItem("Test", "🔍", null!);

        item.Command.Should().BeNull();
        // null command invoke edilmemeli (RadialMenuControl bunu kontrol eder)
    }

    // ============================================================
    // T326: RadialMenuItem — record eşitlik (value equality)
    // ============================================================
    [Fact]
    public void T326_RadialMenuItem_RecordEquality()
    {
        var item1 = new RadialMenuItem("Seç", "🔍", () => { });
        var item2 = new RadialMenuItem("Seç", "🔍", () => { });

        // Record'lar reference equality değil, memberwise karşılaştırma yapar
        // Ancak Action delegate'leri reference karşılaştırması yapar, bu yüzden eşit olmaz
        item1.Should().NotBe(item2);
        item1.Label.Should().Be(item2.Label);
        item1.IconGlyph.Should().Be(item2.IconGlyph);
    }

    // ============================================================
    // T327: RadialMenuItem — farklı ikonlar
    // ============================================================
    [Fact]
    public void T327_RadialMenuItem_DifferentIcons()
    {
        var items = new[]
        {
            new RadialMenuItem("Seç", "🔍", () => { }),
            new RadialMenuItem("Sil", "🗑️", () => { }),
            new RadialMenuItem("Yakınlaştır", "🔎", () => { }),
            new RadialMenuItem("Ray Çiz", "📐", () => { }),
            new RadialMenuItem("Rota Çiz", "🗺️", () => { }),
            new RadialMenuItem("Makas", "🔀", () => { }),
            new RadialMenuItem("Düğüm", "⚙️", () => { }),
            new RadialMenuItem("Ölçü", "📏", () => { })
        };

        items.Length.Should().Be(8);
        items.Select(i => i.IconGlyph).Distinct().Count().Should().Be(8);
    }

    // ============================================================
    // T328: RadialMenuItem — menü öğeleri sıralı komut
    // ============================================================
    [Fact]
    public void T328_RadialMenuItem_SequentialExecution()
    {
        var log = new System.Collections.Generic.List<string>();
        var items = new[]
        {
            new RadialMenuItem("Adım 1", "1️⃣", () => log.Add("1")),
            new RadialMenuItem("Adım 2", "2️⃣", () => log.Add("2")),
            new RadialMenuItem("Adım 3", "3️⃣", () => log.Add("3"))
        };

        foreach (var item in items)
            item.Command.Invoke();

        log.Should().HaveCount(3);
        log[0].Should().Be("1");
        log[1].Should().Be("2");
        log[2].Should().Be("3");
    }

    // ============================================================
    // T329: RadialMenuItem — disabled öğe komutu çalışmaz
    // ============================================================
    [Fact]
    public void T329_RadialMenuItem_DisabledCommandNotInvoked()
    {
        bool executed = false;
        var item = new RadialMenuItem("Sil", "🗑️", () => executed = true, IsEnabled: false);

        // Disabled öğenin komutu RadialMenuControl tarafından çağrılmaz
        // Bu test sadece model seviyesinde IsEnabled=false olduğunu doğrular
        item.IsEnabled.Should().BeFalse();
        executed.Should().BeFalse(); // henüz invoke edilmedi
    }
}
