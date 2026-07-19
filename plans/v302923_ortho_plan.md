# v3.0.29.23 — Ortho Mode (F10) + Polar Tracking + Dynamic Input

**Plan tarihi:** 2026-07-19  
**Kaynak:** `Roadmap.md > FAZ D3 G2 > v3.0.29.23`  
**Test kimlik bloğu:** T491–T492

---

## 1. Mevcut Durum

- SnapEngine çalışıyor, ISnapProvider zinciri var
- ToolContext snap sonucuyla birlikte araçlara geçiliyor
- F10 şu anda kullanılmıyor (F8=Switch mühürlü, F9=Snap mühürlü)
- CadViewportControl fare olaylarını ToolController'a yönlendiriyor
- TrackTool, RouteTool, SwitchTool, RampTool ITool implementasyonları var

## 2. Kapsam

| # | Özellik | Katman |
|---|---------|--------|
| 1 | `ToolContext`'e ortho/polar alanları: `IsOrthoEnabled`, `OrthoAngle` | Cad |
| 2 | `OrthoSnapProvider` — açı kısıtlaması (0/90/180/270) | Cad |
| 3 | F10 tuşu ortho toggle | App |
| 4 | StatusBar'da ortho durum göstergesi | App |
| 5 | Polar tracking (ileri sürüm, şimdilik plan dışı) | — |
| 6 | Dynamic Input (ileri sürüm, şimdilik plan dışı) | — |

> ⚠ **SADECE Ortho Mode bu sürümde.** Polar Tracking ve Dynamic Input D3 G2'nin son sürümü değil, v3.0.29.23'ün scope'u Roadmap'teki gibi: Ortho F10. Polar tracking ve dynamic input v3.0.29.30+ veya D3 G5'e kaydırılabilir.

## 3. Detaylı Tasarım

### 3.1 ToolContext genişletmesi

```csharp
public sealed record ToolContext(CadDocument Document, CommandStack Commands, SelectionService Selection)
{
    public bool ModifierAdd { get; init; }
    public double ClickToleranceWorld { get; init; } = 50.0;
    public ClipboardService Clipboard { get; init; } = default!;
    public bool IsOrthoEnabled { get; init; }  // ★ YENİ
}
```

### 3.2 OrthoSnapProvider

```csharp
// Priority = 5 (en yüksek — diğer snap'lerden önce)
// Eğer ortho aktifse, imleci en yakın yatay/dikey eksene çeker
public sealed class OrthoSnapProvider : ISnapProvider
{
    public int Priority => 5;
    public SnapResult? TrySnap(Vector2D cursor, double tol, CadDocument doc);
}
```

### 3.3 EditorViewModel

```csharp
[ObservableProperty] private bool _isOrthoEnabled;
[RelayCommand] private void ToggleOrtho() => IsOrthoEnabled = !IsOrthoEnabled;
public string OrthoStatusText => IsOrthoEnabled ? "[ORTHO]" : "";
```

### 3.4 Ribbon — GÖRÜNÜM display grubuna Ortho butonu

```csharp
new("ToggleOrtho", "Ortho", "AxisArrow", "(F10)", "ToggleOrtho", null, IsToggle: true),
```

### 3.5 EditorView.xaml — F10 key binding + StatusBar ortho LED

F10 InputBinding + durum çubuğunda Ortho göstergesi.

## 4. Dosyalar

| Dosya | Durum |
|-------|-------|
| `Cad/Tools/ToolInput.cs` | DEĞİŞECEK (+IsOrthoEnabled) |
| `Cad/Snapping/OrthoSnapProvider.cs` | YENİ |
| `App/ViewModels/EditorViewModel.cs` | DEĞİŞECEK (+IsOrthoEnabled + ToggleOrtho) |
| `App/Controls/Ribbon/RibbonDefinition.cs` | DEĞİŞECEK (+Ortho butonu) |
| `App/Views/Pages/EditorView.xaml` | DEĞİŞECEK (+F10 binding + Ortho LED) |
| `App/App.xaml.cs` | DEĞİŞECEK (+OrthoSnapProvider DI) |

## 5. Test Planı — T491–T492

| Test | İçerik |
|------|--------|
| T491 | OrthoSnapProvider açı kısıtlaması (0/90/180/270) |
| T492 | RibbonDefinition Ortho butonu + IsToggle |

## 6. Mühürlü Davranış

- F8=Switch: ETKİLENMEZ (F10 kullanılır) ✅
- F9=Snap: ETKİLENMEZ ✅
- Core: DEĞİŞMEZ ✅
- Cad WPF'siz: KORUNUR ✅