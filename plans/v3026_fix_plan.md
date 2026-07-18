# v3.0.26 — Fix Plan: Prefab Model Uyumluluğu

> **Kaynak:** [`Roadmap hy.md`](../.agents/Roadmap%20hy.md) — Switch+Ramp prefab modelleri
> **Önceki Sürüm:** [`v3.0.26 SwitchTool`](../../src/TrainService.Cad/Tools/SwitchTool.cs) — sealed, pushed (`0358f18`)
> **Plan dosyası:** `plans/v3026_fix_plan.md`

---

## ⚠ UYARI: Bu plan, aşağıdaki çekirdek yapılara DOKUNUR

| Yapı | Dosya | İşlem | Arter |
|------|-------|-------|-------|
| **Ramp entity** | [`DomainEntities.cs`](../../src/TrainService.Core/Entities/DomainEntities.cs:55) | Segment-based → Prefab model (Position/RotationDeg/EntryNodeId/ExitNodeId) | **A2** (Domain Modelleri) — genişletilir, kırılmaz |
| **TrainDbContext** | [`TrainDbContext.cs`](../../src/TrainService.Data/TrainDbContext.cs:32) | `RailSwitch.Position` + `Ramp.Position` ComplexProperty eklenir | **A4** (SQLite Şeması) — migration ile genişletilir |
| **TrackGraph** | [`TrackGraph.cs`](../../src/TrainService.Core/Topology/TrackGraph.cs:27) | Yeni `Build()` overload + switch sorgu metodları | **A1** (Katmanlı Mimari+DI) — yeni overload eklenir, eskisi silinmez |
| **RouteTool** | [`RouteTool.cs`](../../src/TrainService.Cad/Tools/RouteTool.cs:13) | `RouteStep.SwitchState?` alanı + switch port kontrolü | **A3** (MQTT/SwitchState) — nullable alan eklenir |

**Hiçbir yapı yeniden yazılmaz, sadece GENİŞLETİLİR.** Mevcut public API korunur, geriye dönük uyumluluk bozulmaz.

---

## 1. Kapsam Sınırı (Scope Guard)

