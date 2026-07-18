# v3.0.27 SwitchTool — Uygulama Planı

> **Kaynak:** [`Roadmap hy.md`](../Roadmap%20hy.md) — v3.0.27
> **Mikro-İstek:** "1 tıklamada prefab yerleştirme (RailSwitch + 3 port düğümü: Entry/MainExit/DivergingExit), SwitchTool, PreviewSwitchPlace ghost, CompositeCadCommand (tek undo), BoundServoDeviceId."
> **Önceki Sürüm:** [`v3.0.26`](v3026_switchtool_plan.md) — RampTool (rama prefab) — delivered, pushed
> **Plan dosyası:** `plans/v3027_switchtool_plan.md`

---

## 1. Durum Tespiti: v3.0.26'dan Gelenler

v3.0.26 SwitchTool planı aslında **mevcut düğüme 3 tıklamayla makas atama** (existing-node) yaklaşımıydı. Ancak roadmap swap sonrası v3.0.26 = RampTool olarak teslim edildi, SwitchTool v3.0.27'ye taşındı.

**Kritik fark:** Yeni roadmap'de v3.0.27 SwitchTool, **RampTool gibi 1-click prefab** (yeni entity oluşturma) yaklaşımıdır — mevcut düğüme tıklama değil.

### v3.0.26'dan Kalan Altyapı (SwitchTool için HAZIR)

| # | Bileşen | Dosya | Durum |
|---|---------|-------|-------|
| 1 | `RailSwitch` entity (Position, RotationDeg, EntryNodeId, MainExitNodeId, DivergingExitNodeId, State, BoundServoDeviceId) | [`DomainEntities.cs:44-53`](../../src/TrainService.Core/Entities/DomainEntities.cs) | ✅ HAZIR |
| 2 | `PreviewSwitchPlace` record | [`ITool.cs:42-48`](../../src/TrainService.Cad/Tools/ITool.cs) | ✅ HAZIR |
| 3 | `TrackGraph.Build(nodes, segments, switches)` overload | [`TrackGraph.cs:51-62`](../../src/TrainService.Core/Topology/TrackGraph.cs) | ✅ HAZIR |
| 4 | `TrackGraph.IsSwitchPort()`, `GetSwitchState()`, `GetSwitchForPort()` | [`TrackGraph.cs:64-88`](../../src/TrainService.Core/Topology/TrackGraph.cs) | ✅ HAZIR |
| 5 | `CadColors` — SwitchMarkerFill/Pen, SwitchNodeFill/Pen, SwitchMainPen, SwitchDivergingPen | [`CadColors.cs:28-36`](../../src/TrainService.App/Resources/CadColors.cs) | ✅ HAZIR |
| 6 | `CadViewportControl` — PreviewSwitchPlace Y-shaped ghost render (3 port circles + 2 lines) | [`CadViewportControl.cs:375-389`](../../src/TrainService.App/Controls/CadCanvas/CadViewportControl.cs) | ✅ HAZIR |
| 7 | `CadViewportControl` — RailSwitch diamond marker model render | [`CadViewportControl.cs:508-526`](../../src/TrainService.App/Controls/CadCanvas/CadViewportControl.cs) | ✅ HAZIR |
| 8 | `CadViewportControl` — SwitchNode role highlight (magenta) | [`CadViewportControl.cs:494-503`](../../src/TrainService.App/Controls/CadCanvas/CadViewportControl.cs) | ✅ HAZIR |
| 9 | EditorView.xaml — Switch toolbar button (BranchFork24 icon, F8 tooltip) | [`EditorView.xaml:62-64`](../../src/TrainService.App/Views/Pages/EditorView.xaml) | ✅ HAZIR |
| 10 | EditorView.xaml.cs — F8 → SwitchTool() shortcut | [`EditorView.xaml.cs:41-46,99`](../../src/TrainService.App/Views/Pages/EditorView.xaml.cs) | ✅ HAZIR |
| 11 | `NodeRole.SwitchNode` enum value | [`Enums`](../src/TrainService.Core/Enums/) | ✅ HAZIR |
| 12 | `SwitchState.Main` / `SwitchState.Diverging` enum | [`Enums`](../src/TrainService.Core/Enums/) | ✅ HAZIR |

### v3.0.27'de Oluşturulacaklar

