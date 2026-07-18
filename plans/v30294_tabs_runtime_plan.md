# Plan: v3.0.29.4 — Çalışma Zamanı Entegrasyonu (Runtime Integration)

## Amaç
v3.0.29.3'te hazırlanan sekme şeridi UI'sini çalışma zamanına entegre etmek. Aktif sekme değişiminde Viewport, FeatureTree, ToolController yeniden bağlanır.

## Değişen Dosyalar
- `src/TrainService.App/Views/Pages/EditorView.xaml.cs` — Constructor + ReattachActiveTab()
- `src/TrainService.App/ViewModels/EditorViewModel.cs` — ActiveTab proxy
- `src/TrainService.App/ViewModels/DocumentTabsViewModel.cs` — CanUndo/CanRedo proxy
- `src/TrainService.App/App.xaml.cs` — EditorView oluşturma (DocumentTabsViewModel geçir)

## Dokunulmayacak (Mühürlü)
- `RibbonControl.xaml/cs`, `CadViewportControl.cs`, `FeatureTreeControl`, `EditorTabModel.cs`

## Test Bloğu: T360–T367