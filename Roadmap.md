
# Restoran Otonom Tren Servis Otomasyonu ve Simülasyonu (TCP)
## v3.0.0 Mimari Tasarım Dokümanı ve Sıfırdan Uygulama Yol Haritası

> **Platform:** .NET 8.0 · WPF (Wpf.Ui / Fluent Dark Mica) · MVVM (CommunityToolkit.Mvvm)
> **Veri:** SQLite + EF Core 8 · **Haberleşme:** MQTTnet v4 (Gömülü Broker) · **Firmware:** ESP32 (Arduino-C++), PlatformIO-CLI
> **Sürümleme İlkesi:** Her adım bağımsız, derlenebilir ve geri dönüşsüz mikro-sürümdür (v3.0.1, v3.0.2, ...).

---

# BÖLÜM 0 — MİMARİ ANAYASA (ANA ARTERLERİN KORUNMASI)

Bu projenin en kritik riski, yol haritasının ortasında "keşke şunu baştan yapsaydık" dedirtecek mimari kırılmalardır.
Bunu önlemek için aşağıdaki **5 Ana Arter**, daha hiçbir görsel özellik yazılmadan, **Faz 0 ve Faz 1'de eksiksiz ve genişletilebilir** olarak kurulur ve proje boyunca **asla** yeniden yazılmaz; sadece üzerine eklenir.

| # | Ana Arter | Neden İlk Kurulur? | Koruma Kuralı |
|---|-----------|--------------------|---------------|
| A1 | **Katmanlı Solution Yapısı + DI** | Tüm servisler arayüzler (interface) üzerinden konuşur; sonradan simülasyon veya donanım eklemek sadece yeni implementasyon eklemektir. | UI projesi asla Data/Messaging'e doğrudan referans vermez; her şey `Core`'daki arayüzler üzerinden akar. |
| A2 | **Domain Veri Modelleri (mm-bazlı geometri dahil)** | CAD, simülasyon ve firmware üretimi aynı modelleri okur. Model bir kez doğru kurulursa simülasyon "sadece bir tüketici" olur. | Modeller `TrainService.Core`'da yaşar; hiçbir modele UI tipi (Point, Brush vb.) sızamaz. Geometri `double` mm cinsindendir. |
| A3 | **MQTT Konu Sözleşmesi (Topic Contract)** | Gerçek ESP32 ile simüle ESP32 **aynı konuları** kullanır. Simülasyon en sonda geldiğinde, sistem farkı bile anlamaz (Digital Twin). | Konu şeması ve JSON payload sözleşmeleri v3.0.x'in başında dondurulur; sadece yeni konu **eklenebilir**, mevcut konu değiştirilemez. |
| A4 | **SQLite Şeması + State Recovery** | Senaryolar, çizimler, cihazlar ve son durum tek şemada. Migration disiplini ile şema sadece ileri gider. | Kolon silinmez/yeniden adlandırılmaz; her değişiklik EF Core migration'ı olarak eklenir. |
| A5 | **Merkezî Log Otobüsü (Terminal Bus)** | 4 satırlık kalıcı terminal panel her sayfada olduğu için, log altyapısı Gün 1'de kurulur; tüm servisler doğuştan bu otobüse yazar. | Loglama `ILogBus` arayüzü ile yapılır; hiçbir servis `Console.WriteLine` veya doğrudan UI'ya yazamaz. |

**Simülasyon Sözleşmesi (Digital Twin İlkesi):**
Simülasyon motoru, sisteme **sanal MQTT istemcileri** olarak bağlanır. Yani simüle edilmiş bir ESP32, gerçek ESP32 ile aynı konulara abone olur, aynı payload'ları üretir. Bu sayede:
- Uygulamanın geri kalanı (Mutfak, Dashboard, Loglar) gerçek/simüle ayrımı yapmaz.
- Simülasyonun en sona bırakılması hiçbir mimari borç yaratmaz; çünkü "takılacağı soket" (A3) baştan hazırdır.

---

# BÖLÜM 1 — SOLUTION VE PROJE KLASÖR YAPISI (v3.0.0 ARCHITECTURE)

```
TrainService.sln
│
├── src/
│   ├── TrainService.App/                     # WPF UI (Wpf.Ui, MVVM) — sadece görsel katman
│   │   ├── App.xaml / App.xaml.cs            # DI Host (Microsoft.Extensions.Hosting)
│   │   ├── Views/
│   │   │   ├── Shell/MainWindow.xaml         # NavigationView + Kalıcı Terminal Panel (alt dock)
│   │   │   ├── HomeView.xaml
│   │   │   ├── EditorView.xaml
│   │   │   ├── ElectronicsView.xaml
│   │   │   ├── KitchenView.xaml
│   │   │   ├── InfoView.xaml
│   │   │   └── SettingsView.xaml
│   │   ├── ViewModels/                       # Her View için 1 VM + TerminalPanelViewModel
│   │   ├── Controls/
│   │   │   ├── TerminalPanel.xaml            # 4 satırlık kalıcı log paneli (her sayfada)
│   │   │   ├── CadCanvas/                    # Editor'ün render host'u
│   │   │   │   ├── CadViewportControl.cs     # Pan/Zoom, dünya<->ekran dönüşümü
│   │   │   │   ├── CadRenderLayer.cs         # DrawingVisual tabanlı hızlı render
│   │   │   │   └── Adorners/                 # Snap işaretçileri, marquee, rubber-band
│   │   │   ├── NodeGraph/                    # ElectronicsView node-based şema kontrolleri
│   │   │   └── RadialMenu/                   # Sağ tık radyal menü
│   │   ├── Converters/  Behaviors/  Resources/ (Theme, ikonlar)
│   │   └── Services/                         # Sadece UI servisleri (Dialog, Navigation, Theme)
│   │
│   ├── TrainService.Core/                    # ★ A2: Domain modelleri + tüm arayüzler (bağımlılıksız)
│   │   ├── Geometry/                         # Vector2D, Vector3D, Polyline2D, Mat3 (mm bazlı)
│   │   ├── Entities/                         # TrackSegment, Route, Switch, Ramp, Station, Train...
│   │   ├── Enums/                            # LayerKind, LogLevel, DeviceKind, SwitchState...
│   │   ├── Messaging/Contracts/              # ★ A3: Topic sabitleri + Payload DTO'ları (record)
│   │   ├── Abstractions/                     # ILogBus, IMqttHub, IProjectRepository, ISimulationEngine,
│   │   │                                     #   IFirmwareManager, IDeviceRegistry, IStateRecovery...
│   │   └── Events/                           # Zayıf bağlı domain event'leri (Messenger)
│   │
│   ├── TrainService.Cad/                     # CAD çekirdeği (UI'sız, saf matematik — birim test edilebilir)
│   │   ├── Snapping/                         # SnapEngine, GridSnapProvider, EndpointSnapProvider
│   │   ├── Tools/                            # ITool durum makineleri: TrackTool, RouteTool, HybridTool...
│   │   ├── Selection/                        # MarqueeSelector, HitTester
│   │   ├── Clipboard/                        # CadClipboard (Kopyala/Kes/Yapıştır)
│   │   ├── UndoRedo/                         # Command pattern: ICadCommand, CommandStack
│   │   └── Topology/                         # TrackGraph: düğüm/kenar grafı, rota doğrulama, blok bölme
│   │
│   ├── TrainService.Messaging/               # ★ A3: Gömülü MQTTnet Broker + Hub + Device Registry
│   │   ├── EmbeddedBrokerService.cs
│   │   ├── MqttHub.cs                        # Publish/Subscribe soyutlaması (IMqttHub impl.)
│   │   ├── DeviceRegistry.cs                 # LWT + heartbeat ile cihaz online/offline takibi
│   │   └── PingService.cs                    # ICMP ping (Electronics port durumları)
│   │
│   ├── TrainService.Data/                    # ★ A4: EF Core + SQLite
│   │   ├── TrainDbContext.cs
│   │   ├── Migrations/
│   │   ├── Repositories/                     # ProjectRepository, ScenarioRepository, StateRepository
│   │   └── StateRecoveryService.cs           # Anlık durum yazma + açılışta geri yükleme
│   │
│   ├── TrainService.Firmware/                # C++ üretim + derleme + OTA
│   │   ├── Templates/                        # .scriban şablonları (station.cpp, train.cpp, config.h)
│   │   ├── FirmwareGenerator.cs              # Model -> C++ kaynak üretimi
│   │   ├── BuildService.cs                   # PlatformIO-CLI / Arduino-CLI Process sarmalayıcı
│   │   ├── OtaUploader.cs                    # espota (UDP 3232) veya HTTP OTA
│   │   └── FirmwareManager.cs                # Orkestrasyon: Generate -> Build -> Upload -> Verify
│   │
│   ├── TrainService.Simulation/              # ★ FINAL STAGE: Fizik motoru (Faz 0'da boş iskelet olarak var)
│   │   ├── Engine/                           # SimulationLoop (sabit zaman adımı), SimClock
│   │   ├── Physics/                          # TrainDynamics (ivme, sürtünme, rampa, fren)
│   │   ├── Kinematics/                       # ArcLengthPath: s-parametreli yol takibi, yönelim
│   │   ├── VirtualDevices/                   # VirtualStationEsp32, VirtualTrainController (C++ SM eşleniği)
│   │   └── Signaling/                        # BlockSignalController (blok işgal tablosu)
│   │
│   └── firmware/                             # Gerçek gömülü C++ projeleri (PlatformIO workspace)
│       ├── station_esp32/                    # Masa istasyonu firmware'i
│       ├── train_esp32c3/                    # Tren otonom kart firmware'i
│       └── common/                           # Ortak durum makinesi çekirdeği (NonBlockingTask.h)
│
├── tests/
│   ├── TrainService.Cad.Tests/               # Snap, topoloji, clipboard birim testleri
│   ├── TrainService.Core.Tests/
│   └── TrainService.Simulation.Tests/        # Fizik doğrulama (en son faz)
│
└── docs/  (Roadmap.md, TopicContract.md, DbSchema.md)
```

