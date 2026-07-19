# Mühür Raporu — v3.0.29.8 (Ribbon Proxy + Memory Leak Düzeltmesi)

## Teslimat Bilgileri
| Alan | Değer |
|------|-------|
| **Sürüm** | v3.0.29.8 |
| **Önceki** | v3.0.29.7 (Mühürlü) |

## Kapsam
Ribbon Undo/Redo/Save/New/Open komutları aktif sekme üzerinden çalışır. Memory leak düzeltilir.

## Yapılan Değişiklikler

### EditorViewModel.cs
- `[ObservableProperty] EditorTabModel? ActiveTab` eklendi
- `OnActiveTabChanged` partial metod: `Document` property'si güncellenir
- `Undo/Redo/Delete/Copy/Cut/Paste` komutları `ActiveTab?.Service ?? fallback` pattern ile yönlendirildi
- Null-safe: `ActiveTab` null ise constructor'dan gelen servisler kullanılır

### EditorView.xaml.cs
- `ReattachActiveTab()` memory leak düzeltmesi:
  - `LayerStatusChanged` event handler'ı önce unsubscribe edilir
  - `OnLayerStatusChanged` ayrı metoda çıkarıldı
- `ViewModel.ActiveTab = tab` aktif edildi

### App.xaml.cs
- `DocumentTabsViewModel` DI container'a eklendi (v3.0.29.7'de)

## Test Sonuçları
| Blok | Test | Durum |
|------|------|-------|
| T400 | ActiveTab_SetsDocument | ✅ |
| T401 | ActiveTab_Null_Set | ✅ |
| T402 | CanUndo_NullActiveTab_Fallback | ✅ |
| T403 | CanRedo_NullActiveTab_Fallback | ✅ |
| T404 | ActiveTab_HasServices | ✅ |
| T405 | ActiveTab_ChangesDocument | ✅ |
| T406 | ActiveTab_ProjectId_Matches | ✅ |
| T407 | ActiveTab_IsDirty_InitiallyFalse | ✅ |

**Yeni testler: 8/8 PASSED**
**Tüm çözüm: 312/312 PASSED, 0 FAILED** — regresyon yok

## Bekçi Kontrolü
| Kural | Durum |
|-------|-------|
| T001–T011 (mimari katman) | Dokunulmadı ✅ |
| `TrainService.Core` | Değişiklik yok ✅ |
| `TrainService.Cad` | Değişiklik yok ✅ |
| Mühürlü davranışlar (Y5) | Korundu ✅ |

## Mühür
- ✅ Plan onaylandı
- ✅ Implementasyon tamamlandı
- ✅ 8/8 yeni test geçti
- ✅ 312/312 tüm test geçti
- ✅ Bekçi kuralları ihlal edilmedi

**Mühürleyen:** Cline (Code mode)