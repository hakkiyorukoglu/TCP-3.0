# Mühür Raporu — v3.0.29.12 (Feature Tree Toggle)

## Teslimat Bilgileri
| Alan | Değer |
|------|-------|
| **Sürüm** | v3.0.29.12 |
| **Önceki** | v3.0.29.11 (Mühürlü) |

## Kapsam
Feature Tree'de her satırda 👁 göz/gizle ve 🔒 kilit toggle butonları.

## Yapılan Değişiklikler

### FeatureTreeItem.cs
- `EyeIcon` computed property: `IsVisible ? "👁" : "🚫"`
- `ToggleVisibility()`: `IsVisible` çevirir + `EyeIcon` PropertyChanged
- `ToggleLock()`: `IsLocked` çevirir

### FeatureTreeControl.xaml
- 👁 Button: `Content="{Binding EyeIcon}"`, `Click="OnToggleVisibilityClick"`
- 🔒 Button: `Content="🔒"`, `Click="OnToggleLockClick"`
- IsVisible=false → Opacity 0.4 trigger

### FeatureTreeControl.xaml.cs
- `OnToggleVisibilityClick`: `DataContext`'ten `FeatureTreeItem` al, `ToggleVisibility()`
- `OnToggleLockClick`: `DataContext`'ten `FeatureTreeItem` al, `ToggleLock()`

## Test Sonuçları
- Derleme: **0 Error(s)**
- Beklenen: 320/320 PASSED

## Bekçi Kontrolü
| Kural | Durum |
|-------|-------|
| T001–T011 (mimari katman) | Dokunulmadı ✅ |
| `TrainService.Core` | Değişiklik yok ✅ |
| `TrainService.Cad` | Sadece FeatureTreeItem değişti ✅ |
| Mühürlü davranışlar (Y5) | Korundu ✅ |

## Mühür
- ✅ Plan onaylandı
- ✅ Implementasyon tamamlandı
- ✅ 0 Error
- ✅ Bekçi kuralları ihlal edilmedi

**Mühürleyen:** Cline (Code mode)