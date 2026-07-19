# v3.0.29.21 — Selection Modları (Window, Crossing, Fence)

**Plan tarihi:** 2026-07-19  
**Kaynak:** `Roadmap.md > FAZ D3 G2 > v3.0.29.21`  
**Test kimlik bloğu:** T479–T484

---

## 1. Mevcut Durum Analizi

### 1.1 Halihazırda Çalışanlar
- `SelectTool.cs` marquee seçimi ZATEN yapıyor:
  - Sol → Sağ sürükleme = **Window** (tamamen içeren) — `box.Contains(eb)`
  - Sağ → Sol sürükleme = **Crossing** (kesişen) — `box.IntersectsWith(eb)`
- `PreviewRectangle` marquee önizlemesi render ediliyor
- `SelectionService` HashSet tabanlı, çalışıyor
- `BoundingBox` — `Contains()` + `IntersectsWith()` mevcut

### 1.2 Eksik Olan
- **Fence (Lasso/Çokgen) seçimi** hiç yok
- **MarqueeSelector** ayrı bir sınıf değil — tüm mantık `SelectTool` içinde gömülü
- **Seçim modu göstergesi** yok (UI'da Window/Crossing/Fence modu görünmüyor)
- `PreviewFence` çokgen önizleme tipi yok

---

## 2. Kapsam (Neler Yapılacak)

| # | Özellik | Katman |
|---|---------|--------|
| 1 | `SelectionMode` enum (Window, Crossing, Fence) | Cad |
| 2 | `MarqueeSelector` sınıfı — seçim mantığını SelectTool'dan AYIR | Cad |
| 3 | Fence seçimi: poligon çizimi + Point-In-Polygon | Cad |
| 4 | `PreviewFence` preview shape | Cad (ITool.cs) |
| 5 | `SelectTool`'a Fence modu entegrasyonu | Cad |
| 6 | Ribbon'da seçim modu butonları | App |
| 7 | Status bar'da aktif seçim modu göstergesi | App |
| 8 | `EditorViewModel.SelectionMode` property | App |

---

## 3. Yeni/Değişen Dosyalar

```
src/TrainService.Cad/
├── Selection/
│   └── MarqueeSelector.cs              (YENİ)
├── Tools/
│   ├── SelectionMode.cs                (YENİ)
│   ├── ITool.cs                        (DEĞİŞECEK — PreviewFence)
│   └── SelectTool.cs                   (DEĞİŞECEK)

src/TrainService.App/
├── ViewModels/
│   └── EditorViewModel.cs              (DEĞİŞECEK)
├── Controls/Ribbon/
│   └── RibbonDefinition.cs             (DEĞİŞECEK)
├── Views/Pages/
│   └── EditorView.xaml                 (DEĞİŞECEK — status bar)
│   └── EditorView.xaml.cs              (DEĞİŞECEK — SelectionModeChanged handler)

tests/TrainService.App.Tests/
└── T479_T484_SelectionModeTests.cs     (YENİ)

plans/
└── v302921_selection_plan.md           (BU DOSYA)
```

---

## 4. Detaylı Tasarım

### 4.1 SelectionMode enum (TrainService.Cad.Tools)

```csharp
namespace TrainService.Cad.Tools;

public enum SelectionMode
{
    Window,    // Sol→Sağ: tamamen içeren
    Crossing,  // Sağ→Sol: kesişen (varsayılan)
    Fence      // Çokgen/lasso: içerde kalan
}
```

### 4.2 MarqueeSelector (TrainService.Cad.Selection)

Statik sınıf, saf fonksiyonlar:
- `WindowSelect(CadDocument, BoundingBox)` → List<Guid>
- `CrossingSelect(CadDocument, BoundingBox)` → List<Guid>
- `FenceSelect(CadDocument, IReadOnlyList<Vector2D>)` → List<Guid>
- `IsPointInPolygon(Vector2D, IReadOnlyList<Vector2D>)` → bool (Ray Casting)

### 4.3 PreviewFence (ITool.cs)

```csharp
public sealed record PreviewFence(IReadOnlyList<Vector2D> Points, bool IsClosed) : PreviewShape;
```

### 4.4 SelectTool — Fence akışı

- Sol tık → poligon noktası ekle
- SağTık/Enter → poligon kapat → MarqueeSelector.FenceSelect()
- Esc → iptal, noktaları temizle
- Preview: PreviewFence ile canlı çokgen ipi

### 4.5 Ribbon & ViewModel

- GİRİŞ sekmesine "Seçim" grubu (Window/Crossing/Fence toggle)
- `EditorViewModel.ActiveSelectionMode` + `SetSelectionModeCommand`
- `SelectionModeChanged` event'i → SelectTool.SetMode()

---

## 5. Test Planı — T479–T484

| Test | İçerik |
|------|--------|
| T479 | SelectionMode enum varlığı ve değerleri |
| T480 | MarqueeSelector.WindowSelect() |
| T481 | MarqueeSelector.CrossingSelect() |
| T482 | MarqueeSelector.FenceSelect() + IsPointInPolygon() |
| T483 | SelectTool Fence modu durum geçişleri |
| T484 | EditorViewModel.SelectionMode + Ribbon toggle |

## 6. Mühürlü Davranış Kontrolü

| Kural | Durum |
|-------|-------|
| F9=snap | ETKİLENMEZ ✅ |
| Esc=İPTAL | GÜÇLENDİRİLİR (Fence iptal) ✅ |
| Enter/SağTık=COMMIT | KULLANILIR (Fence commit) ✅ |
| SabitKatmanlar GUID | ETKİLENMEZ ✅ |
| Core'a dokunma | Core DEĞİŞMEZ ✅ |
| Cad WPF'siz | Cad'de WPF tipi YOK ✅ |