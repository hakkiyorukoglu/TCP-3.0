# TCP 3.0 (Train Control Platform)

![Platform](https://img.shields.io/badge/Platform-Windows-blue)
![Framework](https://img.shields.io/badge/Framework-.NET%208.0-512BD4)
![UI](https://img.shields.io/badge/UI-WPF%20%2B%20Fluent-success)

TCP (Train Control Platform) 3.0, trenlerin otonom hareketlerini simüle eden, uzaktan komuta sağlayan ve MQTT üzerinden IoT tabanlı haberleşme ile gerçek zamanlı veri akışını yöneten yeni nesil bir masaüstü kontrol merkezidir.

## 🚀 Özellikler

- **Modern Kullanıcı Arayüzü**: WPF ve Wpf.Ui kütüphaneleriyle hazırlanmış karanlık mod ve Mica destekli Fluent tasarım.
- **Modüler Mimari**: Bağımlılık Enjeksiyonu (Dependency Injection) ile ayrıştırılmış; Core, App, Cad, Data, Firmware, Messaging ve Simulation katmanlarından oluşan temiz yapı.
- **Gerçek Zamanlı Haberleşme**: MQTTnet tabanlı Hub yapısıyla cihazlardan ve istasyonlardan gelen verilerin anlık işlenmesi.
- **Merkezi Veritabanı ve Repository**: Entity Framework Core 8 ve SQLite kullanılarak, sistemdeki tüm trenlerin, cihazların ve ray geometrilerinin kalıcı olarak depolanması.
- **Gelişmiş Log Otobüsü (LogBus)**: Uygulama içi terminaline ek olarak, alınan tüm önemli olayların asenkron bir biçimde veritabanındaki `EventLogs` tablosuna aynalanması.

## 🏗️ Mimari Yapı

Proje toplamda **7 katmanlı** bir Clean Architecture (Temiz Mimari) yaklaşımını benimser:
- `TrainService.Core`: Arayüzler (Interfaces), modeller, enumlar ve çekirdek iş kuralları.
- `TrainService.App`: Uygulama sunumu (WPF), MVVM mimarisi ve arayüz servisleri.
- `TrainService.Messaging`: İletişim (MQTT) altyapısı.
- `TrainService.Data`: Veritabanı (EF Core) bağlantıları.
- `TrainService.Simulation`: Tren davranışları ve simülasyon algoritmaları.
- `TrainService.Cad`: CAD dosyalarının okunması, harita motorları.
- `TrainService.Firmware`: Gömülü sistemlere (ESP vb.) dair kod/yardımcı dosyalar.

## ⚙️ Kurulum ve Çalıştırma

### Gereksinimler
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Windows 10/11 (WPF arayüzü nedeniyle)
- MQTT Broker (Örn: Mosquitto - Varsayılan `127.0.0.1:1883`)

### Çalıştırma
Projeyi derlemek ve başlatmak için terminalden aşağıdaki komutları kullanabilirsiniz:

```bash
# Bağımlılıkları yükle ve projeyi derle
dotnet build

# Uygulamayı başlat
dotnet run --project src/TrainService.App/TrainService.App.csproj
```

> **Not:** Windows masaüstündeki `TrainService_Baslat.bat` dosyasını çalıştırarak uygulamayı hızlıca başlatabilirsiniz.

## 📝 Sürüm Geçmişi (Changelog)

- **v3.0.26**: `SwitchTool` (Makas Çizimi) tam implementasyonu tamamlandı. 4 durumlu state machine (Idle→NodeSelected→MainSelected→DivergingSelected) ile 3 tıkta makas oluşturur: (1) Endpoint snap ile düğüm seçimi, (2) OnSegment snap ile ana hat segmenti seçimi, (3) OnSegment snap ile sapak segmenti seçimi → otomatik commit. `PreviewSwitch` record'u seçilen düğüm/segmentleri ve aday segment vurgusunu önizler. `SetNodeRoleCommand` ile TrackNode.Role otomatik SwitchNode'a yükseltilir, Undo'da geri alınır. `CompositeCadCommand` (AddEntityCommand + SetNodeRoleCommand) ile tek Ctrl+Z ile geri alınabilir. Bayat entity koruması (`_committed` flag), aynı segment reddi, az bağlantılı düğüm reddi (<2), zaten SwitchNode reddi, Escape ile iptal. 10 test (T270-T279) ile kapsama alındı; 116/116 Cad.Tests yeşil.
  - **Düzeltme 1 — PreviewSwitch render**: Plan WPF katmanını kapsamadığı için `CadViewportControl.RenderToolLayer`'da `PreviewSwitch` önizlemesi hiç render edilmiyordu. `CadColors`'a 6 yeni switch rengi ve `RenderToolLayer`'a 4 aşamalı görsel geri bildirim (aday düğüm→seçili düğüm→ana hat→sapak) eklendi.
  - **Düzeltme 2 — RailSwitch model render**: `RailSwitch` entity'leri ve `TrackNode.Role == SwitchNode` durumu `RenderModelBake`'de render edilmiyordu. TrackNode artık magenta kare, üzerinde eşkenar dörtgen (diamond) ile işaretleniyor.

- **v3.0.25**: `HybridTool` (Eşzamanlı Ray+Hat) tam implementasyonu tamamlandı. TrackTool ve RouteTool davranışlarını birleştiren hibrit araç: sol tık ile segment üstünde chaining başlatır, her tık bir `TrackNode` + `TrackSegment` + `RouteStep` üretir. `PreviewHybrid` record'u ile hem çizgi (From/To/SegmentGecerli) hem rota (Steps/AdaySegmentId/AdayGecerli) önizlemesi tek seferde render edilir. Commit anında tüm oluşturulan entity'ler tek `CompositeCadCommand` içinde sarılır — tek Ctrl+Z ile geri alınır. Bayat graf koruması (`_tiklananSegmentIds` ile doc entity varlık denetimi), segmente komşu olmayan aday reddi, Escape ile iptal. 10 test (T260-T269) ile kapsama alındı; 187/187 tüm çözüm yeşil.

- **v3.0.24**: `RouteTool` (Hat Çizimi) tam implementasyonu tamamlandı. Sadece segment-üstü snap kabul eden, TrackGraph ile komşuluk doğrulaması yapan, yön okları render eden rota çizim aracı. `PreviewRoute` record'u ile aday segment + yön önizlemesi. Boş alana hat çizilemez; ardışık adımlar grafikte BFS ile doğrulanır. T010 bekçi ispatı metodolojisi (`//[Fact]` kesme) sabitlendi.

- **v3.0.22**: **Pano (Clipboard)** eklendi. `CadClipboard` ile CAD nesnelerinin kopyalama (Ctrl+C), kesme (Ctrl+X) ve yapıştırma (Ctrl+V) işlemleri. Deep clone ile bağımsız ID'ler üretilir, yapıştırma imleç konumuna ofsetli yapılır. Snap ile düğüm birleşimi desteklenir. `PasteCommand` undo/redo uyumlu.

- **v3.0.21**: `SelectTool` tam implementasyonu tamamlandı. AutoCAD referanslı Marquee seçim: soldan-sağa = Window (mavi, Contains), sağdan-sola = Crossing (yeşil kesikli, IntersectsWith). Hover vurgusu (cyan), seçim vurgusu (beyaz kesikli), `DeleteEntitiesCommand` (undo'lu silme) ve merkezi `CadColors` paleti eklendi.

- **v3.0.20**: `TrackGraph` ile ray ağının mantıksal topolojisi, komşuluk analizleri, rota doğrulama ve blok bölümleme mekanizması Core katmanına eklendi. İlk geniş çaplı "5-Sürüm Geriye Dönük Denetim" gerçekleştirildi. README sürüm geçmişi düzenli güncellenmeye başlandı.

- **v3.0.19**: `SnapEngine v2` geliştirilerek Endpoint (uç nokta) ve OnSegment (hat üzeri) yakalama özellikleri `SpatialHash` mimarisiyle eklendi.
- **v3.0.18**: `TrackTool` entegre edilerek ekranda tıkla-tıkla ray çizim mekanizması ve Ctrl+S (Undo/Redo CommandStack) ilişkisel kayıt altyapısı sağlandı.
- **v3.0.17**: `SnapEngine v1` ile GridSnap (ızgaraya hizalama) altyapısı kuruldu.
- **v3.0.16**: `CadDocument`, `CommandStack` ve `SelectionService` mimarisi eklendi. Çizim alanının doküman bazlı mutasyon ve Undo/Redo özellikleri tamamlandı.
- **v3.0.15**: CAD Editörü altyapısı (CadViewportControl) oluşturuldu. Endüstriyel render performansı elde etmek adına `Shape` veya primitif render yerine, doğrudan DirectX/GPU iletişimine dayalı `StreamGeometry` ve Batch-Render (VBO benzeri) mimarisi sisteme entegre edildi. Z-Index sorunları giderilerek 60 FPS donanım seviyesinde çalışan gerçek UI sayaçları ve X/Y milimetrik koordinat panelleri eklendi. Ana mimari kuralları korunarak 10.000 çizgi testinde pürüzsüz akıcılık (Retained Mode) doğrulandı.
- **v3.0.14**: Ağ üzerindeki cihazların canlı durumlarını (DeviceHealth) takip etmek üzere PingService ve DeviceRegistry MQTT LWT sistemi birleştirildi. ElectronicsView canvasındaki cihazlara canlı durum (LED) göstergeleri eklendi. `CommunityToolkit.Mvvm` bağımlılığı Messaging katmanından ayrılarak saf C# event'lerine (`Action`) geçirildi; bu sayede temiz mimari (Clean Architecture) bağımlılık kuralları (Core -> Messaging -> App) tamamen sağlandı. PingService için `PingServiceTests` xUnit testleri projeye dâhil edildi.
- **v3.0.13**: Node-based network topology canvas (ElectronicsView) eklendi. Switch'ler ve cihazlar DataGrid üzerinde render edilmek yerine tuval üzerinde gösterilmeye başlandı. Cihaz pozisyonları otomatik hesaplandı.
- **v3.0.12 - Öncesi**: EF Core (SQLite) altyapısı, MQTT Embedded broker kurulumu, Dispatch (ACK/Timeout tabanlı) komut kuyruğu mekanizmaları ve birim testleri başarıyla tamamlandı.

---
*Geliştirme aşamasındadır (v3.0.x).*