| # | Bileşen | Dosya | İşlem |
|---|---------|-------|-------|
| F1 | `SwitchDefaults.cs` — geometri sabitleri | [`../../src/TrainService.Cad/SwitchDefaults.cs`](../../src/TrainService.Cad/SwitchDefaults.cs) | **YENİ** |
| F2 | `SwitchTool.cs` — 1-click prefab tool | [`../../src/TrainService.Cad/Tools/SwitchTool.cs`](../../src/TrainService.Cad/Tools/SwitchTool.cs) | **YENİ** |
| F3 | `T280_SwitchToolTests.cs` — 9 test (T296–T304) | [`../../tests/TrainService.Cad.Tests/Tools/T280_SwitchToolTests.cs`](../../tests/TrainService.Cad.Tests/Tools/T280_SwitchToolTests.cs) | **YENİ** |
| F4 | T010 eşik güncellemesi: 125 → 134 | [`../../tests/TrainService.Architecture.Tests/T010_KapsamBekcisi.cs`](../../tests/TrainService.Architecture.Tests/T010_KapsamBekcisi.cs) | **DEĞİŞİKLİK** |
| F5 | `muhur-v3027.ps1` — mühür script | [`../../tools/muhur-v3027.ps1`](../../tools/muhur-v3027.ps1) | **YENİ** |
| F6 | `sapma.txt` — v3.0.27 sapma kaydı | [`../../tools/sapma.txt`](../../tools/sapma.txt) | **DEĞİŞİKLİK** |
| F7 | `README.md` — Sürüm Geçmişi güncellemesi | [`../../README.md`](../../README.md) | **DEĞİŞİKLİK** |

---

## 2. Kapsam Sınırı (Scope Guard)

### BU SÜRÜMDE VAR
- `SwitchTool.cs` — 1-click prefab tool (RampTool pattern)
- `SwitchDefaults.cs` — RailSwitch geometri sabitleri (RampDefaults pattern)
- `T280_SwitchToolTests.cs` — 9 test (T296–T304)
- T010 eşik: 125 → 134
- `tools/muhur-v3027.ps1`
- `tools/sapma.txt` v3.0.27 kaydı
- `README.md` sürüm geçmişi

### BU SÜRÜMDE YOK (dokunmak yasak)
- `RailSwitch` entity'sinde değişiklik (A2 arteri — zaten hazır, dokunulmaz)
- `ITool` arayüzünde değişiklik (PreviewSwitchPlace zaten var)
- `CadViewportControl.cs` — render kodu zaten hazır
- `CadColors.cs` — renkler zaten hazır
- `EditorView.xaml` / `EditorView.xaml.cs` — UI zaten hazır
- `TrackGraph` değişikliği (A1 arteri)
- Varolan testlerde değişiklik (T010 hariç, T001–T295)
- Mimarî bekçiler (T001–T008)
- WPF katmanı / UI değişikliği
- App projesine dokunulmaz (render zaten hazır)
- `SetNodeRoleCommand` — mevcut düğüm modeli kullanılmıyor, prefab yeni düğüm oluşturur

---

## 3. Tasarım Kararları

### 3.1 1-Click Prefab Yaklaşımı (RampTool Pattern)

SwitchTool, [`RampTool`](../../src/TrainService.Cad/Tools/RampTool.cs) ile aynı deseni kullanır:

1. **OnPointerMove:** Fare pozisyonuna göre `PreviewSwitchPlace` ghost oluştur
2. **OnPointerDown (Left):** 4 entity oluştur (3 TrackNode + 1 RailSwitch)
3. **CompositeCadCommand:** Tümünü tek undo adımında topla
4. **SetSelection:** Yeni RailSwitch seçili gelsin
5. **Escape:** Preview'i gizle

### 3.2 Geometri — SwitchDefaults

```
Rotation=0 varsayılan yön: Entry solda (-X), MainExit sağda (+X), DivergingExit sağ-üstte (+X,+Y)

            MainExit (HalfLength, 0)
           /
Entry ---(+) 
           \
            DivergingExit (HalfLength*cos(divAngle), HalfLength*sin(divAngle))
```

| Sabit | Değer | Açıklama |
|-------|-------|----------|
| `LengthMm` | 80.0 | Toplam makas boyu (mm) — ramp (100) 'den kısa, gerçekçi |
| `DivergingAngleDeg` | 30.0 | Sapma açısı (derece) — standart turnout açısı |
| `HalfLength` | 40.0 | LengthMm / 2 |
| `EntryOffset(rot)` | `Rotate((-HalfLength, 0), rot)` | Sol port |
| `MainExitOffset(rot)` | `Rotate((HalfLength, 0), rot)` | Sağ düz port |
| `DivergingExitOffset(rot)` | `Rotate((H*cos(divRad), H*sin(divRad)), rot)` | Sağ açılı port |

### 3.3 Entity Oluşturma Sırası

