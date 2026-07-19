# TCP 3.0 (Train Control Platform)

![Platform](https://img.shields.io/badge/Platform-Windows-blue)
![Framework](https://img.shields.io/badge/Framework-.NET%208.0-512BD4)
![UI](https://img.shields.io/badge/UI-WPF%20%2B%20Fluent-success)
![Version](https://img.shields.io/badge/Version-v3.0.29.18-orange)

TCP (Train Control Platform) 3.0, trenlerin otonom hareketlerini simüle eden, uzaktan komuta sağlayan ve MQTT üzerinden IoT tabanlı haberleşme ile gerçek zamanlı veri akışını yöneten yeni nesil bir masaüstü kontrol merkezidir.

---

# 🤖 YAPAY ZEKA DEVİR TESLİM (AI HANDOVER)

> **Bu bölümü okuyan AI Model için:** Bu proje bir önceki AI tarafından belirli kurallar ve sürüm disiplini ile geliştirilmiştir. Aşağıdaki bilgiler, projeyi devraldığında hızlıca oryante olman için hazırlanmıştır.

---

## 📍 ŞU ANKİ DURUM (Current State)

| Alan | Değer |
|------|-------|
| **Son Sürüm** | v3.0.29.18 |
| **Son Git Commit** | `c6af693` (pushed to GitHub) |
| **Son Yapılan** | Sağ Properties Panel + Hover Highlight (SelectionService senkron) |
| **Sıradaki Sürüm** | v3.0.29.19 — Alt Komut Satırı + Prompt Area + Coordinate Input Fields |
| **Aktif Faz** | FAZ D3 GRUP 1 — Görsel Temel |
| **Build Durumu** | ✅ 0 Error, 0 Warning |
| **Test Durumu** | Tüm App.Tests geçiyor |

---

## 📁 KRİTİK DOSYA KONUMLARI

### Yol Haritası ve Planlar
| Dosya | Açıklama |
|-------|----------|
| `Roadmap.md` | **ANA YOL HARİTASI** — Tüm sürümler, fazlar, gruplar. Burası TEK DOĞRULUK KAYNAĞIDIR. |
| `plans/` | Her sürüm için plan ve mühür raporları (`v3{versiyon}_aciklama_plan.md` + `_muhur.md`) |
| `onceki_talimat.txt` | **AGENT KURALLARI** — Her versiyonda değişmesi gerekenler, test numaralandırma, mühür şablonu |

### Agent Kuralları (`onceki_talimat.txt`)
- Her sürüm için 7 adımlı akış: PLAN → TDD → KOD → TEST → MANUEL TUR → MÜHÜR → ROADMAP GÜNCELLE → GIT
- Test numaralandırma tablosu (D3 için T460–T587, D3-SONRASI için T588–T615)
- Mühür raporu şablonu
- Bekçi kontrol listesi (T001–T011)
- Hatalı durum kuralları

### Mühürlü Davranışlar (Y5 — ASLA DEĞİŞMEZ)
- F9 = Snap toggle
- Esc = İPTAL (aktif tool'u iptal eder)
- Enter/SağTık = COMMIT (tool zincirini bitirir)
- SabitKatmanlar GUID'leri: `11111111-1111-1111-1111-111111111111` (Zemin), `22222222-...` (AltKat), `33333333-...` (ÜstKat)
- IsVisible/IsSelectable tek kaynak: `CadDocument` üzerinden
- CadColors merkezî renk tanımları

### Sapma Kaydı
| Dosya | Açıklama |
|-------|----------|
| `tools/sapma.txt` | Y5 sapma kaydı — v3.0.29.32'de dinamik katman sistemine geçiş için |

---

## 🏗️ MİMARİ YAPI

Proje toplamda **7 katmanlı** Clean Architecture:
- `TrainService.Core`: Arayüzler, modeller, enumlar — **hiçbir projeye bağımlı değil**
- `TrainService.Cad`: CAD çekirdeği (Snap, Tools, Selection, UndoRedo, Topology) — **sadece Core'a bağımlı, WPF'siz**
- `TrainService.App`: WPF UI (MVVM), Controllers, ViewModels — **tüm servislere DI üzerinden erişir**
- `TrainService.Messaging`: MQTTnet broker + Hub + Device Registry
- `TrainService.Data`: EF Core 8 + SQLite
- `TrainService.Firmware`: C++ kod üretimi + PlatformIO
- `TrainService.Simulation`: Fizik motoru (şu an boş iskelet)

**Bağımlılık yönü:** `App → (Cad, Messaging, Data, Firmware, Simulation) → Core`

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
| G2 | v3.0.29.20–23 | Seçim ve Snap (Ortho F10!) | T475–T492 | ⏳ |
| G3 | v3.0.29.24–27 | Modify Araçları | T493–T512 | ⏳ |
| G4 | v3.0.29.28–30 | Draw Araçları | T513–T530 | ⏳ |
| G5 | v3.0.29.31–33 | Ribbon ve UI (dinamik katman!) | T531–T545 | ⏳ |
| G6 | v3.0.29.34–36 | Annotation | T546–T560 | ⏳ |
| G7 | v3.0.29.37–39 | Verimlilik | T561–T575 | ⏳ |
| G8 | v3.0.29.40–42 | Son Dokunuşlar | T576–T587 | ⏳ |

### FAZ D3-SONRASI (v3.0.29.43 → v3.0.29.48) ⏳
D2'den ertelenen özellikler: Feature Tree v2, Radyal Menü v2, Görünüm Kolaylıkları, Durum Çubuğu, Seçim Filtreleri, Kısayol Haritası + F1. Testler: T588–T615.

### FAZ E–H (v3.0.30 → v3.0.48)
Donanım Eşleme, Firmware & OTA, Operasyon, Simülasyon. Detaylar `Roadmap.md`'de.

---

## 🔧 GELİŞTİRME İŞ AKIŞI (Her Sürüm İçin)

1. **ADIM 0 — PLAN:** `plans/v3{versiyon}_aciklama_plan.md` oluştur → DUR → kullanıcı onayı
2. **ADIM 1 — TDD:** Testi önce yaz, KIRMIZI gör
3. **ADIM 2 — KOD:** Plan dosyasındaki değişen dosyaları implemente et, her dosyada `dotnet build`
4. **ADIM 3 — TEST:** `dotnet test` — TÜM testler geçmeli, regresyon OLMAMALI
5. **ADIM 4 — MANUEL TUR:** `dotnet run --project src/TrainService.App` → kullanıcı test eder
6. **ADIM 5 — MÜHÜR:** `plans/v3{versiyon}_muhur.md` oluştur
7. **ADIM 6 — ROADMAP:** `Roadmap.md`'de sürümü `(MÜHÜRLENDİ)` işaretle
8. **ADIM 7 — GIT:** `git commit -m "v{sürüm}: {açıklama}"` → kullanıcı "pushla" derse `git push`

---

## 🧪 TEST KOMUTLARI

```bash
# Tüm testler
dotnet test

# Sadece App testleri
dotnet test tests/TrainService.App.Tests/

# Belirli test grubu
dotnet test tests/TrainService.App.Tests/ --filter "FullyQualifiedName~T460"

# Build + test (test dosyası değiştiyse)
dotnet build tests/TrainService.App.Tests/TrainService.App.Tests.csproj && dotnet test tests/TrainService.App.Tests/ --no-build --filter "FullyQualifiedName~T465"
```

---

## ⚠️ ÖNEMLİ NOTLAR

1. **Roadmap TEK DOĞRULUK KAYNAĞIDIR.** Sıradaki sürüm = `(MÜHÜRLENDİ)` işareti OLMAYAN ilk satır.
2. **F8 tuşu ÇAKIŞMASI:** v3.0.29.1'de F8 = Switch aracı. v3.0.29.23'te Ortho Mode için **F10** kullanılacak.
3. **Dinamik Katman (v3.0.29.32):** Y5 sapması! SabitKatmanlar GUID'leri seed olarak korunur, yeni katman ID'leri dinamik üretilir.
4. **Circle/Arc (v3.0.29.29):** Segmentlere ayrıştırılarak depolanır, yeni CadEntity türü EKLENMEZ.
5. **Katman (A1 arteri):** Core'a dokunulmaz. Cad sadece Core'a bağımlıdır. WPF tipleri Cad/Core'a sızamaz.

---

## 📝 SÜRÜM GEÇMİŞİ (Changelog)

### v3.0.29.18 — Sağ Properties Panel + Hover Highlight ✅
- **YENİ:** `Controls/PropertiesPanel/PropertiesPanelControl.cs` — Sağ kenar paneli; ID, Layer, X, Y, Z, Tür alanları (TrackNode/TrackSegment/Route/Switch/Ramp destekli). `AttachSelection(SelectionService, CadDocument)` + `SelectionChanged` event'i ile otomatik güncelleme. `GetPropertyValue()` test helper'ı.
- **DEĞİŞEN:** `Views/Pages/EditorView.xaml` — 3 kolon layout (250 + * + 220), xmlns:props namespace, `<props:PropertiesPanelControl>` elementi.
- **DEĞİŞEN:** `Views/Pages/EditorView.xaml.cs` — `ReattachActiveTab()` içinde `PropertiesPanel.AttachSelection(tab.SelectionService, tab.Document)` bağlantısı.
- **TEST:** 6 reflection testi (T465–T470) — PropertiesPanelControl sınıfı/property'leri, AttachSelection metodu, GetPropertyValue, EditorView entegrasyonu, hover _hoveredId alanı, namespace kontrolü.
- 6/6 PASSED. Build: 0 Error.

### v3.0.29.17 — İkon Paketi (MahApps.Metro.IconPacks MaterialDesign) + Crosshair Cursor ✅
- **DEĞİŞEN:** `TrainService.App.csproj` — `MahApps.Metro.IconPacks` v5.0.0 NuGet paketi.
- **DEĞİŞEN:** `Controls/Ribbon/RibbonDefinition.cs` — `RibbonItem`'a `IconPack` property'si (varsayılan "MaterialDesign"). Tüm IconKind değerleri MaterialDesign enum isimleriyle güncellendi (23 item).
- **DEĞİŞEN:** `Controls/Ribbon/RibbonControl.xaml.cs` — `SymbolIcon`/`CreateIcon()` kaldırıldı, `CreateIconPacks(string kind, string pack)` eklendi. Try-catch fallback ile geçersiz ikon → null.
- **DEĞİŞEN:** `Controls/CadCanvas/CadViewportControl.cs` — `_crosshairVisual` (DrawingVisual), `RenderCrosshair(Point)` (20px kesikli çizgili artı işareti + 2px merkez nokta), `OnMouseMove`'da `RenderCrosshair(currentPos)`, `OnMouseLeave`'de crosshair temizleme.
- **TEST:** 6 test (T460–T464).
- **Mühür:** `plans/v302917_icons_cursor_muhur.md`.

### v3.0.29.16 — Snap Göstergeleri (İmleç Rengi Değişimi) ✅
- Plan: `plans/v302916_snapcolor_plan.md` · Mühür: `plans/v302916_snapcolor_muhur.md`

### v3.0.29.15 — Zoom Kontrol (Slider + Fit Butonu) ✅
- Plan: `plans/v302915_zoom_plan.md` · Mühür: `plans/v302915_zoom_muhur.md`

### v3.0.29.14 — Kısayol Düzeltme + Sağ Tık Undo ✅
- Plan: `plans/v302914_shortcuts_plan.md` · Mühür: `plans/v302914_shortcuts_muhur.md`

### v3.0.29.13 — Status Bar Düzeltme (Koordinat Paneli + Kaydet Görsel) ✅
- Plan: `plans/v302913_statusbar_plan.md` · Mühür: `plans/v302913_statusbar_muhur.md`

### v3.0.29.12 — Feature Tree Toggle (Göz/Gizle + Kilit) ✅
- Mühür: `plans/v302912_featuretree_toggle_muhur.md` (doğrudan mühür)

### v3.0.29.11 — Katman Seçici (Ribbon Layer Dropdown) ✅
- Plan: `plans/v302911_layerselector_plan.md` · Mühür: `plans/v302911_layerselector_muhur.md`

### v3.0.29.10 — TerminalPanel Entegrasyonu ✅
- Plan: `plans/v302910_terminal_plan.md` · Mühür: `plans/v302910_terminal_muhur.md`

### v3.0.29.9 — Katman Yönetimi (Layers) ✅
- Plan: `plans/v30299_layers_plan.md` · Mühür: `plans/v30299_layers_muhur.md`
- v3.0.29.9-fix: README + MVVMTK0034 Düzeltmesi — `plans/v30299_fix_muhur.md`
- **8 test (T410–T417)**: Aktif katman varsayılanı, SetActiveLayer, visibility/lock entity etkisi, katman sayısı, isimler, geçersiz ID, Z yükseklikleri.

### v3.0.29.8 — Ribbon Proxy + Memory Leak Düzeltmesi ✅
- Plan: `plans/v30298_ribbon_proxy_plan.md` · Mühür: `plans/v30298_ribbon_proxy_muhur.md`
- **8 test (T400–T407)**: ActiveTab Document güncelleme, null fallback, Undo/Redo routing.

### v3.0.29.7 — Gerçek UI Entegrasyonu ✅
- `DocumentTabsControl.xaml/cs` + `EditorView.xaml.cs` ReattachActiveTab. Plan/Mühür: `plans/v30297_ui_binding_*.md`

### v3.0.29.6 — Gerçek Çalışma Zamanı Entegrasyonu Testleri ✅
- 8 test (T380–T387). Plan/Mühür: `plans/v30296_runtime_binding_*.md`

### v3.0.29.5 — Sekme Değişiminde Yeniden Bağlama Testleri ✅
- 8 test (T370–T377). Plan/Mühür: `plans/v30295_tabs_reattach_*.md`

### v3.0.29.4 — Çalışma Zamanı Entegrasyonu ✅
- 8 test (T360–T367). Plan/Mühür: `plans/v30294_tabs_runtime_*.md`

### v3.0.29.3 — DocumentTabs UI (Sekmeli Çoklu Belge) ✅
- **WPF sekme şeridi**: + butonu, kirli gösterge (★), X kapatma, aktif sekme vurgusu.
- **8 test (T350–T357)**. Plan/Mühür: `plans/v30293_tabs_ui_*.md`

### v3.0.29.2 — DocumentTabs Arka Uç (Sekmeli Çoklu Belge) ✅
- `EditorTabModel.cs`, `DocumentTabsViewModel.cs` — izole CadDocument/CommandStack/SelectionService.
- **8 test (T340–T347)**. Plan/Mühür: `plans/v30292_tabs_*.md`

### v3.0.29.1-fix — Kritik Bug Düzeltmeleri ✅
- PasteEntitiesCommand, ClipboardService.Klonla, CadDocument.RestoreEntity, EditorViewModel.ProjectId.
- **9 test (T336–T344)**. Plan/Mühür: `plans/v30291_fix_*.md`

### v3.0.29.1 — Ribbon (Şerit Arayüzü) ✅
- 4 sekme (GİRİŞ, ÇİZİM, DÜZEN, GÖRÜNÜM) + QuickAccess, 22 RibbonItem, 15 kısayol.
- **6 test (T330–T335)**. Plan/Mühür: `plans/v30291_ribbon_*.md`

### v3.0.29 → v3.0.0 (Önceki Sürümler)
Detaylar için `Roadmap.md` Bölüm 0'a bakınız. Tümü mühürlüdür.

---

## ⚙️ KURULUM VE ÇALIŞTIRMA

### Gereksinimler
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Windows 10/11 (WPF arayüzü nedeniyle)

### Çalıştırma
```bash
# Bağımlılıkları yükle ve projeyi derle
dotnet build

# Uygulamayı başlat
dotnet run --project src/TrainService.App/TrainService.App.csproj
```

### Hata Durumunda
- `dberror.txt` oluşursa → `del trainservice.db` ile DB'yi sil, yeniden başlat
- Migration hatası → `dotnet ef database update --project src/TrainService.Data`

---

*Son güncelleme: 2026-07-19 · v3.0.29.18 · Git: c6af693*