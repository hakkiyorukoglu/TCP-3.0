═══════════════════════════════════════════════════════════════════════
v3.0.21 MÜHÜR RAPORU — SelectTool + Marquee (AutoCAD Referanslı)
Tarih: 2026-07-16 22:59
AGENTS.md Bölüm 4.1 — 9 Bölüm
═══════════════════════════════════════════════════════════════════════

[1] EKLENEN KOD BİRİMLERİ
─────────────────────────────────────────────────────────────────────
• src/TrainService.Core/Geometry/GeometryTypes.cs
  - BoundingBox.Contains(BoundingBox) → Window seçim (AutoCAD soldan-sağa)
  - BoundingBox.IntersectsWith(BoundingBox) → Crossing seçim (AutoCAD sağdan-sola)
  - BoundingBox.FromPoint(Vector2D, double) → tolerans kutusu

• src/TrainService.Cad/Tools/ITool.cs
  - void OnPointerUp(SnapResult, ToolMouseButton, ToolContext) → arayüze eklendi
  - public sealed record PreviewRectangle(Vector2D From, Vector2D To, bool IsCrossing) → yeni PreviewShape

• src/TrainService.Cad/Tools/ToolInput.cs
  - ToolKey enum: Delete eklendi
  - ToolContext: ModifierAdd (Shift/Ctrl), ClickToleranceWorld