```csharp
// 1. Önce 3 TrackNode (entry, mainExit, divergingExit)
var entryNode = new TrackNode
{
    Position = SwitchDefaults.EntryOffset(rot) + pos,
    Z = 0,
    Role = NodeRole.Plain,
    LayerId = activeLayer
};
var mainExitNode = new TrackNode { ... MainExitOffset ... };
var divergingExitNode = new TrackNode { ... DivergingExitOffset ... };

// 2. Sonra RailSwitch
var railSwitch = new RailSwitch
{
    Position = pos,
    RotationDeg = rot,
    EntryNodeId = entryNode.Id,
    MainExitNodeId = mainExitNode.Id,
    DivergingExitNodeId = divergingExitNode.Id,
    State = SwitchState.Main,
    LayerId = activeLayer
};

// 3. CompositeCadCommand — 4 entity tek undo
var cmds = new List<ICadCommand>
{
    new AddEntityCommand(entryNode),
    new AddEntityCommand(mainExitNode),
    new AddEntityCommand(divergingExitNode),
    new AddEntityCommand(railSwitch)
};
var composite = new CompositeCadCommand("Makas Yerleştir", cmds);
ctx.Commands.Do(composite, ctx.Document);

// 4. Yeni switch seçili gelsin
ctx.Selection.Set(new[] { railSwitch.Id });
```

### 3.4 Neden SetNodeRoleCommand Gerekmez?

v3.0.26'nın orijinal SwitchTool planı mevcut bir `TrackNode`'un `Role`'ünü `SwitchNode`'a çeviriyordu. Yeni prefab yaklaşımı **yeni TrackNode'lar oluşturur**, Role'leri `Plain` kalır. `RailSwitch` entity'si zaten 3 port Id'sini taşır — bağlı portlar `TrackGraph.IsSwitchPort()` ile tespit edilir.

### 3.5 Preview Tasarımı

`PreviewSwitchPlace` zaten [`ITool.cs:42-48`](../../src/TrainService.Cad/Tools/ITool.cs) adresinde hazır:

```csharp
public sealed record PreviewSwitchPlace(
    Vector2D Position,
    double RotationDeg,
    Vector2D EntryPos,
    Vector2D MainExitPos,
    Vector2D DivergingExitPos
) : PreviewShape;
```

Tool'un OnPointerMove'u bu record'u doldurur.

---

## 4. Dosya-Dosya Değişiklik Listesi

### F1: [`SwitchDefaults.cs`](../../src/TrainService.Cad/SwitchDefaults.cs) — YENİ

```csharp
using System;
using TrainService.Core.Geometry;

namespace TrainService.Cad;

/// <summary>
/// RailSwitch prefab geometri sabitleri ve yardımcı offset hesaplamaları.
/// Rotation=0 varsayılan yön: Entry solda (-X), MainExit sağda (+X),
/// DivergingExit sağ-üstte (+X, +Y, DivergingAngleDeg=30).
/// </summary>
public static class SwitchDefaults
{
    /// <summary>Toplam makas boyu (mm).</summary>
    public const double LengthMm = 80.0;

    /// <summary>Sapma açısı (derece).</summary>
    public const double DivergingAngleDeg = 30.0;

    /// <summary>Merkezden portlara yarım boy (mm).</summary>
    public const double HalfLength = LengthMm / 2;

    /// <summary>Giriş portunun merkeze göre ofseti (Rotation=0 için sol).</summary>
    public static Vector2D EntryOffset(double rotDeg) =>
        Rotate(new Vector2D(-HalfLength, 0), rotDeg);

    /// <summary>Ana çıkış portunun ofseti (Rotation=0 için sağ, düz).</summary>
    public static Vector2D MainExitOffset(double rotDeg) =>
        Rotate(new Vector2D(HalfLength, 0), rotDeg);

    /// <summary>Sapma çıkış portunun ofseti (Rotation=0 için sağ-üst).</summary>
    public static Vector2D DivergingExitOffset(double rotDeg)
    {
        double rad = DivergingAngleDeg * Math.PI / 180;
        return Rotate(new Vector2D(HalfLength * Math.Cos(rad), HalfLength * Math.Sin(rad)), rotDeg);
    }

    /// <summary>2D vektörü derece cinsinden döndürür.</summary>
    private static Vector2D Rotate(Vector2D v, double deg)
    {
        double rad = deg * Math.PI / 180;
        double cos = Math.Cos(rad);
        double sin = Math.Sin(rad);
        return new Vector2D(v.X * cos - v.Y * sin, v.X * sin + v.Y * cos);
    }
}
```

### F2: [`SwitchTool.cs`](../../src/TrainService.Cad/Tools/SwitchTool.cs) — YENİ

