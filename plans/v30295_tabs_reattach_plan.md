# Plan: v3.0.29.5 — Sekme Değişiminde Viewport/FeatureTree Yeniden Bağlama

## Amaç
v3.0.29.3'teki sekme şeridi UI'si ile v3.0.29.2'deki arka uç modeli arasındaki çalışma zamanı entegrasyonunu tamamlamak.

## Değişen Dosyalar
- `src/TrainService.App/Controls/DocumentTabs/DocumentTabsControl.xaml` — MouseLeftButtonDown
- `src/TrainService.App/Controls/DocumentTabs/DocumentTabsControl.xaml.cs` — OnTabClicked
- `src/TrainService.App/Views/Pages/EditorView.xaml.cs` — Constructor + ReattachActiveTab()
- `src/TrainService.App/App.xaml.cs` — DocumentTabsViewModel oluşturma

## Dokunulmayacak (Mühürlü)
- RibbonControl, CadViewportControl (public API), FeatureTreeControl, EditorTabModel, DocumentTabsViewModel (public API)

## Test Bloğu: T370–T377