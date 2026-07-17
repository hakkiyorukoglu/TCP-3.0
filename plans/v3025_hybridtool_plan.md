# v3.0.25 HybridTool — Uygulama Planı

> **Kaynak:** [`Roadmap.md:567`](../.agents/Roadmap.md:567)
> **Mikro-İstek:** "HybridTool (Eşzamanlı): Tek harekette Track+Route, tek undo adımı (CompositeCommand). Hibrit çizim tek Ctrl+Z ile geri alınır."
> **Önceki Sürüm:** [`v3.0.24 RouteTool`](../../src/TrainService.Cad/Tools/RouteTool.cs) — sealed, pushed (`340a15a`)
> **Plan dosyası:** `plans/v3025_hybridtool_plan.md`

---

## 1. Kapsam Sınırı (Scope Guard)

### BU SÜRÜMDE VAR
- [`HybridTool`](../../src/TrainService.Cad/Tools/HybridTool.cs) sınıfı (`ITool` implementasyonu) — **YENİ**
- [`PreviewHybrid`](../../src/TrainService.Cad/Tools/ITool.cs) önizleme record'u — **ITool.cs altına ek**
- [`T260_HybridToolTests.cs`](../../tests/TrainService.Cad.Tests/Tools/T260_HybridToolTests.cs) — 10 test (T260–T269) — **YENİ**
- [`T010_KapsamBekcisi.cs`](../../tests/TrainService.Architecture.Tests/T010_KapsamBekcisi.cs) eşik güncellemesi: 96 → **106**

