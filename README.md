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

### 🚀 İLK 5 DAKİKA — NEREDEN BAŞLAMALI?

1. **Bu README'yi oku** (zaten okuyorsun)
2. **`onceki_talimat.txt`'i oku** — Agent kuralları, iş akışı, test numaralandırma
3. **`Roadmap.md`'yi aç, FAZ D3 G1'e git** — Sıradaki sürümü ve kapsamını gör
4. **`dotnet build` çalıştır** — Build altyapısını doğrula
5. **`dotnet test tests/TrainService.App.Tests/` çalıştır** — Mevcut testlerin geçtiğini gör
6. **Sıradaki sürüm için PLAN yaz** → kullanıcıya onaylat → TDD ile başla

---

## 📍 ŞU ANKİ DURUM (Current State)

| Alan | Değer |
|------|-------|
| **Son Sürüm** | v3.0.29.18 |
| **Son Git Commit** | `cffd963` (pushed to GitHub) |
| **Son Yapılan** | Sağ Properties Panel + Hover Highlight + README AI handover (tam oryantasyon) + Agent kuralları güncellemesi |
| **Sıradaki Sürüm** | v3.0.29.19 — Alt Komut Satırı + Prompt Area + Coordinate Input Fields |
| **Aktif Faz** | FAZ D3 GRUP 1 — Görsel Temel (v3.0.29.17–19) |
| **Build Durumu** | ✅ 0 Error, 0 Warning |
| **Test Durumu** | Tüm App.Tests geçiyor (12 yeni test T460–T470) |

---

## 📁 KRİTİK DOSYA KONUMLARI

### Yol Haritası ve Planlar
| Dosya | Açıklama |
|-------|----------|
| `Roadmap.md` | **ANA YOL HARİTASI** — TEK DOĞRULUK KAYNAĞI. Sıradaki sürüm burada. |
| `plans/` | Her sürüm için plan ve mühür raporları (`v3{versiyon}_aciklama_plan.md` + `_muhur.md`) |
| `onceki_talimat.txt` | **AGENT KURALLARI** — İş akışı, test numaralandırma, mühür şablonu, README güncelleme adımları |
| `tools/sapma.txt` | Y5 sapma kaydı |
| `muhur_talimati.txt` | Eski mühür talimatı (referans) |

