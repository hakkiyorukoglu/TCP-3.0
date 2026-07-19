# v3.0.29.22 — Manüel Test Adımları

**Başlangıç:** `dotnet run --project src/TrainService.App/TrainService.App.csproj`

> Önce Track tool (T) ile birkaç ray çiz. Düğümler ve segmentler olsun.

---

## M-S1: Tüm snap türleri aktif

| # | Adım | Beklenen |
|---|------|----------|
| 1.1 | Track tool (T) ile çizime başla | İmleç hareket ederken endpoint/midpoint/on-segment/grid snap çalışsın |
| 1.2 | İmleci bir düğüme yaklaştır | Düğüm snap'i öncelikli (kare işaretçi) |
| 1.3 | İmleci segmentin tam ortasına yaklaştır | Orta nokta snap'i çalışsın |

---

## M-S2: Endpoint toggle

| # | Adım | Beklenen |
|---|------|----------|
| 2.1 | Ribbon GÖRÜNÜM → "Uç" butonuna tıkla | Toggle kapansın |
| 2.2 | Bir düğüme yaklaş | Düğüme **SNAP YAPILMASIN** |
| 2.3 | Orta noktaya yaklaş | Midpoint çalışsın |
| 2.4 | "Uç" tekrar tıkla | Endpoint aktif olsun |

---

## M-S3: Midpoint toggle

| # | Adım | Beklenen |
|---|------|----------|
| 3.1 | "Orta" butonuna tıkla | Midpoint kapansın |
| 3.2 | Orta noktaya yaklaş | **SNAP YAPILMASIN** |
| 3.3 | Düğüme yaklaş | Endpoint çalışsın |

---

## M-S4: F9 toplu snap

| # | Adım | Beklenen |
|---|------|----------|
| 4.1 | **F9** bas | Tüm snap kapansın, `[OFF]` yazsın |
| 4.2 | Hiçbir yere snap yapılmasın | İmleç serbest |
| 4.3 | **F9** tekrar bas | Snap açılsın, `[GRID]` yazsın |

---

## M-S5: Tüm butonları kapat

| # | Adım | Beklenen |
|---|------|----------|
| 5.1 | "Uç","Orta","Kenar","GridS" hepsini kapat | Hiç snap çalışmasın |
| 5.2 | Çizim yap | Serbest çizim olsun |
| 5.3 | Hepsini tekrar aç | Tüm snap'ler geri gelsin |

---

## M-S6: Öncelik sırası

| # | Adım | Beklenen |
|---|------|----------|
| 6.1 | İmleci düğüme çok yakın getir | Endpoint kazansın (10 < 15) |
| 6.2 | İmleci orta noktaya + kenara yakın getir | Midpoint kazansın (15 < 20) |
| 6.3 | Kenar + grid yakın | OnSegment kazansın (20 < 100) |

---

## M-S7: Regresyon

| # | Adım | Beklenen |
|---|------|----------|
| 7.1 | S → Seçim aracı | Çalışsın |
| 7.2 | T → Ray çiz | Çalışsın |
| 7.3 | R → Rota çiz | Çalışsın |
| 7.4 | H → Hibrit çizim | Çalışsın |
| 7.5 | F8 → Makas | Çalışsın |
| 7.6 | Ctrl+Z / Ctrl+Y | Undo/Redo çalışsın |
| 7.7 | Esc | Tool iptal olsun |
| 7.8 | Enter / SağTık | Tool commit olsun |

---

## M-S8: Ribbon görünüm

| # | Adım | Beklenen |
|---|------|----------|
| 8.1 | GÖRÜNÜM sekmesine tıkla | 4 snap butonu (Uç/Orta/Kenar/GridS) görünsün |
| 8.2 | Fareyi buton üzerine getir | Tooltip çıksın |
| 8.3 | İkonlar görünsün | Her butonda ikon olsun |