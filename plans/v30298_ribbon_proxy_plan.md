# Plan: v3.0.29.8 — Ribbon Proxy + Memory Leak Düzeltmesi

## Amaç
Ribbon Undo/Redo/Save/New/Open komutları aktif sekme üzerinden çalışır. Memory leak düzeltilir.

## Değişen Dosyalar
- `src/TrainService.App/ViewModels/EditorViewModel.cs` — ActiveTab property + komut yönlendirmesi
- `src/TrainService.App/Controls/Ribbon/RibbonControl.xaml.cs` — Proxy pattern
- `src/TrainService.App/Views/Pages/EditorView.xaml.cs` — Memory leak düzeltmesi

## Dokunulmayacak (Mühürlü)
- RibbonControl.xaml, CadViewportControl, FeatureTreeControl, EditorTabModel, DocumentTabsViewModel

## Test Bloğu: T400–T407