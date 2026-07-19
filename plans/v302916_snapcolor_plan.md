# Plan: v3.0.29.16 — Snap Göstergeleri (İmleç Rengi Değişimi)

## Amaç
Snap türüne göre status paneldeki renk değişimi:
- Grid → LimeGreen
- Endpoint → Orange
- OnSegment → MediumPurple
- OFF → Red

## Değişen Dosyalar
- `src/TrainService.App/ViewModels/EditorViewModel.cs` — SnapStatusColor
- `src/TrainService.App/Controls/CadCanvas/CadViewportControl.cs` — SnapColorChanged event
- `src/TrainService.App/Views/Pages/EditorView.xaml` — Foreground binding
- `src/TrainService.App/Views/Pages/EditorView.xaml.cs` — Event handler

## Test Bloğu: T444–T446