# Mühür Raporu — v3.0.29.2 (Sekmeli Çoklu Belge)

## Teslimat Bilgileri

| Alan | Değer |
|------|-------|
| **Sürüm** | v3.0.29.2 |
| **FAZ** | D2 — Sekmeli Çoklu Belge (Document Tabs) |
| **Tarih** | 2026-07-19 |
| **Önceki** | v3.0.29.1-fix (Mühürlü) |

---

## 1. Kapsam

Her sekme kendi **izole** `CadDocument` + `CommandStack` + `SelectionService` + `SnapEngine` + `ClipboardService` setini barındırır. Sekmeler arası veri kaçağı yok.

| # | Özellik | Açıklama |
|---|---------|----------|
| 1 | `EditorTabModel` | Sekme başına izole veri seti (Model katmanı) |
| 2 | `DocumentTabsViewModel` | Sekme listesi yönetimi: Ekle, Kaldır, Aktifleştir, Yeniden Adlandır |
| 3 | Izolasyon | Sekme A'daki entity, sekme B'ye sıçramaz |
| 4 | IsDirty | Her sekme kendi kirli bayrağını taşır |
| 5 | Son sekme kapatma | Otomatik yeni boş sekme oluşturulur |
| 6 | Kirli sekme kapatma | `TryCloseTab` `false` döner (vazgeç) — UI MessageBox sonraki sürümde |

---

## 2. Yeni Dosyalar

| Dosya | Açıklama |
|-------|----------|
| `src/TrainService.App/Models/EditorTabModel.cs` | Sekme başına izole veri seti |
| `src/TrainService.App/ViewModels/DocumentTabsViewModel.cs` | Sekme yöneticisi |
| `tests/TrainService.App.Tests/T340_T347_TabsTests.cs` | 8 test (T340–T347) |

---

## 3. Değişen Dosyalar

Yok — mevcut `EditorView` ve `EditorViewModel` dokunulmadı. `DocumentTabsViewModel` yeni bir katman olarak eklendi; mevcut yapı üzerine inşa edildi.

---

## 4. Test Sonuçları

| Blok | Test | Açıklama | Durum |
|------|------|----------|-------|
| T340 | AddTab_CreatesIsolatedSet | Yeni sekme izole set üretir | ✅ |
| T341 | TabIsolation_DocA_DoesNotAffectDocB | Sekme A'daki çizim B'ye sıçramaz | ✅ |
| T342 | TabIsolation_UndoStackA_DoesNotAffectStackB | Undo yığınları izole | ✅ |
| T343 | IsDirty_ReflectedInTabHeader | Kirli bayrak çalışır | ✅ |
| T344 | RenameTab_UpdatesDisplayName | Sekme adı değişir | ✅ |
| T345 | CloseTab_LastTab_CreatesNewEmpty | Son sekme kapanınca yeni boş | ✅ |
| T346 | CloseTab_Dirty_ReturnsFalseWhenCancelled | Kirli sekme vazgeç | ✅ |
| T347 | ActiveTab_SwitchesCorrectly | Aktif sekme değişimi | ✅ |

**Yeni testler: 8/8 PASSED**

**Tüm çözüm:** 272/272 PASSED, 0 FAILED — regresyon yok.

---

## 5. Bekçi Kontrolü

| Kural | Durum |
|-------|-------|
| T001–T011 (mimari katman) | Dokunulmadı ✅ |
| `TrainService.Core` | Değişiklik yok ✅ |
| `TrainService.Cad` | Değişiklik yok ✅ |
| NuGet paketleri | Yeni bağımlılık eklenmedi ✅ |
| Mühürlü davranışlar (Y5) | Korundu — F9, Esc, Enter, katmanlar, renkler ✅ |
| Mevcut `EditorViewModel` | Dokunulmadı ✅ |

---

## 6. Mühür

Bu sürüm, çoklu belge desteğinin **arka uç katmanını** (VM + Model) hazırlar. UI entegrasyonu (sekme şeridi XAML) bir sonraki sürümde.

- ✅ Plan onaylandı
- ✅ TDD (RED → GREEN) tamamlandı
- ✅ 8/8 yeni test geçti
- ✅ 272/272 tüm test geçti (regresyon yok)
- ✅ Bekçi kuralları ihlal edilmedi

**Mühürleyen:** Cline (Code mode)
**Sıradaki:** v3.0.29.3 — Sekme şeridi UI (XAML) + EditorView entegrasyonu