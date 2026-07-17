# v3.0.26 REVİZE — SwitchTool (Prefab Makas Yerleştirme)

> **Önceki plan:** `v3026_switchtool_plan.md` (ESKİ — segment-ten makas oluşturma)
> **Bu plan:** Prefab nesne olarak 1 tıkla makas yerleştirme
> **Temel fark:** Switch artık başkalarının referansçısı değil, kendi portları olan bağımsız prefab

---

## 1. KAPSAM SINIRI (SCOPE GUARD)

### BU SÜRÜMDE VAR
- `RailSwitch` entity modelinin prefab tasarıma göre güncellenmesi
- `SwitchTool`: 1 tıkla prefab Switch yerleştirme (pozisyon + 3 port node'u otomatik oluşur)
- `PreviewSwitchPlace` record'u ile fare altında ghost önizleme
- `PlaceSwitchCommand` ile tek undo adımında tüm entity'lerin eklenmesi
- Visual render: switch sembolü + 3 port noktası (tool layer + model bake)
- Geçici UI: F8 + toolbar butonu (öncekiyle aynı, tool içi değişti)
- Testler: T270-T279 (yeni davranışa göre yeniden yazıldı)

### BU SÜRÜMDE YOK (dokunmak yasak)
- Ramp nesnesi (v3.0.27'ye kaldı)
- Switch'e rotation UI (fare tekeri ile döndürme) — ilk versiyonda Rotation=0 sabit
- Feature Tree'de switch görünümü (v3.0.28)
- Hardware Binding (v3.0.30-31)
- Mevcut TrackTool, RouteTool, HybridTool, SelectTool, Pano — hiçbiri değişmez
- Mevcut Route, TrackNode, TrackSegment entity modelleri — değişmez
- Mevcut SnapEngine, CommandStack, SelectionService — değişmez

---

## 2. TASARIM KARARLARI

### 2.1 Kullanıcı Deneyimi Akışı

```
1. Kullanıcı SwitchTool'a geçer (F8 veya toolbar)
2. Fareyi hareket ettirir → ghost switch preview imleci takip eder
3. Sol tık → switch o pozisyona yerleşir
   - RailSwitch entity'si oluşur (Position, Rotation=0)
   - 3 adet TrackNode oluşur (Entry, MainExit, DivergingExit)
   - Tümü tek CompositeCadCommand ile eklenir
4. Araç Idle'da kalır → bir sonraki switch'i yerleştirmeye hazır
5. Escape veya başka bir araca geçiş → SwitchTool kapanır
```

### 2.2 Switch Geometrisi (Rotation=0)

```
                 M (MainExit)
                 ↑
                 |
    (Entry)  E───┼─── (Position = center)
                 |
                 ↓
                 D (DivergingExit)
                            (25° açıyla sağa)
```

Varsayılan boyutlar (`SwitchDefaults`):
- `DefaultLengthMm = 80.0` — toplam boy
- `DefaultDivergingAngleDeg = 25.0` — sapak açısı
- Entry: `Position + (0, -40)` — merkezin 40mm altı
- MainExit: `Position + (0, +40)` — merkezin 40mm üstü
- DivergingExit: `Position + (20 * sin25°, 40 * cos25°)` — sağa açılı

### 2.3 Entity İlişkisi

```
RailSwitch (Prefab)
  ├── EntryNodeId ───→ TrackNode (Entry) ───→ TrackSegment (bağlanacak)
  ├── MainExitNodeId ──→ TrackNode (Main) ───→ TrackSegment (bağlanacak)
  └── DivergingExitNodeId → TrackNode (Div) ──→ TrackSegment (bağlanacak)
```

3 TrackNode, `CadDocument.Entities`'de normal entity olarak yaşar.
SnapEngine bu node'lara Endpoint snap ile yakalanabilir.
TrackTool bu node'lara segment bağlayabilir.

### 2.4 Neden TrackNode Ayrı Entity?

- TrackSegment.StartNodeId/EndNodeId zaten TrackNode.Id referans eder
- SnapEngine sadece TrackNode türünü tanır
- SelectTool ile seçilebilir/silinebilir olmalı
- Gelecekte switch silinince port node'ları da silinecek (cascade)

---

## 3. ENTITY MODEL DEĞİŞİKLİĞİ

### ESKİ (SİLİNECEK)
```csharp
public sealed class RailSwitch : CadEntity
{
    public Guid NodeId { get; set; }
    public Guid MainSegmentId { get; set; }
    public Guid DivergingSegmentId { get; set; }
    public SwitchState State { get; set; }
}
```

### YENİ
```csharp
public sealed class RailSwitch : CadEntity
{
    public Vector2D Position { get; set; }            // yerleşim merkezi (mm)
    public double RotationDeg { get; set; }           // dönüş açısı (derece)
    public Guid EntryNodeId { get; set; }             // giriş portu TrackNode.Id
    public Guid MainExitNodeId { get; set; }          // ana hat çıkış portu
    public Guid DivergingExitNodeId { get; set; }     // sapak çıkış portu
    public SwitchState State { get; set; }            // Main / Diverging
    public Guid? BoundServoDeviceId { get; set; }     // (ileride)
}
```

---

## 4. DOSYA DEĞİŞİKLİK LİSTESİ

### SİLİNECEK DOSYALAR (ESKİ YAKLAŞIM)
| # | Dosya | Gerekçe |
|---|-------|---------|
| D1 | `src/TrainService.Cad/Tools/SwitchTool.cs` | Eski 3-tıklı segment-ten yapma aracı |
| D2 | `src/TrainService.Cad/UndoRedo/SetNodeRoleCommand.cs` | Artık node rolü değişmiyor |
| D3 | `tests/TrainService.Cad.Tests/Tools/T270_SwitchToolTests.cs` | Eski testler, yeni davranışla uyuşmaz |

### DEĞİŞTİRİLECEK DOSYALAR
| # | Dosya | Değişiklik |
|---|-------|------------|
| M1 | `src/TrainService.Core/Entities/DomainEntities.cs` | RailSwitch entity güncelle |
| M2 | `src/TrainService.Cad/Tools/ITool.cs` | PreviewSwitch → PreviewSwitchPlace, SwitchToolState kaldır |
| M3 | `src/TrainService.App/Resources/CadColors.cs` | Switch renklerini prefab görsele göre güncelle |
| M4 | `src/TrainService.App/Controls/CadCanvas/CadViewportControl.cs` | RenderToolLayer + RenderModelBake güncelle |
| M5 | `src/TrainService.App/Views/Pages/EditorView.xaml` | Buton tooltip/metin güncelle (opsiyonel) |
| M6 | `src/TrainService.App/Views/Pages/EditorView.xaml.cs` | Tool oluşturma satırı güncellenebilir |
| M7 | `tests/TrainService.Architecture.Tests/T010_KapsamBekcisi.cs` | Eşik güncellemesi (test sayısı değişebilir) |

### YENİ DOSYALAR
| # | Dosya | İçerik |
|---|-------|--------|
| N1 | `src/TrainService.Cad/Tools/SwitchTool.cs` | Yeni SwitchTool (prefab yerleştirme) |
| N2 | `src/TrainService.Cad/SwitchDefaults.cs` | Switch geometri sabitleri + yardımcı metodlar |
| N3 | `src/TrainService.Cad/UndoRedo/PlaceSwitchCommand.cs` | CompositeCadCommand: RailSwitch + 3x TrackNode |
| N4 | `tests/TrainService.Cad.Tests/Tools/T270_SwitchToolTests.cs` | Yeni testler (T270-T279) |

---

## 5. SwitchTool TASARIMI

### 5.1 State Machine

```
                 ┌──────────┐
                 │   Idle   │ ◄──── Escape/Deactivate
                 └────┬─────┘
                      │ OnPointerMove → Preview ghost güncellenir
                      │ OnPointerDown (left click) → PlaceSwitch
                      │
                 ┌────▼─────┐
                 │  Placed   │  (tek commit, hemen Idle'a dön)
                 └──────────┘
```

Sadece **1 durum** var: Idle. Her tık yeni bir switch yerleştirir ve Idle'da kalır.

### 5.2 Preview Record (ITool.cs)

```csharp
public sealed record PreviewSwitchPlace(
    Vector2D Position,           // imleç dünya pozisyonu
    double RotationDeg,          // dönüş açısı (şimdilik 0)
    Vector2D EntryPos,           // hesaplanmış entry node pozisyonu
    Vector2D MainExitPos,        // hesaplanmış main exit node pozisyonu
    Vector2D DivergingExitPos    // hesaplanmış diverging exit node pozisyonu
) : PreviewShape;
```

Her OnPointerMove'da 3 port pozisyonu `SwitchDefaults` ile hesaplanır ve Preview'a yazılır.

### 5.3 Yerleştirme (OnPointerDown)

```csharp
void OnPointerDown(SnapResult snapped, ToolMouseButton button, ToolContext ctx)
{
    if (button != ToolMouseButton.Left) return;
    
    var pos = snapped.Point;  // snap yoksa imleç pozisyonu
    double rot = 0;           // şimdilik rotation=0
    
    // 1. 3 TrackNode oluştur
    var entryNode = new TrackNode 
    { 
        Position = SwitchDefaults.EntryOffset(rot) + pos,
        LayerId = ctx.Document.ActiveLayerId,
        Role = NodeRole.Plain  // sadece port olarak kullanılacak
    };
    var mainNode = new TrackNode 
    { 
        Position = SwitchDefaults.MainExitOffset(rot) + pos,
        LayerId = ctx.Document.ActiveLayerId
    };
    var divNode = new TrackNode 
    { 
        Position = SwitchDefaults.DivergingExitOffset(rot) + pos,
        LayerId = ctx.Document.ActiveLayerId
    };
    
    // 2. RailSwitch oluştur
    var railSwitch = new RailSwitch
    {
        Position = pos,
        RotationDeg = rot,
        EntryNodeId = entryNode.Id,
        MainExitNodeId = mainNode.Id,
        DivergingExitNodeId = divNode.Id,
        State = SwitchState.Main,
        LayerId = ctx.Document.ActiveLayerId
    };
    
    // 3. CompositeCadCommand
    var cmds = new List<ICadCommand>
    {
        new AddEntityCommand(entryNode),
        new AddEntityCommand(mainNode),
        new AddEntityCommand(divNode),
        new AddEntityCommand(railSwitch)
    };
    var composite = new CompositeCadCommand("Makas Yerleştir", cmds);
    ctx.Commands.Do(composite, ctx.Document);
    
    // 4. Yeni switch seçili gelsin
    ctx.Selection.Set(new[] { railSwitch.Id });
}
```

### 5.4 OnPointerMove (Ghost Preview)

```csharp
void OnPointerMove(SnapResult snapped, ToolContext ctx)
{
    var pos = snapped.Point;
    Preview = new PreviewSwitchPlace(
        pos, 0,
        SwitchDefaults.EntryOffset(0) + pos,
        SwitchDefaults.MainExitOffset(0) + pos,
        SwitchDefaults.DivergingExitOffset(0) + pos
    );
}
```

### 5.5 Escape Davranışı

```csharp
void OnKeyDown(ToolKey key, ToolContext ctx)
{
    if (key == ToolKey.Escape)
    {
        Preview = null;  // ghost gizle
        // Idle'da kal, sadece preview temizle
    }
}
```

### 5.6 Deactivate

```csharp
void Deactivate(ToolContext ctx)
{
    Preview = null;  // ghost gizle
}
// Activate'de yapılacak özel bir şey yok
```

---

## 6. SwitchDefaults (YARDIMCI SINIF)

```csharp
// src/TrainService.Cad/SwitchDefaults.cs
public static class SwitchDefaults
{
    public const double LengthMm = 80.0;
    public const double DivergingAngleDeg = 25.0;
    public const double HalfLength => LengthMm / 2;

    public static Vector2D EntryOffset(double rotDeg) =>
        Rotate(new Vector2D(0, -HalfLength), rotDeg);

    public static Vector2D MainExitOffset(double rotDeg) =>
        Rotate(new Vector2D(0, HalfLength), rotDeg);

    public static Vector2D DivergingExitOffset(double rotDeg)
    {
        double rad = DivergingAngleDeg * Math.PI / 180;
        return Rotate(new Vector2D(HalfLength * Math.Sin(rad), HalfLength * Math.Cos(rad)), rotDeg);
    }

    private static Vector2D Rotate(Vector2D v, double deg)
    {
        double rad = deg * Math.PI / 180;
        double cos = Math.Cos(rad), sin = Math.Sin(rad);
        return new Vector2D(v.X * cos - v.Y * sin, v.X * sin + v.Y * cos);
    }
}
```

---

## 7. PlaceSwitchCommand (UNDO/REDO)

```csharp
// src/TrainService.Cad/UndoRedo/PlaceSwitchCommand.cs
// NOT: Ayrı bir sınıf yerine, SwitchTool.OnPointerDown içinde
// doğrudan CompositeCadCommand oluşturulabilir.
// PlaceSwitchCommand sınıfı SADECE şu durumda gerekir:
// - Geri alınabilir switch yerleştirme işlemi özel mantık gerektiriyorsa
// - Şimdilik CompositeCadCommand yeterli, PlaceSwitchCommand'e gerek yok
```

**Karar:** Ayrı bir `PlaceSwitchCommand` sınıfı YOK. `SwitchTool.OnPointerDown` içinde doğrudan `CompositeCadCommand` + `AddEntityCommand` kullanılır. Bu, HybridTool ve TrackTool'daki desenle tutarlıdır.

---

## 8. RENDER DEĞİŞİKLİKLERİ

### 8.1 RenderToolLayer — PreviewSwitchPlace

Eski PreviewSwitch render bloğu silinir, yerine:

```
PreviewSwitchPlace → ghost:
  1. Switch merkezinde küçük daire (SwitchCenterBrush)
  2. Entry node'unda küçük kare (SwitchPortBrush)
  3. MainExit node'unda küçük kare (SwitchPortBrush)
  4. DivergingExit node'unda küçük kare (SwitchPortBrush)
  5. Entry → MainExit arası düz çizgi (SwitchMainPen)
  6. Entry → DivergingExit arası çizgi (SwitchDivergingPen)
  7. Sapak yönünü belirten küçük ok/üçgen
```

### 8.2 RenderModelBake — RailSwitch Modeli

Eski RailSwitch render bloğu silinir, yerine:

```
Her RailSwitch entity'si için:
  1. Entry/MainExit/DivergingExit TrackNode pozisyonlarını bul
  2. Entry → MainExit arası kalın çizgi (SwitchMainPen)
  3. Entry → DivergingExit arası çizgi (SwitchDivergingPen)
  4. SwitchState.Main ise: Main çizgisi daha kalın/belirgin
  5. SwitchState.Diverging ise: Diverging çizgisi daha kalın/belirgin
  6. Switch merkezinde küçük dolgu sembolü
```

---

## 9. TEST PLANI

### 9.1 Test Sahnesi: SahneKavsak

Yeni `SahneKavsak()` artık mevcut TrackNode+TrackSegment'lerle değil,
sadece boş bir CadDocument ile başlar. Testler switch yerleştirme sonrası
oluşan entity'leri kontrol eder.

```csharp
private static CadDocument BosDokuman()
{
    var doc = new CadDocument();
    return doc;
}
```

### 9.2 Test Tablosu

| Kimlik | Davranış | Assert |
|--------|----------|--------|
| **T270** | SwitchTool yerleştirme → 1 RailSwitch + 3 TrackNode oluşur | Entities.OfType<RailSwitch>().Count() == 1; TrackNode sayısı 3 |
| **T271** | Yerleşen RailSwitch entity alanları doğru | Position, EntryNodeId, MainExitNodeId, DivergingExitNodeId dolu; State == Main |
| **T272** | Entry TrackNode doğru pozisyonda | EntryNode.Position == SwitchDefaults.EntryOffset(0) + clickPos |
| **T273** | MainExit TrackNode doğru pozisyonda | MainExitNode.Position == SwitchDefaults.MainExitOffset(0) + clickPos |
| **T274** | DivergingExit TrackNode doğru pozisyonda | DivergingExitNode.Position == SwitchDefaults.DivergingExitOffset(0) + clickPos |
| **T275** | Undo → tüm entity'ler silinir | st.Undo → 4 entity de gitti; selection boş |
| **T276** | Redo → tüm entity'ler geri gelir | st.Redo → 4 entity geri; RailSwitch.Id selection'da |
| **T277** | Preview OnPointerMove'da güncellenir | Preview is PreviewSwitchPlace; EntryPos/MainExitPos/DivergingExitPos dolu |
| **T278** | Escape preview'i temizler (null yapar) | OnKeyDown(Escape) → Preview == null |
| **T279** | Art arda 2 switch yerleştirme → 2 RailSwitch + 6 TrackNode | 2. placement sonrası toplam RailSwitch 2, TrackNode 6 |

### 9.3 Test Sayısı

- Cad.Tests: +10 test (T270-T279)
- Önceki testler silindiği için net artış: +0 (10 silindi, 10 eklendi)
- **T010 eşik:** Değişmez (116)

---

## 10. RENK PALETİ (CadColors.cs)

Eski switch renkleri kaldırılır, yenileri eklenir:

| Renk | Kullanım |
|------|----------|
| `SwitchCenterBrush` | Ghost'ta merkez daire (yarı-saydam mor) |
| `SwitchPortBrush` | Ghost'ta port noktaları (yarı-saydam cyan) |
| `SwitchMainPen` | Ana hat çizgisi (yeşil, 3px) |
| `SwitchDivergingPen` | Sapak çizgisi (turuncu, 3px) |
| `SwitchStateMainPen` | Model'de State=Main aktif çizgisi (yeşil, 5px) |
| `SwitchStateDivergingPen` | Model'de State=Diverging aktif çizgisi (turuncu, 5px) |

---

## 11. UYGULAMA SIRASI (IMPLEMENTATION ORDER)

| Adım | Dosya(lar) | İşlem | Doğrulama |
|------|-----------|-------|-----------|
| **F1** | `DomainEntities.cs` | RailSwitch entity güncelle | `dotnet build` temiz |
| **F2** | `ITool.cs` | PreviewSwitch → PreviewSwitchPlace, SwitchToolState kaldır | `dotnet build` temiz |
| **F3** | `SwitchDefaults.cs` | Yeni dosya: geometri sabitleri | `dotnet build` temiz |
| **F4** | `SwitchTool.cs` (eski) | SİL | - |
| **F5** | `SwitchTool.cs` (yeni) | Yeni dosya: prefab SwitchTool | `dotnet build` temiz |
| **F6** | `SetNodeRoleCommand.cs` | SİL | `dotnet build` temiz |
| **F7** | `CadColors.cs` | Renk paleti güncelle | `dotnet build` temiz |
| **F8** | `CadViewportControl.cs` | RenderToolLayer + RenderModelBake güncelle | `dotnet build` temiz |
| **F9** | `T270_SwitchToolTests.cs` (eski) | SİL | - |
| **F10** | `T270_SwitchToolTests.cs` (yeni) | Yeni testler (T270-T279) | `dotnet test` yeşil |
| **F11** | `EditorView.xaml`, `.xaml.cs` | Buton/metin güncelle (tool ismi aynı) | `dotnet build` temiz |
| **F12** | T010 eşik kontrolü | Test sayısı değişmediyse dokunma | T010 yeşil |
| **F13** | **Tüm çözüm test** | `dotnet test -c Release` | 197/197 (veya güncel) yeşil |

---

## 12. MANUEL TEST PLANI (M Serisi)

| # | Test | Adım | Beklenen |
|---|------|------|----------|
| M1 | SwitchTool'a geç | F8 tuşuna bas | İmleç ghost switch göstermeye başlar |
| M2 | Switch yerleştir | Boş alana sol tık | Switch sembolü + 3 port noktası görünür |
| M3 | Preview takibi | Fareyi gezdir | Ghost imleci takip eder |
| M4 | Escape | Escape tuşuna bas | Ghost kaybolur |
| M5 | Tekrar ghost | Fareyi hareket ettir | Ghost geri gelir |
| M6 | İkinci switch | Başka yere tıkla | 2. switch oluşur, toplam 2 switch |
| M7 | Undo | Ctrl+Z | Son switch + portları silinir |
| M8 | Redo | Ctrl+Y | Switch geri gelir |
| M9 | Track bağlantısı | TrackTool'a geç, switch portuna yakın tıkla | Endpoint snap port node'u yakalar |
| M10 | Segment çiz | Port'tan başka node'a segment çiz | Segment switch'e bağlanır |

---

## 13. KABUL KRİTERLERİ

- [ ] Tüm testler yeşil (Cad.Tests 116/116, tüm çözüm güncel)
- [ ] `dotnet build -c Release` 0 hata
- [ ] T010 bekçisi yeşil (eşik korunuyor)
- [ ] SwitchTool 1 tıkla prefab yerleştiriyor
- [ ] 3 port node'u otomatik oluşuyor
- [ ] Ghost preview fareyi takip ediyor
- [ ] Undo/Redo tüm entity'leri doğru yönetiyor
- [ ] TrackTool switch port node'una snap ile bağlanabiliyor
- [ ] Render: switch sembolü + port noktaları + aktif yol görseli
- [ ] Eskiden hiçbir kod/type kalmadı (PreviewSwitch, SwitchToolState, SetNodeRoleCommand)

---

## 14. RİSK DEĞERLENDİRMESİ

| Risk | Olasılık | Etki | Önlem |
|------|----------|------|-------|
| Render'da port node pozisyonları yanlış | Düşük | Orta | SwitchDefaults testleri (T272-T274) |
| TrackTool switch port'una snap yapamaz | Düşük | Yüksek | M9-M10 manuel test; port node'ları normal TrackNode |
| Undo sırası karışıklığı | Düşük | Orta | CompositeCadCommand sırası: node'lar önce, switch sonra |
| Eski kod kalıntısı (PreviewSwitch) | Orta | Düşük | F2'de ITool.cs'den temizle; `findstr` ile tarama |
| T010 eşik sapması | Düşük | Düşük | 10 sil + 10 ekle = net 0; F12'de kontrol et |

---

## 15. SAPIENT NOTE

Bu plan, AGENTS.md'deki şu kurallara uygun hazırlanmıştır:
- **Bölüm 1.4** — Kapsam sınırı (scope guard) zorunlu → Bölüm 1
- **Bölüm 2.3** — Planlama: dosya-dosya değişiklik listesi, test kimlik tablosu, kabul kriteri, manuel test → Bölüm 4, 9, 12, 13
- **Bölüm 3.2** — Gerçek test tanımı: her test üretim kodunu çağırır, anlamlı assert yapar → Bölüm 9.2
- **Bölüm 3.7** — Kimlik disiplini: T### kimlikleri taşır → Bölüm 9.2
- **Bölüm 5.3** — Arayüz bugünden doğru kurulur → PreviewSwitchPlace record'u
- **Bölüm 1.3** — Geriye dönük kırma yasak → Mevcut tool'lar değişmez
- **Bölüm 5** — 10x Mühendis Davranışı: Önce oku, sonra yaz → Tüm mevcut kod okundu

---

*Plan versiyonu: 1.0 — 2026-07-18*
*Bu plan onay beklemektedir. Onay alınmadan kod yazılmaz.*