### BU SÜRÜMDE VAR
- Ramp entity'sinin prefab modele dönüştürülmesi (SegmentId korunur, yeni alanlar EKLENİR)
- `RailSwitch.Position` + `Ramp.Position` için EF Core ComplexProperty mapping
- Migration: `FixRailSwitchRampMapping` — yeni kolonlar eklenir
- `TrackGraph`'e `IEnumerable<RailSwitch>` kabul eden yeni `Build()` overload
- `TrackGraph`'e `IsSwitchPort()`, `GetSwitchState()`, `GetSwitchForPort()` metodları
- `RouteStep.SwitchState?` nullable alanı (geriye dönük uyumlu)
- `RouteTool`'da switch port kontrolü (ortak düğüm switch portuysa SwitchState atanır)
- `RampDefaults` sabit sınıfı (SwitchDefaults pattern'inde)
- `RampTool` (SwitchTool pattern'inde — 1 tıkla prefab)
- `PreviewRampPlace` record'u (ITool.cs altına)
- `CadColors`'a Ramp renkleri (RampFill, RampPen, RampMarkerFill, RampMarkerPen)
- `CadViewportControl`'da Ramp preview ve model render
- Testler: T280–T298 (19 test — Core.Tests + Cad.Tests)
- Data.Tests Vector2D mapping fix (23 test yeşile döner)
- T010 eşik güncellemesi

### BU SÜRÜMDE YOK (dokunmak yasak)
- `RailSwitch` entity'sinde değişiklik (zaten doğru prefab model ✅)
- `SwitchTool` değişikliği (zaten doğru çalışıyor ✅)
- `SwitchDefaults` değişikliği (zaten doğru ✅)
- `ITool` arayüzü değişikliği (sadece yeni `PreviewRampPlace` record'u EKLENİR)
- Varolan testlerde değişiklik (T010 ve Data.Tests mapping fix HARİÇ)
- Mimarî bekçiler (T001–T008)
- WPF katmanı / UI değişikliği (CadViewportControl ve CadColors genişletilir — App katmanı)
- `CadDocument`, `CommandStack`, `AddEntityCommand`, `CompositeCadCommand` değişiklikleri (A1 arteri)
- Snap sistemi değişikliği
- `SwitchState` / `NodeRole` enum'larında değişiklik
- Simülasyon / MQTT katmanı (kapsam dışı)

---

## 2. Problem Analizi

### P1: Data.Tests 23 başarısız — Vector2D mapping eksik

**Kök neden:** [`TrainDbContext.cs:34`](../../src/TrainService.Data/TrainDbContext.cs:34) sadece `TrackNode.Position` için `ComplexProperty` tanımlar. `RailSwitch.Position` (Vector2D) ve gelecekteki `Ramp.Position` için mapping YOK. SQLite provider Vector2D'yi otomatik serialize edemez.

**Mevcut durum:**
```csharp
// SADECE TrackNode.Position mapped:
modelBuilder.Entity<TrackNode>().ComplexProperty(e => e.Position);
```

**Gerekli:**
```csharp
// RailSwitch ve Ramp için de ComplexProperty:
modelBuilder.Entity<RailSwitch>().ComplexProperty(e => e.Position);
modelBuilder.Entity<Ramp>().ComplexProperty(e => e.Position);
```

**Migration:** `FixRailSwitchRampMapping` — RailSwitch tablosuna Position_X, Position_Y kolonları eklenir.

---

### P2: Ramp entity hala segment-based — prefab modele dönüşmeli

**Kök neden:** [`DomainEntities.cs:55-66`](../../src/TrainService.Core/Entities/DomainEntities.cs:55) — `RailSwitch` prefab modele dönüştü (Position/RotationDeg/EntryNodeId/MainExitNodeId/DivergingExitNodeId) ama `Ramp` hala `SegmentId` kullanıyor.

**Mevcut:**
```csharp
public sealed class Ramp : CadEntity
{
    public Guid SegmentId { get; set; }  // <-- ESKİ: segment-based
    public double StartZ { get; set; }
    public double EndZ { get; set; }
    public double LengthMm { get; set; }
}
```

**Hedef:**
```csharp
public sealed class Ramp : CadEntity
{
    public Guid SegmentId { get; set; }  // Korunur (geriye uyumluluk)
    public Vector2D Position { get; set; }  // YENİ: prefab merkez
    public double RotationDeg { get; set; } // YENİ
    public Guid EntryNodeId { get; set; }   // YENİ
    public Guid ExitNodeId { get; set; }    // YENİ
    public double StartZ { get; set; }
    public double EndZ { get; set; }
    public double LengthMm { get; set; }
    public double GradePercent => ... // aynen kalır
}
```

`SegmentId` kalır ama kullanılmaz. Migration'da `Position_X`, `Position_Y`, `RotationDeg`, `EntryNodeId`, `ExitNodeId` kolonları eklenir.

---

### P3: TrackGraph RailSwitch farkında değil

**Kök neden:** [`TrackGraph.cs:27`](../../src/TrainService.Core/Topology/TrackGraph.cs:27) — `Build()` sadece `IEnumerable<TrackNode>` ve `IEnumerable<TrackSegment>` alır. RailSwitch entity'lerini bilmez.

**Mevcut:**
```csharp
public static TrackGraph Build(IEnumerable<TrackNode> nodes, IEnumerable<TrackSegment> segments)
```

**Hedef:** Bu overload KORUNUR (geriye uyumluluk). Yeni overload eklenir:
```csharp
public static TrackGraph Build(
    IEnumerable<TrackNode> nodes,
    IEnumerable<TrackSegment> segments,
    IEnumerable<RailSwitch> switches)  // YENİ
```

Ve şu sorgu metodları:
- `IsSwitchPort(Guid nodeId)` — düğüm bir switch'in portu mu?
- `GetSwitchState(Guid nodeId)` — bu port hangi SwitchState'de (Main/Diverging)?
- `GetSwitchForPort(Guid nodeId)` — bu porta ait RailSwitch entity'si (null olabilir)

---

### P4: RouteTool SwitchState farkında değil

**Kök neden:** [`RouteTool.cs:74`](../../src/TrainService.Cad/Tools/RouteTool.cs:74) — `RouteStep` sadece `(SegmentId, Direction)` taşır. Bir segment üzerinde ilerlerken o segmentin bir switch portu olup olmadığı, hangi branştan geçildiği kaydedilmez.

**Mevcut:**
```csharp
public sealed record RouteStep(Guid SegmentId, TravelDirection Direction);
```

**Hedef:**
```csharp
public sealed record RouteStep(
    Guid SegmentId,
    TravelDirection Direction,
    SwitchState? SwitchState = null  // YENİ: nullable, geriye uyumlu
);
```

**RouteTool değişikliği:** Ortak düğüm bulunduğunda, `TrackGraph.IsSwitchPort(ortak)` kontrolü yapılır. Eğer switch portuysa, `GetSwitchState(ortak)` çağrılır ve `RouteStep`'e eklenir.

---

### P5: Ramp prefab implementasyonu EKSİK

**Kök neden:** `SwitchTool` (1-click prefab) var, `RampDefaults` var ama `RampTool` YOK. Ramp hala segment-based.

**Gerekli yeni dosyalar/değişiklikler:**
1. `RampDefaults.cs` — Geometri sabitleri (LengthMm=100, MaxGradePercent=15, DefaultStartZ=0, DefaultEndZ=350)
2. ITool.cs → `PreviewRampPlace` record'u
3. `RampTool.cs` — SwitchTool pattern'inde, 1 tıkla prefab yerleştirme
4. CadColors → Ramp renkleri (RampFill, RampPen, RampMarkerFill, RampMarkerPen)
5. CadViewportControl → `PreviewRampPlace` render + Ramp model render (dikdörtgen + GradePercent etiketi)

---

## 3. Dosya-Dosya Değişiklik Listesi

| # | Dosya | İşlem | Açıklama |
|---|-------|-------|----------|
| **F1** | [`src/TrainService.Core/Entities/DomainEntities.cs`](../../src/TrainService.Core/Entities/DomainEntities.cs:55) | **DEĞİŞİKLİK** | Ramp: Position/RotationDeg/EntryNodeId/ExitNodeId alanları EKLENİR. SegmentId KORUNUR (kullanılmaz). |
| **F2** | [`src/TrainService.Data/TrainDbContext.cs`](../../src/TrainService.Data/TrainDbContext.cs:32) | **DEĞİŞİKLİK** | OnModelCreating: `RailSwitch.Position` + `Ramp.Position` için ComplexProperty eklenir. |
| **F3** | `src/TrainService.Data/Migrations/XXX_FixRailSwitchRampMapping.cs` | **YENİ migration** | Ramp tablosuna: Position_X, Position_Y, RotationDeg, EntryNodeId, ExitNodeId. RailSwitch tablosuna: Position_X, Position_Y (zaten var ama ComplexProperty değil). |
| **F4** | [`src/TrainService.Core/Topology/TrackGraph.cs`](../../src/TrainService.Core/Topology/TrackGraph.cs:27) | **DEĞİŞİKLİK** | Yeni `Build(ITrackNode, ITrackSegment[], IRailSwitch[])` overload. `_switches` Dictionary. `IsSwitchPort()`, `GetSwitchState()`, `GetSwitchForPort()` metodları. |
| **F5** | [`src/TrainService.Core/Entities/DomainEntities.cs`](../../src/TrainService.Core/Entities/DomainEntities.cs:34) | **DEĞİŞİKLİK** | `RouteStep` record'u: `SwitchState?` nullable alanı EKLENİR. |
| **F6** | [`src/TrainService.Cad/Tools/RouteTool.cs`](../../src/TrainService.Cad/Tools/RouteTool.cs:74) | **DEĞİŞİKLİK** | Ortak düğüm switch portu mu kontrolü + SwitchState ataması. Activate'de switch'ler de grafa eklenir. |
| **F7** | [`src/TrainService.Cad/RampDefaults.cs`](../../src/TrainService.Cad/RampDefaults.cs) | **YENİ** | Geometri sabitleri (SwitchDefaults pattern'inde). Offset hesaplamaları. |
| **F8** | [`src/TrainService.Cad/Tools/ITool.cs`](../../src/TrainService.Cad/Tools/ITool.cs:42) | **DEĞİŞİKLİK** | `PreviewRampPlace` record'u eklenir (PreviewSwitchPlace'den sonra). |
| **F9** | [`src/TrainService.Cad/Tools/RampTool.cs`](../../src/TrainService.Cad/Tools/RampTool.cs) | **YENİ** | RampTool — SwitchTool pattern'inde, 1 tıkla prefab. |
| **F10** | [`src/TrainService.App/Resources/CadColors.cs`](../../src/TrainService.App/Resources/CadColors.cs:28) | **DEĞİŞİKLİK** | Ramp renkleri: RampFill, RampPen, RampMarkerFill, RampMarkerPen. |
| **F11** | [`src/TrainService.App/Controls/CadCanvas/CadViewportControl.cs`](../../src/TrainService.App/Controls/CadCanvas/CadViewportControl.cs:376) | **DEĞİŞİKLİK** | `PreviewRampPlace` render (SwitchPlace branch'inden sonra) + Ramp model render (RailSwitch branch'inden sonra). |
| **F12** | `tests/TrainService.Core.Tests/T280_TrackGraphSwitchTests.cs` | **YENİ** | T280–T286: TrackGraph switch metodları testleri (Core.Tests, ~200 satır) |
| **F13** | `tests/TrainService.Cad.Tests/Tools/T280_RampToolTests.cs` | **YENİ** | T287–T295: RampTool testleri (Cad.Tests, ~250 satır) |
| **F14** | `tests/TrainService.Architecture.Tests/T010_KapsamBekcisi.cs` | **DEĞİŞİKLİK** | Eşik güncellemesi: Core.Tests +5, Cad.Tests +9 |

---

## 4. Test Planı

### 4.1 T280–T286: TrackGraph Switch Metodları (Core.Tests)

| Kimlik | Test Adı | Davranış | Ön Koşul |
|--------|----------|----------|----------|
| **T280** | `Build_WithSwitches_SwitchPortsMapped` | Build( nodes, segments, switches ) → switch port düğümleri IsSwitchPort=true döner | 1 RailSwitch + 3 TrackNode + 3 TrackSegment |
| **T281** | `IsSwitchPort_NonSwitchNode_ReturnsFalse` | Sıradan bir düğüm → IsSwitchPort=false | TrackNode (Plain) |
| **T282** | `GetSwitchState_MainPort_ReturnsMain` | Entry portu → SwitchState.Main (switch State=Main) | T280 aynı scene |
| **T283** | `GetSwitchState_DivergingPort_ReturnsDiverging` | Diverging portu → SwitchState.Diverging | T280 aynı scene |
| **T284** | `GetSwitchForPort_EntryNode_ReturnsSwitch` | EntryNodeId ile sorgu → doğru RailSwitch döner | T280 aynı scene |
| **T285** | `GetSwitchForPort_PlainNode_ReturnsNull` | Switch olmayan düğüm → null | TrackNode (Plain) |
| **T286** | `AreAdjacent_CrossSwitch_Works` | Switch'in entry portundaki segment ile main portundaki segment komşu mu → true | T280 aynı scene |

### 4.2 T287–T295: RampTool (Cad.Tests)

| Kimlik | Test Adı | Davranış | Ön Koşul |
|--------|----------|----------|----------|
| **T287** | `RampTool_SolTik_PrefabOlusur` | 1 sol tık → 1 Ramp + 2 TrackNode oluşur | RampTool active |
| **T288** | `RampTool_RampDogruAlanlar` | Commit sonrası → Ramp.Position doğru, EntryNodeId!=Guid.Empty, ExitNodeId!=Guid.Empty, StartZ=0, EndZ=350 | T287 sonrası |
| **T289** | `RampTool_GradePercent_Dogru` | LengthMm=100, StartZ=0, EndZ=350 → GradePercent=3.5 | T287 sonrası |
| **T290** | `RampTool_Undo_TumuGeriAlinir` | Commit → Undo → 0 Ramp, 0 TrackNode yeni | T287 sonrası |
| **T291** | `RampTool_Redo_TumuGeriDoner` | Undo → Redo → Ramp geri gelir, GradePercent doğru | T290 sonrası |
| **T292** | `RampTool_Esc_Iptal` | Tık yokken Esc → preview null, entity eklenmez | RampTool active |
| **T293** | `RampTool_Preview_Dolu` | MouseMove → PreviewRampPlace dolu, EntryPos/MainExitPos doğru offset'te | RampTool active |
| **T294** | `RampTool_IkinciTik_YeniRamp` | 1. tık → 2. tık farklı yerde → 2 Ramp oluşur | RampTool active |
| **T295** | `RampTool_RampDefaults_Sabitler` | LengthMm=100, MaxGradePercent=15, DefaultStartZ=0, DefaultEndZ=350 | RampDefaults sınıfı |

---

## 5. Tasarım Kararları

### 5.1 RampDefaults — Geometri Sabitleri

```csharp
public static class RampDefaults
{
    public const double LengthMm = 100.0;
    public const double MaxGradePercent = 15.0;
    public const double DefaultStartZ = 0;
    public const double DefaultEndZ = 350;

    public const double HalfLength = LengthMm / 2;

    /// Giriş portunun merkeze göre ofseti (Rotation=0 için SOL (-X)).
    public static Vector2D EntryOffset(double rotDeg) =>
        Rotate(new Vector2D(-HalfLength, 0), rotDeg);

    /// Çıkış portunun merkeze göre ofseti (Rotation=0 için SAĞ (+X)).
    public static Vector2D ExitOffset(double rotDeg) =>
        Rotate(new Vector2D(HalfLength, 0), rotDeg);

    private static Vector2D Rotate(Vector2D v, double deg) { ... } // SwitchDefaults ile aynı
}
```

### 5.2 PreviewRampPlace Record

```csharp
// ITool.cs altına, PreviewSwitchPlace'den sonra:
public sealed record PreviewRampPlace(
    Vector2D Position,
    double RotationDeg,
    Vector2D EntryPos,
    Vector2D ExitPos
) : PreviewShape;
```

### 5.3 RampTool Tasarımı

SwitchTool ile aynı pattern:
- 1 sol tık → Position'da 2 TrackNode (Entry/Exit) + 1 Ramp entity
- Tümü `CompositeCadCommand("Rampa Yerleştir", cmds)` ile tek undo adımı
- Esc → preview gizle
- Rotation=0 sabit (ileride RotationTool ile genişletilir)

### 5.4 Migration Stratejisi

`SegmentId` kolonu silinmez — sadece yeni kolonlar eklenir. Bu, geriye dönük uyumluluk sağlar:
```csharp
migrationBuilder.AddColumn<double>("Position_X", "Rampp");
migrationBuilder.AddColumn<double>("Position_Y", "Rampp");
migrationBuilder.AddColumn<double>("RotationDeg", "Rampp");
migrationBuilder.AddColumn<string>("EntryNodeId", "Rampp", nullable: false);
migrationBuilder.AddColumn<string>("ExitNodeId", "Rampp", nullable: false);
```

RailSwitch tablosu için ComplexProperty migration'ı da eklenir (Position_X, Position_Y zaten varsa atlanır, EF Core sadece gerekli kolonları ekler).

### 5.5 RouteStep.SwitchState? — Geriye Uyumluluk

```csharp
// DomainEntities.cs:
public sealed record RouteStep(
    Guid SegmentId,
    TravelDirection Direction,
    SwitchState? SwitchState = null  // YENİ: nullable default
);
```

Bu değişiklik **binary uyumludur** — mevcut `new RouteStep(id, dir)` çağrıları değişmeden çalışır. Sadece yeni çağrılar `new RouteStep(id, dir, switchState)` yapabilir.

### 5.6 TrackGraph Genişletmesi

Mevcut `Build()` overload'u **korunur** (dokunulmaz). Yeni overload eklenir:

```csharp
public static TrackGraph Build(
    IEnumerable<TrackNode> nodes,
    IEnumerable<TrackSegment> segments,
    IEnumerable<RailSwitch> switches)
{
    var g = Build(nodes, segments);  // mevcut mantığı çağır
    foreach (var sw in switches)
    {
        g._switches[sw.Id] = sw;
        // Port düğümlerini kaydet: EntryNodeId, MainExitNodeId, DivergingExitNodeId
    }
    return g;
}
```

---

## 6. Render Detayları

### 6.1 CadViewportControl — PreviewRampPlace

```csharp
// SwitchPlace branch'inden sonra (line ~389):
else if (ToolController?.ActiveTool?.Preview is PreviewRampPlace rp)
{
    var entryPt = Transform.WorldToScreen(rp.EntryPos);
    var exitPt = Transform.WorldToScreen(rp.ExitPos);
    
    // Dikdörtgen gövde: entry→exit arası
    var rect = new Rect(entryPt, exitPt);
    dc.DrawRectangle(CadColors.RampFill, CadColors.RampPen, rect);
    
    // 2 ghost port circle
    dc.DrawEllipse(CadColors.RampMarkerFill, CadColors.RampMarkerPen, entryPt, 4, 4);
    dc.DrawEllipse(CadColors.RampMarkerFill, CadColors.RampMarkerPen, exitPt, 4, 4);
}
```

### 6.2 CadViewportControl — Ramp Model Render

```csharp
// Switch model render'dan sonra (RailSwitch branch'i, line ~500):
foreach (var entity in _document.Entities)
{
    if (!_document.IsVisible(entity.Id)) continue;
    if (entity is Ramp ramp)
    {
        // Giriş ve çıkış düğümlerini bul
        if (_document.TryGetEntity(ramp.EntryNodeId, out var ea) && ea is TrackNode entryNode &&
            _document.TryGetEntity(ramp.ExitNodeId, out var eb) && eb is TrackNode exitNode)
        {
            var p1 = new Point(entryNode.Position.X, entryNode.Position.Y);
            var p2 = new Point(exitNode.Position.X, exitNode.Position.Y);
            
            // Dikdörtgen çiz
            var rect = new Rect(p1, p2);
            dc.DrawRectangle(CadColors.RampFill, CadColors.RampPen, rect);
            
            // GradePercent etiketi (metin)
            // İleriki sürümde FormattedText ile
        }
    }
}
```

---

## 7. Kabul Kriteri

- [ ] `dotnet build -c Release` — 0 hata, 0 uyarı
- [ ] T280–T286 testlerinin tümü yeşil (Core.Tests — 7 test)
- [ ] T287–T295 testlerinin tümü yeşil (Cad.Tests — 9 test)
- [ ] Data.Tests 23 başarısız test yeşile döndü
- [ ] T010 eşiği güncellendi: Core.Tests +5, Cad.Tests +9
- [ ] Migration `FixRailSwitchRampMapping` eklendi, `dotnet ef database update` çalıştı
- [ ] `dotnet test TrainService.sln -c Release` — Fail=0, Skip=0
- [ ] App başlatılabilir, SwitchTool+RampTool toolbar'da görünür
- [ ] SwitchTool çalışıyor (regresyon yok)
- [ ] RouteTool SwitchState-aware (regresyon yok — nullable default sayesinde)

---

## 8. Manuel Test Maddeleri (M-serisi)

| # | Test | Beklenen |
|---|------|----------|
| M1 | EditorView → SwitchTool seç, 1 tıkla makas oluştur | RailSwitch + 3 TrackNode oluşur (regresyon testi) |
| M2 | EditorView → RampTool seç, 1 tıkla rampa oluştur | Ramp + 2 TrackNode oluşur, dikdörtgen görünür |
| M3 | Ramp'a tıkla (SelectTool) → PropertyGrid | GradePercent doğru (StartZ=0, EndZ=350, LengthMm=100 → %3.5) |
| M4 | Switch oluştur → RouteTool ile switch üzerinden rota çiz | RouteStep.SwitchState dolu, rota geçerli |

---

## 9. Uygulama Sırası

```
Adım 1:  DomainEntities.cs → Ramp prefab alanları + RouteStep.SwitchState? (F1+F5)
Adım 2:  TrainDbContext.cs → ComplexProperty ekle (F2)
Adım 3:  Migration → FixRailSwitchRampMapping (F3)
Adım 4:  TrackGraph.cs → yeni Build() overload + switch metodları (F4)
Adım 5:  Derle → dotnet build -c Release
Adım 6:  T280_TrackGraphSwitchTests.cs → 7 test (F12)
Adım 7:  Test → dotnet test tests/TrainService.Core.Tests -c Release
Adım 8:  RampDefaults.cs → sabit sınıfı (F7)
Adım 9:  ITool.cs → PreviewRampPlace record (F8)
Adım 10: RampTool.cs → tam sınıf (F9)
Adım 11: CadColors.cs → Ramp renkleri (F10)
Adım 12: CadViewportControl.cs → Ramp preview + model render (F11)
Adım 13: RouteTool.cs → SwitchState-aware routing (F6)
Adım 14: Derle → dotnet build -c Release
Adım 15: T280_RampToolTests.cs → 9 test (F13)
Adım 16: Data.Tests koş → 23 mapping testi yeşil
Adım 17: T010 eşik güncelle (F14)
Adım 18: Tam koşum → dotnet test TrainService.sln -c Release
Adım 19: App çalıştır → dotnet run (M1-M4 manuel test)
Adım 20: Mühür raporu + push (kullanıcı onayıyla)
```

---

## 10. T010 Eşik Güncellemesi

```csharp
// Core.Tests: mevcut + 5 (T280-T286)
coreTests.Should().BeGreaterThanOrEqualTo(mevcut+5, "Core.Tests tabanı güncellendi (v3.0.26 fix — T280 TrackGraph switch testleri eklendi)");

// Cad.Tests: mevcut (116) + 9 (T287-T295)
cadTests.Should().BeGreaterThanOrEqualTo(125, "Cad.Tests tabanı 125'e çıkarıldı (v3.0.26 fix — T287 RampTool testleri eklendi)");
```

---

## 11. Riskler ve Önlemler

| Risk | Olasılık | Etki | Önlem |
|------|----------|------|-------|
| `RouteStep` record'da `SwitchState?` eklenmesi mevcut serialization'ı bozabilir | Düşük | Orta | OwnsMany + RouteSteps tablosu migration gerektirmez çünkü nullable alan; değeri null kalır |
| Migration `FixRailSwitchRampMapping` mevcut RailSwitch verilerini bozabilir | Düşük | Yüksek | RailSwitch'te Position zaten var (veri kaybı yok). Sadece ComplexProperty mapping eklenir |
| Ramp.SegmentId kolonu artık kullanılmıyor ama silinemez | Düşük | Düşük | AGENTS.md 4.5 gereği kolon silinmez — bayat kolon olarak kalır |
| TrackGraph yeni overload'u varolan Build() çağrılarını bozabilir | Düşük | Yüksek | Mevcut overload korunur, overload resolution C# tarafından otomatik yapılır |
| RampTool preview'da performans (her MouseMove'da offset hesaplama) | Düşük | Düşük | SwitchTool ile aynı seviyede; sabit rotasyon (0°) sadece 2 offset hesaplar |
