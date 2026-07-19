# Plan: v3.0.29.7 — Gerçek UI Entegrasyonu (Runtime Binding Implementation)

## Amaç
Sekme değişiminde Viewport, FeatureTree, ToolController aktif sekmenin dokümanına gerçekten yeniden bağlanır.

## Değişen Dosyalar
- `src/TrainService.App/Controls/DocumentTabs/DocumentTabsControl.xaml` — MouseLeftButtonDown
- `src/TrainService.App/Controls/DocumentTabs/DocumentTabsControl.xaml.cs` — OnTabClicked
- `src/TrainService.App/Views/Pages/EditorView.xaml.cs` — Constructor + ReattachActiveTab() + Loaded
- `src/TrainService.App/App.xaml.cs` — DocumentTabsViewModel oluşturma

## Dokunulmayacak (Mühürlü)
- RibbonControl, CadViewportControl (public API), FeatureTreeControl, EditorTabModel, DocumentTabsViewModel (public API)

## Test Bloğu: T390–T397