```csharp
using System;
using System.Collections.Generic;
using TrainService.Cad.Snapping;
using TrainService.Cad.UndoRedo;
using TrainService.Core.Entities;
using TrainService.Core.Enums;
using TrainService.Core.Geometry;

namespace TrainService.Cad.Tools;

/// <summary>
/// SwitchTool — 1 tıkla prefab RailSwitch yerleştirme aracı.
/// Her sol tık, bulunulan pozisyona bir RailSwitch + 3 TrackNode (Entry/MainExit/DivergingExit)
/// oluşturur. Tümü tek CompositeCadCommand ile undo/redo yapılır.
/// Escape: preview'i gizler. Activate/Deactivate: preview sıfırlanır.
/// </summary>
public sealed class SwitchTool : ITool
{
    public string Name => "Switch";

    public PreviewShape? Preview { get; private set; }

    public void Activate(ToolContext ctx) { /* preview yok, move'da oluşur */ }

    public void Deactivate(ToolContext ctx) => Preview = null;

    public void OnPointerMove(SnapResult snapped, ToolContext ctx)
    {
        var pos = snapped.Point;
        double rot = 0; // İlk versiyonda rotation=0 sabit

        Preview = new PreviewSwitchPlace(
            pos, rot,
            SwitchDefaults.EntryOffset(rot) + pos,
            SwitchDefaults.MainExitOffset(rot) + pos,
            SwitchDefaults.DivergingExitOffset(rot) + pos
        );
    }

    public void OnPointerDown(SnapResult snapped, ToolMouseButton button, ToolContext ctx)
    {
        if (button != ToolMouseButton.Left) return;

        var pos = snapped.Point;
        double rot = 0;
        var activeLayer = ctx.Document.ActiveLayerId;

        // 1. Üç TrackNode oluştur
        var entryNode = new TrackNode
        {
            Position = SwitchDefaults.EntryOffset(rot) + pos,
            Z = 0,
            Role = NodeRole.Plain,
            LayerId = activeLayer
        };
        var mainExitNode = new TrackNode
        {
            Position = SwitchDefaults.MainExitOffset(rot) + pos,
            Z = 0,
            Role = NodeRole.Plain,
            LayerId = activeLayer
        };
        var divergingExitNode = new TrackNode
        {
            Position = SwitchDefaults.DivergingExitOffset(rot) + pos,
            Z = 0,
            Role = NodeRole.Plain,
            LayerId = activeLayer
        };

        // 2. RailSwitch entity'si
        var railSwitch = new RailSwitch
        {
            Position = pos,
            RotationDeg = rot,
            EntryNodeId = entryNode.Id,
            MainExitNodeId = mainExitNode.Id,
            DivergingExitNodeId = divergingExitNode.Id,
            State = SwitchState.Main,
            LayerId = activeLayer
        };

        // 3. CompositeCadCommand — tek undo adımı (4 entity)
        var cmds = new List<ICadCommand>
        {
            new AddEntityCommand(entryNode),
            new AddEntityCommand(mainExitNode),
            new AddEntityCommand(divergingExitNode),
            new AddEntityCommand(railSwitch)
        };
        var composite = new CompositeCadCommand("Makas Yerleştir", cmds);
        ctx.Commands.Do(composite, ctx.Document);

        // 4. Yeni switch seçili gelsin
        ctx.Selection.Set(new[] { railSwitch.Id });
    }

    public void OnPointerUp(SnapResult snapped, ToolMouseButton button, ToolContext ctx) { }

    public void OnKeyDown(ToolKey key, ToolContext ctx)
    {
        if (key == ToolKey.Escape)
        {
            Preview = null;
        }
    }
}
```

### F3: [`T280_SwitchToolTests.cs`](../../tests/TrainService.Cad.Tests/Tools/T280_SwitchToolTests.cs) — YENİ

Test helper'ları ve 9 test (T296–T304):

