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

# BÖLÜM 7 — SIFIRDAN UYGULAMA YOL HARİTASI (MİKRO-SÜRÜMLER)

**Kurallar:**
1. Her sürüm **tek bir mikro-istek** büyüklüğündedir, tek başına derlenir ve çalışır durumda bırakır.
2. Ana arterler (Faz 0–2) tamamlanmadan hiçbir görsel/fonksiyonel özellik fazına geçilmez.
3. Hiçbir sürüm önceki sürümün public API'sini kırmaz; sadece ekler.
4. Simülasyon (Faz H) ve fizik testleri **en sondadır**; ondan önceki her faz simülasyonsuz, gerçek anlamıyla kullanılabilir bir ürün bırakır.

## FAZ 0 — Temel İskelet ve Ana Arterler I (Solution + Log Otobüsü)

| Sürüm | Mikro-İstek | Kabul Kriteri |
|---|---|---|
| **v3.0.0** | Solution + 7 proje + tests klasörü (Bölüm 1 yapısı birebir), Directory.Build.props (net8.0, nullable enable), NuGet sabitleri (Wpf.Ui, CommunityToolkit.Mvvm, MQTTnet, EFCore.Sqlite, Scriban). Simulation projesi **boş iskelet** olarak dahil. | `dotnet build` temiz; boş pencere açılır. |
| v3.0.1 | Generic Host + DI: `App.xaml.cs` içinde `IHost`; tüm servisler arayüz üzerinden kayıt. | Servisler ctor-injection ile çözülüyor. |
| v3.0.2 | Shell: Wpf.Ui `NavigationView`, Dark Mica + Fluent tema, 6 boş sayfa (Home, Editor, Electronics, Kitchen, Info, Settings) ve navigasyon. | 6 sayfa arası gezinme çalışır, Mica aktif. |
| v3.0.3 | **A5 — ILogBus + TerminalPanel:** halkalı tampon (max 2000), Info/Warn/Error/Success renkleri, tüm sayfaların altına dock'lu 4 satırlık kalıcı panel; InfoView = aynı otobüsün tam ekran görünümü + filtre. | Her sayfada terminal görünür; test logları renkli akar. |

## FAZ A — Ana Arterler II (Domain Modelleri + SQLite)

| Sürüm | Mikro-İstek | Kabul Kriteri |
|---|---|---|
| v3.0.4 | **A2 + A3:** `Core/Geometry` (Vector2D/3D, BoundingBox), tüm Entity sınıfları (Bölüm 2.2), enum'lar ve **Topic Contract** (sabitler + payload record'ları). Bu sürümle sözleşme **dondurulur**. | Core.Tests: geometri birim testleri yeşil. |
| v3.0.5 | **A4:** `TrainDbContext` + Bölüm 4.3'teki **tüm tablolar** tek migration'da (`InitialSchema`), WAL modu, SQLite yolu SettingsView modelinde. | DB dosyası oluşur; tüm tablolar mevcut. |
| v3.0.6 | Repository katmanı: `IProjectRepository`, `IScenarioRepository`, `IStateRepository` + CRUD; `EventLogs`'a ILogBus aynası (halkalı temizlik). | Örnek proje kaydet/yükle round-trip testi yeşil. |
| v3.0.7 | SettingsView (gerçek): tema seçimi, DB yolu, MQTT portu; ayarlar `settings.json` + anında uygulanma. | Ayar değişimi kalıcı. |

## FAZ B — Ana Arterler III (Gömülü MQTT + Cihaz Kaydı)

| Sürüm | Mikro-İstek | Kabul Kriteri |
|---|---|---|
| v3.0.8 | `EmbeddedBrokerService` (IHostedService) + loopback `MqttHub`; broker başlat/durdur logları terminale. | Harici MQTT Explorer ile bağlanılıp mesaj görülür. |
| v3.0.9 | `DeviceRegistry`: LWT + retained status ile online/offline; `restaurant/log/#` aboneliği → cihaz logları terminale. | Sahte istemci bağlan/kop → durum değişimi loglanır. |
| v3.0.10 | `PingService` (ICMP, 5 sn) + cihaz IP listesi; ping sonuçları DeviceRegistry ile birleşik `DeviceHealth` durumu. | Health durumu (Yeşil/Sarı/Kırmızı) event olarak yayınlanır. |
| v3.0.11 | Komut hattı: `restaurant/commands` yayıncısı + `ack` akış servisi (`DispatchService`) — henüz UI'sız, test istemcisiyle uçtan uca. | Komut → sahte istasyon → ACK round-trip logda. |

