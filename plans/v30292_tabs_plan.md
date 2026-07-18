# Plan: v3.0.29.2 — Sekmeli Çoklu Belge (Document Tabs)

## Amaç
Alphacam/tarayıcı tarzı üstte sekme şeridi; her sekme izole CadDocument + CommandStack + SelectionService.

## Yeni Dosyalar
- `src/TrainService.App/Models/EditorTabModel.cs` — Sekme başına izole veri seti
- `src/TrainService.App/ViewModels/DocumentTabsViewModel.cs` — Sekme yöneticisi
- `src/TrainService.App/Controls/DocumentTabs/DocumentTabsControl.xaml/cs` — Sekme UI
- `tests/TrainService.App.Tests/T340_T347_TabsTests.cs` — 8 test

## Değişen Dosyalar
- `src/TrainService.App/Views/Pages/EditorView.xaml` — Sekme şeridi + ContentControl
- `src/TrainService.App/Views/Pages/EditorView.xaml.cs` — DocumentTabsViewModel kullanımı

## YOK
- Ayrı pencere koparma, split-view, oturum geri yükleme

## Test Bloğu: T340–T347