```csharp
using System;
using System.Linq;
using FluentAssertions;
using Xunit;
using TrainService.Core.Entities;
using TrainService.Core.Geometry;
using TrainService.Cad.Tools;
using TrainService.Cad.UndoRedo;
using TrainService.Cad.Selection;
using TrainService.Cad.Snapping;

namespace TrainService.Cad.Tests.Tools;

public class T280_SwitchToolTests
{
    private static (CadDocument doc, CommandStack st, SelectionService sel) Sahne()
    {
        var doc = new CadDocument();
        return (doc, new CommandStack(), new SelectionService());
    }

    private ToolContext CreateCtx(CadDocument doc, CommandStack st, SelectionService sel)
        => new(doc, st, sel) { Clipboard = null! };

    [Fact]
    public void T296_SwitchTool_PointerMove_PreviewOlusur()
    {
        var (doc, st, sel) = Sahne();
        var tool = new SwitchTool();
        var ctx = CreateCtx(doc, st, sel);
        tool.Activate(ctx);

        var pos = new Vector2D(200, 100);
        tool.OnPointerMove(new SnapResult(pos, SnapKind.Grid, null), ctx);

        var preview = tool.Preview.Should().BeOfType<PreviewSwitchPlace>().Subject;
        preview.Position.Should().Be(pos);
        preview.RotationDeg.Should().Be(0);
        // EntryPos = pos + (-HalfLength, 0) = (200-40, 100) = (160, 100)
        preview.EntryPos.Should().Be(new Vector2D(160, 100));
        // MainExitPos = pos + (HalfLength, 0) = (200+40, 100) = (240, 100)
        preview.MainExitPos.Should().Be(new Vector2D(240, 100));
        // DivergingExitPos = pos + (HalfLength*cos(30), HalfLength*sin(30))
        // = (200 + 40*0.866, 100 + 40*0.5) = (234.64, 120)
    }

    [Fact]
    public void T297_SwitchTool_SolTik_EntitylerOlusur()
    {
        var (doc, st, sel) = Sahne();
        var tool = new SwitchTool();
        var ctx = CreateCtx(doc, st, sel);
        tool.Activate(ctx);

        tool.OnPointerMove(new SnapResult(new Vector2D(100, 50), SnapKind.Grid, null), ctx);
        tool.OnPointerDown(new SnapResult(new Vector2D(100, 50), SnapKind.Grid, null), ToolMouseButton.Left, ctx);

        // 3 node + 1 switch = 4 yeni entity
        doc.Entities.Count.Should().Be(4);
        doc.Entities.OfType<TrackNode>().Should().HaveCount(3);
        doc.Entities.OfType<RailSwitch>().Should().HaveCount(1);
    }

    [Fact]
    public void T298_SwitchTool_Commit_TekUndo()
    {
        var (doc, st, sel) = Sahne();
        var tool = new SwitchTool();
        var ctx = CreateCtx(doc, st, sel);
        tool.Activate(ctx);

        tool.OnPointerMove(new SnapResult(new Vector2D(100, 50), SnapKind.Grid, null), ctx);
        tool.OnPointerDown(new SnapResult(new Vector2D(100, 50), SnapKind.Grid, null), ToolMouseButton.Left, ctx);

        int ilkKume = doc.Entities.Count; // 4

        // Tek Ctrl+Z → hepsi gider
        st.Undo(doc);
        doc.Entities.Count.Should().Be(0);
        doc.Entities.OfType<RailSwitch>().Should().BeEmpty();
        doc.Entities.OfType<TrackNode>().Should().BeEmpty();
    }

    [Fact]
    public void T299_SwitchTool_Escape_PreviewGizlenir()
    {
        var (doc, st, sel) = Sahne();
        var tool = new SwitchTool();
        var ctx = CreateCtx(doc, st, sel);
        tool.Activate(ctx);

        tool.OnPointerMove(new SnapResult(new Vector2D(100, 50), SnapKind.Grid, null), ctx);
        tool.Preview.Should().NotBeNull();

        tool.OnKeyDown(ToolKey.Escape, ctx);
        tool.Preview.Should().BeNull();
    }

    [Fact]
    public void T300_SwitchTool_SagTik_YokSayilir()
    {
        var (doc, st, sel) = Sahne();
        var tool = new SwitchTool();
        var ctx = CreateCtx(doc, st, sel);
        tool.Activate(ctx);

        tool.OnPointerMove(new SnapResult(new Vector2D(100, 50), SnapKind.Grid, null), ctx);
        tool.OnPointerDown(new SnapResult(new Vector2D(100, 50), SnapKind.Grid, null), ToolMouseButton.Right, ctx);

        // Sağ tık hiçbir entity oluşturmaz
        doc.Entities.Count.Should().Be(0);
    }

    [Fact]
    public void T301_SwitchTool_TrackNodePozisyonlari()
    {
        var (doc, st, sel) = Sahne();
        var tool = new SwitchTool();
        var ctx = CreateCtx(doc, st, sel);
        tool.Activate(ctx);

        var pos = new Vector2D(200, 100);
        tool.OnPointerMove(new SnapResult(pos, SnapKind.Grid, null), ctx);
        tool.OnPointerDown(new SnapResult(pos, SnapKind.Grid, null), ToolMouseButton.Left, ctx);

        var nodes = doc.Entities.OfType<TrackNode>().ToList();
        nodes.Should().HaveCount(3);

        // Entry node solda (-X)
        nodes.Any(n => n.Position == new Vector2D(160, 100)).Should().BeTrue("Entry node solda olmalı");
        // MainExit node sağda (+X)
        nodes.Any(n => n.Position == new Vector2D(240, 100)).Should().BeTrue("MainExit node sağda olmalı");
        // DivergingExit node sağ-üstte
        nodes.Any(n => n.Position.X > 200 && n.Position.Y > 100).Should().BeTrue("DivergingExit node sağ-üstte olmalı");
    }

    [Fact]
    public void T302_SwitchTool_RailSwitchProperties()
    {
        var (doc, st, sel) = Sahne();
        var tool = new SwitchTool();
        var ctx = CreateCtx(doc, st, sel);
        tool.Activate(ctx);

        tool.OnPointerMove(new SnapResult(new Vector2D(100, 50), SnapKind.Grid, null), ctx);
        tool.OnPointerDown(new SnapResult(new Vector2D(100, 50), SnapKind.Grid, null), ToolMouseButton.Left, ctx);

        var sw = doc.Entities.OfType<RailSwitch>().Single();
        sw.Position.Should().Be(new Vector2D(100, 50));
        sw.RotationDeg.Should().Be(0);
        sw.State.Should().Be(SwitchState.Main);
        sw.EntryNodeId.Should().NotBe(Guid.Empty);
        sw.MainExitNodeId.Should().NotBe(Guid.Empty);
        sw.DivergingExitNodeId.Should().NotBe(Guid.Empty);
        // Her port farklı TrackNode'a ait olmalı
        sw.EntryNodeId.Should().NotBe(sw.MainExitNodeId);
        sw.MainExitNodeId.Should().NotBe(sw.DivergingExitNodeId);
        sw.DivergingExitNodeId.Should().NotBe(sw.EntryNodeId);
    }

    [Fact]
    public void T303_SwitchTool_ActivateDeactivate_PreviewSifirlanir()
    {
        var (doc, st, sel) = Sahne();
        var tool = new SwitchTool();
        var ctx = CreateCtx(doc, st, sel);

        tool.Activate(ctx);
        tool.Preview.Should().BeNull();

        tool.OnPointerMove(new SnapResult(new Vector2D(100, 50), SnapKind.Grid, null), ctx);
        tool.Preview.Should().NotBeNull();

        tool.Deactivate(ctx);
        tool.Preview.Should().BeNull();

        // Activate tekrar çağrılınca preview hala null
        tool.Activate(ctx);
        tool.Preview.Should().BeNull();
    }

    [Fact]
    public void T304_SwitchTool_Selection_SonSwitchSecili()
    {
        var (doc, st, sel) = Sahne();
        var tool = new SwitchTool();
        var ctx = CreateCtx(doc, st, sel);
        tool.Activate(ctx);

        tool.OnPointerMove(new SnapResult(new Vector2D(100, 50), SnapKind.Grid, null), ctx);
        tool.OnPointerDown(new SnapResult(new Vector2D(100, 50), SnapKind.Grid, null), ToolMouseButton.Left, ctx);

        var sw = doc.Entities.OfType<RailSwitch>().Single();
        sel.SelectedIds.Should().ContainSingle().Which.Should().Be(sw.Id);
    }
}
```

