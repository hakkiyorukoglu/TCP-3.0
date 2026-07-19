# TCP 3.0 (Train Control Platform)

![Platform](https://img.shields.io/badge/Platform-Windows-blue)
![Framework](https://img.shields.io/badge/Framework-.NET%208.0-512BD4)
![UI](https://img.shields.io/badge/UI-WPF%20%2B%20Fluent-success)
![Version](https://img.shields.io/badge/Version-v3.0.29.18-orange)
![GitHub](https://img.shields.io/badge/GitHub-hakkiyorukoglu%2FTCP--3.0-lightgrey)

> GitHub: `https://github.com/hakkiyorukoglu/TCP-3.0.git`

TCP (Train Control Platform) 3.0, trenlerin otonom hareketlerini simüle eden, uzaktan komuta sağlayan ve MQTT üzerinden IoT tabanlı haberleşme ile gerçek zamanlı veri akışını yöneten yeni nesil bir masaüstü kontrol merkezidir.

---

# 🤖 YAPAY ZEKA DEVİR TESLİM (AI HANDOVER)

> **Bu bölümü okuyan AI Model için:** Bu proje bir önceki AI tarafından belirli kurallar ve sürüm disiplini ile geliştirilmiştir. Aşağıdaki bilgiler, projeyi devraldığında hızlıca oryante olman için hazırlanmıştır.

---

## 📍 ŞU ANKİ DURUM (Current State)

| Alan | Değer |
|------|-------|
| **Son Sürüm** | v3.0.29.18 |
| **Son Git Commit** | `aef61f7` (pushed to GitHub) |
| **Son Yapılan** | Sağ Properties Panel (SelectionService senkron) + Hover Highlight + README güncellemesi |
| **Sıradaki Sürüm** | v3.0.29.19 — Alt Komut Satırı + Prompt Area + Coordinate Input Fields |
| **Aktif Faz** | FAZ D3 GRUP 1 — Görsel Temel (v3.0.29.17–19) |
| **Build Durumu** | ✅ 0 Error, 0 Warning |
| **Test Durumu** | Tüm App.Tests geçiyor (6+6=12 yeni test T460–T470 arası) |

---

## 📁 KRİTİK DOSYA KONUMLARI

### Yol Haritası ve Planlar
| Dosya | Açıklama |
|-------|----------|
| `Roadmap.md` | **ANA YOL HARİTASI** — Tüm sürümler, fazlar, gruplar, kabul kriterleri, manuel test adımları. **TEK DOĞRULUK KAYNAĞI.** |
| `plans/` | Her sürüm için plan ve mühür raporları (`v3{versiyon}_aciklama_plan.md` + `_muhur.md`) |
| `onceki_talimat.txt` | **AGENT KURALLARI** — Her versiyonda değişmesi gerekenler, test numaralandırma, mühür şablonu, hatalı durum kuralları |
| `tools/sapma.txt` | Y5 sapma kaydı — v3.0.29.32'de dinamik katman sistemine geçiş için |
| `muhur_talimati.txt` | Eski mühür talimatı (v3.0.18 dönemi) |
| `dberror.txt` | SQLite migration hata log'u (oluşursa DB'yi sil) |

### Kaynak Kod
| Klasör | Açıklama |
|--------|----------|
| `src/TrainService.App/` | WPF UI katmanı — Views, ViewModels, Controls, Services |
| `src/TrainService.Cad/` | CAD çekirdeği — Snapping, Tools, Selection, Clipboard, UndoRedo, Topology |
| `src/TrainService.Core/` | Domain modelleri — Entities, Geometry (Vector2D), Enums, Abstractions, Events |
| `src/TrainService.Data/` | EF Core 8 + SQLite — TrainDbContext, Migrations, Repositories |
| `src/TrainService.Messaging/` | MQTTnet broker + Hub + DeviceRegistry + PingService |
| `src/TrainService.Firmware/` | C++ kod üretimi + PlatformIO build + OTA |
| `src/TrainService.Simulation/` | Fizik motoru (boş iskelet — Faz H'de doldurulacak) |
| `tests/` | 7 test projesi (App, Cad, Core, Data, Messaging, Simulation, Architecture) |
| `tools/` | PowerShell script'leri (mühür, denetim, push) |
| `firmware/` | Gerçek ESP32 C++ projeleri (station_esp32, train_esp32c3, common) |

### Test Projeleri
| Proje | Ne Test Eder? |
|-------|---------------|
| `TrainService.App.Tests` | UI kontrolleri, ViewModel'ler, Ribbon, Properties Panel, ikonlar |
| `TrainService.Cad.Tests` | Snap, Tools, UndoRedo, Clipboard, Topology, Selection |
| `TrainService.Core.Tests` | Entity modelleri, Geometry, Vector2D |
| `TrainService.Data.Tests` | CRUD, persistence, round-trip |
| `TrainService.Messaging.Tests` | MQTT broker, pub/sub, LWT, retained mesajlar |
| `TrainService.Simulation.Tests` | Simülasyon (boş) |
| `TrainService.Architecture.Tests` | Bekçi testleri (T001–T011), katman bağımlılıkları |

---

## 🏗️ SOLUTION YAPISI

```
TrainService.sln
├── src/
│   ├── TrainService.App/          # WPF UI (Wpf.Ui + MVVM)
│   │   ├── App.xaml / App.xaml.cs # DI Host (IHost)
│   │   ├── Views/Pages/
│   │   │   ├── EditorView.xaml/cs  # ★ Ana editör sayfası (Feature Tree + Viewport + Properties)
│   │   │   ├── HomeView.xaml       # Dashboard
│   │   │   ├── ElectronicsView.xaml# Ağ topolojisi
│   │   │   ├── KitchenView.xaml    # Sipariş yönetimi
│   │   │   ├── InfoView.xaml       # Sistem bilgisi
│   │   │   └── SettingsView.xaml   # Ayarlar
│   │   ├── ViewModels/
│   │   │   ├── EditorViewModel.cs  # Editor VM (ActiveLayerId, RibbonTabs, komutlar)
│   │   │   ├── DocumentTabsViewModel.cs # Sekme yöneticisi
│   │   │   └── ...
│   │   ├── Controls/
│   │   │   ├── Ribbon/             # RibbonControl, RibbonDefinition (23 item, 4 tab)
│   │   │   ├── CadCanvas/          # CadViewportControl (DrawingVisual render, crosshair)
│   │   │   ├── FeatureTree/        # Sol panel entity ağacı
│   │   │   ├── PropertiesPanel/    # Sağ panel — seçili entity özellikleri (v3.0.29.18)
│   │   │   ├── DocumentTabs/       # Sekme şeridi kontrolü
│   │   │   ├── TerminalPanel/      # Alt log paneli
│   │   │   └── RadialMenu/         # Sağ tık bağlam menüsü
│   │   ├── Models/EditorTabModel.cs # Sekme başına izole veri seti
│   │   ├── Services/               # UI servisleri (Navigation, Dialog, Theme)
│   │   └── Resources/CadColors.cs  # Merkezî renk paleti
│   ├── TrainService.Core/          # Domain + Arayüzler (bağımlılıksız)
│   ├── TrainService.Cad/           # CAD çekirdeği (UI'sız, WPF'siz, test edilebilir)
│   ├── TrainService.Data/          # EF Core 8 + SQLite
│   ├── TrainService.Messaging/     # MQTTnet v4 (gömülü broker)
│   ├── TrainService.Firmware/      # C++ kod üretimi + PlatformIO
│   └── TrainService.Simulation/    # Fizik motoru (boş iskelet)
├── tests/ (7 proje)
├── firmware/ (ESP32 C++)
└── tools/ (PowerShell script'leri)
```

**Bağımlılık yönü (asla ters akmaz):**
`App → (Cad, Messaging, Data, Firmware, Simulation) → Core`
`Core` hiçbir projeye referans vermez.

**5 Ana Arter (A1–A5):**
- A1: Katmanlı Solution + DI — tüm servisler interface üzerinden
- A2: Domain Modelleri (mm bazlı geometri) — UI tipi sızamaz
- A3: MQTT Topic Contract — donduruldu, sadece eklenir
- A4: SQLite Migration disiplini — kolon silinmez/yeniden adlandırılmaz
- A5: LogBus — tüm servisler ILogBus üzerinden yazar

---

## 📦 NUGET PAKETLERİ

| Paket | Versiyon | Kullanım |
|-------|----------|----------|
| `CommunityToolkit.Mvvm` | 8.4.2 | MVVM (ObservableProperty, RelayCommand) |
| `MahApps.Metro.IconPacks` | 5.0.0 | **v3.0.29.17'de eklendi** — Ribbon ikonları (MaterialDesign) |
| `Microsoft.EntityFrameworkCore.Design` | 8.0.6 | EF Core migration'ları |
| `Microsoft.Extensions.Hosting` | 10.0.10 | DI Host (Generic Host) |
| `WPF-UI` | 3.0.4 | Fluent Design UI framework (NavigationView, SymbolIcon, Button) |
| `MQTTnet` | v4 | Gömülü broker + client (Messaging katmanı) |
| `FluentAssertions` | - | Test assertion framework (tüm test projelerinde) |
| `xUnit` + `xunit.runner.visualstudio` | - | Test framework |
| `NetArchTest.Rules` | - | Mimari bekçi testleri (Architecture.Tests) |

---

## 🔧 Agent Kuralları (`onceki_talimat.txt`)

### Her Sürüm İçin 7 Adımlı İş Akışı:
1. **ADIM 0 — PLAN:** `plans/v3{versiyon}_aciklama_plan.md` oluştur → DUR → kullanıcı onayı
2. **ADIM 1 — TDD:** Testi önce yaz, KIRMIZI gör
3. **ADIM 2 — KOD:** Plan dosyasındaki değişen dosyaları implemente et
4. **ADIM 3 — TEST:** `dotnet test` — TÜM testler geçmeli, regresyon OLMAMALI
5. **ADIM 4 — MANUEL TUR:** `dotnet run --project src/TrainService.App` → kullanıcı test eder
6. **ADIM 5 — MÜHÜR:** `plans/v3{versiyon}_muhur.md` oluştur (aşağıdaki şablona göre)
7. **ADIM 6 — ROADMAP:** `Roadmap.md`'de sürümü `(MÜHÜRLENDİ)` işaretle
8. **ADIM 7 — GIT:** `git commit -m "v{sürüm}: {açıklama}"` → kullanıcı "pushla" derse `git push`

### Her Sürümde OTOMATİK Değişmesi Gerekenler:
- ✅ Plan dosyası (`plans/v3{versiyon}_plan.md`)
- ✅ Test dosyası (`tests/.../T{baslangic}_{bitis}_Tests.cs`)
- ✅ Mühür raporu (`plans/v3{versiyon}_muhur.md`)
- ✅ Roadmap.md — sürüm durumu güncelle
- ✅ Git commit

### Mühür Raporu Şablonu:
```markdown
# Mühür Raporu — v{sürüm} ({açıklama})
## Teslimat Bilgileri
| Sürüm, Önceki |
## Kapsam
## Yapılan Değişiklikler
## Test Sonuçları (**{passed}/{total} PASSED**)
## Bekçi Kontrolü (T001–T011, Core/Cad değişiklik, Y5)
## Mühür (Plan onaylandı + Implementasyon + Test + Bekçi)
```

---

## 🛡️ MÜHÜRLÜ DAVRANIŞLAR (Y5 — ASLA DEĞİŞMEZ)

| Kural | Açıklama |
|-------|----------|
| F9 | Snap toggle (aç/kapa) |
| Esc | İPTAL — aktif tool'u iptal eder, diyalog kapatır |
| Enter / SağTık | COMMIT — tool zincirini bitirir |
| SabitKatmanlar GUID | `11111111-1111-1111-1111-111111111111` (Zemin), `2222...` (AltKat), `3333...` (ÜstKat) — seed olarak korunur |
| IsVisible / IsSelectable | Tek kaynak: `CadDocument` üzerinden |
| CadColors | Merkezî renk tanımları (`src/TrainService.App/Resources/CadColors.cs`) |

### ⚠ Sapma Kaydı (`tools/sapma.txt`)
- **v3.0.29.32:** SabitKatmanlar GUID'leri seed olarak korunur, dinamik katman sistemine geçişte yeni ID'ler `Guid.NewGuid()` ile üretilir. Gerekçe: Profesyonel CAD için 3 katman yetersiz.

---

## 🧪 BEKÇİ TESTLERİ (T001–T011)

| Test | Açıklama | Katman |
|------|----------|--------|
| T001 | Core hiçbir projeye bağımlı değil | Architecture |
| T002 | Cad sadece Core'a bağımlı | Architecture |
| T003 | Core'da WPF tipi yok (System.Windows/PresentationCore) | Architecture |
| T004 | Cad'de WPF tipi yok (headless test edilebilirlik) | Architecture |
| T005 | Messaging Data'ya bağımlı değil | Architecture |
| T006 | Simulation iskelet boş (≤2 public tip) | Architecture |
| T007 | Tüm Entity'ler CadEntity'den türer | Architecture |
| T008 | Tüm Command'ler ICadCommand uygular | Architecture |
| T009 | DI kayıtları tam (varsa) | Architecture |
| T010 | Kapsam bekçisi eşiği güncel (public tip sayısı) | Architecture |
| T011 | Trivial test guard (IL ≤12 byte yakalama) | Architecture |

---

## 📊 FAZ VE VERSİYON ÖZETİ

### FAZ D2 — RIBBON & ÇOKLU BELGE (v3.0.29.1 → v3.0.29.7) ✅ MÜHÜRLÜ
Ribbon şerit, sekmeli çoklu belge, runtime entegrasyonu. Testler: T330–T394.

### FAZ D2-DEVAM — EDİTÖR CİLASI (v3.0.29.8 → v3.0.29.16) ✅ MÜHÜRLÜ
Hızlı cilalar: Ribbon Proxy, Layers, TerminalPanel, Katman Seçici, Feature Tree Toggle, Status Bar, Kısayol, Zoom, Snap. Testler: T400–T417.

### FAZ D3 — EDİTÖR PROFESYONEL CİLA (v3.0.29.17 → v3.0.29.42)
26 sürüm, 8 grup:

| Grup | Versiyon | Tema | Test | Durum |
|------|----------|------|------|-------|
| G1 | v3.0.29.17–19 | Görsel Temel | T460–T474 | v3.0.29.17-18 ✅, v3.0.29.19 ⏳ |
| G2 | v3.0.29.20–23 | Seçim ve Snap (Ortho **F10**!) | T475–T492 | ⏳ |
| G3 | v3.0.29.24–27 | Modify Araçları | T493–T512 | ⏳ |
| G4 | v3.0.29.28–30 | Draw Araçları (Circle/Arc!) | T513–T530 | ⏳ |
| G5 | v3.0.29.31–33 | Ribbon ve UI (dinamik katman ⚠) | T531–T545 | ⏳ |
| G6 | v3.0.29.34–36 | Annotation | T546–T560 | ⏳ |
| G7 | v3.0.29.37–39 | Verimlilik | T561–T575 | ⏳ |
| G8 | v3.0.29.40–42 | Son Dokunuşlar | T576–T587 | ⏳ |

### FAZ D3-SONRASI (v3.0.29.43 → v3.0.29.48) ⏳
D2'den ertelenen özellikler. Testler: T588–T615.

### FAZ E → H (v3.0.30 → v3.0.48)
Donanım Eşleme, Firmware & OTA, Operasyon, Simülasyon. Detaylar `Roadmap.md`'de.

---

## 🚀 TEST NUMARALANDIRMA TABLOSU

| Versiyon Aralığı | Test Bloğu | Açıklama |
|------------------|------------|----------|
| v3.0.29.1–.7     | T330–T394  | FAZ D2 (Ribbon & Çoklu Belge) |
| v3.0.29.8–.16    | T400–T417  | FAZ D2-DEVAM (Editör Cilası) |
| v3.0.29.17–.19   | T460–T474  | FAZ D3 G1 (Görsel Temel) |
| v3.0.29.20–.23   | T475–T492  | FAZ D3 G2 (Seçim ve Snap) |
| v3.0.29.24–.27   | T493–T512  | FAZ D3 G3 (Modify Araçları) |
| v3.0.29.28–.30   | T513–T530  | FAZ D3 G4 (Draw Araçları) |
| v3.0.29.31–.33   | T531–T545  | FAZ D3 G5 (Ribbon ve UI) |
| v3.0.29.34–.36   | T546–T560  | FAZ D3 G6 (Annotation) |
| v3.0.29.37–.39   | T561–T575  | FAZ D3 G7 (Verimlilik) |
| v3.0.29.40–.42   | T576–T587  | FAZ D3 G8 (Son Dokunuşlar) |
| v3.0.29.43–.48   | T588–T615  | FAZ D3-SONRASI |
| v3.0.30–.32      | T400–T428  | FAZ E (Donanım Eşleme) |
| v3.0.33–.36      | T430–T452  | FAZ F (Firmware & OTA) |
| v3.0.37–.41      | T460–T499  | FAZ G (Operasyon) |
| v3.0.42–.48      | T500–T552  | FAZ H (Simülasyon) |

**Kural:** Test numarası, roadmap'teki atanmış aralıktan seçilir. Çakışma durumunda boş aralığa kaydırılır.

---

## 🧪 TEST KOMUTLARI

```bash
# Tüm testler (7 proje)
dotnet test

# Sadece App testleri
dotnet test tests/TrainService.App.Tests/

# Belirli test grubu (T460–T464)
dotnet test tests/TrainService.App.Tests/ --filter "FullyQualifiedName~T460"

# Build + test (test dosyası değiştiyse — restore süresini atlar)
dotnet build tests/TrainService.App.Tests/TrainService.App.Tests.csproj && dotnet test tests/TrainService.App.Tests/ --no-build --filter "FullyQualifiedName~T465"

# Sadece build
dotnet build src/TrainService.App/TrainService.App.csproj
```

---

## ⚠️ ÖNEMLİ NOTLAR & PÜF NOKTALARI

1. **Roadmap TEK DOĞRULUK KAYNAĞIDIR. `Roadmap.md`**'ye her zaman başvur. Sıradaki sürüm = `(MÜHÜRLENDİ)` işareti OLMAYAN ilk satır.
2. **F8 tuşu ÇAKIŞMASI:** v3.0.29.1'de F8 = Switch aracı olarak mühürlendi. v3.0.29.23'te Ortho Mode için **F10** kullanılacak.
3. **Dinamik Katman (v3.0.29.32):** Y5 sapması! SabitKatmanlar GUID'leri seed olarak korunur, yeni katman ID'leri dinamik üretilir. `tools/sapma.txt`'de kayıtlı.
4. **Circle/Arc (v3.0.29.29):** Segmentlere ayrıştırılarak TrackSegment olarak depolanır — yeni CadEntity türü EKLENMEZ. Bölüm 2.2'deki "yoktur" ifadesi güncellendi.
5. **Katman (A1 arteri):** Core'a dokunulmaz. Cad sadece Core'a bağımlıdır. WPF tipleri (Point, Brush vb.) Cad/Core'a SIZAMAZ.
6. **EditorView Layout:** 3 kolon — sol 250px (Feature Tree), orta * (Viewport), sağ 220px (Properties Panel — v3.0.29.18'de eklendi).
7. **gitignore:** `dberror.txt`, `*.db`, `src/TrainService.App/Logs/` hariç tutuluyor. `.gitignore`'a yeni artifact eklersen ONCEKI TALIMAT'a da ekle.
8. **DB hatasında:** `del trainservice.db` yap, yeniden çalıştır. Migration otomatik oluşur.
9. **Mühür raporu şablonu `onceki_talimat.txt`'te.** Her sürüm sonunda bu şablona göre rapor yaz.
10. **Test KIRMIZI → ÜRETİM KODUNU düzelt.** Asla testi zayıflatma. Bekçi ihlali → `sapma.txt`'ye yaz.

---

## 🖥️ UYGULAMA BAŞLATMA

```bash
# Hızlı başlat (bat dosyası)
1-click

# veya manuel
dotnet run --project src/TrainService.App/TrainService.App.csproj
```

### Uygulama Sayfaları (NavigationView)
- **Home** — Dashboard
- **Editor** — CAD editörü (Feature Tree + Viewport + Properties Panel)
- **Electronics** — Ağ topolojisi / cihaz yönetimi
- **Kitchen** — Sipariş yönetimi
- **Info** — Sistem bilgisi
- **Settings** — Ayarlar

---

## 📝 TAM SÜRÜM GEÇMİŞİ (Changelog)

### v3.0.29.18 — Sağ Properties Panel + Hover Highlight ✅
- **YENİ:** `Controls/PropertiesPanel/PropertiesPanelControl.cs` — Sağ kenar paneli; ID, Layer, X, Y, Z, Tür alanları.
- **DEĞİŞEN:** `Views/Pages/EditorView.xaml` — 3 kolon layout (250 + * + 220).
- **DEĞİŞEN:** `Views/Pages/EditorView.xaml.cs` — ReattachActiveTab()'de PropertiesPanel bağlantısı.
- **TEST:** 6 reflection testi (T465–T470). 6/6 PASSED.

### v3.0.29.17 — İkon Paketi (MahApps.Metro.IconPacks) + Crosshair Cursor ✅
- NuGet: `MahApps.Metro.IconPacks` v5.0.0. RibbonDefinition'da 23 item için MaterialDesign ikonları.
- `CadViewportControl.cs`: `_crosshairVisual` + `RenderCrosshair()` (kesikli çizgili artı işareti).
- **TEST:** T460–T464 (6 test). **Mühür:** `plans/v302917_icons_cursor_muhur.md`.

### v3.0.29.16 → v3.0.29.1 (tümü MÜHÜRLÜ)
Detaylı değişiklikler ve plan/mühür referansları için `Roadmap.md` FAZ D2 ve D2-DEVAM bölümlerine bakınız.

---

## 🔗 İLGİLİ DOKÜMANLAR

| Dosya | Açıklama |
|-------|----------|
| `Roadmap.md` | Tam yol haritası — mimari anayasa, faz planları, kabul kriterleri |
| `docs/DbSchema.md` | Veritabanı şeması |
| `docs/TopicContract.md` | MQTT konu sözleşmesi |
| `docs/Roadmap.md` | Eski roadmap kopyası |

---

*Son güncelleme: 2026-07-19 · v3.0.29.18 · Git: aef61f7 · Aktif Faz: D3 G1*