**Bağımlılık yönü (asla ters akmaz):**
`App → (Cad, Messaging, Data, Firmware, Simulation) → Core`
`Core` hiçbir projeye referans vermez. Simülasyon projesi Faz 0'da **boş iskelet** olarak oluşturulur; böylece son fazda eklenirken solution yapısı değişmez (A1 korunur).

---

# BÖLÜM 2 — CAD EDİTÖRÜ: MATEMATİKSEL VE SINIFSAL ALTYAPI

Editör genel amaçlı CAD değildir; **yalnızca demiryolu otomasyonu** primitifleri vardır: Ray, Hat (Rota), Makas, Rampa, İstasyon, Donanım-Bağı. Çember/yay/çokgen **yoktur**.

## 2.1 Koordinat Sistemi ve Dünya ↔ Ekran Dönüşümü

- **Dünya birimi:** milimetre, `double` hassasiyet. Model asla piksel bilmez.
- **Z ekseni:** katman yüksekliği (Zemin=0 mm, AltKat/ÜstKat ayarlanabilir). 2D çizim + Z alanı = 2.5D model; rampa interpolasyonu bu Z üzerinden yapılır.

```csharp
// TrainService.Core/Geometry
public readonly record struct Vector2D(double X, double Y)
{
    public double Length => Math.Sqrt(X * X + Y * Y);
    public Vector2D Normalized() { var l = Length; return l < 1e-9 ? default : new(X / l, Y / l); }
    public static double Dot(Vector2D a, Vector2D b) => a.X * b.X + a.Y * b.Y;
    public static double Cross(Vector2D a, Vector2D b) => a.X * b.Y - a.Y * b.X; // yönelim işareti
    public Vector2D PerpendicularCW() => new(Y, -X);
}
```

```csharp
// TrainService.App/Controls/CadCanvas — tek doğruluk noktası
public sealed class ViewportTransform
{
    public double Scale { get; private set; } = 1.0;   // piksel / mm
    public Vector2D PanOffset { get; private set; }     // mm cinsinden

    public Point WorldToScreen(Vector2D w) => new((w.X - PanOffset.X) * Scale,
                                                  (w.Y - PanOffset.Y) * Scale);
    public Vector2D ScreenToWorld(Point s)  => new(s.X / Scale + PanOffset.X,
                                                   s.Y / Scale + PanOffset.Y);
    // Zoom-to-cursor: imleç altındaki dünya noktası sabit kalacak şekilde
    public void ZoomAt(Point cursor, double factor) { /* pan = w - s/scale' */ }
}
```

**Render stratejisi:** Binlerce segmentte akıcılık için `Shape` nesneleri yerine **`DrawingVisual` havuzu** (retained-mode) kullanılır; sadece kirlenen (dirty) bölge yeniden çizilir. Snap işaretçileri, marquee ve rubber-band çizgileri ayrı bir **Adorner katmanında** yaşar — model render'ını asla kirletmez.

## 2.2 Varlık (Entity) Modeli

```csharp
public abstract class CadEntity
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid LayerId { get; set; }              // Zemin / AltKat / ÜstKat
    public bool IsSelected { get; set; }
    public abstract BoundingBox Bounds { get; }    // Marquee ve hit-test için
}

public sealed class TrackNode : CadEntity          // Ray düğümü (uç nokta / makas / rfid ankrajı)
{
    public Vector2D Position { get; set; }         // mm
    public double Z { get; set; }                  // katman yüksekliği (rampa interpolasyonunda değişir)
    public List<Guid> ConnectedSegments { get; } = new();
    public NodeRole Role { get; set; }             // Plain, SwitchNode, RfidAnchor, StationEntry
}

public sealed class TrackSegment : CadEntity       // Fiziksel ray: iki düğüm arası doğru parça
{
    public Guid StartNodeId { get; set; }
    public Guid EndNodeId { get; set; }
    public double LengthMm { get; set; }           // önbelleklenmiş |B - A|
}

public sealed class Route : CadEntity              // Mantıksal hat: SADECE ray üzerine çizilir
{
    public List<RouteStep> Steps { get; } = new(); // sıralı segment + yön listesi
}
public sealed record RouteStep(Guid SegmentId, TravelDirection Direction); // Forward = Start->End

public sealed class RailSwitch : CadEntity         // Makas: prefab nesnesi
{
    public Vector2D Position { get; set; }         // merkez konumu (mm)
    public double RotationDeg { get; set; }        // dönüş açısı (derece)
    public Guid EntryNodeId { get; set; }          // giriş port düğümü
    public Guid MainExitNodeId { get; set; }       // ana hat çıkış port düğümü
    public Guid DivergingExitNodeId { get; set; }  // sapma çıkış port düğümü
    public SwitchState State { get; set; }         // Main / Diverging
    public Guid? BoundServoDeviceId { get; set; }  // Hardware Binding sonucu
}

public sealed class Ramp : CadEntity               // Katmanlar arası Z interpolasyonu (prefab)
{
    public Vector2D Position { get; set; }         // merkez konumu (mm)
    public double RotationDeg { get; set; }        // dönüş açısı (derece)
    public Guid EntryNodeId { get; set; }          // giriş port düğümü
    public Guid ExitNodeId { get; set; }           // çıkış port düğümü
    public double StartZ { get; set; }             // başlangıç Z (mm)
    public double EndZ { get; set; }               // bitiş Z (mm)
}
```

**Rota geçerlilik kuralı (Hat Çizimi):** `RouteTool`, tıklanan her noktayı `TrackGraph` üzerinde en yakın segmente projekte eder. Segment bulunamazsa (tolerans dışı) tıklama **reddedilir** — boş alana hat çizmek matematiksel olarak imkânsızdır. Ardışık iki adım, grafikte komşu olmalıdır (BFS ile bağlantı doğrulaması). Ok işaretleri, her segmentin orta noktasında `Direction` vektörüyle render edilir.

## 2.3 Topoloji Grafı (TrackGraph) — Simülasyonun Gizli Temeli

```csharp
public sealed class TrackGraph
{
    // Node -> bağlı segmentler (adjacency). Makaslar 3 dereceli düğümdür.
    public IReadOnlyDictionary<Guid, TrackNode> Nodes { get; }
    public IReadOnlyDictionary<Guid, TrackSegment> Segments { get; }

    public bool AreAdjacent(Guid segA, Guid segB);           // Rota doğrulama
    public IReadOnlyList<Guid> FindPath(Guid fromNode, Guid toNode, ISet<Guid> switchStates);
    public IReadOnlyList<Block> PartitionIntoBlocks();       // RFID ankrajları arası blok bölümleme (sinyalizasyon)
}
```
Bu graf Faz "D"de kurulur ama **simülasyonun (Faz H) ve blok sinyalizasyonun tek veri kaynağıdır**. Ana arter kuralı gereği simülasyon geldiğinde grafa dokunulmaz, sadece okunur.

## 2.4 Mıknatıs (Snap) Sistemi — Alphacam Hassasiyeti