### F4: T010 eşik güncellemesi (125 → 134)

Dosya: [`T010_KapsamBekcisi.cs`](../../tests/TrainService.Architecture.Tests/T010_KapsamBekcisi.cs)

```csharp
// Değişiklik: 125 → 134
// Gerekçe: v3.0.27 — T280 SwitchTool testleri eklendi (+9)
cadTests.Should().BeGreaterThanOrEqualTo(134, "Cad.Tests tabanı 134'e çıkarıldı (v3.0.27 — T280 SwitchTool testleri eklendi)");
```

### F5: [`muhur-v3027.ps1`](../../tools/muhur-v3027.ps1) — YENİ

v3.0.26 mühür script'inden türetilecek, referanslar güncellenecek:
- v3.0.26 → v3.0.27
- RampTool → SwitchTool
- T287–T295 → T296–T304
- PreviewRampPlace → PreviewSwitchPlace
- RampDefaults → SwitchDefaults

### F6: [`sapma.txt`](../../tools/sapma.txt) — DEĞİŞİKLİK

Aşağıdaki sapma kaydı eklenecek:

```
================================================================================
v3.0.27 — SwitchTool
================================================================================
1. SwitchTool, RampTool deseniyle 1-click prefab olarak uygulandı (yeni entity
   oluşturma). Orijinal v3.0.26 planındaki 3-click existing-node yaklaşımından
   farklıdır. Bunun nedeni roadmap swap: v3.0.26 = RampTool, v3.0.27 = SwitchTool.
2. SetNodeRoleCommand KULLANILMADI. Prefab yeni TrackNode'lar oluşturur, mevcut
   düğümün Role'ünü değiştirmez. Role = Plain kalır; RailSwitch entity'si port
   Id'lerini taşır. TrackGraph.IsSwitchPort() ile port tespiti yapılır.
3. Render katmanı (CadViewportControl, CadColors) ve UI (EditorView toolbar+F8)
   v3.0.26 RampTool teslimatında zaten SwitchTool için hazırdı. v3.0.27 sadece
   SwitchTool.cs + SwitchDefaults.cs + testleri ekler.
4. DivergingAngleDeg = 30° sabit (ilk versiyon). Gelecekte ayarlanabilir hale
   getirilebilir (v3.0.2x+).
```

