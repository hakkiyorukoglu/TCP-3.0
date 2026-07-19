# TCP 3.0 (Train Control Platform)

![Platform](https://img.shields.io/badge/Platform-Windows-blue)
![Framework](https://img.shields.io/badge/Framework-.NET%208.0-512BD4)
![UI](https://img.shields.io/badge/UI-WPF%20%2B%20Fluent-success)
![Version](https://img.shields.io/badge/Version-v3.0.29.22-orange)
![GitHub](https://img.shields.io/badge/GitHub-hakkiyorukoglu%2FTCP--3.0-lightgrey)

> GitHub: `https://github.com/hakkiyorukoglu/TCP-3.0.git`

TCP (Train Control Platform) 3.0, trenlerin otonom hareketlerini simüle eden, uzaktan komuta sağlayan ve MQTT üzerinden IoT tabanlı haberleşme ile gerçek zamanlı veri akışını yöneten yeni nesil bir masaüstü kontrol merkezidir.

---

# 🤖 YAPAY ZEKA DEVİR TESLİM (AI HANDOVER)

> **Bu bölümü okuyan AI Model için:** Bu proje bir önceki AI tarafından belirli kurallar ve sürüm disiplini ile geliştirilmiştir.

### 🚀 İLK 5 DAKİKA — NEREDEN BAŞLAMALI?
1. **Bu README'yi oku** (zaten okuyorsun)
2. **`onceki_talimat.txt`'i oku**
3. **`Roadmap.md`'yi aç, FAZ D3 G2'ye git**
4. **`dotnet build` çalıştır**
5. **`dotnet test tests/TrainService.App.Tests/` çalıştır**
6. **Sıradaki sürüm için PLAN yaz** → onaylat → TDD ile başla

---

## 📍 ŞU ANKİ DURUM

| Alan | Değer |
|------|-------|
| **Son Sürüm** | v3.0.29.22 |
| **Son Git Commit** | `cd7a563c` |
| **Son Yapılan** | Snap Mode Butonları — MidpointSnapProvider + SnapEngine.DisabledKinds + Ribbon toggle |
| **Sıradaki Sürüm** | v3.0.29.23 — Ortho Mode (F10) + Polar Tracking + Dynamic Input |
| **Aktif Faz** | FAZ D3 GRUP 2 — Seçim ve Snap (v3.0.29.20–23) |
| **Build Durumu** | ✅ 0 Error |
| **Test Durumu** | ✅ 146 Cad + 129 App = 275 test PASSED |

---

## 📊 FAZ VE VERSİYON ÖZETİ

### FAZ D3 — EDİTÖR PROFESYONEL CİLA (v3.0.29.17 → v3.0.29.42)

| Grup | Versiyon | Tema | Test | Durum |
|------|----------|------|------|-------|
| G1 | v3.0.29.17–19 | Görsel Temel | T460–T474 | ✅ |
| G2 | v3.0.29.20–23 | Seçim ve Snap | T475–T492 | v3.0.29.20 ✅ v3.0.29.21 ✅ v3.0.29.22 ✅ |
| G3 | v3.0.29.24–27 | Modify Araçları | T493–T512 | ⏳ |
| G4 | v3.0.29.28–30 | Draw Araçları | T513–T530 | ⏳ |
| G5 | v3.0.29.31–33 | Ribbon ve UI | T531–T545 | ⏳ |
| G6 | v3.0.29.34–36 | Annotation | T546–T560 | ⏳ |
| G7 | v3.0.29.37–39 | Verimlilik | T561–T575 | ⏳ |
| G8 | v3.0.29.40–42 | Son Dokunuşlar | T576–T587 | ⏳ |

---

## 📝 SÜRÜM GEÇMİŞİ (son 3)

### v3.0.29.22 — Snap Mode Butonları ✅
- **YENİ:** `Cad/Snapping/MidpointSnapProvider.cs` — Priority=15, segment orta noktası snap
- **DEĞİŞEN:** `SnapKind.cs` (+Midpoint=40), `SnapEngine.cs` (+DisabledKinds HashSet)
- **DEĞİŞEN:** `App.xaml.cs` (+MidpointSnapProvider DI), `EditorViewModel.cs` (+4 toggle), `RibbonDefinition.cs` (+4 snap buton)
- **TEST:** T485–T490 (8 test). 8/8 PASSED.

### v3.0.29.21 — Selection Modları (Window, Crossing, Fence) ✅
- **YENİ:** `Cad/Tools/SelectionMode.cs`, `Cad/Selection/MarqueeSelector.cs`, `PreviewFence`
- **DEĞİŞEN:** `SelectTool.cs`, `RibbonDefinition.cs`, `EditorViewModel.cs`
- **TEST:** T479–T484 (9 test). 9/9 PASSED.

### v3.0.29.20 — Grip Editing ✅
- **YENİ:** `Adorners/GripAdorner.cs` — 10 grip + GripType enum
- **DEĞİŞEN:** `CadViewportControl.cs`
- **TEST:** T475–T478 (4 test).

---

## 🔗 İLGİLİ DOKÜMANLAR
`Roadmap.md` · `onceki_talimat.txt` · `docs/DbSchema.md` · `docs/TopicContract.md` · `tools/sapma.txt`

---

*Son güncelleme: 2026-07-19 · v3.0.29.22 · Git: cd7a563c · Aktif Faz: D3 G2*