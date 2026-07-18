# v3.0.29.3 Manuel Test Rehberi

> ⚠️ **Önemli:** Bu sürümde sekme şeridi **görsel altyapısı** hazırlandı. Çoklu sekme davranışı (aktif sekme değişiminde Viewport/FeatureTree yeniden bağlama) henüz tamamlanmadı — `v3.0.29.4`'te gelecek.

---

## Ön Hazırlık

```bash
# Projeyi derle
dotnet build

# Uygulamayı başlat
dotnet run --project src/TrainService.App/TrainService.App.csproj
```

---

## M1: Sekme Şeridi Görsel Kontrolü

| Adım | Beklenen | Başarılı? |
|------|----------|-----------|
| 1. Uygulama açıldığında, Ribbon'ın hemen altında koyu gri bir şerit görünür | `DocumentTabsControl` görünür | ☐ |
| 2. Şeridin solunda **"+"** butonu var | Yeşil `+` ikonu | ☐ |
| 3. Varsayılan olarak **"Yeni Proje"** başlıklı tek sekme var | Sekme başlığı okunur | ☐ |

---

## M2: Yeni Sekme Ekleme (+ butonu)

| Adım | Beklenen | Başarılı? |
|------|----------|-----------|
| 1. **+** butonuna tıkla | Yeni sekme şeride eklenir | ☐ |
| 2. Yeni sekmenin adı **"Yeni Proje"** | Başlık doğru | ☐ |
| 3. 3-4 kez daha + butonuna tıkla | Her biri ayrı sekme olarak görünür | ☐ |

---

## M3: Kirli Bayrak (★)

| Adım | Beklenen | Başarılı? |
|------|----------|-----------|
| 1. Herhangi bir sekmede çizim yap (ray çiz, F9 ile snap aç/kapa) | Sekme başlığının yanında **turuncu ★** belirir | ☐ |
| 2. ★ göründüğünde sekme arka planı hafif sarımsı/koyu olur | `#3a3a2a` rengi | ☐ |

---

## M4: Sekme Kapatma (X butonu)

| Adım | Beklenen | Başarılı? |
|------|----------|-----------|
| 1. Birkaç sekme aç (+ butonu) | 3-4 sekme görünür | ☐ |
| 2. Herhangi bir sekmenin **×** butonuna tıkla | Sekme şeridden kalkar | ☐ |
| 3. Son kalan sekmeyi kapat | Otomatik yeni boş **"Yeni Proje"** sekmesi oluşur | ☐ |

---

## M5: Mevcut Davranışların Korunması (Y5 Regresyon)

| Adım | Beklenen | Başarılı? |
|------|----------|-----------|
| 1. `S` → SelectTool aktif | İmleç normal | ☐ |
| 2. `T` → TrackTool aktif | İmleç çapraz | ☐ |
| 3. `R` → RouteTool aktif | İmleç rota modu | ☐ |
| 4. `H` → HybridTool aktif | İmleç hibrit modu | ☐ |
| 5. `F8` → SwitchTool aktif | Y-şekilli ghost görünür | ☐ |
| 6. `F9` → Snap toggle | Snap durumu değişir (status paneli) | ☐ |
| 7. `Ctrl+Z` → Undo | Son işlem geri alınır | ☐ |
| 8. `Ctrl+Y` → Redo | Son işlem tekrarlanır | ☐ |
| 9. `Delete` → Seçili entity silinir | Undo ile geri alınabilir | ☐ |
| 10. Sağ tık → RadialMenu | Dairesel menü açılır | ☐ |
| 11. Ribbon şerit → butonlar çalışır | Her sekme aktif | ☐ |
| 12. Öğe Ağacı (sol panel) | Entity'ler listelenir, çift tıkla zoom | ☐ |

---

## M6: Known Limitations (Bilinen Sınırlamalar)

| # | Sorun | Açıklama |
|---|-------|----------|
| 1 | Viewport tek doc gösterir | Aktif sekme değişse bile Viewport hâlâ ilk `EditorViewModel.Document`'ı gösterir |
| 2 | FeatureTree tek doc gösterir | Öğe ağacı ilk dokümanı listeler |
| 3 | Undo/Redo tek stack kullanır | `CommandStack` henüz sekme başına izole değil (UI'da) |

> Bunlar `v3.0.29.4`'te çözülecek — çalışma zamanı entegrasyonu.

---

## M7: Hata Raporu Şablonu

Bir sorun bulursan:

```
[M7-x] Kısa başlık
- Adım: ...
- Beklenen: ...
- Gerçekleşen: ...
- Ekran görüntüsü: (varsa)
```

---

## Genel Değerlendirme

| Kriter | Durum |
|--------|-------|
| Sekme şeridi görünür | ☐ Evet / ☐ Hayır |
| + butonu çalışır | ☐ Evet / ☐ Hayır |
| Kirli bayrak (★) çalışır | ☐ Evet / ☐ Hayır |
| X butonu çalışır | ☐ Evet / ☐ Hayır |
| Son sekme kapanınca yeni boş oluşur | ☐ Evet / ☐ Hayır |
| Mevcut davranışlar bozulmadı | ☐ Evet / ☐ Hayır |

**Test eden:** _______________
**Tarih:** _______________