### F7: [`README.md`](../../README.md) — DEĞİŞİKLİK

Sürüm Geçmişi'ne eklenecek:

```
### v3.0.27 — SwitchTool (Makas Prefab)
- SwitchTool: 1 tıklamada RailSwitch + 3 TrackNode (Entry/MainExit/DivergingExit) prefab yerleştirme
- SwitchDefaults: geometri sabitleri (LengthMm=80, DivergingAngleDeg=30)
- PreviewSwitchPlace ghost render (Y-shaped: Entry→Main green, Entry→Diverging orange)
- CompositeCadCommand ile tek undo/redo (4 entity tek adım)
- BoundServoDeviceId alanı RailSwitch entity'sinde hazır
- TrackGraph Build overload + IsSwitchPort/GetSwitchState/GetSwitchForPort (v3.0.26'dan)
```

---

## 5. Test Planı (T296–T304)

### 5.1 Test Tablosu

| Kimlik | Test Adı | Davranış | Ön Koşul |
|--------|----------|----------|----------|
| **T296** | `SwitchTool_PointerMove_PreviewOlusur` | Fare hareketinde PreviewSwitchPlace dolu, EntryPos/MainExitPos/DivergingExitPos doğru | Sahne, SwitchTool active |
| **T297** | `SwitchTool_SolTik_EntitylerOlusur` | Sol tık → 3 TrackNode + 1 RailSwitch = 4 entity | Sahne |
| **T298** | `SwitchTool_Commit_TekUndo` | Commit → Undo → 0 entity | Sahne |
| **T299** | `SwitchTool_Escape_PreviewGizlenir` | Move (preview var) → Esc → preview null | Sahne |
| **T300** | `SwitchTool_SagTik_YokSayilir` | Sağ tık → 0 entity oluşur | Sahne |
| **T301** | `SwitchTool_TrackNodePozisyonlari` | 3 node doğru pozisyonlarda (Entry sol, MainExit sağ, DivergingExit sağ-üst) | Sahne |
| **T302** | `SwitchTool_RailSwitchProperties` | RailSwitch alanları doğru (Position, RotationDeg, State=Main, 3 port Id) | Sahne |
| **T303** | `SwitchTool_ActivateDeactivate_PreviewSifirlanir` | Activate→null, Move→dolu, Deactivate→null | Sahne |
| **T304** | `SwitchTool_Selection_SonSwitchSecili` | Commit sonrası son RailSwitch seçili | Sahne |

### 5.2 RampTool'dan Farkları

| RampTool (T287–T295) | SwitchTool (T296–T304) | Fark |
|---------------------|------------------------|------|
| 2 TrackNode + 1 Ramp = 3 entity | 3 TrackNode + 1 RailSwitch = 4 entity | +1 node (3-port) |
| RampDefaults.LengthMm = 100 | SwitchDefaults.LengthMm = 80 | Daha kısa |
| Sadece Entry/Exit ofset | Entry/MainExit/DivergingExit ofset | 3 port |
| GradePercent doğrulaması | SwitchState.Main doğrulaması | Farklı properties |
| Diverging yok | DivergingExit angled (30°) | Yeni geometri |

---

## 6. Uygulama Sırası

```
Adım 1: SwitchDefaults.cs → geometri sabitleri (~35 satır)
Adım 2: SwitchTool.cs → 1-click prefab tool (~80 satır)
Adım 3: Derle → dotnet build -c Release
Adım 4: T280_SwitchToolTests.cs → 9 test (~195 satır)
Adım 5: Test → dotnet test tests/TrainService.Cad.Tests -c Release
Adım 6: T010 eşik güncelle: 125 → 134
Adım 7: Tam koşum → dotnet test TrainService.sln -c Release
Adım 8: App çalıştır → dotnet run --project src/TrainService.App (M1–M7 manuel test)
Adım 9: tools/muhur-v3027.ps1 çalıştır → raporları masaüstüne kopyala
Adım 10: sapma.txt güncelle
Adım 11: README.md Sürüm Geçmişi güncelle
Adım 12: Push (kullanıcı onayıyla)
```