Snap **ekran-piksel toleransıyla** çalışır (zoom'dan bağımsız his): tolerans örn. 10 px → dünya toleransı `10 / Scale` mm.

```csharp
public interface ISnapProvider
{
    int Priority { get; }                                  // düşük sayı = yüksek öncelik
    SnapResult? TrySnap(Vector2D worldCursor, double worldTolerance, CadDocument doc);
}
public sealed record SnapResult(Vector2D Point, SnapKind Kind, Guid? TargetId);

public sealed class SnapEngine
{
    // Öncelik sırası: 1) EndpointSnap (düğüm)  2) OnSegmentSnap (segment üstü projeksiyon)  3) GridSnap
    public SnapResult Resolve(Vector2D cursor, double tol, CadDocument doc)
        => _providers.OrderBy(p => p.Priority)
                     .Select(p => p.TrySnap(cursor, tol, doc))
                     .FirstOrDefault(r => r is not null)
           ?? new SnapResult(cursor, SnapKind.None, null);
}
```

- **GridSnapProvider:** `snapped = Round(p / gridSize) * gridSize`.
- **EndpointSnapProvider:** Uzamsal ızgara indeksi (**spatial hash**, hücre ≈ 250 mm) ile O(1) komşuluk sorgusu; binlerce düğümde bile 60 fps sürüklemede takılma olmaz.
- **OnSegmentSnapProvider:** Noktanın segmente dik projeksiyonu:
  `t = clamp(Dot(P−A, B−A) / |B−A|², 0, 1)`, `Q = A + t·(B−A)` — RouteTool ve RFID bırakma bunu kullanır.
- Aktif snap türü imleçte görsel işaretçiyle gösterilir (kare = düğüm, elmas = segment üstü, nokta = grid).

## 2.5 Araçlar (Tools) — Durum Makinesi Deseni

Her çizim modu bir `ITool` durum makinesidir; `CadInteractionController` fare/klavye olaylarını aktif araca yönlendirir:

- **TrackTool:** Tık → düğüm; ikinci tık → segment; Esc → bitir. Snap zorunlu.
- **RouteTool:** Sadece segment-üstü snap kabul eder; yön, tıklama sırasından türetilir.
- **SwitchTool:** 1 tıklamada makas prefab yerleştirme (RailSwitch + 3 port düğümü: Entry/MainExit/DivergingExit). CompositeCadCommand ile tek undo.
- **RampTool:** 1 tıklamada rampa prefab yerleştirme (Ramp + 2 port düğümü: Entry/Exit), StartZ/EndZ atama, eğim % etiketi.
- **HybridTool (Eşzamanlı Mod):** Her segment onayında hem `TrackSegment` hem eşlenik `RouteStep` üretir (tek `CompositeCadCommand` içinde — tek Undo adımı).
- **SelectTool:** Tek tık hit-test + **MarqueeSelector** (kutu içine alma; soldan-sağa = tamamen içerde, sağdan-sola = kesişen — Alphacam/AutoCAD davranışı).
- **BindTool:** Rubber-band donanım bağlama (Bölüm 3).

## 2.6 Undo/Redo, Pano ve Kısayollar

```csharp
public interface ICadCommand { void Execute(CadDocument doc); void Undo(CadDocument doc); }
// AddSegmentCommand, DeleteEntitiesCommand, MoveEntitiesCommand, PasteCommand, BindHardwareCommand...
```
- **Ctrl+C / Ctrl+X:** Seçim, ID'ler yeniden üretilerek (deep clone) `CadClipboard`'a serileştirilir; kesme = kopya + `DeleteEntitiesCommand`.
- **Ctrl+V:** İmleç konumuna ofsetli yapıştırma; düğüm birleşimi snap ile yapılır.
- **Ctrl+S:** `IProjectRepository.SaveAsync()` → SQLite (Bölüm 4).
- **Ctrl+Z / Ctrl+Y:** `CommandStack` (sınırsız, bellek-içi).

## 2.7 Unsur Ağacı (Feature Tree) ve Katmanlar

Sol panel, `CadDocument` üzerinde yaşayan hiyerarşik VM ağacıdır (göster/gizle, kilitle, yeniden adlandır, çift tık = zoom-to-entity):

```
Proje "RestoranA"
├── Katmanlar
│   ├── Zemin (Z=0)      ├── Alt Kat (Z=-350)   └── Üst Kat (Z=+400)
├── Raylar        (TrackSegment listesi, katmana göre gruplu)
├── Hatlar        (Route listesi + yön özeti)
├── Makaslar / Rampalar
├── Masalar (İstasyonlar)
└── Donanımlar    (ElectronicsView'dan gelen kartlar + bağlanma durumu ✔/✖)
```
Ağaç ile tuval **çift yönlü senkronizedir**: tuvalde seçim ağaçta vurgulanır, ağaçta seçim tuvalde vurgulanır (tek `SelectionService` üzerinden — iki ayrı seçim durumu asla tutulmaz).

---

# BÖLÜM 3 — DONANIM-RAY EŞLEME (RUBBER-BAND BINDING)

## 3.1 Model

```csharp
public sealed class HardwareEndpoint          // ElectronicsView'da tanımlanan her bağlanabilir uç
{
    public Guid Id { get; init; }
    public Guid DeviceId { get; init; }        // hangi ESP32 kartı
    public EndpointKind Kind { get; init; }    // RfidReader | SwitchServo
    public Vector2D FloatingPosition { get; set; }   // editörde yüzen serbest konum
    public Guid? BoundTargetId { get; set; }   // TrackNode (RfidAnchor) veya RailSwitch Id — null = boşta
}
```

## 3.2 Görselleştirme ve Sürükle-Bırak Matematiği

- Boşta uçlar, EditorView açıldığında ray şemasının kenarında **yüzen çipler** olarak listelenir; her çipten kaynağına (Feature Tree'deki donanım kaydına) yarı saydam **elastik Bézier çizgisi** çizilir.
- Sürükleme sırasında çizgi her `MouseMove`'da yeniden hesaplanır: `P0 = çip ankrajı`, `P3 = imleç`, kontrol noktaları `P1/P2` yatay ofsetli → "lastik gibi uzayan" doğal eğri. Bu çizim Adorner katmanındadır (model render'ı etkilenmez).
- **Bırakma anında hedef doğrulama:** `SnapEngine` yalnızca uygun hedefleri kabul eder:
  - `RfidReader` ucu → sadece `NodeRole.RfidAnchor` düğümüne veya segment üstüne (bırakılınca otomatik `RfidAnchor` düğümü oluşturulur, segment ikiye bölünür: `SplitSegmentCommand`).
  - `SwitchServo` ucu → sadece `RailSwitch` düğümüne.
  - Geçersiz hedefte çizgi kırmızıya döner ve uç eski konumuna yaylanarak (Fluent animasyon) geri döner.
- Başarılı bırakma tek bir `BindHardwareCommand` üretir (Undo edilebilir) ve `HardwareBindings` tablosuna yazılır. Bu tablo, **firmware üretiminin ve simülasyonun ortak gerçeğidir**: "İstasyon 5'in RFID'i, Segment X'in s=1240 mm noktasındadır."

---

# BÖLÜM 4 — MQTT, MODEM ŞEMASI, SQLITE VE STATE RECOVERY

## 4.1 Gömülü MQTT Broker (MQTTnet v4)

WPF uygulaması açılışta `MqttServer`'ı `IHostedService` olarak ayağa kaldırır (port varsayılan **1883**, SettingsView'dan değiştirilebilir). Uygulama aynı zamanda broker'a **loopback istemci** olarak bağlanır; böylece iç servisler ve dış cihazlar aynı otobüsü kullanır.

