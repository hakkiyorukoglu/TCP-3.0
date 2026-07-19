# Plan: v3.0.29.9 — Katman Yönetimi (Layers)

## Amaç
Araçlar entity'yi aktif katmana yazar. EditorViewModel katman komutları ve UI entegrasyonu.

## Değişen Dosyalar
- `src/TrainService.App/ViewModels/EditorViewModel.cs` — ActiveLayerId + komutlar
- `src/TrainService.App/Views/Pages/EditorView.xaml` — Katman seçici toolbar
- `src/TrainService.App/Views/Pages/EditorView.xaml.cs` — LayerSelector binding
- `src/TrainService.Cad/Tools/TrackTool.cs` — LayerId = ActiveLayerId
- `src/TrainService.Cad/Tools/RouteTool.cs` — LayerId = ActiveLayerId
- `src/TrainService.Cad/Tools/HybridTool.cs` — LayerId = ActiveLayerId
- `src/TrainService.Cad/Tools/SwitchTool.cs` — LayerId = ActiveLayerId
- `src/TrainService.Cad/Tools/RampTool.cs` — LayerId = ActiveLayerId

## Dokunulmayacak (Mühürlü)
- CadDocument.cs (public API), CadLayer.cs, RibbonControl.xaml/cs

## Test Bloğu: T410–T417