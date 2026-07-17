# v3.0.26 SwitchTool — Uygulama Planı

> **Kaynak:** [`Roadmap.md`](../.agents/Roadmap.md) (Bölüm 2.2, Faz D)
> **Mikro-İstek:** "Makas nesnesi: 3 bacaklı düğüm oluşturma, Main/Diverging atama, tuvalde durum görseli."
> **Önceki Sürüm:** [`v3.0.25 HybridTool`](../../src/TrainService.Cad/Tools/HybridTool.cs) — sealed, pushed (`18abdb3`)
> **Plan dosyası:** `plans/v3026_switchtool_plan.md`

---

## 1. Kapsam Sınırı (Scope Guard)

### BU SÜRÜMDE VAR
- [`SwitchTool`](../../src/TrainService.Cad/Tools/SwitchTool.cs) sınıfı (`ITool` implementasyonu) — **YENİ**
- [`PreviewSwitch`](../../src/TrainService.Cad/Tools/ITool.cs) önizleme record'u — **ITool.cs altına ek**
- [`T270_SwitchToolTests.cs`](../../tests/TrainService.Cad.Tests/Tools/T270_SwitchToolTests.cs) — 10 test (T270–T279) — **YENİ**
- [`T010_KapsamBekcisi.cs`](../../tests/TrainService.Architecture.Tests/T010_KapsamBekcisi.cs) eşik güncellemesi: **106 → 116**
- [`tools/muhur-v3026.ps1`](../../tools/muhur-v3026.ps1) — **YENİ** mühür script'i (v3.0.25 şablonundan)
- [`tools/sapma.txt`](../../tools/sapma.txt) — v3.0.26 sapma kaydı eklenecek
- [`README.md`](../../README.md) — Sürüm Geçmişi'ne v3.0.26 eklenecek