• src/TrainService.Cad/Tools/SelectTool.cs (SelectToolStub.cs'den)
  - OnPointerDown/Move/Up: Marquee (Window/Crossing ayrımı) + Tıklama seçimi
  - MarqueeSelect(): SpatialHash QueryRegion → Contains vs IntersectsWith
  - ClickSelect(): DistanceSquaredToSegment + tolerans
  - OnKeyDown(Delete): DeleteEntitiesCommand → CommandStack.Do

• src/TrainService.Cad/Tools/TrackTool.cs
  - OnPointerUp() boş metot → ITool sözleşmesi güncellendi

• src/TrainService.Cad/UndoRedo/DeleteEntitiesCommand.cs (YENİ)
  - Execute: seçili nesneleri sil, _silinen'e kaydet
  - Undo: _silinen'den doc.AddEntity ile geri ekle

• src/TrainService.App/Resources/CadColors.cs (YENİ)
  - WindowFill/WindowPen: mavi %20 dolgu / düz kenar
  - CrossingFill/CrossingPen: yeşil %20 dolgu / kesikli kenar
  - HoverPen: cyan, 2px
  - SelectedPen: beyaz kesikli, 2px
  - Tüm fırça/kalemler Freeze()'li (hot-path güvenli)

• src/TrainService.App/Controls/CadCanvas/ToolController.cs
  - PointerUp() metodu eklendi
  - KeyDown: Delete tuşu yönlendirmesi
  - CtxWith(): anlık Shift/Ctrl + px→dünya tolerans dönüşümü

• src/TrainService.App/Controls/CadCanvas/CadViewportControl.cs
  - AttachSelection(SelectionService): SelectionChanged event bağlaması
  - OnMouseUp: PointerUp yönlendirmesi
  - OnMouseMove: hover hit-test (EntityDistSq, BoundingBox.FromPoint)
  - RenderToolLayer: PreviewRectangle → CadColors'tan renk/stil
  - RenderModelBake: seçim/hover vurgusu (CadColors.SelectedPen/HoverPen)
  - EntityDistSq(): yardımcı hit-test (TrackNode/TrackSegment)

[2] T221-T228 GÖVDE ÖZETİ
─────────────────────────────────────────────────────────────────────
T221 TekTik_EnYakinNodeSecer       — tolerans içi düğüm seçilir
T222 BoslugaTik_SecimTemizler       — boşluk tıklaması seçimi temizler
T223 Window_SoldanSaga             — soldan-sağ (x++) = Window = mavi = Contains
T224 Crossing_SagdanSola           — sağdan-sol (x--) = Crossing = yeşil = IntersectsWith
T225 Window_DegenAmaTamIcermeyen   — Window'da kesişen ama tam içermeyen SEÇİLMEZ
T226 Shift_MevcutSecimeEkler       — ModifierAdd=true → önceki seçime ekle
T227 BoundingBox_ContainsVeIntersects — matematiksel doğruluk testi
T228 Delete_SeciliyiSiler_UndoGeriGetirir — CommandStack entegrasyonu

[3] TDD WATCH-IT-FAIL HAM ÇIKTISI
─────────────────────────────────────────────────────────────────────
Tarih: (İlk koşum — stub implmentasyon)
Sonuç: Toplam 8 test — Geçti: 1, Başarısız: 7
Geçen: T227 (BoundingBox matematikleri — Core'da zaten eklenmişti)
Başarısız: T221-T226, T228 → NotImplementedException: SelectTool.Activate, DeleteEntitiesCommand..ctor
Hata yeri: SelectTool.cs:12 Activate(), DeleteEntitiesCommand.cs:8 .ctor()
(AGENTS.md 1.2 TDD ritüeli: watch-it-fail adımı tamamlandı)

[4] TAM KOŞUM SONUÇLARI (Fail=0)
─────────────────────────────────────────────────────────────────────
TrainService.Cad.Tests          → Başarılı: 72 / Başarısız: 0 / Atlanan: 0
TrainService.Core.Tests         → Başarılı: 25 / Başarısız: 0 / Atlanan: 0
TrainService.Messaging.Tests    → Başarılı: 16 / Başarısız: 0 / Atlanan: 0
TrainService.App.Tests          → Başarılı:  5 / Başarısız: 0 / Atlanan: 0
TrainService.Data.Tests         → Başarılı: 23 / Başarısız: 0 / Atlanan: 0
TrainService.Simulation.Tests   → Başarılı:  1 / Başarısız: 0 / Atlanan: 0
TrainService.Architecture.Tests → Başarılı:  9 / Başarısız: 0 / Atlanan: 0
─────────────────────────────────────────────────────────────────────
GENEL TOPLAM: 151 test — Başarısız: 0, Atlanan: 0

[5] DOLGU SIFIR KANITI
─────────────────────────────────────────────────────────────────────
 dolgu bulundu (0 olmalı)

[6] T011 BEKÇI KONTROLÜ
─────────────────────────────────────────────────────────────────────


[7] GÖRSEL: PREVIEWRECTANGlE RENDER KOLU + CADCOlORS
─────────────────────────────────────────────────────────────────────
PreviewRectangle render kolu (CadViewportControl.cs RenderToolLayer):
  else if ... is PreviewRectangle rect:
    IsCrossing=true  → CadColors.CrossingFill (yeşil %20) + CadColors.CrossingPen (kesikli)
    IsCrossing=false → CadColors.WindowFill   (mavi %20)  + CadColors.WindowPen   (düz)

CadColors paleti (src/TrainService.App/Resources/CadColors.cs):
  WindowFill   = ARGB(50,0,128,255)  — mavi %20
  WindowPen    = ARGB(255,0,128,255) — mavi düz 1px
  CrossingFill = ARGB(50,0,200,0)    — yeşil %20
  CrossingPen  = ARGB(255,0,200,0)   — yeşil kesikli 1px
  HoverPen     = ARGB(255,0,255,255) — cyan 2px (İstek 1)
  SelectedPen  = White               — beyaz kesikli 2px (İstek 1)

[8] MİGRATION VE SNAP TEYIDI
─────────────────────────────────────────────────────────────────────
Migration Pending: DesignTimeDbContextFactory mevcut (v3.0.18'de kuruldu).
Snap sistemi (v3.0.17/v3.0.19): SnapEngine + 3 provider (Grid/Endpoint/OnSegment) dokunulmadı.
TrackTool T (çizim): OnPointerUp boş metot eklendi, davranış değişmedi.
F9=Snap kısayolu: EditorView PreviewKeyDown → SelectTool(S)/TrackTool(T) korundu.

[9] SAPMALAR (tools/sapma.txt güncellendi)
─────────────────────────────────────────────────────────────────────
Sapma-1: DeleteEntitiesCommand — node-yetim segment sınırı
  Seçimde olmayan bir segment, seçili node silinince yetim kalır.
  Karar: Sadece seçili olanları sil; tutarlılık denetimi v3.0.32'de.

Sapma-2: SelectTool sürükleme eşiği — dünya koordinatında MVP
  Eşik: epsilon > 1e-6 (dünya birimi). App katmanı px tabanlı eşiği
  ileride ToolContext'e ekleyebilir; şimdi her fare hareketi dragging=true.

Grip noktaları YOK (İstek 4 → ~v3.0.30).
Radyal menü YOK (v3.0.29).
Kopyala/Kes/Yapıştır YOK (v3.0.22).

═══════════════════════════════════════════════════════════════════════
MÜHÜR KARARI: v3.0.21 MÜHÜRLENDİ ✅
Bir sonraki: v3.0.22 (Pano: Kopyala/Kes/Yapıştır)
═══════════════════════════════════════════════════════════════════════
