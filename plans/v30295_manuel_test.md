# v3.0.29.5 Manuel Test Rehberi

> ⚠️ **Önemli:** Bu sürümde **test altyapısı** tamamlandı. UI'da sekme değişiminde Viewport/FeatureTree yeniden bağlama henüz çalışmıyor — `v3.0.29.6`'da gelecek.

---

## Ön Hazırlık

```bash
dotnet build
dotnet run --project src/TrainService.App/TrainService.App.csproj
```

---

## M1: Sekme Şeridi Görsel Kontrolü (v3.0.29.3 ile aynı)

| Adım | Beklenen | Başarılı? |
|------|----------|-----------|
| 1. Ribbon altında koyu gri şerit görünür | `DocumentTabsControl` görünür | ☐ |
| 2. Solunda **"+"** butonu var | Yeşil `+` ikonu | ☐ |
| 3. Varsayılan **"Yeni Proje"** başlıklı tek sekme | Sekme başlığı okunur | ☐ |

---

## M2: Yeni Sekme Ekleme

| Adım | Beklenen | Başarılı? |
|------|----------|-----------|
| 1. **+** butonuna tıkla | Yeni sekme eklenir | ☐ |
| 2. Yeni sekme adı **"Yeni Proje"** | Başlık doğru | ☐ |
| 3. 3-4 kez + butonu | Her biri ayrı sekme | ☐ |

---

## M3: Kirli Bayrak (★)

| Adım | Beklenen | Başarılı? |
|------|----------|-----------|
| 1. Çizim yap (ray çiz) | **Turuncu ★** belirir | ☐ |
| 2. Sekme arka planı sarımsı | `#3a3a2a` rengi | ☐ |

---

## M4: Sekme Kapatma (X)

| Adım | Beklenen | Başarılı? |
|------|----------|-----------|
| 1. Birkaç sekme aç | 3-4 sekme görünür | ☐ |
| 2. **×** butonuna tıkla | Sekme kalkar | ☐ |
| 3. Son sekme kapanınca | Yeni boş **"Yeni Proje"** oluşur | ☐ |

---

## M5: Sekme Değişimi (Bilinen Sınırlama)

| Adım | Beklenen | Gerçekleşen | Başarılı? |
|------|----------|-------------|-----------|
| 1. Sekme 1'de çizim yap | Entity görünür | Görünür | ☐ |
| 2. Sekme 2'ye tıkla | Boş doküman | ⚠️ Viewport hâlâ Sekme 1'i gösterir | ☐ |
| 3. Sekme 2'de çizim yap | Yeni entity | ⚠️ Viewport hâlâ Sekme 1'e ekler | ☐ |

> **Bu sınırlama v3.0.29.6'da çözülecek.** Şu an sekme değişimi sadece şerit üzerinde görsel olarak çalışır, içerik alanı (Viewport/FeatureTree) yeniden bağlanmaz.

---

## M6: Mevcut Davranışların Korunması (Y5 Regresyon)

| Adım | Beklenen | Başarılı? |
|------|----------|-----------|
| `S` → SelectTool | İmleç normal | ☐ |
| `T` → TrackTool | İmleç çapraz | ☐ |
| `R` → RouteTool | İmleç rota modu | ☐ |
| `H` → HybridTool | İmleç hibrit modu | ☐ |
| `F8` → SwitchTool | Y-şekilli ghost | ☐ |
| `F9` → Snap toggle | Snap durumu değişir | ☐ |
| `Ctrl+Z` → Undo | Son işlem geri alınır | ☐ |
| `Delete` → Seçili entity silinir | Undo ile geri alınabilir | ☐ |
| Sağ tık → RadialMenu | Dairesel menü açılır | ☐ |
| Ribbon → butonlar çalışır | Her sekme aktif | ☐ |
| Öğe Ağacı | Entity'ler listelenir | ☐ |

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