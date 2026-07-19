# Mühür Raporu — v3.0.29.9 (Katman Yönetimi)

## Teslimat Bilgileri
| Alan | Değer |
|------|-------|
| **Sürüm** | v3.0.29.9 |
| **Önceki** | v3.0.29.8 (Mühürlü) |

## Kapsam
Araçlar entity'yi aktif katmana yazar. EditorViewModel katman komutları ve UI entegrasyonu.

## Keşif: Araçlar Zaten Katman Destekliyor
- `TrackTool.cs`: `LayerId = ctx.Document.ActiveLayerId` ✅
- `RouteTool.cs`: `LayerId = ctx.Document.ActiveLayerId` ✅
- `HybridTool.cs`: `LayerId = ctx.Document.ActiveLayerId` ✅
- `SwitchTool.cs`: `LayerId = activeLayer` ✅
- `RampTool.cs`: `LayerId = activeLayer` ✅

## Yapılan Değişiklikler

### EditorViewModel.cs
- `[ObservableProperty] Guid ActiveLayerId` eklendi
- `OnActiveTabChanged`: `ActiveLayerId = value.Document.ActiveLayerId`
- `OnActiveLayerIdChanged`: `ActiveTab?.Document.SetActiveLayer(value)`
- `ActiveLayerName` property: Aktif katman adını döner

## Test Sonuçları
| Blok | Test | Durum |
|------|------|-------|
| T410 | ActiveLayer_DefaultZemin | ✅ |
| T411 | SetActiveLayer_ChangesActiveLayerId | ✅ |
| T412 | LayerVisibility_HidesEntities | ✅ |
| T413 | LayerLock_PreventsSelection | ✅ |
| T414 | Layers_Count_3 | ✅ |
| T415 | LayerNames_Correct | ✅ |
| T416 | SetActiveLayer_InvalidId_Ignored | ✅ |
| T417 | LayerZHeight_Correct | ✅ |

**Yeni testler: 8/8 PASSED**
**Tüm çözüm: 320/320 PASSED, 0 FAILED** — regresyon yok

## Bekçi Kontrolü
| Kural | Durum |
|-------|-------|
| T001–T011 (mimari katman) | Dokunulmadı ✅ |
| `TrainService.Core` | Değişiklik yok ✅ |
| `TrainService.Cad` | Değişiklik yok (araçlar zaten destekliyordu) ✅ |
| Mühürlü davranışlar (Y5) | Korundu ✅ |

## Mühür
- ✅ Plan onaylandı
- ✅ Implementasyon tamamlandı
- ✅ 8/8 yeni test geçti
- ✅ Tüm test paketi yeşil
- ✅ Bekçi kuralları ihlal edilmedi

**Mühürleyen:** Cline (Code mode)