## FAZ C — ElectronicsView (Node-Based Ağ Şeması)

| Sürüm | Mikro-İstek | Kabul Kriteri |
|---|---|---|
| v3.0.12 | Ağ modeli CRUD: switch/port/cihaz tanımlama formları (DB'ye). | Port dağılımı (1=PC, 2-4=ESP32, 5=Cascade) kaydedilir. |
| v3.0.13 | Node-based şema tuvali: switch kutuları, port soketleri, cihaz düğümleri, bağlantı hatları; cascade alt-switch çizimi. | Şema hiyerarşiyi görsel çizer. |
| v3.0.14 | Canlı durum bağlama: `DeviceHealth` → port LED renkleri + son görülme zamanı tooltip. | Cihaz kopunca LED gerçek zamanlı kırmızıya döner. |

## FAZ D — CAD Editörü (Çekirdek → Araçlar → Ağaç)

| Sürüm | Mikro-İstek | Kabul Kriteri |
|---|---|---|
| v3.0.15 | `CadViewportControl`: DrawingVisual render, pan (orta tuş), zoom-to-cursor, mm ızgara + eksen; durum çubuğunda dünya koordinatı. | 10.000 çizgi ile 60 fps pan/zoom. |
| v3.0.16 | `CadDocument` + `CommandStack` (Undo/Redo altyapısı) + `SelectionService`. | Ctrl+Z/Y boş komutlarla çalışır. |
| v3.0.17 | **SnapEngine v1:** GridSnap + görsel snap işaretçisi. | İmleç ızgaraya yapışır. |
| v3.0.18 | **TrackTool:** tıkla-tıkla ray çizimi (`AddNode/AddSegmentCommand`), Esc ile bitir. | Ray çizilir, undo edilir, DB'ye kaydedilir (Ctrl+S). |
| v3.0.19 | **SnapEngine v2:** EndpointSnap + OnSegmentSnap + spatial hash; öncelik zinciri. | Uç noktaya "mıknatıs" hissi; segment üstü projeksiyon doğru. |
| v3.0.20 | `TrackGraph` (Topology): komşuluk, `AreAdjacent`, düğüm birleşimi; segment bölme (`SplitSegmentCommand`). | Topoloji birim testleri yeşil (ana arter — simülasyonun temeli). |
| v3.0.21 | **SelectTool + Marquee:** tek seçim, kutu seçim (L→R içeren / R→L kesişen), Delete. | Çoklu seçim + toplu silme undo'lu. |
| v3.0.22 | **Pano:** Ctrl+C/X/V (deep clone, ofsetli yapıştırma, snap ile birleşme). | Kopyalanan ray ağı bağımsız ID'lerle yapışır. |
| v3.0.23 | **Katmanlar:** Zemin/Alt Kat/Üst Kat (ZHeight), aktif katman seçimi, görünürlük/kilit; çizim aktif katmana yazılır. | Katman gizle → nesneler görünmez, seçilemez. |
| v3.0.24 | **RouteTool (Hat):** sadece segment-üstü kabul, komşuluk doğrulama (TrackGraph), yön okları render. | Boş alana hat çizilemez; ok yönleri doğru. |
| v3.0.25 | **HybridTool (Eşzamanlı):** tek harekette Track+Route, tek undo adımı (CompositeCommand). | Hibrit çizim tek Ctrl+Z ile geri alınır. |
| v3.0.26 | **Makas nesnesi (prefab):** 1 tıklamada prefab yerleştirme (RailSwitch + 3 port düğümü: Entry/MainExit/DivergingExit), SwitchTool, PreviewSwitchPlace ghost, CompositeCadCommand (tek undo), BoundServoDeviceId. | 1 tıklamada makas+3 port oluşur; Ctrl+Z tek adımda geri alır; TrackTool portlara segment bağlayabilir. |
| v3.0.27 | **Rampa nesnesi (prefab):** 1 tıklamada prefab yerleştirme (Ramp + 2 port düğümü: Entry/Exit), RampTool, StartZ/EndZ atama, eğim % etiketi, katmanlar arası geçiş doğrulaması. | Zemin→Üst Kat rampalı geçiş modellenir; TrackTool portlara bağlanabilir. |
| v3.0.28 | **Feature Tree:** hiyerarşik ağaç + çift yönlü seçim senkronu + çift tık zoom-to-entity + göster/gizle/kilit. | Ağaç ↔ tuval senkron kusursuz. |
| v3.0.29 | Sağ tık **Radyal Menü** (bağlama duyarlı: boşluk/ray/hat/makas farklı komut setleri) + kısayol haritası dokümantasyonu. | Radyal menü Fluent animasyonlu açılır. |

## FAZ E — Donanım-Ray Eşleme (Rubber-Band Binding)

| Sürüm | Mikro-İstek | Kabul Kriteri |
|---|---|---|
| v3.0.30 | `HardwareEndpoint` üretimi: ElectronicsView cihazlarından RFID/Servo uçları; EditorView kenar rafında yüzen çipler + elastik Bézier çizgileri (Adorner). | Uçlar listelenir, çizgiler sürüklemede uzar. |
| v3.0.31 | **BindTool:** sürükle-bırak, hedef doğrulama (RFID→segment/ankraj, Servo→makas), geçersizde kırmızı + geri yaylanma; `BindHardwareCommand` + `HardwareBindings` kaydı (SegmentId + OffsetMm). | Bağlama undo'lu; Feature Tree'de ✔ görünür. |
| v3.0.32 | Tutarlılık denetçisi: bağsız makas/istasyon, çift bağlanmış RFID vb. için uyarı listesi (HomeView kartı + terminal Warn). | Eksik binding'ler raporlanır. |

## FAZ F — Firmware Üretimi ve OTA

| Sürüm | Mikro-İstek | Kabul Kriteri |
|---|---|---|
| v3.0.33 | `firmware/` PlatformIO workspace + `common/NonBlockingTask.h` + istasyon/tren durum makinesi çekirdekleri (Bölüm 5.2) — elle derlenebilir referans. | `pio run` iki ortamda da derler. |
| v3.0.34 | `FirmwareGenerator`: Scriban şablonları + DB (Devices, Bindings) → `config.h`/`main.cpp` üretimi; BindingHash. | Üretilen kod derlenebilir ve cihaz-özgü. |
| v3.0.35 | `BuildService`: pio CLI Process sarmalayıcı, canlı derleme logu terminale, SemaphoreSlim kuyruk. | Tek tık "Derle" → bin + SHA256. |
| v3.0.36 | `OtaUploader` + `Verify`: espota upload, ilerleme %, reboot sonrası sürüm doğrulama; ElectronicsView'da cihaz başına "Güncelle" butonu + güncel değil ⚠ rozeti. | Uçtan uca tek tık OTA (gerçek/loopback test). |

## FAZ G — Senaryolar, Mutfak ve State Recovery

| Sürüm | Mikro-İstek | Kabul Kriteri |
|---|---|---|
| v3.0.37 | Senaryo CRUD: adım listesi (tren, hedef masa, bekleme, öncelik) DB'ye. | Senaryolar kaydedilir/düzenlenir. |
| v3.0.38 | **KitchenView:** masa kartları, sipariş kuyruğu, tek tık senaryo başlat/duraklat; komutlar `DispatchService` üzerinden yayınlanır. | Mutfaktan "Tren 1 → Masa 5" akışı gerçek cihazla çalışır. |
| v3.0.39 | **HomeView (Dashboard):** sistem özeti, aktif senaryo, cihaz sağlık ızgarası, **E-Stop** (QoS2 retained + tüm komut hatlarını kilitler). | E-Stop < 100 ms'de tüm cihazlara ulaşır ve UI kilitlenir. |
| v3.0.40 | **State Recovery (tam):** debounced TrainStates/SwitchStates yazımı, açılışta auto-load + makas senkron yayını + "kaldığı yerden devam" raporu. | Uygulama öldürülüp açıldığında durum birebir döner. |
| v3.0.41 | **BlockSignalController (gerçek mod):** `PartitionIntoBlocks` + komut öncesi blok rezervasyonu; ihlalde komut reddi + Warn. | İki tren aynı bloğa asla yönlendirilmez. |

## FAZ H — SİMÜLASYON MOTORU (FINAL STAGE)

| Sürüm | Mikro-İstek | Kabul Kriteri |
|---|---|---|
| v3.0.42 | `SimulationLoop`: dedicated thread, sabit Δt=1/120, accumulator, SimClock (TimeScale/Pause/Step); çift tamponlu snapshot köprüsü. | UI hiç kasmadan boş dünya 120 Hz tick loglar. |
| v3.0.43 | `ArcLengthPath`: Route → kümülatif uzunluk tablosu, `Sample(s)` (pozisyon/tanjant/Z/eğim); birim testleri. | s-taraması pürüzsüz örnekler döner. |
| v3.0.44 | **TrainDynamics:** ivme/fren/sürtünme/rampa denklemleri (Bölüm 6.3) + hedef hız profili; tren görseli 2-bojeli yönelimle tuvalde interpolasyonlu akar. | Rampada gözle görülür yavaşlama; hareket 60 fps pürüzsüz. |
| v3.0.45 | **VirtualStationEsp32 / VirtualTrainController:** C++ durum makinesi eşleniği, gömülü broker'a gerçek istemci olarak bağlanır; RFID geçiş olayları s-konumundan üretilir; yapay LAN gecikmesi. | Simüle "Tren 1 → Masa 5" akışı, gerçek akışla aynı MQTT trafiğini üretir (terminalden birebir izlenir). |
| v3.0.46 | Lazer engel simülasyonu (öndeki trene s-mesafesi) + `BlockSignalController` sim entegrasyonu: fren mesafesi hesaplı blok durması. | Çok trenli çakışma senaryosu güvenle durur. |
| v3.0.47 | Senaryo oynatıcı entegrasyonu: KitchenView senaryoları simülasyon dünyasında uçtan uca koşar (Digital Twin tamam). | Aynı senaryo gerçek + sanal modda aynı davranır. |
| v3.0.48 | **Sertleştirme ve test finali:** Simulation.Tests (fizik determinizmi, fren mesafesi, blok ihlali imkânsızlığı), yük testi (5 tren/50 istasyon), 24 saat soak koşusu, performans profili. | Tüm test paketi yeşil → **v3.1.0 Release**. |

---

# BÖLÜM 8 — RİSKLER VE MİMARİ KARAR KAYITLARI (ADR ÖZETİ)

1. **ACK penceresi (fiziksel gerçek):** Makas onayı RFID okumasından sonra geldiği için, sahada RFID okuyucu ile makas arasındaki ray mesafesi `v_tren · (t_okuma + t_roundtrip + t_servo)` değerinden uzun olmalıdır. Editöre ileride bu mesafeyi doğrulayan bir kural eklenebilir (mevcut binding modeli — SegmentId+OffsetMm — bunu zaten destekler; arter kırılmaz).
2. **Neden MQTTnet gömülü broker:** Harici broker kurulumu (Mosquitto) saha kurulumunu zorlaştırır; MQTTnet in-process, LWT/QoS2/retained tam destekler.
3. **Neden PlatformIO-CLI (Arduino-CLI yerine):** Ortam tanımı `platformio.ini`'de deterministik; kütüphane sürümleri kilitlenebilir → "her makinede aynı bin" garantisi. Arduino-CLI, `BuildService` arayüzü sayesinde alternatif implementasyon olarak eklenebilir.
4. **Neden DrawingVisual (Shapes değil):** Binlerce segmentte WPF Shape nesneleri layout maliyetiyle çöker; DrawingVisual + spatial hash endüstriyel CAD hissinin ön koşuludur.
5. **Neden s-parametrizasyonu:** 2D serbest fizik (x,y kuvvetleri) raylı sistemde gereksiz ve gürültülüdür; tren raydan çıkamaz → tek serbestlik derecesi `s`. Pürüzsüzlük, determinizm ve blok matematiği bedavaya gelir.
6. **Şema evrimi:** Yeni ihtiyaçlar (ör. çift makas, döner tabla) yeni tablo/kolon **ekleyerek** karşılanır; `SchemaVersion` alanı proje dosyası uyumluluğunu yönetir.

> **Sonraki adım önerisi:** v3.0.0 mikro-isteği ile başlayın: "Bölüm 1'deki solution iskeletini, Directory.Build.props ve boş 6 sayfa navigasyonuyla oluştur." Her sürümün kabul kriteri, bir sonraki isteğe geçiş kapısıdır.
