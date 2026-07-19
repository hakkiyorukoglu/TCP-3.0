# Plan: v3.0.29.13 — Status Bar Düzeltme

## Amaç
1. Koordinat panelini alt sağa taşı
2. Kaydet butonu IsDirty görsel geri bildirim (turuncu)

## Değişen Dosyalar
- `src/TrainService.App/Views/Pages/EditorView.xaml` — Koordinat paneli Right/Bottom
- `src/TrainService.App/Controls/Ribbon/RibbonControl.xaml.cs` — Save butonu IsDirty dinleyici

## Test Bloğu: T432–T435