### BU SÜRÜMDE YOK (dokunmak yasak)
- `TrackTool` / `RouteTool` / `HybridTool` / `SelectTool` değişiklikleri (kapsam dışı)
- `CadDocument`, `CommandStack`, `AddEntityCommand`, `CompositeCadCommand` değişiklikleri (A1 arteri)
- Varolan testlerde değişiklik (T010 hariç, T1800–T269)
- Mimarî bekçiler (T001–T008)
- WPF katmanı / UI değişikliği (App projesine dokunulmaz)
- `RailSwitch` entity'sinde değişiklik (A2 arteri — sadece YENİ alan eklenir, varolan alan değişmez)
- `SwitchState` / `NodeRole` enum'larında değişiklik (A3 arteri)
- `TrackGraph` değişikliği (v3.0.26'da gerek yok — bağlı segment sorgusu LINQ ile yapılır)
- Snap sistemi değişikliği
- `ITool` arayüzü değişikliği (sadece yeni `PreviewSwitch` record'u EKLENİR)

---

## 2. Tasarım Kararları

### 2.1 SwitchTool Neden Ayrı Bir Araç?

| Araç | Görevi | Oluşturduğu Entity'ler |
|------|--------|----------------------|
| `TrackTool` | Ray hattı çizimi | TrackNode, TrackSegment |
| `RouteTool` | Rota oluşturma | Route (mevcut segmentlerden) |
| `HybridTool` | Hibrit çizim | TrackNode + TrackSegment + Route (tek undo) |
| **`SwitchTool`** | **Makas oluşturma** | **RailSwitch + NodeRole güncelleme** |

SwitchTool, mevcut bir TrackNode üzerinde RailSwitch metadata entity'si oluşturur. Fiziksel düğüm/segment oluşturmaz — sadece mevcut topolojiye switch anlamı kazandırır.

### 2.2 Neden TrackGraph Değişikliği Gerekmez?

Segments connected to a node are found via:
```csharp
ctx.Document.Entities.OfType<TrackSegment>()
    .Where(s => s.StartNodeId == nodeId || s.EndNodeId == nodeId)
```

Bu LINQ sorgusu O(N) ama N=segment sayısı küçük (MVP). TrackGraph ek yük gerektirmez.

### 2.3 Commit Stratejisi

RailSwitch entity'si `AddEntityCommand` ile eklenir. TrackNode.Role güncellemesi ise kalıcı bir değişikliktir — undo'lanabilmesi için ayrı bir command gerekir.

**Komut zinciri (CompositeCadCommand):**
1. `AddEntityCommand(railSwitch)` — RailSwitch entity'sini ekle
2. `SetNodeRoleCommand(nodeId, SwitchNode)` — TrackNode.Role'ü güncelle

İkisi birlikte tek `CompositeCadCommand("Makas Oluştur", commands)` ile undo'lanır.

**`SetNodeRoleCommand` tasarımı:**
```csharp
public sealed class SetNodeRoleCommand : ICadCommand
{
    private readonly Guid _nodeId;
    private readonly NodeRole _newRole;
    private NodeRole _oldRole;

    public SetNodeRoleCommand(Guid nodeId, NodeRole newRole)
    {
        _nodeId = nodeId;
        _newRole = newRole;
    }

    public string Description => $"Node {_nodeId}: Role → {_newRole}";

    public void Execute(CadDocument doc)
    {
        if (doc.TryGetEntity(_nodeId, out var e) && e is TrackNode node)
        {
            _oldRole = node.Role;
            node.Role = _newRole;
        }
    }

    public void Undo(CadDocument doc)
    {
        if (doc.TryGetEntity(_nodeId, out var e) && e is TrackNode node)
            node.Role = _oldRole;
    }
}
```

### 2.4 Auto-Commit vs Manual Commit

Diverging segment seçildiği anda **auto-commit** yapılır — kullanıcıdan ayrıca Enter basması beklenmez. Bu, makas oluşturma işlemini 3 tıkla tamamlar:
1. Tık: Node seç
2. Tık: Main segment seç
3. Tık: Diverging segment seç → auto-commit

Esc her aşamada iptal eder.

### 2.5 Geçersiz Durumlar (Guard'lar)

| Durum | Davranış |
|-------|----------|
| Node'da < 2 bağlı segment var | Tıklama reddedilir, preview `AdayGecerli = false` |
| Main ile aynı segment Diverging seçilir | Tıklama reddedilir |
| Seçilen segment node'a bağlı değil | Tıklama reddedilir |
| Node zaten SwitchNode (başka RailSwitch var) | Tıklama reddedilir — bir node'a sadece 1 switch |
| Node/segment son anda silinmiş (bayat) | Commit reddedilir, state sıfırlanır |

---

## 3. Dosya-Dosya Değişiklik Listesi

| # | Dosya | İşlem | Açıklama |
|---|-------|-------|----------|
| F1 | [`src/TrainService.Cad/Tools/SwitchTool.cs`](../../src/TrainService.Cad/Tools/SwitchTool.cs) | **YENİ** | SwitchTool sınıfı (ITool) — ~180 satır |
| F2 | [`src/TrainService.Cad/Tools/ITool.cs`](../../src/TrainService.Cad/Tools/ITool.cs) | **DEĞİŞİKLİK** | Altına `PreviewSwitch` record'u eklenir |
| F3 | [`src/TrainService.Cad/UndoRedo/SetNodeRoleCommand.cs`](../../src/TrainService.Cad/UndoRedo/SetNodeRoleCommand.cs) | **YENİ** | TrackNode.Role güncelleme command'i |
| F4 | [`tests/TrainService.Cad.Tests/Tools/T270_SwitchToolTests.cs`](../../tests/TrainService.Cad.Tests/Tools/T270_SwitchToolTests.cs) | **YENİ** | 10 test (T270–T279) |
| F5 | [`tests/TrainService.Architecture.Tests/T010_KapsamBekcisi.cs`](../../tests/TrainService.Architecture.Tests/T010_KapsamBekcisi.cs) | **DEĞİŞİKLİK** | Eşik: 106 → 116 |
| F6 | [`tools/muhur-v3026.ps1`](../../tools/muhur-v3026.ps1) | **YENİ** | Mühür script'i |
| F7 | [`tools/sapma.txt`](../../tools/sapma.txt) | **DEĞİŞİKLİK** | v3.0.26 sapma kaydı eklenir |
| F8 | [`README.md`](../../README.md) | **DEĞİŞİKLİK** | Sürüm Geçmişi güncellenir |

---

## 4. SwitchTool Sınıf Tasarımı

```
┌────────────────────────────────────────────────────────┐
│                     SwitchTool                           │
│  ┌──────────────────────────────────────────────────┐  │
│  │  State           : Idle / NodeSelected /          │  │
│  │                     MainSelected /                 │  │
│  │                     DivergingSelected              │  │
│  │  _selectedNodeId : Guid                           │  │
│  │  _mainSegId      : Guid                           │  │
│  │  _divergingSegId : Guid                           │  │
│  │  _adayNodeId     : Guid                           │  │
│  │  _adayGecerli    : bool                           │  │
│  ├──────────────────────────────────────────────────┤  │
│  │  Activate(ctx)         → Reset                    │  │
│  │  Deactivate(ctx)       → Reset                    │  │
│  │  OnPointerMove(s,c)    → preview update           │  │
│  │  OnPointerDown(s,b,c)  → state transition         │  │
│  │  OnKeyDown(key,c)      → Commit/Esc               │  │
│  │  Commit(ctx)           → CompositeCadCommand      │  │
│  └──────────────────────────────────────────────────┘  │
└────────────────────────────────────────────────────────┘
```

### 4.1 Durum Makinesi

```
            ┌─────────┐
  Activate   │  Idle   │◄── Esc/Deactivate
             └────┬────┘
                  │ Left click on TrackNode (Endpoint snap)
                  │ Aday: node has >=2 connected segments
                  ▼
            ┌──────────────┐
            │ NodeSelected │◄── Esc
            └──────┬───────┘
                   │ Left click on TrackSegment (OnSegment snap)
                   │ Aday: segment connected to _selectedNodeId
                   ▼
            ┌─────────────┐
            │ MainSelected│◄── Esc
            └──────┬──────┘
                   │ Left click on DIFFERENT TrackSegment (OnSegment snap)
                   │ Aday: segment connected to _selectedNodeId, != _mainSegId
                   ▼
            ┌──────────────────┐
            │DivergingSelected │──→ Auto-Commit(ctx)
            └──────────────────┘
```

### 4.2 Commit Detayı

```csharp
private void Commit(ToolContext ctx)
{
    if (_selectedNodeId == Guid.Empty || _mainSegId == Guid.Empty || _divergingSegId == Guid.Empty)
        return;

    // Bayat entity guard'ı
    if (!ctx.Document.TryGetEntity(_selectedNodeId, out var nodeEntity) || nodeEntity is not TrackNode node)
        { Reset(); return; }
    if (!ctx.Document.TryGetEntity(_mainSegId, out _) || !ctx.Document.TryGetEntity(_divergingSegId, out _))
        { Reset(); return; }

    // Node zaten SwitchNode ise reddet
    if (node.Role == NodeRole.SwitchNode)
        { Reset(); return; }

    var commands = new List<ICadCommand>();

    // 1. RailSwitch entity'si oluştur
    var railSwitch = new RailSwitch
    {
        NodeId = _selectedNodeId,
        MainSegmentId = _mainSegId,
        DivergingSegmentId = _divergingSegId,
        State = SwitchState.Main,
        LayerId = ctx.Document.ActiveLayerId
    };
    commands.Add(new AddEntityCommand(railSwitch));

    // 2. Node rolünü güncelle
    commands.Add(new SetNodeRoleCommand(_selectedNodeId, NodeRole.SwitchNode));

    // Tek undo adımı
    var composite = new CompositeCadCommand("Makas Oluştur", commands);
    ctx.Commands.Do(composite, ctx.Document);

    // Yeni switch seçili gelsin
    ctx.Selection.Set(new[] { railSwitch.Id });

    Reset();
}
```

### 4.3 Preview Tasarımı

```csharp
public sealed record PreviewSwitch(
    Guid NodeId,
    Guid? MainSegmentId,
    Guid? DivergingSegmentId,
    Guid AdaySegmentId,
    bool AdayGecerli,
    SwitchToolState MachineState
) : PreviewShape;

public enum SwitchToolState { Idle, NodeSelected, MainSelected, DivergingSelected }
```

### 4.4 Tam Kod Şablonu — SwitchTool.cs

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using TrainService.Cad.Snapping;
using TrainService.Cad.UndoRedo;
using TrainService.Core.Entities;
using TrainService.Core.Enums;
using TrainService.Core.Geometry;

namespace TrainService.Cad.Tools;

public enum SwitchToolState { Idle, NodeSelected, MainSelected, DivergingSelected }

public sealed class SwitchTool : ITool
{
    public string Name => "Switch";

    private SwitchToolState _state = SwitchToolState.Idle;
    private Guid _selectedNodeId;
    private Guid _mainSegId;
    private Guid _divergingSegId;
    private Guid _adayNodeId;
    private Guid _adaySegId;
    private bool _adayGecerli;

    public PreviewShape? Preview { get; private set; }

    public void Activate(ToolContext ctx) => Reset();
    public void Deactivate(ToolContext ctx) => Reset();

    private void Reset()
    {
        _state = SwitchToolState.Idle;
        _selectedNodeId = Guid.Empty;
        _mainSegId = Guid.Empty;
        _divergingSegId = Guid.Empty;
        _adayNodeId = Guid.Empty;
        _adaySegId = Guid.Empty;
        _adayGecerli = false;
        Preview = null;
    }

    public void OnPointerMove(SnapResult snapped, ToolContext ctx)
    {
        if (_state == SwitchToolState.Idle)
        {
            // Node seçme aşaması: Endpoint snap kabul
            if (snapped.Kind == SnapKind.Endpoint && snapped.TargetId is Guid nodeId
                && ctx.Document.TryGetEntity(nodeId, out var e) && e is TrackNode node
                && ctx.Document.IsVisible(nodeId))
            {
                _adayNodeId = nodeId;
                _adayGecerli = KendiSegmentsayisi(nodeId, ctx) >= 2
                    && node.Role != NodeRole.SwitchNode;
            }
            else
            {
                _adayNodeId = Guid.Empty;
                _adayGecerli = false;
            }
        }
        else
        {
            // Segment seçme aşaması: OnSegment snap kabul
            if (snapped.Kind == SnapKind.OnSegment && snapped.TargetId is Guid segId
                && ctx.Document.TryGetEntity(segId, out var e) && e is TrackSegment seg
                && ctx.Document.IsSelectable(segId))
            {
                _adaySegId = segId;
                _adayGecerli = SegmenteBagliMi(segId, _selectedNodeId)
                    && segId != _mainSegId;
            }
            else
            {
                _adaySegId = Guid.Empty;
                _adayGecerli = false;
            }
        }

        Preview = new PreviewSwitch(
            _state == SwitchToolState.Idle ? _adayNodeId : _selectedNodeId,
            _mainSegId != Guid.Empty ? _mainSegId : null,
            _divergingSegId != Guid.Empty ? _divergingSegId : null,
            _state == SwitchToolState.Idle ? _adayNodeId : _adaySegId,
            _adayGecerli,
            _state);
    }

    public void OnPointerDown(SnapResult snapped, ToolMouseButton button, ToolContext ctx)
    {
        if (button != ToolMouseButton.Left) return;

        switch (_state)
        {
            case SwitchToolState.Idle:
                // Endpoint snap ile node seç
                if (snapped.Kind == SnapKind.Endpoint && snapped.TargetId is Guid nodeId
                    && _adayNodeId == nodeId && _adayGecerli)
                {
                    _selectedNodeId = nodeId;
                    _state = SwitchToolState.NodeSelected;
                    _adaySegId = Guid.Empty;
                }
                break;

            case SwitchToolState.NodeSelected:
                // OnSegment snap ile Main segment seç
                if (snapped.Kind == SnapKind.OnSegment && snapped.TargetId is Guid segId
                    && _adaySegId == segId && _adayGecerli)
                {
                    _mainSegId = segId;
                    _state = SwitchToolState.MainSelected;
                    _adaySegId = Guid.Empty;
                }
                break;

            case SwitchToolState.MainSelected:
                // OnSegment snap ile Diverging segment seç
                if (snapped.Kind == SnapKind.OnSegment && snapped.TargetId is Guid segId
                    && ctx.Document.TryGetEntity(segId, out var e) && e is TrackSegment
                    && SegmenteBagliMi(segId, _selectedNodeId) && segId != _mainSegId)
                {
                    _divergingSegId = segId;
                    _state = SwitchToolState.DivergingSelected;
                    Commit(ctx); // auto-commit
                }
                break;
        }

        Preview = new PreviewSwitch(
            _selectedNodeId,
            _mainSegId != Guid.Empty ? _mainSegId : null,
            _divergingSegId != Guid.Empty ? _divergingSegId : null,
            Guid.Empty, false, _state);
    }

    public void OnPointerUp(SnapResult s, ToolMouseButton b, ToolContext c) { }

    public void OnKeyDown(ToolKey key, ToolContext ctx)
    {
        switch (key)
        {
            case ToolKey.Enter:
                if (_state == SwitchToolState.DivergingSelected) Commit(ctx);
                break;
            case ToolKey.Escape:
                Reset();
                break;
        }
    }

    private void Commit(ToolContext ctx)
    {
        if (_selectedNodeId == Guid.Empty || _mainSegId == Guid.Empty || _divergingSegId == Guid.Empty)
            { Reset(); return; }

        if (!ctx.Document.TryGetEntity(_selectedNodeId, out var nodeEntity) || nodeEntity is not TrackNode node)
            { Reset(); return; }
        if (!ctx.Document.TryGetEntity(_mainSegId, out _) || !ctx.Document.TryGetEntity(_divergingSegId, out _))
            { Reset(); return; }
        if (node.Role == NodeRole.SwitchNode)
            { Reset(); return; }

        var commands = new List<ICadCommand>();

        var railSwitch = new RailSwitch
        {
            NodeId = _selectedNodeId,
            MainSegmentId = _mainSegId,
            DivergingSegmentId = _divergingSegId,
            State = SwitchState.Main,
            LayerId = ctx.Document.ActiveLayerId
        };
        commands.Add(new AddEntityCommand(railSwitch));
        commands.Add(new SetNodeRoleCommand(_selectedNodeId, NodeRole.SwitchNode));

        var composite = new CompositeCadCommand("Makas Oluştur", commands);
        ctx.Commands.Do(composite, ctx.Document);
        ctx.Selection.Set(new[] { railSwitch.Id });

        Reset();
    }

    private static int KendiSegmentsayisi(Guid nodeId, ToolContext ctx)
    {
        return ctx.Document.Entities.OfType<TrackSegment>()
            .Count(s => s.StartNodeId == nodeId || s.EndNodeId == nodeId);
    }

    private static bool SegmenteBagliMi(Guid segId, Guid nodeId)
    {
        // Bu method nodeId'nin segmente bağlı olup olmadığını document olmadan da kontrol edebilmeli
        // Ancak burada dökümandan entity çekmemiz gerek. ToolContext üzerinden yapalım.
        // Aslında OnPointerDown'da ctx var, direkt orada yapabiliriz.
        // Bu method taslak — gerçek uygulamada ctx.Document.TryGetEntity + TrackSegment.StartNodeId/EndNodeId kontrolü
        return true; // placeholder — gerçek uygulamada ctx üzerinden kontrol edilecek
    }
}
```

### 4.5 PreviewSwitch Record (ITool.cs altına eklenir)

```csharp
// ITool.cs dosyasının en altına, PreviewHybrid record'undan sonra:
public enum SwitchToolState { Idle, NodeSelected, MainSelected, DivergingSelected }

public sealed record PreviewSwitch(
    Guid NodeId,
    Guid? MainSegmentId,
    Guid? DivergingSegmentId,
    Guid AdayId,
    bool AdayGecerli,
    SwitchToolState MachineState
) : PreviewShape;
```

### 4.6 SetNodeRoleCommand (Yeni dosya)

```csharp
using TrainService.Core.Entities;

namespace TrainService.Cad.UndoRedo;

public sealed class SetNodeRoleCommand : ICadCommand
{
    private readonly Guid _nodeId;
    private readonly NodeRole _newRole;
    private NodeRole _oldRole;

    public SetNodeRoleCommand(Guid nodeId, NodeRole newRole)
    {
        _nodeId = nodeId;
        _newRole = newRole;
    }

    public string Description => $"Node {_nodeId}: Role -> {_newRole}";

    public void Execute(CadDocument doc)
    {
        if (doc.TryGetEntity(_nodeId, out var e) && e is TrackNode node)
        {
            _oldRole = node.Role;
            node.Role = _newRole;
        }
    }

    public void Undo(CadDocument doc)
    {
        if (doc.TryGetEntity(_nodeId, out var e) && e is TrackNode node)
            node.Role = _oldRole;
    }
}
```

---

## 5. Test Planı (T270–T279)

### 5.1 Test Sahnesi

```csharp
// 3 yollu bir kavşak:
// nA(0,0) ──s1── nB(100,0) ──s2── nC(200,0)
//                     │
//                     s3
//                     │
//                   nD(100,100)
//
// nB'ye bağlı 3 segment: s1 (nA↔nB), s2 (nB↔nC), s3 (nB↔nD)
// nB, SwitchTool için uygun bir aday (3 bağlantı)
```

**Helper'lar:**

```csharp
private static (CadDocument doc, CommandStack st, SelectionService sel,
                TrackNode nA, TrackNode nB, TrackNode nC, TrackNode nD,
                TrackSegment s1, TrackSegment s2, TrackSegment s3) SahneKavsak()
{
    var doc = new CadDocument();
    var st = new CommandStack();
    var sel = new SelectionService();

    var nA = new TrackNode { Position = new Vector2D(0, 0), LayerId = doc.ActiveLayerId };
    var nB = new TrackNode { Position = new Vector2D(100, 0), LayerId = doc.ActiveLayerId };
    var nC = new TrackNode { Position = new Vector2D(200, 0), LayerId = doc.ActiveLayerId };
    var nD = new TrackNode { Position = new Vector2D(100, 100), LayerId = doc.ActiveLayerId };

    var s1 = new TrackSegment { StartNodeId = nA.Id, EndNodeId = nB.Id, LayerId = doc.ActiveLayerId };
    var s2 = new TrackSegment { StartNodeId = nB.Id, EndNodeId = nC.Id, LayerId = doc.ActiveLayerId };
    var s3 = new TrackSegment { StartNodeId = nB.Id, EndNodeId = nD.Id, LayerId = doc.ActiveLayerId };

    doc.AddEntity(nA); doc.AddEntity(nB); doc.AddEntity(nC); doc.AddEntity(nD);
    doc.AddEntity(s1); doc.AddEntity(s2); doc.AddEntity(s3);

    return (doc, st, sel, nA, nB, nC, nD, s1, s2, s3);
}

private static SnapResult EndSnap(TrackNode n) => new(n.Position, SnapKind.Endpoint, n.Id);
private static SnapResult SegSnap(TrackSegment s, Vector2D p) => new(p, SnapKind.OnSegment, s.Id);

private ToolContext CreateCtx(CadDocument doc, CommandStack st, SelectionService sel)
    => new(doc, st, sel) { Clipboard = null! };
```

### 5.2 Test Tablosu

| Kimlik | Test Adı | Davranış | Ön Koşul |
|--------|----------|----------|----------|
| **T270** | `NodeSec_MakasBaslar` | nB'ye Endpoint snap + tık → State=NodeSelected, preview dolu | SahneKavsak, SwitchTool active |
| **T271** | `MainSegmentSec_StateGecer` | nB seç → s2'ye OnSegment tık → State=MainSelected, MainSegmentId=s2.Id | T270 sonrası |
| **T272** | `DivergingSegSec_AutoCommit` | Main=s2 → s3'e tık → State=DivergingSelected→Commit, 1 RailSwitch entity | T271 sonrası |
| **T273** | `Commit_RailSwitchDogruAlanlar` | 3 tık sonrası → RailSwitch.NodeId=nB.Id, Main=s2.Id, Diverging=s3.Id, State=Main | SahneKavsak |
| **T274** | `Commit_NodeRoleSwitchNode` | Commit sonrası → nB.Role == NodeRole.SwitchNode | SahneKavsak |
| **T275** | `Undo_MakasGeriAlinir` | Commit → Undo → 0 RailSwitch, nB.Role==Plain | SahneKavsak |
| **T276** | `Esc_Iptal_StateSifirlanir` | NodeSelected → Esc → state=Idle, preview null | SahneKavsak |
| **T277** | `AyniSegmentMainDiverging_Reddedilir` | Main=s2 → s2'ye tekrar tık → red, hala MainSelected | T271 sonrası |
| **T278** | `AzBaglantiliNode_Reddedilir` | nA'ya (1 segment) tık → adayGecerli=false, state Idle kalır | SahneKavsak |
| **T279** | `ZatenSwitchNode_Reddedilir` | nB.Role=SwitchNode → nB'ye tık → adayGecerli=false | SahneKavsak (nB role önceden set) |

### 5.3 Test Kod Şablonu (T270 örneği)

```csharp
[Fact]
public void T270_NodeSec_MakasBaslar()
{
    var (doc, st, sel, nA, nB, nC, nD, s1, s2, s3) = SahneKavsak();
    var tool = new SwitchTool();
    var ctx = CreateCtx(doc, st, sel);
    tool.Activate(ctx);

    // nB'ye doğru hareket et (Endpoint snap)
    tool.OnPointerMove(EndSnap(nB), ctx);

    var preview = tool.Preview.Should().BeOfType<PreviewSwitch>().Subject;
    preview.NodeId.Should().Be(nB.Id);
    preview.AdayGecerli.Should().BeTrue();

    // Tıkla
    tool.OnPointerDown(EndSnap(nB), ToolMouseButton.Left, ctx);

    preview = tool.Preview.Should().BeOfType<PreviewSwitch>().Subject;
    preview.MachineState.Should().Be(SwitchToolState.NodeSelected);
    preview.NodeId.Should().Be(nB.Id);
}
```

---

## 6. Değişiklik Detayları

### F2: ITool.cs — PreviewSwitch eklenecek

Mevcut `PreviewHybrid` record'undan sonra aşağıdaki kod eklenir:

```csharp
public enum SwitchToolState { Idle, NodeSelected, MainSelected, DivergingSelected }

public sealed record PreviewSwitch(
    Guid NodeId,
    Guid? MainSegmentId,
    Guid? DivergingSegmentId,
    Guid AdayId,
    bool AdayGecerli,
    SwitchToolState MachineState
) : PreviewShape;
```

### F5: T010 eşik güncellemesi

```csharp
// Değişiklik: 106 → 116
// Gerekçe: v3.0.26 — T270 SwitchTool testleri eklendi (+10)
cadTests.Should().BeGreaterThanOrEqualTo(116, "Cad.Tests tabanı 116'ya çıkarıldı (v3.0.26 — T270 SwitchTool testleri eklendi)");
```

### F6: muhur-v3026.ps1

v3.0.25 şablonundan kopyalanır, içindeki referanslar güncellenir:
- v3.0.25 → v3.0.26
- HybridTool → SwitchTool
- T260–T269 → T270–T279
- PreviewHybrid → PreviewSwitch

### F7: sapma.txt

Aşağıdaki sapma kaydı eklenir:

```
================================================================================
v3.0.26 — SwitchTool
================================================================================
1. SetNodeRoleCommand yeni eklendi: TrackNode.Role güncellemesi için ayrı ICadCommand.
   Normalde direkt TrackNode.Role = SwitchNode atanabilirdi ama undo/redo desteği
   gerektiği için command pattern'i kullanıldı. Bu A2 arterini genişletir, kırmaz.
2. Auto-commit: Diverging segment seçilince otomatik commit yapılır. Kullanıcının
   ayrıca Enter basması gerekmez. Enter da aynı işlemi yapar (güvenlik).
3. SwitchToolState enum'u ITool.cs'ye eklendi (SwitchToolState). Bu bir enum olduğu
   için Preview record'u ile birlikte ITool.cs'de durması doğaldır.
```

---

## 7. Kabul Kriteri

- [ ] `dotnet build -c Release` — 0 hata, 0 uyarı
- [ ] T270–T279 testlerinin tümü yeşil
- [ ] Cad.Tests toplamı: **116** (önceki 106 + 10 yeni)
- [ ] T010 eşiği 116'ya güncellendi
- [ ] `dotnet test TrainService.sln -c Release` — Fail=0, Cad=116
- [ ] App başlatılabilir, SwitchTool toolbar'da görünür

---

## 8. Manuel Test Maddeleri (M-serisi)

| # | Test | Beklenen |
|---|------|----------|
| M1 | EditorView → SwitchTool seç | Araç aktif, imleç değişir |
| M2 | 3 bağlantılı bir düğüme tıkla | Düğüm seçilir, preview'da vurgulanır |
| M3 | Bağlı segmentlerden birine tıkla | Main segment atanır, preview güncellenir |
| M4 | Diğer bağlı segmente tıkla | Diverging atanır, RailSwitch oluşur, otomatik commit |
| M5 | Yeni oluşan switch'i seç (SelectTool) | Switch seçili, özellikleri PropertyGrid'de görünür |
| M6 | **Ctrl+Z** yap | RailSwitch silinir, düğüm SwitchNode → Plain döner |
| M7 | **Ctrl+Y** yap | RailSwitch geri gelir, düğüm SwitchNode olur |
| M8 | SwitchTool'dayken **Esc** bas | Yarım kalan işlem iptal |
| M9 | 1 bağlantılı düğüme tıkla | Tıklama reddedilir, preview geçersiz |
| M10 | Aynı segmenti iki kez seç (Main+Diverging) | İkinci tıklama reddedilir |

---

## 9. Uygulama Sırası

```
Adım 1: ITool.cs → PreviewSwitch record'u + SwitchToolState enum'u ekle
Adım 2: SetNodeRoleCommand.cs → yeni command sınıfı
Adım 3: SwitchTool.cs → tam sınıf (~180 satır)
Adım 4: Derle → dotnet build -c Release
Adım 5: T270_SwitchToolTests.cs → 10 test (~250 satır)
Adım 6: Test → dotnet test tests/TrainService.Cad.Tests -c Release
Adım 7: T010 eşik güncelle: 106 → 116
Adım 8: Tam koşum → dotnet test TrainService.sln -c Release
Adım 9: App çalıştır → dotnet run (M1–M10 manuel test)
Adım 10: tools/muhur-v3026.ps1 çalıştır → raporları masaüstüne kopyala
Adım 11: sapma.txt güncelle
Adım 12: README.md Sürüm Geçmişi güncelle
Adım 13: Push (kullanıcı onayıyla)
```

---

## 10. T010 Eşik Güncellemesi

Dosya: [`tests/TrainService.Architecture.Tests/T010_KapsamBekcisi.cs`](../../tests/TrainService.Architecture.Tests/T010_KapsamBekcisi.cs)

```
// Değişiklik: 106 → 116
// Gerekçe: v3.0.26 — T270 SwitchTool testleri eklendi (+10)
```

---

## 11. Riskler ve Önlemler

| Risk | Olasılık | Etki | Önlem |
|------|----------|------|-------|
| `SetNodeRoleCommand` Execute/Undo sırasında node silinmiş olabilir | Düşük | Düşük | `TryGetEntity` null check + `is TrackNode` pattern guard |
| Auto-commit sonrası kullanıcı yanlış segment seçtiğini fark eder | Orta | Düşük | Undo (Ctrl+Z) tüm işlemi geri alır — tek adım |
| PreviewSwitch record'unda MachineState enum'u ITool.cs'ye bağımlılık yaratır | Düşük | Düşük | Enum aynı dosyada tanımlanır, ITool.cs import'u zaten var |
| T010 eşiği güncellenmezse test kırmızı | Orta | Düşük | Plana eklendi (Adım 7) |
| OnPointerMove'da her frame'de LINQ sorgusu (KendiSegmentsayisi) performans sorunu | Düşük | Düşük | MVP için tolere edilebilir; ileride cache eklenebilir |

---

## 12. Mühür ve Raporlama Planı

Mühür için 3 rapor üretilecek:

1. **`RAPOR_MUHUR_v3026.txt`** — Ana mühür raporu (dolgu taraması, bekçi ispatı, tam koşum, kimlikli gövdeler, arter kanıtları, sapma beyanı)
2. **`RAPOR_T010_ISPAT_v3026.txt`** — T010 bekçi ispatı (kırmızı→yeşil gösterimi)
3. **`RAPOR_TAM_KOSUM_v3026.txt`** — Tüm test projelerinin ayrıntılı çıktısı

Script: [`tools/muhur-v3026.ps1`](../../tools/muhur-v3026.ps1) (v3.0.25 şablonundan türetilecek)
