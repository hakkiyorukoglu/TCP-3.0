# Plan: v3.0.29.15 — Zoom Kontrol (Slider + Fit Butonu)

## Amaç
Viewport'ta zoom seviyesi slider'ı, % göstergesi ve Fit butonu.

## Değişen Dosyalar
- `src/TrainService.App/ViewModels/EditorViewModel.cs` — ZoomScale + ZoomPercentText
- `src/TrainService.App/Views/Pages/EditorView.xaml` — Zoom Panel
- `src/TrainService.App/Views/Pages/EditorView.xaml.cs` — Zoom buton handler'ları

## Test Bloğu: T440–T443