**Konu Sözleşmesi (A3 — v3.0.4'te dondurulur, sadece eklenir):**

| Konu | Yön | Payload (JSON) | Amaç |
|---|---|---|---|
| `restaurant/commands` | PC → tüm | `{cmdId, trainId, targetStationId, action}` | "TREN 1 → MASA 5" emri (QoS 1) |
| `restaurant/telemetry/{stationId}/rfid` | ESP32 → PC | `{stationId, tagId, ts}` | "Tren 1 önümden geçiyor" |
| `restaurant/telemetry/{trainId}/state` | Tren → PC | `{speed, obstacle, lastRfid, battery}` | Tren durum yayını |
| `restaurant/ack/{stationId}` | PC → ESP32 | `{cmdId, divert:true/false}` | "Evet, hedef burası → makası aç" |
| `restaurant/status/{deviceId}` | Cihaz → PC | `online/offline` (retained + **LWT**) | Cihaz varlık takibi |
| `restaurant/estop` | PC → tüm | `{active:true}` (QoS 2, retained) | Acil durdurma |
| `restaurant/ota/{deviceId}/notify` | PC → Cihaz | `{version, url/size}` | OTA tetikleme |
| `restaurant/log/{deviceId}` | Cihaz → PC | `{level, msg}` | Terminale akan cihaz logları |

**Kritik zamanlama notu:** "RFID okundu → PC onayı → makas aç" döngüsü ağ gecikmesine tabidir. Tren geçiş hızı ve RFID-makas arası mesafe, bu round-trip (~10–50 ms LAN) için yeterli **onay penceresi** bırakmalıdır. ESP32 firmware'inde onay `X` ms içinde gelmezse **güvenli varsayılan = makas ana hatta kalır** (tren cebi pas geçer, sistem yeniden dener). Bu kural hem gerçek firmware'de hem sanal cihazda birebir aynıdır.

## 4.2 Modem/Switch Port Hiyerarşisi (ElectronicsView Modeli)

Node-based şema, `NetworkNode` grafı olarak modellenir; **cascade (Port 5 → ikinci switch)** yapısı ağaç derinliğiyle temsil edilir:

```csharp
public sealed class NetworkSwitch { public Guid Id; public string Name; public int PortCount = 5; }
public sealed class SwitchPort   { public Guid SwitchId; public int PortNo;        // 1..5
                                   public PortRole Role;                            // Uplink, Device, Cascade, Empty
                                   public Guid? ConnectedDeviceId;                  // ESP32 veya PC
                                   public Guid? CascadeSwitchId; }                  // Port 5 → alt switch
public sealed class Device       { public Guid Id; public string Name; public DeviceKind Kind; // PC, StationEsp32, TrainEsp32
                                   public string Ip; public string Mac; public string MqttClientId; }
```

**Canlı durum:** `PingService` (5 sn periyot, ICMP) + `DeviceRegistry` (MQTT LWT/heartbeat) sonuçları birleşir → port LED'i: Yeşil (Ping+MQTT), Sarı (sadece Ping), Kırmızı (ikisi de yok), Gri (boş). Aynı `Device` kayıtları Feature Tree'ye ve rubber-band uçlarına kaynaklık eder — **cihaz tek yerde tanımlanır, her yerde referanslanır.**

## 4.3 SQLite Şeması (EF Core 8 — Migration disiplini)

```
Projects        (Id, Name, CreatedAt, SchemaVersion)
Layers          (Id, ProjectId, Name, ZHeightMm, Order, IsVisible, IsLocked)
TrackNodes      (Id, ProjectId, LayerId, X, Y, Z, Role)
TrackSegments   (Id, ProjectId, LayerId, StartNodeId, EndNodeId, LengthMm)
Routes          (Id, ProjectId, Name, ColorArgb)
RouteSteps      (Id, RouteId, OrderIndex, SegmentId, Direction)
Switches        (Id, ProjectId, PositionX, PositionY, RotationDeg, EntryNodeId, MainExitNodeId, DivergingExitNodeId, DefaultState)
Ramps           (Id, ProjectId, PositionX, PositionY, RotationDeg, EntryNodeId, ExitNodeId, StartZ, EndZ)
Stations        (Id, ProjectId, TableNo, Name, EntrySwitchId, PocketRouteId, Priority)
Trains          (Id, ProjectId, Name, NfcTagId, MaxSpeedMmS, AccelMmS2, DecelMmS2, MassKg)
Devices         (Id, ProjectId, Name, Kind, Ip, Mac, MqttClientId, FirmwareVersion)
NetworkSwitches (Id, ProjectId, Name, PortCount)
SwitchPorts     (Id, NetworkSwitchId, PortNo, Role, ConnectedDeviceId?, CascadeSwitchId?)
HardwareBindings(Id, ProjectId, EndpointKind, DeviceId, TargetEntityId, SegmentId?, OffsetMm?)  -- RFID: segment+s konumu
Scenarios       (Id, ProjectId, Name, IsDefault, CreatedAt)
ScenarioSteps   (Id, ScenarioId, OrderIndex, TrainId, TargetStationId, WaitSeconds, PriorityOverride?)
SystemState     (Id=1 tek satır, ProjectId, ActiveScenarioId?, SavedAt)          -- ★ State Recovery
TrainStates     (TrainId PK, LastRfidBindingId?, LastSegmentId?, OffsetMm, DirectionSign, SpeedMmS)
SwitchStates    (SwitchId PK, State, UpdatedAt)
EventLogs       (Id, Ts, Level, Source, Message)                                  -- terminal geçmişi (halkalı, max N)
```

## 4.4 State Recovery Akışı

1. **Yazma (debounced):** `StateRecoveryService`, olay bazlı tetiklenir — her RFID geçişi `TrainStates`'i, her makas komutu `SwitchStates`'i günceller; 500 ms debounce ile tek transaction'da yazılır (SQLite **WAL modu** açık: UI'yı asla bloklamaz, elektrik kesintisine dayanıklıdır).
2. **Kapanış:** `OnExit` → son tam snapshot + `SystemState.SavedAt`.
3. **Açılış:** Son proje → aktif senaryo → tren konumları (`LastSegmentId + OffsetMm`) → makas konumları sırayla yüklenir; makaslara `restaurant/commands` üzerinden **senkronizasyon emri** yayınlanır ("dijital durum = fiziksel durum" garantisi) ve durum HomeView'da "Kaldığı yerden devam edildi ✔" olarak raporlanır.

---

# BÖLÜM 5 — FIRMWARE ÜRETİMİ, DERLEME VE OTA MİMARİSİ

## 5.1 FirmwareManager Servis Hattı (C# tarafı)

```
FirmwareManager.DeployAsync(deviceId)
 ├─ 1. FirmwareGenerator   : Şablon (Scriban) + DB modeli → benzersiz C++ kaynak
 │      girdi: Device, HardwareBindings, Stations, MQTT ayarları, pin haritası
 │      çıktı: firmware/generated/{deviceId}/src/main.cpp + include/config.h
 ├─ 2. BuildService        : PlatformIO-CLI ("pio run -e esp32c3") Process olarak sessiz çalıştırılır
 │      stdout/stderr satır satır ILogBus'a akar (terminalde canlı derleme logu)
 │      çıktı: .pio/build/.../firmware.bin  (+ SHA256 özeti)
 ├─ 3. OtaUploader         : espota protokolü (UDP 3232, "pio run -t upload --upload-port <ip>")
 │      alternatif: cihazdaki HTTP OTA endpoint'ine bin POST edilir; ilerleme % terminale akar
 └─ 4. Verify              : cihaz reboot sonrası restaurant/status/{id} + FirmwareVersion raporu beklenir
        (timeout 30 sn → Error log + eski sürüme işaret)
```

- Derleme kuyruğu tekil `SemaphoreSlim` ile serileştirilir (CLI çakışması olmaz); tüm işlem `IProgress<FirmwareStage>` ile HomeView/ElectronicsView'a ilerleme bildirir.
- `config.h` içine **FirmwareVersion (semver) + DeviceId + BindingHash** gömülür; PC, sahadaki sürümün çizimle uyumlu olup olmadığını hash karşılaştırmasıyla anlar ("çizim değişti → İstasyon 5 firmware'i güncel değil ⚠" uyarısı).

## 5.2 ESP32 Non-Blocking C++ Mimarisi (İstasyon ve Tren ortak çekirdek)

`delay()` yasak; her şey `millis()` tabanlı **kooperatif görevler + durum makinesi**:

```cpp
// common/NonBlockingTask.h — kooperatif zamanlayıcı çekirdeği
class Task {
public:
    virtual void tick(uint32_t nowMs) = 0;   // asla bloklamaz
};
class Scheduler {
    Task* tasks[8]; uint8_t count = 0;
public:
    void add(Task* t) { tasks[count++] = t; }
    void loopOnce() { uint32_t now = millis(); for (uint8_t i = 0; i < count; ++i) tasks[i]->tick(now); }
};
```

```cpp
// station_esp32 — İstasyon durum makinesi (ana akış)
enum class StationState : uint8_t {
    BOOT, WIFI_CONNECTING, MQTT_CONNECTING, IDLE,
    TAG_DETECTED,      // RFID okundu → telemetri yayınla, ACK bekle (deadline = now + ACK_WINDOW_MS)
    AWAIT_ACK,         // ACK{divert:true} → SWITCH_DIVERGE ; timeout → IDLE (güvenli varsayılan)
    SWITCH_DIVERGE,    // servo cebe; tren cebe girdi onayı (ikincil RFID/süre) → SWITCH_RESTORE
    SWITCH_RESTORE,    // servo ana hatta geri
    OTA_UPDATE, ERROR_SAFE
};
```

```cpp
// train_esp32c3 — Tren durum makinesi (ağdan bağımsız otonom çekirdek)
enum class TrainState : uint8_t {
    IDLE, ACCELERATING, CRUISING,
    BRAKING_FOR_OBSTACLE,   // lazer ToF mesafesi < eşik → kontrollü fren (non-blocking rampa)
    BRAKING_FOR_STOP, STOPPED_AT_STATION, RESUMING, FAULT
};
// RFID okumaları yalnızca telemetri yayını + hız profili güncellemesi için kullanılır;
// tren, MQTT kopsa bile lazer + RFID ile güvenli otonom ilerler (gereksinim birebir).
```

