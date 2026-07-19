# Plan: v3.0.29.14 — Kısayol Düzeltme + Sağ Tık Undo

## Amaç
1. `Z` çakışması çöz (ZoomExtents → Ctrl+Shift+Z)
2. Radyal menüye Undo/Redo ekle

## Değişen Dosyalar
- `src/TrainService.App/Views/Pages/EditorView.xaml` — Z → Ctrl+Shift+Z
- `src/TrainService.App/Controls/Ribbon/RibbonDefinition.cs` — Shortcut güncelle
- `src/TrainService.App/Controls/CadCanvas/CadViewportControl.cs` — Radyal menü Undo/Redo

## Test Bloğu: T436–T439