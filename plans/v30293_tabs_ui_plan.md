# Plan: v3.0.29.3 — Sekmeli Çoklu Belge UI Entegrasyonu

## Amaç
v3.0.29.2'de hazırlanan `DocumentTabsViewModel` + `EditorTabModel`'i `EditorView`'e entegre etmek.

## Yeni Dosyalar
- `src/TrainService.App/Controls/DocumentTabs/DocumentTabsControl.xaml/cs` — Sekme şeridi UI
- `tests/TrainService.App.Tests/T350_T357_TabsUiTests.cs` — 8 UI entegrasyon testi

## Değişen Dosyalar
- `src/TrainService.App/Views/Pages/EditorView.xaml` — Sekme şeridi + ContentControl
- `src/TrainService.App/Views/Pages/EditorView.xaml.cs` — ActiveTab değişim handler'ı
- `src/TrainService.App/ViewModels/DocumentTabsViewModel.cs` — Proxy komutlar

## Dokunulmayacak (Mühürlü)
- `EditorViewModel.cs`, `RibbonControl.xaml/cs`, `CadViewportControl.cs`, `FeatureTreeControl`

## Test Bloğu: T350–T357