- MQTT: `AsyncMqttClient` (tam asenkron, ISR-güvenli kuyruk); Wi-Fi/Ethernet kopması durum makinesinde `*_CONNECTING`'e düşer, motor/servo güvenli konuma alınır.
- OTA: `ArduinoOTA` (espota) servis görevi olarak scheduler'a eklidir; güncelleme sırasında durum `OTA_UPDATE`'e kilitlenir, servo güvenli konumdadır.
- **Aynı durum makineleri Faz H'de `VirtualStationEsp32` / `VirtualTrainController` olarak C#'ta birebir aynalanır** — simülasyon "bu kod mantığına göre akar" gereksiniminin teknik karşılığı budur (durum geçiş tablosu tek bir `docs/StateMachineSpec.md`'de tutulur; iki dil de bu speci uygular).

---

# BÖLÜM 6 — SİMÜLASYON MOTORU MİMARİSİ (FINAL STAGE)

## 6.1 Motor Deseni: Sabit Zaman Adımlı Game Loop + Render İnterpolasyonu

Harici motor (Unity vb.) **gerekmez**; doğru desen, WPF içinde saf .NET ile kurulur:

- **Fizik döngüsü:** Arka plan dedicated `Thread` (UI'dan tamamen bağımsız), **sabit Δt = 1/120 sn**. `Stopwatch` tabanlı **accumulator** deseni:
  `accumulator += frameTime; while (accumulator >= dt) { Step(dt); accumulator -= dt; }`
  Sabit adım → fizik deterministik, kare hızından bağımsız (test edilebilir).
- **Render:** `CompositionTarget.Rendering` (~60 fps) son iki fizik durumunu `alpha = accumulator/dt` ile **doğrusal interpole eder** → tren hareketi pürüzsüz. Fizik → UI veri akışı kilitsiz **çift tamponlu snapshot** (`ImmutableArray<TrainSnapshot>` swap) ile yapılır; UI thread asla fizik kilidi beklemez.
- **Hız kontrolü:** SimClock `TimeScale` (0.25×–8×) ve Pause/Step destekler.

## 6.2 Yol Takibi: Ark-Uzunluğu (s) Parametrizasyonu

Trenin ana koordinatı 2D nokta değil, **rota üzerindeki mesafedir (s, mm)** — tüm virajlar/rampalarda pürüzsüzlüğün anahtarı:

```csharp
public sealed class ArcLengthPath   // Route -> önceden hesaplanmış kümülatif uzunluk tablosu
{
    // s -> (Position2D, TangentUnit, Z, GradePercent)  — O(log n) binary search + lineer interp.
    public PathSample Sample(double s);
    public double TotalLength { get; }
}
```
- **Yönelim (heading):** `angle = atan2(tangent.Y, tangent.X)`; köşe düğümlerinde ani açı sıçramasını önlemek için heading, küçük bir pencere üzerinden yumuşatılır (tren gövdesi 2 bojeli örneklenir: ön/arka nokta = s ve s−L → gövde açısı iki noktadan türetilir; gerçek vagon dönüş davranışı).
- **Rampa:** `Ramp` kayıtlarından Z ve eğim `Sample(s)` içinde hazırdır.

## 6.3 Boylamsal Dinamik (1-D Fizik — gerçekçi ve yeterli)

Her fizik adımında tren başına:

```
F_tahrik  = motor kuvveti (durum makinesi hedef hızına PID/rampa ile)
F_yuvarlanma = Crr · m · g · cos(θ)         (sürtünme)
F_egim    = m · g · sin(θ)                  (rampada yavaşlatır/hızlandırır; θ = atan(grade))
F_fren    = durum BRAKING ise sabit güvenli yavaşlama a_brake

a = (F_tahrik − F_yuvarlanma − F_egim − F_fren) / m
v = clamp(v + a·dt, 0, v_max) ;  s = s + v·dt
Fren mesafesi (blok kontrolü için): d_stop = v² / (2·a_brake) + v·t_reaksiyon
```
Sonuç: rampa çıkarken doğal yavaşlama, inişte hız artışı, engelde mesafeye bağlı kontrollü fren — hepsi tek tutarlı denklemden.

## 6.4 Sanal Cihaz Katmanı ve Blok Sinyalizasyon

- `VirtualStationEsp32` / `VirtualTrainController`, Bölüm 5.2'deki durum makinelerinin C# eşleniğidir ve **gömülü broker'a gerçek MQTT istemcisi olarak bağlanır** (opsiyonel yapay gecikme: 5–40 ms jitter → gerçek LAN provası). Tren `s` konumu bir `HardwareBindings` RFID noktasını (±yarım etiket toleransı) geçtiğinde sanal istasyon "tag okudu" olayını üretir — zincirin kalanı gerçek hayattakiyle bit-bit aynıdır.
- **BlockSignalController:** `TrackGraph.PartitionIntoBlocks()` çıktısı üzerinde işgal tablosu tutar. Kural: bir blok tek trene aittir; sonraki blok doluysa trene `BRAKING_FOR_STOP` hedefi verilir (fren mesafesi d_stop blok sınırından önce tamamlanacak şekilde). Aynı controller gerçek modda da çalışır — komut yayınlamadan önce blok rezervasyonu yapar (çakışma önleme yazılımsal ana arterdir).

---










# TrainService (TCP) — YOL HARİTASI (Roadmap)
## Sürüm: 2026-07-18 · FAZ D kapanışı sonrası tam güncelleme

> **BU DOSYAYI OKUYAN MODEL İÇİN — ÖNCE OKU:**
> 1. Bu roadmap `AGENTS.md`'ye TABİDİR (aynı klasörde). Çelişkide AGENTS.md kazanır.
> 2. Sıra ATLANMAZ, numara UYDURULMAZ. Sıradaki sürüm = `(MÜHÜRLENDİ)` işareti OLMAYAN ilk satır.
> 3. Her sürüm için akış: PLAN yaz → DUR → kullanıcı onayı → TDD (önce test, KIRMIZI gör) → kod →
>    `dotnet run` → DUR → kullanıcı manuel turu → mühür raporu (SABİT tools/muhur.ps1) → kullanıcı
>    "pushla" derse commit+push. Bir oturumda TEK sürüm.
> 4. Mühürlü davranışlar SÖKÜLMEZ (Y5): F9=snap, Esc=İPTAL, Enter/sağ-tık=COMMIT, SabitKatmanlar
>    (11111111/22222222/33333333), IsVisible/IsSelectable tek-kaynak, CadColors merkezî renk.
> 5. Bekçi ispatı SADECE `//[Fact]` yöntemi (Y12). Test sorunu TESTTE çözülür, üretim sökülmez (Y13).
> 6. 🔍 işaretli sürümlerde mühre EK olarak geriye-dönük denetim raporu üretilir
>    (VERSIYON_KONTROL_DENETIMI.txt — önceki 🔍'den bu yana tüm sürümler).
> 7. Ara-numara kuralı: v3.0.29.1–v3.0.29.7 GERÇEK sürümlerdir (FAZ D2). v3.0.30+ numaraları eski
>    belgelerle uyum için DEĞİŞTİRİLMEMİŞTİR.

═══════════════════════════════════════════════════════════════════
# BÖLÜM 0 — MÜHÜRLÜ GEÇMİŞ (v3.0.0 → v3.0.29) — DOKUNULMAZ
═══════════════════════════════════════════════════════════════════
| Aralık | İçerik | Durum |
|---|---|---|
| v3.0.0–v3.0.9 | İskelet, DI, LogBus, MQTT broker, SQLite şema, CRUD, Dark Mica kabuk | (MÜHÜRLENDİ) |
| v3.0.10–v3.0.19 | CadCanvas, pan/zoom, grid, TrackTool, undo/redo, Ctrl+S ilişkisel kayıt, SnapEngine, SpatialHash | (MÜHÜRLENDİ) |
| v3.0.20 | TrackGraph (komşuluk, AreAdjacent, blok temeli) | (MÜHÜRLENDİ) |
| v3.0.21 | SelectTool + Marquee (mavi window / yeşil crossing), hover, CadColors | (MÜHÜRLENDİ) |
| v3.0.22 | Pano Ctrl+C/X/V (derin kopya, +20 ofset, GUID haritası) | (MÜHÜRLENDİ) |
| v3.0.23 | Katmanlar (SabitKatmanlar, gizle/kilit, güvenlik ağı) | (MÜHÜRLENDİ) |
| v3.0.24 | RouteTool (yön okları, bayat-graf reddi, Route kalıcılığı) | (MÜHÜRLENDİ) |
| v3.0.25 | HybridTool (tek harekette Track+Route, tek undo) | (MÜHÜRLENDİ) |
| v3.0.26 | RampTool 1-tık prefab (Entry/Exit, RampDefaults 100mm/%15) | (MÜHÜRLENDİ) |
| v3.0.27 | SwitchTool 1-tık prefab (3 port, BoundServoDeviceId) | (MÜHÜRLENDİ) |
| v3.0.28 | Feature Tree (çift yönlü seçim senkronu, çift-tık zoom, gizle/kilit) | (MÜHÜRLENDİ) |
| v3.0.29 | Radyal Menü (bağlam duyarlı, SADECE Idle'da açılır) + Ramp/RailSwitch store round-trip (T560/T561) | (MÜHÜRLENDİ) |

Mevcut test tabanı (blok mühür anı): **246 test** — Cad 146, Core 32, Data 26, Messaging 16, App 15, Arch 10, Sim 1.
Migration listesi (hepsi applied): InitialSchema, AddElectronicsSchema, RemoveCadProjectJson, AddMissingTables, FixRailSwitchRampMapping.

═══════════════════════════════════════════════════════════════════
# FAZ D2 — EDİTÖR ARAYÜZ CİLASI (YENİ) · v3.0.29.1 → v3.0.29.7
═══════════════════════════════════════════════════════════════════
**Fazın amacı:** Backend'i tamamlanan editörün üstüne Alphacam sınıfı kullanım kolaylıkları giydirmek.
Bu fazda ÇEKİRDEK MANTIK DEĞİŞMEZ — yalnızca App katmanı (XAML/ViewModel) genişler; Cad/Core'a dokunuş
sadece komutları dışarı açan ince arayüzlerle sınırlıdır. Görsel/backend ayrımı kararı gereği bu faz,
"görsel roadmap"in çekirdeğe bitişik ilk yarısıdır.

**UI test kimlik bloğu:** T330–T399 (App.Tests + Cad.Tests FeatureTree/ViewModel testleri).
**İkon standardı (tüm faz):** Birincil = Wpf.Ui `SymbolIcon` (Fluent System Icons — pakette hazır,
ek bağımlılık YOK). Eksik sembol olursa ikincil = `Material.Icons.WPF` (MIT lisans) NuGet paketi;
üçüncül = `MahApps.Metro.IconPacks.Modern` (MIT). Yeni paket eklemek plan onayı gerektirir; PNG/asset
dosyası eklenmez, yalnızca font/vektör ikon kullanılır (DPI bağımsızlığı).

---
## v3.0.29.1 — Üst Ribbon: Sekmeli Komut Şeridi + Quick Access
**Amaç:** Alphacam'deki sekmeli üst şerit düzeni: tüm araçlar ikonlu butonlarla üstte, kısayol
ipuçları tooltip'te ("ScreenTip'te kısayol göster" davranışı).
**İçerik:**
- Üst bölge iki katman: (1) **Quick Access mini-bar** (pencere başlığı hizasında): Kaydet(Ctrl+S),
  Geri Al(Ctrl+Z), Yinele(Ctrl+Y) — her zaman görünür. (2) **Sekmeli şerit**: `Giriş` · `Çizim` ·
  `Düzen` · `Görünüm` sekmeleri.
- `Giriş`: Seç(S), Taşı-yakında, Sil(Del), Kopyala/Kes/Yapıştır, katman ComboBox + göz/kilit (mevcut).
- `Çizim`: Ray(T), Hat(R), Hibrit(H), Rampa, Makas — mevcut SetTool komutlarına bağlanır; aktif araç
  butonu vurgulu (toggle görünümü, tek doğruluk kaynağı EditorViewModel.ActiveToolName).
- `Düzen`: Undo/Redo, Delete, SplitSegment, (ilerisi için boş grup — Fillet/Trim YOK, bkz. YOK listesi).
- `Görünüm`: Zoom Extents, Zoom Window, Grid ayarı, Snap toggle (F9 ile aynı komut), tema.
- Her buton: SymbolIcon + başlık + ToolTip'te "İşlev (Kısayol)" formatı.
- Şerit tanımı VERİ-SÜRÜMLÜ: `RibbonDefinition.cs` içinde komut listesi (id, ikon, kısayol, grup) —
  XAML bu listeden ItemsControl ile üretilir. (Alphacam'in "toolbar customize" temeli; kullanıcı
  özelleştirmesi BU sürümde YOK, sadece veri yapısı hazırlanır.)
**YOK:** Kullanıcı toolbar özelleştirme UI'ı (ileride), Fillet/Trim/Offset gibi geometrik komutlar
(çekirdek desteklemiyor — eklemek Y11 kapsam taşması), ribbon collapse animasyonları.
**Kabul:** Tüm mevcut araçlara şeritten erişilir; aktif araç vurgulanır; tooltip'lerde kısayol görünür;
klavye kısayolları AYNEN çalışmaya devam eder (S/T/R/H/F9/Esc/Enter/Del/Ctrl+C-X-V-Z-Y-S).
**Test:** T330–T335 (ViewModel: SetTool eşlemesi, ActiveToolName senkronu, RibbonDefinition bütünlüğü —
her komutun geçerli ikon+kısayol+handler'ı var; kısayol çakışma taraması testi).

---
## v3.0.29.2 — Sekmeli Çoklu Belge (Üstte Sayfalar)
**Amaç:** Alphacam/tarayıcı tarzı: üstte SAYFA sekmeleri; birden çok proje/çizim aynı anda açık.
**İçerik:**
- `DocumentTabsViewModel`: açık belgeler listesi (her biri kendi `CadDocument` + `CommandStack` +
  `SelectionService` + ToolController seti — İZOLE; sekmeler arası hiçbir paylaşım yok).
- Sekme şeridi (şeridin altı, tuvalin üstü): sekme başlığı = proje adı; ★ kirli (kaydedilmemiş) işareti;
  X kapat butonu; çift-tık = yeniden adlandır (DB'ye Project.Name yazılır); sürükle-bırak = yeniden sırala.
- `+` butonu: yeni boş proje (yeni ProjectId, SabitKatmanlar seed'i standart akışla).
- Kapatırken kirliyse Wpf.Ui MessageBox: Kaydet / Kaydetme / Vazgeç.
- Ctrl+Tab = sonraki sekme, Ctrl+W = sekme kapat (kaydet uyarılı).
- Ctrl+S yalnız AKTİF sekmeyi kaydeder (mevcut CadDocumentStore, ProjectId'siyle — kod değişmez, çağrı bağlanır).
**YOK:** Sekmeyi ayrı pencereye koparma, split-view, belgeler arası kopyala-yapıştır (pano zaten
in-process tek — bu doğal ÇALIŞIR ve çalışması KABUL, engellenmez), oturum geri yükleme (son açık sekmeler).
**Kabul:** İki proje açıkken çizimler, undo yığınları ve seçimler birbirine KARIŞMAZ (en kritik kriter);
kirli sekme kapatılırken uyarı çıkar; yeniden adlandırma DB'ye yansır ve yeniden açılışta korunur.
**Test:** T340–T347 (izolasyon: sekme-A'da çizim sekme-B'nin CommandStack'ine düşmez; kirli bayrak;
rename persist; kapat-uyarı akışı ViewModel testi; Ctrl+Tab döngüsü).

---
## v3.0.29.3 — Feature Tree Kolaylıkları (Katman Ağacı v2)
**Amaç:** v3.0.28 ağacını Alphacam "Project Manager" konforuna çıkarmak.
**İçerik:**
- **Arama/filtre kutusu** (ağacın üstü): ada + türe göre canlı süzme; eşleşen düğümün ataları otomatik açılır.
- **Tür grupları:** Katman düğümü altında ikinci seviye: Raylar / Hatlar / Makaslar / Rampalar
  (sayaçlarıyla: "Makaslar (3)").
- **Toplu göz/kilit:** grup ve katman düğümlerinde göz+kilit ikonları → altındaki TÜM öğelere uygular
  (mevcut SetLayerVisibility/SetLayerLock + entity bazlı IsVisible tek-kaynağı ÜZERİNDEN — yeni
  görünürlük yolu AÇILMAZ).
- **Solo/İzole:** sağ-tık → "Yalnız bunu göster" (diğer katmanlar gizlenir; tekrar seçilince geri).
  İzole durumu bir UI durumudur, belgeye YAZILMAZ.
- **Sağ-tık ağaç menüsü:** Yeniden adlandır (katman/rota), Sil (undo'lu, mevcut DeleteEntitiesCommand),
  Zoom (çift-tık ile aynı), Solo, Tümünü Göster.
- **Sürükle-bırak katman değiştirme:** entity düğümünü başka katman düğümüne bırak →
  `ChangeLayerCommand` (YENİ, ICadCommand, undo'lu — Cad katmanına eklenen TEK sınıf).
**YOK:** Ağaçtan çoklu-sürükleme, katman ekleme/silme (3 sabit katman kararı korunur), tür grubu
altında yeniden sıralama.
**Kabul:** Arama 200+ nesnede takılmadan süzer (canlı); solo→geri tam döner; sürükle-bırak katman
değişimi Ctrl+Z ile geri alınır; toplu göz/kilit tuval render'ına ANINDA yansır.
**Test:** T350–T357 (filtre eşleşme+ata-açma; grup sayaçları; ChangeLayerCommand execute/undo; solo
durum makinesi; toplu görünürlük → IsVisible sonuçları).

---
## v3.0.29.4 — Radyal Menü v2 (Görsel + İşlev Genişlemesi)
**Amaç:** v3.0.29 radyalini Alphacam/oyun-motoru kalitesine çıkarmak: ikonlu dilimler, alt halka,
son-komut tekrarı.
**İçerik:**
- Dilimlerde SymbolIcon + etiket; hover'da dilim büyür (Fluent animasyon, 120ms).
- **Alt-halka (submenu):** "Çizim ▸" dilimi → ikinci halka (Ray/Hat/Hibrit/Rampa/Makas). Tek seviye
  derinlik SINIRI (iki halkadan fazlası YOK).
- **Merkez buton = son komut:** son çalıştırılan radyal komutu merkezde ikonuyla; tıkla → tekrar
  (Alphacam'de sağ-tık-tekrar alışkanlığının karşılığı).
- Bağlam setleri genişler: boşluk / segment / rota / makas / rampa / ÇOKLU-SEÇİM (yeni: Sil, Kopyala,
  Katman değiştir▸) / Feature Tree üstü (ağaç sağ-tık menüsüyle AYNI komut kaynağı — komut tanımı tek yerde).
- Klavye: radyal açıkken ok tuşları dilim gezdirir, Enter seçer, Esc kapatır (Esc=iptal ilkesi burada da).
- MÜHÜRLÜ KURAL KORUNUR: radyal SADECE araç Idle iken açılır; araç meşgulken sağ-tık = zincir bitir/commit.
**YOK:** Serbest özelleştirilebilir dilimler, ikiden derin halkalar, radyalden metin girişi.
**Kabul:** Idle-dışı açılmama davranışı REGRESYONsuz; alt-halka ve merkez-tekrar çalışır; tüm dilim
komutları ribbon'daki eşdeğerleriyle AYNI handler'ı kullanır (çift mantık yok).
**Test:** T360–T366 (bağlam→dilim seti seçimi; son-komut kaydı/tekrarı; Idle guard regresyon; klavye
gezinme durum makinesi; çoklu-seçim seti).

---
## v3.0.29.5 — Görünüm Kolaylıkları + Durum Çubuğu
**Amaç:** Alphacam'in Z=Zoom All refleksi ve profesyonel CAD durum çubuğu.
**İçerik:**
- **Zoom Extents** (tüm çizimi kadrajla — kısayol `Z`, Alphacam birebir) ve **Zoom Window**
  (W ile pencere çiz-yaklaş; Esc iptal). Her ikisi Görünüm şeridinde ikonlu.
- **Snap toolbar'ı** (durum çubuğunda toggle grubu): Endpoint / OnSegment / Grid snap AYRI AYRI
  açılıp kapanır (SnapEngine'e üç bayrak; F9 = üçünü birden aç/kapa — F9 davranışı DEĞİŞMEZ,
  "hepsi kapalı↔son kombinasyon" olarak çalışır).
- **Durum çubuğu** (pencere altı): imleç dünya koordinatı (X: Y: mm, AwayFromZero yuvarlak),
  aktif katman adı, aktif araç, snap durum LED'leri, zoom yüzdesi, seçili nesne sayısı.
- Orta-tuş çift-tık = Zoom Extents (CAD refleksi).
**YOK:** Mini-harita, kaydedilmiş görünümler (named views), 3D/izometrik görünüm.
**Kabul:** Z her durumda tüm çizimi kadrajlar (boş belgede no-op, sıfıra bölme guard'lı — AGENTS 7);
snap bayrakları teker teker çalışır ve F9 eski davranışını bozmaz; koordinat göstergesi
MouseMove'da TAHSISSIZ güncellenir (hot-path — string.Format değil, önbellekli StringBuilder/binding).
**Test:** T370–T376 (Zoom Extents matematiği: bounds→transform; boş belge guard; snap bayrak
kombinasyonları SnapEngine testi; F9 toggle regresyon; koordinat yuvarlama AwayFromZero).

---
## v3.0.29.6 — Seçim Filtreleri + Hızlı Seçim Komutları
**Amaç:** Kalabalık çizimde Alphacam tarzı hedefli seçim.
**İçerik:**
- **Seçim filtresi** (durum çubuğunda açılır): Tümü / Sadece Ray / Sadece Hat / Sadece Makas /
  Sadece Rampa / Sadece Düğüm. Aktifken tık VE marquee yalnız o türü seçer (SelectTool'a tür
  yüklemi enjekte edilir — SelectTool İMZASI değişmez, ToolContext'e `SelectionTypeFilter` eklenir).
- **Ctrl+A** = filtreye uyan görünür+kilitsiz HERŞEYİ seç.
- **Benzerlerini Seç** (sağ-tık/radyal, seçiliyken): aynı türden tümünü seç.
- **Seçimi Tersine Çevir** (Ctrl+Shift+I).
- Filtre aktifken durum çubuğunda uyarı rozeti (yanlışlıkla "niye seçemiyorum" karışıklığına karşı).
**YOK:** Kaydedilmiş seçim setleri, özellik-bazlı sorgu (uzunluğa göre vb.).
**Kabul:** Filtre marquee'nin window/crossing matematiğini DEĞİŞTİRMEZ (yalnız sonucu süzer);
Ctrl+A gizli/kilitliyi ASLA seçmez (IsSelectable tek-kaynak); tersine çevirme filtreye saygılıdır.
**Test:** T380–T386 (filtreli tık/marquee; Ctrl+A gizli-hariç; benzer-seç; tersine çevir; rozet durumu).

---
## v3.0.29.7 — Kısayol Haritası + F1 Yardım Kaplaması + 🔍 D2 DENETİMİ
**Amaç:** Fazı belgeleyip mühürlemek: tüm kısayollar tek kaynakta, F1'de görsel yardım.
**İçerik:**
- **`ShortcutMap.cs` TEK KAYNAK:** tüm kısayollar (tuş, komut id, açıklama, bağlam) burada;
  ribbon tooltip'leri, radyal etiketleri ve F1 ekranı BU haritadan beslenir (üç ayrı liste YASAK).
  Çakışma denetimi testle kilitlenir.
- **F1 = Kısayol Kaplaması:** yarı saydam tam-ekran panel; kategorilere ayrılmış kısayol kartları
  (Araçlar / Düzenleme / Görünüm / Seçim / Sistem); arama kutusu; Esc/F1 kapatır.
- Kısayol tablosunun NİHAİ hali (mühürlü davranışlar + Alphacam eklentileri):
  `S` Seç · `T` Ray · `R` Hat · `H` Hibrit · `Z` Zoom Extents · `W` Zoom Window · `Del` Sil ·
  `F9` Snap (tümü) · `F1` Yardım · `Esc` İPTAL · `Enter/SağTık` COMMIT · `Ctrl+C/X/V` Pano ·
  `Ctrl+Z/Y` Undo/Redo · `Ctrl+S` Kaydet · `Ctrl+A` Tümünü Seç · `Ctrl+Shift+I` Seçimi Çevir ·
  `Ctrl+Tab` Sekme Değiştir · `Ctrl+W` Sekme Kapat.
  (NOT: Alphacam'in M=Move/F=Fillet/X=Explode/E=Extend/T=Trim harfleri BİLİNÇLİ ALINMADI — karşılık
  gelen çekirdek komutlar yok; T zaten Ray aracımız. Bu not, gelecekteki modellerin "Alphacam'de
  vardı" diye kapsam taşırmasını önlemek içindir.)
- **🔍 D2 DENETİM DURAĞI:** v3.0.29.1–.29.7 geriye-dönük denetim → `VERSIYON_KONTROL_DENETIMI.txt`:
  D-serisi döküm (araç kaynakları D1 tarzı, gövdeler, tam koşum, T010+T011 kırmızı-yeşil, yapısal
  taramalar: inline renk 0, hot-path LINQ 0, ShortcutMap çakışma 0, Idle-guard yerinde) + kullanıcı
  BLOK manuel turu (aşağıdaki M listesi).
**Kabul/Manuel (D2 blok turu):** M1 şeritten her araca eriş + tooltip kısayolları; M2 iki sekmede
izole çizim + kirli-kapat uyarısı; M3 ağaçta ara/solo/sürükle-katman-değiştir + Ctrl+Z; M4 radyal
alt-halka + merkez-tekrar + çizim ortasında AÇILMAMA; M5 Z/W zoom + snap toggle'ları + durum çubuğu
koordinatı; M6 filtreli seçim + Ctrl+A; M7 F1 kaplaması + arama; M8 REGRESYON: F9, Esc/Enter,
katman gizle/kilit, kopyala-yapıştır, rota okları, makas/rampa yerleştirme, Ctrl+S round-trip.
**Test:** T390–T394 (ShortcutMap çakışma+bütünlük; F1 panel ViewModel; tooltip-harita eşleşmesi).

═══════════════════════════════════════════════════════════════════
# FAZ E — DONANIM EŞLEME · v3.0.30 → v3.0.32
═══════════════════════════════════════════════════════════════════
## v3.0.30 — HardwareEndpoint Üretimi (Yüzen İpler)
**Amaç:** ElectronicsView'da tanımlı cihazlardan (RFID/Servo) editör kenarında bağlanmayı bekleyen
uçlar üretmek. **İçerik:** `HardwareEndpoint` modeli (DeviceId, Tür, bağlı-mı); EditorView sağ kenar
rafında yüzen ip listesi (Feature Tree "Donanım" grubuyla senkron); her uçtan kaynağına yarı saydam
elastik Bézier (Adorner katmanı; render hot-path kuralları geçerli — Freeze'li geometri, tahsissiz
güncelleme); cihaz LED durumu (Yeşil/Sarı/Kırmızı/Gri — PingService+DeviceRegistry mevcut altyapısından).
Ribbon `Görünüm`e "Donanım Rafı" toggle'ı; radyal boşluk setine "Donanım Rafını Aç". **Kabul:** Cihaz
tek yerde tanımlanır her yerde referanslanır; raf toggle'ı durum çubuğuna işlenir; ipler sürüklemede
uzar (bağlama YAPMAZ — o v3.0.31). **Test:** T400–T406.

## v3.0.31 — BindTool (Sürükle-Bağla)
**Amaç:** Ucu hedefe sürükleyip bırakarak donanım-geometri bağı kurmak. **İçerik:** hedef doğrulama
(RFID→segment/ankraj noktası +OffsetMm; Servo→makas); geçersiz hedefte kırmızı vurgu + geri yaylanma
animasyonu; `BindHardwareCommand` (undo'lu) + `HardwareBindings` tablosuna kayıt (mevcut şema; şema
yetmezse DUR-sor, Y8); bağlanan uç raftan düşer, Feature Tree'de cihaz⚡ rozeti; makasın
BoundServoDeviceId'si bu akışla dolar. Esc=sürüklemeyi iptal (ilke korunur). **Kabul:** bağ undo ile
çözülür; yanlış tür hedefe bağlanamaz; Ctrl+S sonrası kapat-aç bağları geri getirir (round-trip test).
**Test:** T410–T417 (+Data round-trip T562).

## v3.0.32 — Tutarlılık Denetçisi
**Amaç:** Sahaya çıkmadan önce eksik/çelişkili kurulum uyarıları. **İçerik:** kural motoru (saf Core,
WPF'siz): bağsız makas (servo yok), bağsız istasyon RFID'i, çift-bağlı cihaz, yetim rota adımı
(v3.0.24'te ertelenen borç BURADA kapanır), boş katmanda kilit, port'suz segment ucu. Sonuçlar:
HomeView kartı + terminale Warn (LogBus) + Feature Tree'de ⚠ rozeti + durum çubuğu sayacı; karta
tıkla → ilgili nesneye zoom. Denetçi Ctrl+S öncesi otomatik + ribbon `Giriş`te "Denetle" butonu.
**Kabul:** her kural için en az bir pozitif+negatif test; uyarı nesneye zoom götürür; denetçi 500
nesnede <100ms (performans testi). **Test:** T420–T428.

═══════════════════════════════════════════════════════════════════
# FAZ F — FIRMWARE & OTA · v3.0.33 → v3.0.36
═══════════════════════════════════════════════════════════════════
## v3.0.33 — PlatformIO Çatısı + Referans Firmware
`firmware/` workspace; `common/NonBlockingTask.h`; istasyon+tren durum makinesi çekirdekleri (C++,
elle derlenebilir referans; `pio run` iki ortamda derler). UI dokunuşu yok. **Test:** T430–T433
(host-side: üretilen dosya varlığı/derlenebilirlik sarmalayıcı testleri).
## v3.0.34 — FirmwareGenerator 🔍
Scriban şablonları + DB (Devices, Bindings) → cihaz-özgü `config.h`/`main.cpp`; BindingHash (üretilen
kod hangi bağ setinden çıktı — değişince "güncel değil"). ElectronicsView cihaz kartında "Kod Üret".
**🔍 DENETİM:** v3.0.30–.34 (FAZ E+F yarısı) geriye-dönük denetim raporu. **Test:** T434–T439.
## v3.0.35 — BuildService
pio CLI Process sarmalayıcı; canlı derleme logu terminal paneline (LogBus kanalı); SemaphoreSlim tek
kuyruk; çıktı bin + SHA256. Cihaz kartında "Derle" + ilerleme. **Test:** T440–T445 (sahte-pio ile
process/kuyruk/hash testleri — gerçek altyapı ilkesine loopback istisnası sapmaya yazılır).
## v3.0.36 — OtaUploader + Verify
espota upload, ilerleme %, reboot sonrası sürüm doğrulama (MQTT heartbeat'ten BindingHash karşılaştır);
cihaz başına "Güncelle" + "güncel değil" rozeti. Uçtan uca tek tık OTA (loopback test). **Test:** T446–T452.

═══════════════════════════════════════════════════════════════════
# FAZ G — OPERASYON · v3.0.37 → v3.0.41
═══════════════════════════════════════════════════════════════════
## v3.0.37 — Senaryo CRUD
Adım listesi (tren, hedef masa/istasyon, bekleme sn, öncelik) DB'ye; senaryo düzenleyici sayfası
(sekmeli belge sistemine "Senaryo" sekme türü olarak eklenir — v3.0.29.2 altyapısı yeniden kullanılır).
**Test:** T460–T466 (+CRUD round-trip).
## v3.0.38 — KitchenView 🔍
Masa kartları, sipariş kuyruğu, tek tık senaryo başlat/duraklat; `DispatchService` üzerinden MQTT
komut yayını. **🔍 DENETİM:** v3.0.35–.38. **Test:** T470–T476.
## v3.0.39 — HomeView Dashboard + E-Stop
Sistem özeti, aktif senaryo, cihaz sağlık ızgarası; **E-Stop**: QoS2 retained + tüm komut hatları
kilitlenir + UI kilit bandı; <100ms hedefi ölçümle kanıtlanır. E-Stop durum çubuğunda ve HER sayfada
erişilebilir (global komut). **Test:** T480–T487.
## v3.0.40 — State Recovery
Uygulama/broker yeniden başlarsa: retained mesajlar + DB'den son bilinen durum; yarım senaryo devam/iptal
diyaloğu. **Test:** T488–T493.
## v3.0.41 — BlockSignalController
TrackGraph bloklarına sinyal ataması; bir blokta tek tren kuralının KOMUT tarafı (donanım sinyali +
yazılım kilidi); ihlalde otomatik durdurma + Warn. Editörde blok sınırlarını göster toggle'ı (Görünüm).
**Test:** T494–T499.

═══════════════════════════════════════════════════════════════════
# FAZ H — SİMÜLASYON · v3.0.42 → v3.0.48 → v3.1.0
═══════════════════════════════════════════════════════════════════
## v3.0.42 — SimulationLoop 🔍
Sabit Δt (accumulator) çekirdek döngü (Simulation projesi NİHAYET dolar — T006 iskelet bekçisi bu
sürümde bilinçli güncellenir, kullanıcı onaylı, sapmaya yazılır). Oynat/Duraklat/Hız (1×/4×/10×)
ribbon `Görünüm`e. **🔍 DENETİM:** v3.0.39–.42. **Test:** T500–T507 (determinizm: aynı tohum+adım
sayısı → birebir aynı durum).
## v3.0.43 — ArcLengthPath
s-parametrizasyonu: rota → toplam uzunluk + s→(x,y,z) örnekleyici; segment geçişleri TrackGraph
komşuluğundan; rampa Z interpolasyonu. **Test:** T508–T514.
## v3.0.44 — TrainDynamics
İvme/fren/sürtünme/rampa eğim etkisi; hız limitleri; durma mesafesi hesabı (blok sinyal entegrasyonu
için). **Test:** T515–T522 (fizik sınır durumları: sıfır uzunluk, tam eğim, ani dur).
## v3.0.45 — Virtual ESP32/Train
C++ durum makinesinin C# eşleniği; MQTT üzerinde GERÇEK cihazla AYNI sözleşme (A3 arteri) — DispatchService
farkı BİLMEZ. Sanal tren editör tuvalinde hareketli işaretçi olarak render (hot-path kuralları).
**Test:** T523–T530 (sözleşme eşdeğerlik testleri: aynı komut→aynı telemetri şeması).
## v3.0.46 — Lazer Engel + Güvenlik Simülasyonu 🔍
Sanal lazer bariyer; engel algılanınca tren durur, senaryo bekler; E-Stop simülasyonda da çalışır.
**🔍 DENETİM:** v3.0.43–.46. **Test:** T531–T536.
## v3.0.47 — Senaryo Oynatıcı (Uçtan Uca Sanal)
KitchenView senaryosu TAMAMEN sanal filoyla koşar: çoklu tren, blok kilitleri, istasyon bekleme;
çakışma-sız kanıt testi (iki tren aynı bloğa asla giremez — property-tabanlı test). **Test:** T537–T544.
## v3.0.48 — Sertleştirme + Kapanış Denetimi
Tüm FAZ H performans/stres (10 tren × 10× hız × 30 dk sızıntı testi), belge güncellemeleri, teknik
borç listesi kapanış kararları (FA lisansı dahil), TAM geriye-dönük denetim (v3.0.42–.48) → **v3.1.0
ETİKETİ** (kullanıcı onayıyla tag+push). **Test:** T545–T552.

═══════════════════════════════════════════════════════════════════
# EK — 🔍 DENETİM DURAKLARI ÖZETİ
═══════════════════════════════════════════════════════════════════
v3.0.29.7 (FAZ D2) · v3.0.34 · v3.0.38 · v3.0.42 · v3.0.46 · v3.0.48 (final).
Her durakta: D-serisi ham döküm + T010/T011 kırmızı-yeşil + kullanıcı blok manuel turu zorunludur.