---

## 7. Kabul Kriteri

- [ ] `dotnet build -c Release` — 0 hata, 0 uyarı
- [ ] T296–T304 testlerinin tümü yeşil
- [ ] Cad.Tests toplamı: **134** (önceki 125 + 9 yeni)
- [ ] T010 eşiği 134'e güncellendi
- [ ] `dotnet test TrainService.sln -c Release` — Fail=0, Cad=134
- [ ] App başlatılabilir, SwitchTool toolbar'da görünür, F8 ile seçilir
- [ ] PreviewSwitchPlace ghost (Y-shaped: green + orange lines, 3 circles) fare ile hareket eder
- [ ] Sol tık → 4 entity oluşur, diamond marker görünür, seçili gelir
- [ ] Ctrl+Z → tümü gider

---

## 8. Manuel Test Maddeleri (M-serisi)

| # | Test | Beklenen |
|---|------|----------|
| M1 | EditorView → SwitchTool seç (F8 veya toolbar) | Araç aktif, imleç değişir |
| M2 | Boş alanda fare gezdir | PreviewSwitchPlace ghost (Y-şekilli: Entry→Main yeşil, Entry→Diverging turuncu, 3 sarı port çemberi) görünür |
| M3 | Sol tıkla | 3 TrackNode + 1 RailSwitch oluşur, diamond marker (magenta) görünür, switch seçili gelir |
| M4 | TrackTool'a geç, Entry/MainExit/DivergingExit portlarına segment bağla | Segmentler düzgün bağlanır |
| M5 | **Ctrl+Z** yap | 4 entity de silinir |
| M6 | **Ctrl+Y** yap | 4 entity geri gelir, RailSwitch seçili |
| M7 | Esc bas | Preview gizlenir (yarım kalan işlem yok) |

---

## 9. Riskler ve Önlemler

| Risk | Olasılık | Etki | Önlem |
|------|----------|------|-------|
| DivergingExit offset hesaplaması Cos/Sin ile floating-point hata | Düşük | Düşük | Test T296 toleranslı karşılaştırma kullanır |
| 4 entity tek CompositeCadCommand → undo sırasında sıra önemli | Düşük | Düşük | CompositeCadCommand zaten ters sırada undo yapar (LIFO) |
| Tool zaten UI'da SwitchTool() referansıyla bağlı ama SwitchTool.cs yok → derleme hatası | YOK | YOK | SwitchTool.cs bu sürümde oluşturulacak |
| T010 eşiği güncellenmezse test kırmızı | Orta | Düşük | Plana eklendi (Adım 6) |
| PreviewSwitchPlace zaten ITool.cs'de var, başka yerde değişiklik gerekmez | YOK | YOK | Sadece kullan |

---

## 10. v3.0.26 Orijinal Planından Farklar

| Konu | v3.0.26 Orijinal Plan (SwitchTool) | v3.0.27 Yeni Plan (SwitchTool) |
|------|-----------------------------------|-------------------------------|
| Yaklaşım | 3 tıkla mevcut düğüm + segment seç | 1 tıkla prefab (yeni entity) |
| Entity sayısı | 1 RailSwitch + 1 SetNodeRoleCommand | 3 TrackNode + 1 RailSwitch |
| SetNodeRoleCommand | Gerekli (Role güncellemesi) | Gereksiz (yeni Plain node'lar) |
| Preview | PreviewSwitch (NodeId, SegmentId) | PreviewSwitchPlace (Position, 3 port offset) |
| UI durumu | SwitchTool.cs yok, render hazır değil | SwitchTool.cs yok ama render + UI HAZIR |
| TrackGraph | Değişiklik gerekmez | Build overload zaten var |
| Test sayısı | 10 (T270–T279) | 9 (T296–T304) |

---

## 11. Mühür ve Raporlama Planı

Mühür için 3 rapor üretilecek:
1. **`RAPOR_MUHUR_v3027.txt`** — Ana mühür raporu (dolgu taraması, bekçi ispatı, tam koşum, kimlikli gövdeler, arter kanıtları, sapma beyanı)
2. **`RAPOR_T010_ISPAT_v3027.txt`** — T010 bekçi ispatı (kırmızı→yeşil gösterimi)
3. **`RAPOR_TAM_KOSUM_v3027.txt`** — Tüm test projelerinin ayrıntılı çıktısı

Script: [`tools/muhur-v3027.ps1`](../../tools/muhur-v3027.ps1) (v3.0.26 şablonundan türetilecek)
