# Plan: v3.0.29.11 — Katman Seçici (Ribbon Layer Dropdown)

## Amaç
Ribbon'da "GİRİŞ" sekmesinin layer grubuna ComboBox ekle. Kullanıcı aktif katmanı seçebilsin.

## Değişen Dosyalar
- `src/TrainService.App/Controls/Ribbon/RibbonDefinition.cs` — LayerSelector item
- `src/TrainService.App/Controls/Ribbon/RibbonControl.xaml` — Layer grubu ComboBox template

## Dokunulmayacak (Mühürlü)
- EditorViewModel.cs (ActiveLayerId zaten var)
- CadDocument.cs (Layers zaten var)

## Test Bloğu: T424–T427