### BU SÜRÜMDE YOK (dokunmak yasak)
- `TrackTool` / `RouteTool` / `SelectTool` değişiklikleri (kapsam dışı)
- `CadDocument`, `CommandStack`, `AddEntityCommand` değişiklikleri (A1 arteri)
- Varolan testlerde değişiklik (T231–T257, T010–T011)
- Mimarî bekçiler (T001–T008)
- WPF katmanı / UI değişikliği (App projesine dokunulmaz)
- `ITool` arayüzü değişikliği (sadece yeni `PreviewHybrid` record'u EKLENİR)

---

## 2. Tasarım Kararları

### 2.1 Neden Birikmeli Commit?

| Araç | Commit Stratejisi | Undo |
|------|-------------------|------|
| `TrackTool` | Her tıklamada ayrı `CompositeCadCommand` | Her segment kendi undo adımı |
| `RouteTool` | Sadece Enter/RightClick'te `AddEntityCommand(route)` | Sadece route undo'lanır |
| **`HybridTool`** | **Enter/RightClick'te TEK `CompositeCadCommand`** | **Tüm zincir TEK Ctrl+Z ile geri alınır** |

TrackTool her tıklamada commit eder (her segment kendi undo adımı). HybridTool ise tüm zinciri TEK `CompositeCadCommand` ile commit eder — roadmap'teki "tek undo adımı" gereksinimi budur.

### 2.2 Preview Stratejisi

İki ayrı önizleme aynı anda:
- **Çizgi önizleme:** TrackTool'daki gibi son düğüm ↔ imleç (geçerli segment ön izlemesi)
- **Rota önizleme:** RouteTool'daki gibi birikmiş adımlar + aday segment

İkisini birden taşımak için yeni [`PreviewHybrid`](../../src/TrainService.Cad/Tools/ITool.cs) record'u.

### 2.3 Snap Kuralı

Sadece `OnSegment` snap kabul edilir (RouteTool kuralı). Endpoint/Grid snap reddedilir — hybrid çizim rotaya eklenecek bir segment üretmelidir.

### 2.4 Yön Belirleme (RouteTool ile aynı strateji)

```
1. tık (segA)      → RouteStep(segA, Forward) [geçici]
2. tık (segB)      → pendSeg [segA]
                       _steps.Count==0 → Forward [geçici]
3. tık (segC)      → pendSeg [segA, segB, segC]
                       _steps.Count==1 → segA yönü KESİNLEŞİR (segA↔segB ortak düğüme göre)
                       segB yönü eklenir (segB↔segC ortak düğüme göre)
4. tık (segD)      → pendSeg [segA, segB, segC, segD]
                       _steps.Count==2 → segC yönü EKLENİR (atla, zaten doğru)
                                          segD yönü eklenir (segC↔segD ortak düğüme göre)
```

Yön belirleme, `_pendingSegments[^2]` (son eklenen segmentten bir önceki) ile yeni segment arasındaki ortak düğüme bakılarak yapılır.

---

## 3. Dosya-Dosya Değişiklik Listesi

| # | Dosya | İşlem | Açıklama |
|---|-------|-------|----------|
| F1 | [`src/TrainService.Cad/Tools/HybridTool.cs`](../../src/TrainService.Cad/Tools/HybridTool.cs) | **YENİ** | HybridTool sınıfı (ITool) — ~200 satır |
| F2 | [`src/TrainService.Cad/Tools/ITool.cs`](../../src/TrainService.Cad/Tools/ITool.cs) | **DEĞİŞİKLİK** | Altına `PreviewHybrid` record'u eklenir |
| F3 | [`tests/TrainService.Cad.Tests/Tools/T260_HybridToolTests.cs`](../../tests/TrainService.Cad.Tests/Tools/T260_HybridToolTests.cs) | **YENİ** | 10 test (T260–T269) |
| F4 | [`tests/TrainService.Architecture.Tests/T010_KapsamBekcisi.cs`](../../tests/TrainService.Architecture.Tests/T010_KapsamBekcisi.cs) | **DEĞİŞİKLİK** | Eşik: 96 → 106 |

---

## 4. HybridTool Sınıf Tasarımı

```
┌─────────────────────────────────────────────────────┐
│                   HybridTool                         │
│  ┌───────────────────────────────────────────────┐  │
│  │  State          : Idle / Chaining             │  │
│  │  _pendingNodes  : List<CadEntity>             │  │
│  │  _pendingSegments: List<CadEntity>            │  │
│  │  _steps         : List<RouteStep>             │  │
│  │  _chainTail     : TrackNode?                  │  │
│  │  _cursor        : Vector2D                    │  │
│  │  _adayId        : Guid                        │  │
│  │  _adayGecerli   : bool                        │  │
│  ├───────────────────────────────────────────────┤  │
│  │  Activate(ctx)         → Reset                │  │
│  │  Deactivate(ctx)       → Reset                │  │
│  │  OnPointerMove(s,c)    → preview update       │  │
│  │  OnPointerDown(s,b,c)  → add node/segment     │  │
│  │  OnKeyDown(key,c)      → Commit/Esc           │  │
│  │  Commit(ctx)           → CompositeCadCommand  │  │
│  └───────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────┘
```

### 4.1 Durum Makinesi

```
           ┌─────────┐
  Activate │  Idle   │◄── Esc/Deactivate
           └────┬────┘
                │ Left click (OnSegment snap kabul)
                ▼
           ┌──────────┐
           │ Chaining │◄── Left click (zincire ekle)
           └────┬─────┘
                │ Enter / Right click
                ▼
           Commit(ctx)
           - CompositeCadCommand:
               AddEntityCommand(node1..N)
               AddEntityCommand(seg1..N)
               AddEntityCommand(route)
           - Clear state → Idle
```

### 4.2 Commit Detayı

```csharp
private void Commit(ToolContext ctx)
{
    if (_pendingNodes.Count == 0) return;

    var guncelGraf = TrackGraph.Build(
        ctx.Document.Entities.OfType<TrackNode>(),
        ctx.Document.Entities.OfType<TrackSegment>());

    var commands = new List<ICadCommand>();
    foreach (var n in _pendingNodes) commands.Add(new AddEntityCommand(n));
    foreach (var s in _pendingSegments) commands.Add(new AddEntityCommand(s));

    if (_steps.Count > 0)
    {
        var rota = new Route { LayerId = ctx.Document.ActiveLayerId };
        rota.Steps.AddRange(_steps);
        rota.CachedBounds = HesaplaBounds(rota, ctx.Document);
        
        if (!guncelGraf.ValidateRoute(rota)) { Reset(); return; }
        commands.Add(new AddEntityCommand(rota));
        
        var composite = new CompositeCadCommand("Hybrid Rota", commands);
        ctx.Commands.Do(composite, ctx.Document);
        ctx.Selection.Set(new[] { rota.Id });
    }
    else
    {
        // Sadece TrackSegment var (henüz Route yok) — roadmap'de belirtilmemiş durum
        // Yine de geçerli: tek Composite ile ekle
        var composite = new CompositeCadCommand("Hybrid Ray", commands);
        ctx.Commands.Do(composite, ctx.Document);
    }
    
    Reset();
}
```

### 4.3 Preview Tasarımı

```csharp
public sealed record PreviewHybrid(
    Vector2D From, Vector2D To, bool SegmentGecerli,
    IReadOnlyList<RouteStep> Steps,
    Guid AdaySegmentId, bool AdayGecerli
) : PreviewShape;
```

### 4.4 Tam Kod Şablonu

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using TrainService.Cad.Snapping;
using TrainService.Cad.UndoRedo;
using TrainService.Core.Entities;
using TrainService.Core.Enums;
using TrainService.Core.Geometry;
using TrainService.Core.Topology;

namespace TrainService.Cad.Tools;

public sealed class HybridTool : ITool
{
    public string Name => "Hybrid";

    private enum State { Idle, Chaining }
    private State _state = State.Idle;
    private readonly List<CadEntity> _pendingNodes = new();
    private readonly List<CadEntity> _pendingSegments = new();
    private readonly List<RouteStep> _steps = new();
    private TrackNode? _chainTail;
    private Vector2D _cursor;
    private Guid _adayId;
    private bool _adayGecerli;

    public PreviewShape? Preview { get; private set; }

    public void Activate(ToolContext ctx) => Reset();
    public void Deactivate(ToolContext ctx) => Reset();

    private void Reset()
    {
        _state = State.Idle;
        _pendingNodes.Clear();
        _pendingSegments.Clear();
        _steps.Clear();
        _chainTail = null;
        _adayId = Guid.Empty;
        _adayGecerli = false;
        Preview = null;
    }

    private TrackNode YeniNode(Vector2D pos, ToolContext ctx)
    {
        double z = 0;
        if (ctx.Document.TryGetLayer(ctx.Document.ActiveLayerId, out var layer))
            z = layer.ZHeightMm;
        return new TrackNode { Position = pos, Z = z, LayerId = ctx.Document.ActiveLayerId };
    }

    public void OnPointerMove(SnapResult snapped, ToolContext ctx)
    {
        _cursor = snapped.Point;

        if (snapped.Kind == SnapKind.OnSegment && snapped.TargetId is Guid segId
            && ctx.Document.TryGetEntity(segId, out var e) && e is TrackSegment
            && ctx.Document.IsSelectable(segId))
        {
            _adayId = segId;
            _adayGecerli = GecerliAdayMi(segId, ctx);
        }
        else
        {
            _adayId = Guid.Empty;
            _adayGecerli = false;
        }

        Preview = _state == State.Idle
            ? new PreviewHybrid(default, _cursor, false, Array.Empty<RouteStep>(), _adayId, _adayGecerli)
            : new PreviewHybrid(_chainTail!.Position, _cursor,
                (_cursor - _chainTail.Position).Length > 1e-6,
                _steps, _adayId, _adayGecerli);
    }

    private bool GecerliAdayMi(Guid segId, ToolContext ctx)
    {
        if (_steps.Count == 0) return true;
        if (_steps.Any(a => a.SegmentId == segId)) return false;
        var graf = TrackGraph.Build(ctx.Document.Entities.OfType<TrackNode>(),
                                    ctx.Document.Entities.OfType<TrackSegment>());
        return graf.AreAdjacent(_steps[^1].SegmentId, segId);
    }

    public void OnPointerDown(SnapResult snapped, ToolMouseButton button, ToolContext ctx)
    {
        if (button == ToolMouseButton.Right) { Commit(ctx); return; }
        if (button != ToolMouseButton.Left) return;
        if (_adayId == Guid.Empty || !_adayGecerli) return;
        if (!ctx.Document.TryGetEntity(_adayId, out var e) || e is not TrackSegment) return;

        var pos = snapped.Point;
        var node = YeniNode(pos, ctx);
        _pendingNodes.Add(node);

        if (_state == State.Idle)
        {
            _chainTail = node;
            _state = State.Chaining;
        }
        else if (_state == State.Chaining && _chainTail != null)
        {
            double dist = (pos - _chainTail.Position).Length;
            if (dist <= 1e-6) return;

            var segment = new TrackSegment
            {
                StartNodeId = _chainTail.Id,
                EndNodeId = node.Id,
                LengthMm = dist,
                LayerId = ctx.Document.ActiveLayerId
            };
            _pendingSegments.Add(segment);

            if (_steps.Count == 0)
            {
                _steps.Add(new RouteStep(segment.Id, TravelDirection.Forward));
            }
            else
            {
                var onceki = (TrackSegment)_pendingSegments[^2];
                var ortak = OrtakDugum(onceki, segment);

                if (_steps.Count == 1)
                {
                    _steps[0] = new RouteStep(onceki.Id,
                        onceki.EndNodeId == ortak
                            ? TravelDirection.Forward
                            : TravelDirection.Backward);
                }

                _steps.Add(new RouteStep(segment.Id,
                    segment.StartNodeId == ortak
                        ? TravelDirection.Forward
                        : TravelDirection.Backward));
            }

            _chainTail = node;
        }

        Preview = new PreviewHybrid(_chainTail!.Position, _cursor,
            (_cursor - _chainTail.Position).Length > 1e-6, _steps, Guid.Empty, false);
    }

    public void OnPointerUp(SnapResult s, ToolMouseButton b, ToolContext c) { }

    public void OnKeyDown(ToolKey key, ToolContext ctx)
    {
        switch (key)
        {
            case ToolKey.Enter: Commit(ctx); break;
            case ToolKey.Escape: Reset(); break;
        }
    }

    private void Commit(ToolContext ctx)
    {
        if (_pendingNodes.Count == 0) return;

        var commands = new List<ICadCommand>();
        foreach (var n in _pendingNodes) commands.Add(new AddEntityCommand(n));
        foreach (var s in _pendingSegments) commands.Add(new AddEntityCommand(s));

        if (_steps.Count > 0)
        {
            var guncelGraf = TrackGraph.Build(
                ctx.Document.Entities.OfType<TrackNode>(),
                ctx.Document.Entities.OfType<TrackSegment>());

            var rota = new Route { LayerId = ctx.Document.ActiveLayerId };
            rota.Steps.AddRange(_steps);
            rota.CachedBounds = HesaplaBounds(rota, ctx.Document);

            if (!guncelGraf.ValidateRoute(rota)) { Reset(); return; }
            commands.Add(new AddEntityCommand(rota));

            var composite = new CompositeCadCommand("Hybrid Rota", commands);
            ctx.Commands.Do(composite, ctx.Document);
            ctx.Selection.Set(new[] { rota.Id });
        }
        else
        {
            var composite = new CompositeCadCommand("Hybrid Ray", commands);
            ctx.Commands.Do(composite, ctx.Document);
        }

        Reset();
    }

    private static Guid OrtakDugum(TrackSegment a, TrackSegment b)
    {
        if (a.StartNodeId == b.StartNodeId || a.StartNodeId == b.EndNodeId) return a.StartNodeId;
        if (a.EndNodeId == b.StartNodeId || a.EndNodeId == b.EndNodeId) return a.EndNodeId;
        return Guid.Empty;
    }

    private static BoundingBox HesaplaBounds(Route r, CadDocument doc)
    {
        double minX = double.MaxValue, minY = double.MaxValue,
               maxX = double.MinValue, maxY = double.MinValue;
        foreach (var st in r.Steps)
        {
            if (doc.TryGetEntity(st.SegmentId, out var e) && e is TrackSegment s
                && doc.TryGetEntity(s.StartNodeId, out var na) && na is TrackNode a
                && doc.TryGetEntity(s.EndNodeId, out var nb) && nb is TrackNode b)
            {
                minX = Math.Min(minX, Math.Min(a.Position.X, b.Position.X));
                maxX = Math.Max(maxX, Math.Max(a.Position.X, b.Position.X));
                minY = Math.Min(minY, Math.Min(a.Position.Y, b.Position.Y));
                maxY = Math.Max(maxY, Math.Max(a.Position.Y, b.Position.Y));
            }
        }
        return minX > maxX ? default : new BoundingBox(minX, minY, maxX, maxY);
    }
}
```

---

## 5. PreviewHybrid Record (ITool.cs altına eklenir)

```csharp
// ITool.cs dosyasının en altına, PreviewRoute record'undan sonra:
public sealed record PreviewHybrid(
    Vector2D From, Vector2D To, bool SegmentGecerli,
    IReadOnlyList<RouteStep> Steps,
    Guid AdaySegmentId, bool AdayGecerli
) : PreviewShape;
```

---

## 6. Test Planı (T260–T269)

| Kimlik | Test Adı | Davranış | Ön Koşul |
|--------|----------|----------|----------|
| **T260** | `IlkTik_ChainingBaslar` | 1. OnSegment tık → State=Chaining, 1 node pending, preview dolu | SahneYol, HybridTool active |
| **T261** | `IkinciTik_SegmentVeStepEklenir` | 2. komşu tık → 2 node, 1 segment pending, 1 RouteStep | T260 sonrası |
| **T262** | `UcTik_ZincirUcSegment` | 3 tık (3 segment zinciri) → 4 node, 3 segment, 3 step | SahneYol |
| **T263** | `Enter_Commit_TekUndo` | Enter → 1 Route entity, Ctrl+Z → 0 entity kalır (hepsi gider) | 2 tık sonrası |
| **T264** | `Esc_Iptal_HicbirSeyEklenmez` | Esc → dokümanda 0 yeni entity, preview null | 2 tık sonrası |
| **T265** | `OnSegmentDisiSnap_Reddedilir` | Endpoint snap → aday yok, tıklama yok sayılır | SahneYol |
| **T266** | `KomsuOlmayanTik_YokSayilir` | s1 tıkla → s3 tıkla (komşu değil) → adayGecerli=false, eklenmez | SahneYol |
| **T267** | `BayatGraf_CommitleReddeder` | 2 tık sonrası s2 sil → Enter → commit red, preview null | SahneYol |
| **T268** | `PreviewHybrid_IkiBilesen` | Preview hem From/To (çizgi) hem Steps (rota) taşır | 2 tık sonrası |
| **T269** | `UcSegmentRoute_YonlerDogru` | s1(0→100), s2(100→200), s3(200→300) → 3 step, tümü Forward | SahneYol (düz zincir) |

### Test Sahnesi (SahneDuzZincir)

```csharp
// T250'deki SahneYol pattern'inden farklı olarak DÜZ bir zincir:
// nA(0,0) ──s1── nB(100,0) ──s2── nC(200,0) ──s3── nD(300,0)
// s1 = nA→nB, s2 = nB→nC, s3 = nC→nD
```

T250'deki `SahneYol()`'da s2 = nC→nB (ters). HybridTool testleri için düz zincir gerekli.

### Test Helper'ları

```csharp
// T250'deki SegSnap() ve CreateCtx() aynen kullanılabilir
private static SnapResult SegSnap(TrackSegment s, Vector2D p)
    => new(p, SnapKind.OnSegment, s.Id);

private ToolContext CreateCtx(CadDocument doc, CommandStack st, SelectionService sel)
    => new(doc, st, sel) { Clipboard = null! };
```

---

## 7. Kabul Kriteri

- [ ] `dotnet build -c Release` — 0 hata, 0 uyarı
- [ ] T260–T269 testlerinin tümü yeşil
- [ ] Cad.Tests toplamı: **106** (önceki 96 + 10 yeni)
- [ ] T010 eşiği 106'ya güncellendi
- [ ] `dotnet test TrainService.sln -c Release` — Fail=0, Cad=106
- [ ] App başlatılabilir, HybridTool toolbar'da görünür

---

## 8. Manuel Test Maddeleri (M-serisi)

| # | Test | Beklenen |
|---|------|----------|
| M1 | EditorView → HybridTool seç | Araç aktif, imleç değişir |
| M2 | Bir ray parçası üstüne tıkla | TrackNode oluşur (beklemede) |
| M3 | Komşu segmente tıkla | TrackNode+TrackSegment+RouteStep eklenir, preview güncellenir |
| M4 | 3 segment boyunca tıkla | Zincir uzar, her adımda preview doğru |
| M5 | **Enter** bas | Route oluşur, tüm entity'ler dokümanda |
| M6 | **Ctrl+Z** yap | Tüm zincir (4 node + 3 segment + route) TEK adımda geri alınır |
| M7 | **Ctrl+Y** yap | Tüm zincir tek adımda geri gelir |
| M8 | HybridTool'dayken **Esc** bas | Yarım kalan zincir iptal, hiçbir şey eklenmez |
| M9 | Boş alana tıkla (snap yok) | Tıklama reddedilir |
| M10 | Birden fazla hybrid zincir çiz, her biri ayrı Ctrl+Z ile geri alınır | Her zincir bağımsız undo adımı |

---

## 9. Uygulama Sırası

```
Adım 1: ITool.cs → PreviewHybrid record'u ekle
Adım 2: HybridTool.cs → tam sınıf
Adım 3: Derle → dotnet build -c Release
Adım 4: T260_HybridToolTests.cs → 10 test
Adım 5: Test → dotnet test tests/TrainService.Cad.Tests -c Release
Adım 6: T010 eşik güncelle: 96 → 106
Adım 7: Tam koşum → dotnet test TrainService.sln -c Release
Adım 8: App çalıştır → dotnet run
Adım 9: Manuel test (M1–M10)
Adım 10: Seal raporu → tools/muhur.ps1 -Surum "v3.0.25"
Adım 11: Push (kullanıcı onayıyla)
```

---

## 10. T010 Eşik Güncellemesi

Dosya: [`tests/TrainService.Architecture.Tests/T010_KapsamBekcisi.cs`](../../tests/TrainService.Architecture.Tests/T010_KapsamBekcisi.cs)

```
// Değişiklik: 96 → 106
// Gerekçe: v3.0.25 — T260 HybridTool testleri eklendi (+10)
```

---

## 11. Riskler ve Önlemler

| Risk | Olasılık | Etki | Önlem |
|------|----------|------|-------|
| `_pendingSegments[^2]` indeks hatası (2. tıklamada Count=1 iken ^2 çağrılırsa) | Düşük | Yüksek | `_steps.Count == 0` branch'i bu durumu önler; `^2` sadece `_steps.Count > 0` branch'inde çağrılır |
| Preview'da `_chainTail` null referansı (Chaining state'inde) | Düşük | Orta | `_state == State.Chaining` kontrolü öncesinde null guard |
| Commit sonrası Route entity'si bayat segment referansı içerebilir | Düşük | Düşük | `ValidateRoute` güncel doküman entity'lerine karşı çalışır |
| T010 eşiği güncellenmezse test kırmızı | Orta | Düşük | Plana eklendi (Adım 6) |
