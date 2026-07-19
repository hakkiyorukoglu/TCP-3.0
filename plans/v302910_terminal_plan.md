# Plan: v3.0.29.10 — TerminalPanel Entegrasyonu

## Amaç
EditorView'e alt dock'ta TerminalPanel ekle. TerminalPanel zaten implemente edilmiş.

## Değişen Dosyalar
- `src/TrainService.App/Views/Pages/EditorView.xaml` — RowDefinitions + TerminalPanel
- `src/TrainService.App/ViewModels/EditorViewModel.cs` — TerminalPanelViewModel property
- `src/TrainService.App/App.xaml.cs` — EditorViewModel DI kontrolü

## Dokunulmayacak (Mühürlü)
- TerminalPanel.xaml, TerminalPanelViewModel.cs, LogBus.cs

## Test Bloğu: T420–T423