# Mühür Raporu — v3.0.29.1-fix (Kritik Bug Düzeltmeleri)

## Teslimat Bilgileri

| Alan | Değer |
|------|-------|
| **Sürüm** | v3.0.29.1-fix |
| **FAZ** | D2 — Kritik Bug Düzeltmeleri (Paste/RestoreEntity/Clipboard/ProjectId) |
| **Tarih** | 2026-07-19 |
| **Önceki** | v3.0.29.1 (Mühürlü) |

---

## 1. Kapsam

Tarama sonucu bulunan 5 kritik hata düzeltildi:

| # | Hata | Neden | Çözüm | Test |
|---|------|-------|-------|------|
| 1 | `Paste()` undo/redo dışı | `RestoreEntity` doğrudan çağrılıyordu, `CommandStack`'e yazılmıyordu | `PasteEntitiesCommand` oluşturuldu, `CommandStack.Do()` ile çağrılıyor | T336 |
| 2 | `Paste()` `IsDirty` kaçırılıyor | `RestoreEntity` `IsDirty` set etmiyordu | `RestoreEntity` → `IsDirty=true` + `Changed event` | T337, T338 |
| 3 | `RestoreEntity`/`AddEntity` tutarsızlığı | Biri event/IsDirty set ediyordu, diğeri etmiyordu | `RestoreEntity` `AddEntity` ile aynı davranışa getirildi | T339 |
| 4 | Clipboard yeni entity türlerini desteklemiyor | `Klonla` sadece `TrackNode`/`TrackSegment` içeriyordu | `RailSwitch`, `Ramp`, `Route` klonlama eklendi | T340–T342 |
| 5 | `Guid.Empty` sabit kullanımı | Save/Load her zaman `Guid.Empty` kullanıyordu | `EditorViewModel` constructor'ına `projectId` eklendi, default `Guid.NewGuid()` | T344 |

---

## 2. Yeni Dosyalar

- `src/TrainService.Cad/UndoRedo/PasteEntitiesCommand.cs` — Undo/redo'lu paste komutu
- `tests/TrainService.App.Tests/T336_T344_FixTests.cs` — 9 yeni test (T336–T344)

---

## 3. Değişen Dosyalar

| Dosya | Değişiklik |
|-------|-----------|
| `src/TrainService.App/ViewModels/EditorViewModel.cs` | `Paste()` → `PasteEntitiesCommand` kullanımı; `_projectId` field + constructor parametresi; `Save()`/`Load()` `Guid.Empty` → `_projectId` |
| `src/TrainService.Cad/CadDocument.cs` | `RestoreEntity()` → `IsDirty=true` + `Changed?.Invoke(Added, ...)` |
| `src/TrainService.Cad/Clipboard/ClipboardService.cs` | `Klonla()` → `RailSwitch`, `Ramp`, `Route` dalları eklendi |

---

## 4. PasteEntitiesCommand Davranışı

```
Constructor(clipboardEntities)
├── 1. ID Remap: Her entity'ye yeni Guid
├── 2. Referans Çözümleme: StartNodeId/EndNodeId/EntryNodeId/ExitNodeId/SegmentId → yeni ID'ler
├── 3. Offset: TrackNode pozisyonları +20mm
└── Execute(doc) → doc.RestoreEntity(e) for each
    Undo(doc) → doc.RemoveEntity(e.Id) for each
```

---

## 5. Test Sonuçları

| Blok | Test | Açıklama | Durum |
|------|------|----------|-------|
| T336 | PasteCommand_IsUndoable | Yapıştırma Ctrl+Z ile geri alınabilmeli | ✅ |
| T337 | PasteCommand_SetsIsDirty | Yapıştırma dokümanı kirli yapmalı | ✅ |
| T338 | RestoreEntity_SetsIsDirty | `RestoreEntity` `IsDirty=true` | ✅ |
| T339 | RestoreEntity_FiresChangedEvent | `RestoreEntity` `Changed` event fırlatmalı | ✅ |
| T340 | Clipboard_CanClone_RailSwitch | `RailSwitch` panoya kopyalanabilmeli | ✅ |
| T341 | Clipboard_CanClone_Ramp | `Ramp` panoya kopyalanabilmeli | ✅ |
| T342 | Clipboard_CanClone_Route | `Route` panoya kopyalanabilmeli | ✅ |
| T343 | PruneMissing_DoesNotLeakOnMultipleCalls | Çoklu `PruneMissing` tek handler (zaten çalışıyordu) | ✅ |
| T344 | SaveLoad_UsesProjectId | `SaveDocumentAsync` `Guid.Empty` değil gerçek ID kullanmalı | ✅ |

**Yeni testler: 9/9 PASSED**

**Eski testler:** Tüm mevcut testler (T330–T335 dahil) başarıyla çalışmaya devam ediyor — regresyon yok.

---

## 6. Bekçi Kontrolü

| Kural | Durum |
|-------|-------|
| T001–T011 (mimari katman) | Dokunulmadı ✅ |
| `TrainService.Core` | Değişiklik yok ✅ |
| `TrainService.Cad` | Sadece `CadDocument.RestoreEntity`, `ClipboardService.Klonla`, `PasteEntitiesCommand` (yeni) ✅ |
| NuGet paketleri | Yeni bağımlılık eklenmedi ✅ |
| Mühürlü davranışlar (Y5) | Korundu — F9, Esc, Enter, katmanlar, renkler ✅ |

---

## 7. Mühür

Bu sürüm, v3.0.29.1'de bulunan **kritik veri kaybı ve undo hatalarını** düzeltir.

- ✅ Plan onaylandı
- ✅ TDD (RED → GREEN) tamamlandı
- ✅ 9/9 yeni test geçti
- ✅ Eski testler regresyonsuz
- ✅ Bekçi kuralları ihlal edilmedi

**Mühürleyen:** Cline (Code mode)
**Sıradaki:** v3.0.29.2 — FAZ D2 devamı (Sekmeli Çoklu Belge)