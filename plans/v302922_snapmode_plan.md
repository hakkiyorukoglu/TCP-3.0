# v3.0.29.22 — Snap Mode Butonları (Endpoint, Midpoint, OnSegment, Grid)

**Plan tarihi:** 2026-07-19  
**Kaynak:** `Roadmap.md > FAZ D3 G2 > v3.0.29.22`  
**Test kimlik bloğu:** T485–T490

---

## 1. Kapsam
1. `SnapKind.Midpoint = 40` enum değeri
2. `SnapEngine.DisabledKinds` HashSet — provider-toggle
3. `MidpointSnapProvider` (Priority=15) — segment orta noktasına snap
4. DI kaydı: `MidpointSnapProvider` singleton
5. `EditorViewModel` 4 snap toggle property + 4 command + 4 renk property
6. Ribbon GÖRÜNÜM display grubuna 4 snap butonu
7. EditorView.xaml snap LED göstergeleri

## 2. Değişen Dosyalar
- `src/TrainService.Cad/Snapping/SnapKind.cs` (DEĞİŞECEK)
- `src/TrainService.Cad/Snapping/SnapEngine.cs` (DEĞİŞECEK)
- `src/TrainService.Cad/Snapping/MidpointSnapProvider.cs` (YENİ)
- `src/TrainService.App/App.xaml.cs` (DEĞİŞECEK)
- `src/TrainService.App/ViewModels/EditorViewModel.cs` (DEĞİŞECEK)
- `src/TrainService.App/Controls/Ribbon/RibbonDefinition.cs` (DEĞİŞECEK)
- `src/TrainService.App/Views/Pages/EditorView.xaml` (DEĞİŞECEK)
- `tests/TrainService.App.Tests/T485_T490_SnapModeTests.cs` (YENİ)

## 3. Test Planı (T485–T490)
| Test | İçerik |
|------|--------|
| T485 | SnapKind.Midpoint enum değeri (=40) |
| T486 | SnapEngine.DisabledKinds — Endpoint disable |
| T487 | MidpointSnapProvider segment orta noktası snap |
| T488 | Öncelik: Endpoint > Midpoint > OnSegment > Grid |
| T489 | EditorViewModel snap toggle property'leri |
| T490 | RibbonDefinition snap butonları + benzersiz ID |

## 4. Mühürlü Davranış Kontrolü
- F9=Snap toggle: KORUNUR ✅
- Core: DEĞİŞMEZ ✅
- Cad WPF'siz: KORUNUR ✅
- Hot-path: O(1) HashSet lookup, tahsis yok ✅