### Kaynak Kod
| Klasör | Açıklama |
|--------|----------|
| `src/TrainService.App/` | WPF UI katmanı |
| `src/TrainService.Cad/` | CAD çekirdeği (Snapping, Tools, Selection, Clipboard, UndoRedo, Topology) |
| `src/TrainService.Core/` | Domain modelleri — Entities, Geometry (Vector2D), Enums, Abstractions |
| `src/TrainService.Data/` | EF Core 8 + SQLite |
| `src/TrainService.Messaging/` | MQTTnet v4 broker + Hub + DeviceRegistry |
| `src/TrainService.Firmware/` | C++ kod üretimi + PlatformIO build + OTA |
| `src/TrainService.Simulation/` | Fizik motoru (boş iskelet — Faz H'de doldurulacak) |
| `tests/` | 7 test projesi |
| `tools/` | PowerShell script'leri |
| `firmware/` | ESP32 C++ projeleri |

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
│   │   ├── Views/Pages/EditorView.xaml/cs  # ★ Ana editör sayfası
│   │   ├── ViewModels/EditorViewModel.cs   # Editor VM
│   │   ├── Controls/
│   │   │   ├── Ribbon/             # RibbonControl, RibbonDefinition (23 item, 4 tab)
│   │   │   ├── CadCanvas/          # CadViewportControl (DrawingVisual, crosshair)
│   │   │   ├── FeatureTree/        # Sol panel entity ağacı
│   │   │   ├── PropertiesPanel/    # Sağ panel (v3.0.29.18)
│   │   │   ├── DocumentTabs/       # Sekme şeridi
│   │   │   ├── TerminalPanel/      # Alt log paneli
│   │   │   └── RadialMenu/         # Sağ tık bağlam menüsü
│   │   ├── Models/EditorTabModel.cs
│   │   └── Resources/CadColors.cs  # Merkezî renk paleti
│   ├── TrainService.Core/          # Domain + Arayüzler (bağımlılıksız)
│   ├── TrainService.Cad/           # CAD çekirdeği (UI'sız, WPF'siz)
│   ├── TrainService.Data/          # EF Core 8 + SQLite
│   ├── TrainService.Messaging/     # MQTTnet v4
│   ├── TrainService.Firmware/      # C++ kod üretimi + PlatformIO
│   └── TrainService.Simulation/    # Fizik motoru (boş)
├── tests/ (7 proje)
├── firmware/ (ESP32 C++)
└── tools/ (PowerShell)
```

**Bağımlılık yönü (asla ters akmaz):**
`App → (Cad, Messaging, Data, Firmware, Simulation) → Core`

**5 Ana Arter (A1–A5):**
A1: Solution + DI · A2: Domain Modelleri (mm) · A3: MQTT Topic Contract · A4: SQLite Migration · A5: LogBus

---

## 📦 NUGET PAKETLERİ

| Paket | Versiyon | Kullanım |
|-------|----------|----------|
| `CommunityToolkit.Mvvm` | 8.4.2 | MVVM |
| `MahApps.Metro.IconPacks` | 5.0.0 | **v3.0.29.17 eklendi** — Ribbon ikonları (MaterialDesign) |
| `Microsoft.EntityFrameworkCore.Design` | 8.0.6 | EF Core migration |
| `Microsoft.Extensions.Hosting` | 10.0.10 | DI Host |
| `WPF-UI` | 3.0.4 | Fluent UI framework |
| `MQTTnet` | v4 | Gömülü broker + client |
| `FluentAssertions` | - | Test assertion |
| `xUnit` | - | Test framework |
| `NetArchTest.Rules` | - | Mimari bekçi testleri |

---

## 🔧 GELİŞTİRME İŞ AKIŞI (Agent Kuralları)

Her sürüm için **8 adım** (`onceki_talimat.txt`):
1. **PLAN** → `plans/v3{versiyon}_plan.md` → DUR → onay
2. **TDD** → test yaz → KIRMIZI gör
3. **KOD** → implemente et
4. **TEST** → `dotnet test` — tüm testler geçmeli
5. **MANUEL TUR** → `dotnet run` → kullanıcı test eder
6. **MÜHÜR** → `plans/v3{versiyon}_muhur.md`
7. **ROADMAP** → `Roadmap.md` güncelle
8. **README** → Şu Anki Durum, changelog, faz durumu, badge, commit hash güncelle
9. **GIT** → commit + push

---

## ⌨️ KISAYOL TUŞLARI (Editor)

| Tuş | İşlev | Ribbon |
|-----|-------|--------|
| `S` | Seçim aracı | GİRİŞ |
| `T` | Ray çiz | ÇİZİM |
| `R` | Hat (Rota) çiz | ÇİZİM |
| `H` | Hibrit çizim | ÇİZİM |
| `F8` | Makas yerleştir | ÇİZİM |
| `Del` | Seçili entity'yi sil | GİRİŞ/DÜZEN |
| `Ctrl+C` | Kopyala | GİRİŞ |
| `Ctrl+X` | Kes | GİRİŞ |
| `Ctrl+V` | Yapıştır | GİRİŞ |
| `Ctrl+Z` | Geri Al (Undo) | DÜZEN |
| `Ctrl+Y` | Yinele (Redo) | DÜZEN |
| `Ctrl+S` | Kaydet | QuickAccess |
| `Ctrl+Shift+Z` | Zoom Extents | GÖRÜNÜM |
| `W` | Zoom Window | GÖRÜNÜM |
| `F9` | Snap toggle | GÖRÜNÜM |
| `Esc` | İPTAL | Global |
| `Enter` / `SağTık` | COMMIT | Global |

---

## 🏷️ ENTITY TİPLERİ ve PROPERTIES PANEL

| Entity | Panel'de Gösterilen |
|--------|---------------------|
| `TrackNode` | ID, Katman, Tür="TrackNode", X, Y, Z, Role (Plain/SwitchNode/RfidAnchor/StationEntry) |
| `TrackSegment` | ID, Katman, Tür, Start Node, End Node, Uzunluk (mm) |
| `Route` | ID, Katman, Tür, Adım Sayısı, Başlangıç koordinatı |
| `RailSwitch` | ID, Katman, Tür, X, Y, Rotation (°), State (Main/Diverging) |
| `Ramp` | ID, Katman, Tür, X, Y, Start Z, End Z |

---

## 🎨 EDITOR LAYOUT (ASCII)

```
┌──────────────────────────────────────────────────────────┐
│ RIBBON (GİRİŞ | ÇİZİM | DÜZEN | GÖRÜNÜM) + QuickAccess  │
├──────────────────────────────────────────────────────────┤
│ [Tab1★] [Tab2] [+]                     ← Sekme Şeridi    │
├──────────┬──────────────────────┬────────────────────────┤
│ FEATURE  │                      │  PROPERTIES PANEL      │
│ TREE     │     VIEWPORT          │  (220px)               │
│ (250px)  │     (CadCanvas)       │  ÖZELLİKLER           │
│          │  + Zoom Kontrol       │  ID: ...              │
│          │  + Koordinat/FPS      │  Katman: ...          │
│          │  + Crosshair          │  X: ... Y: ...        │
├──────────┴──────────────────────┴────────────────────────┤
│ TERMINAL PANEL (Log Otobüsü — 4 satır)                   │
└──────────────────────────────────────────────────────────┘
```

---

## 🔧 DI KAYITLARI ve BAŞLANGIÇ AKIŞI

**Önemli servisler (`App.xaml.cs`):**
- `CadDocument`, `CommandStack`, `SelectionService`, `ClipboardService` → **Singleton**
- `ISnapProvider[]` → EndpointSnap, OnSegmentSnap, GridSnap → `SnapEngine` (Singleton)
- `TrainDbContext` → SQLite (`trainservice.db`)
- `IEmbeddedBrokerService` → MQTT broker port 1883
- Tüm ViewModel'ler ve Page'ler → **Transient** (her istekte yeni instance)

**Başlangıç akışı:** `App.OnStartup()` → Host.Start → DB.Migrate() → TrainManager.Initialize → EmbeddedBroker.StartAsync(1883) → MainWindow.Show

---

## 🛡️ MÜHÜRLÜ DAVRANIŞLAR (Y5 — ASLA DEĞİŞMEZ)

| Kural | Açıklama |
|-------|----------|
| F9 | Snap toggle |
| Esc | İPTAL |
| Enter / SağTık | COMMIT |
| SabitKatmanlar GUID | `11111111-1111-1111-1111-111111111111` (Zemin), `2222...` (AltKat), `3333...` (ÜstKat) — **seed olarak korunur** |
| IsVisible / IsSelectable | Tek kaynak: `CadDocument` |
| CadColors | Merkezî renk tanımları (`src/TrainService.App/Resources/CadColors.cs`) |

### ⚠ Sapma Kaydı (`tools/sapma.txt`)
- **v3.0.29.32:** SabitKatmanlar GUID'leri seed olarak korunur, yeni katman ID'leri dinamik üretilir.

---

## 🧪 BEKÇİ TESTLERİ (T001–T011)

| Test | Açıklama | Proje |
|------|----------|-------|
| T001 | Core hiçbir projeye bağımlı değil | Architecture |
| T002 | Cad sadece Core'a bağımlı | Architecture |
| T003 | Core'da WPF tipi yok | Architecture |
| T004 | Cad'de WPF tipi yok (headless test edilebilirlik) | Architecture |
| T005 | Messaging Data'ya bağımlı değil | Architecture |
| T006 | Simulation iskelet boş (≤2 public tip) | Architecture |
| T007 | Tüm Entity'ler CadEntity'den türer | Architecture |
| T008 | Tüm Command'ler ICadCommand uygular | Architecture |
| T009 | DI kayıtları tam | Architecture |
| T010 | Kapsam bekçisi eşiği | Architecture |
| T011 | Trivial test guard (IL ≤12 byte) | Architecture |

---

## 📊 FAZ VE VERSİYON ÖZETİ

### FAZ D2 — RIBBON & ÇOKLU BELGE (v3.0.29.1 → v3.0.29.7) ✅ MÜHÜRLÜ
Testler: T330–T394.

### FAZ D2-DEVAM — EDİTÖR CİLASI (v3.0.29.8 → v3.0.29.16) ✅ MÜHÜRLÜ
Testler: T400–T417.

### FAZ D3 — EDİTÖR PROFESYONEL CİLA (v3.0.29.17 → v3.0.29.42)

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
Ertelenen D2 özellikleri. Testler: T588–T615.

### FAZ E → H (v3.0.30 → v3.0.48)
Detaylar `Roadmap.md`'de.

---

## 🚀 TEST NUMARALANDIRMA

| Aralık | Test | Açıklama |
|--------|------|----------|
| T330–T394 | D2 | Ribbon & Çoklu Belge |
| T400–T417 | D2-DEVAM | Editör Cilası |
| T460–T474 | D3 G1 | Görsel Temel |
| T475–T492 | D3 G2 | Seçim ve Snap |
| T493–T512 | D3 G3 | Modify Araçları |
| T513–T530 | D3 G4 | Draw Araçları |
| T531–T545 | D3 G5 | Ribbon ve UI |
| T546–T560 | D3 G6 | Annotation |
| T561–T575 | D3 G7 | Verimlilik |
| T576–T587 | D3 G8 | Son Dokunuşlar |
| T588–T615 | D3-SONRASI | Ertelenen özellikler |
| T400–T428 | E | Donanım Eşleme |
| T430–T452 | F | Firmware & OTA |
| T460–T499 | G | Operasyon |
| T500–T552 | H | Simülasyon |

---

## 🧪 TEST KOMUTLARI

```bash
dotnet test                                          # Tüm testler (7 proje)
dotnet test tests/TrainService.App.Tests/            # Sadece App
dotnet test tests/TrainService.App.Tests/ --filter "FullyQualifiedName~T460"  # Belirli grup
dotnet build src/TrainService.App/TrainService.App.csproj  # Sadece build
```

---

## ⚠️ ÖNEMLİ NOTLAR

1. **Roadmap TEK DOĞRULUK KAYNAĞIDIR.** Sıradaki sürüm = `(MÜHÜRLENDİ)` olmayan ilk satır.
2. **F8 ÇAKIŞMASI:** F8 = Switch (mühürlü). Ortho için **F10** kullanılacak (v3.0.29.23).
3. **Dinamik Katman (v3.0.29.32):** Y5 sapması — `tools/sapma.txt`'ye bak.
4. **Circle/Arc (v3.0.29.29):** Segmentlere ayrıştırılır, yeni CadEntity EKLENMEZ.
5. **Katman kuralı:** Core'a dokunma. Cad WPF'siz. WPF tipleri Core/Cad'e sızamaz.
6. **EditorView Layout:** 3 kolon — 250px + * + 220px (Properties Panel v3.0.29.18).
7. **gitignore:** `dberror.txt`, `*.db`, `Logs/` hariç.
8. **DB hatası:** `del trainservice.db` → yeniden başlat.
9. **Test KIRMIZI → KODU düzelt,** testi değil.
10. **Bekçi ihlali → `sapma.txt`'ye yaz.**

---

## 🖥️ UYGULAMA BAŞLATMA

```bash
1-click                    # Hızlı başlat (bat)
dotnet run --project src/TrainService.App/TrainService.App.csproj
```

### Sayfalar: Home · Editor · Electronics · Kitchen · Info · Settings

---

## 📝 SÜRÜM GEÇMİŞİ

### v3.0.29.18 — Sağ Properties Panel + Hover Highlight ✅
- **YENİ:** `Controls/PropertiesPanel/PropertiesPanelControl.cs` — Sağ panel; ID, Layer, X, Y, Z, Tür.
- **DEĞİŞEN:** `EditorView.xaml` — 3 kolon (250 + * + 220). `EditorView.xaml.cs` — ReattachActiveTab'de bağlantı.
- **TEST:** T465–T470 (6 test). 6/6 PASSED.

### v3.0.29.17 — İkon Paketi (MahApps.Metro.IconPacks) + Crosshair Cursor ✅
- NuGet: `MahApps.Metro.IconPacks` v5.0.0. 23 MaterialDesign ikonu. `CadViewportControl.cs`: `_crosshairVisual` + `RenderCrosshair()`.
- **TEST:** T460–T464 (6 test). **Mühür:** `plans/v302917_icons_cursor_muhur.md`.

### v3.0.29.16 → v3.0.29.1 (tümü MÜHÜRLÜ)
Detaylar `Roadmap.md` FAZ D2 ve D2-DEVAM bölümlerinde.

---

## 🔗 İLGİLİ DOKÜMANLAR

| Dosya | İçerik |
|-------|--------|
| `Roadmap.md` | Tam yol haritası — mimari anayasa, faz planları, kabul kriterleri, manuel test adımları |
| `onceki_talimat.txt` | Agent kuralları — iş akışı, test numaralandırma, mühür şablonu, README güncelleme |
| `docs/DbSchema.md` | Veritabanı şeması |
| `docs/TopicContract.md` | MQTT konu sözleşmesi |
| `tools/sapma.txt` | Y5 sapma kaydı |

---

*Son güncelleme: 2026-07-19 · v3.0.29.18 · Git: cffd963 · Aktif Faz: D3 G1*
