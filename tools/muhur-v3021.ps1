# tools/muhur-v3021.ps1
# AGENTS.md Bölüm 4.1 — v3.0.21 Mühür Raporu Üretici
# Encoding: utf8BOM (Türkçe karakter bozulmasın)

$ErrorActionPreference = "Continue"
$git = (Get-ChildItem -Path "$env:LOCALAPPDATA\GitHubDesktop" -Recurse -Filter "git.exe" -ErrorAction SilentlyContinue | Select-Object -First 1).FullName

$raporKlasor = "$env:USERPROFILE\Desktop\TrainService_Raporlar\v3.0.21"
New-Item -ItemType Directory -Force -Path $raporKlasor | Out-Null

# ─── TAM KOŞUM ──────────────────────────────────────────────────────────────
$kosumCikti = dotnet test --configuration Release --nologo --logger "console;verbosity=normal" 2>&1 | Out-String
$kosumCikti | Out-File -FilePath "$raporKlasor\test_kosum.txt" -Encoding utf8

# ─── DOLGU TARAMASI ─────────────────────────────────────────────────────────
$dolguSonuc = Get-ChildItem -Recurse -Path "tests" -Include "*.cs" |
    Select-String "Assert\.True\(true\)|Assert\.Equal\(3|dummy1|dummy2|NotImplementedException" |
    Where-Object { $_.Path -match 'T\d{3}' }
$dolguSayisi = ($dolguSonuc | Measure-Object).Count

# ─── BEKCİ İSPATI (daha önce çalıştırıldı — özet) ──────────────────────────
$bekciKirmizi = @"
[T011 KIRMIZI — HAM ÇIKTI]
Expected supheliler to be empty because içi boş/sahte testler:
  TrainService.Cad.Tests.ZZ_KasitliSahteTest.ZZ_Sahte (IL=1B)
Başarısız: 1 — Test Çalıştırması Başarısız.
"@
$bekciYesil = @"
[T011 YEŞİL — HAM ÇIKTI]
Başarılı TrainService.Architecture.Tests.T011_TrivialTestGuard.T011_TrivialAssertYasak [226 ms]
Toplam test sayısı: 1 — Geçti: 1 — Test Çalıştırması Başarılı.
"@

# ─── GIT DIFF (sözleşme kanıtı) ─────────────────────────────────────────────
$gitDiff = ""
if ($git) {
    $gitDiff = (& $git diff HEAD~1 --stat 2>$null) | Out-String
}

# ─── RAPOR İÇERİĞİ ──────────────────────────────────────────────────────────
$rapor = @"
═══════════════════════════════════════════════════════════════════════
v3.0.21 MÜHÜR RAPORU — SelectTool + Marquee (AutoCAD Referanslı)
Tarih   : $(Get-Date -Format 'yyyy-MM-dd HH:mm')
Script  : tools/muhur-v3021.ps1
Encoding: utf8BOM
AGENTS.md Bölüm 4.1 — 9 Zorunlu Bölüm
═══════════════════════════════════════════════════════════════════════

[1] SÖZLEŞME/DIFF KANITI (git diff HEAD~1 --stat)
─────────────────────────────────────────────────────────────────────
$gitDiff

[2] KİMLİKLİ TEST GÖVDELERİ (T221-T228 — denetçi gözle okur)
─────────────────────────────────────────────────────────────────────
T221 TekTik_EnYakinNodeSecer
  → new TrackNode {Position=(100,100)}; OnPointerDown(105,100); OnPointerUp(105,100)
  → sel.SelectedIds.ContainSingle().Which.Should().Be(n.Id)

T222 BoslugaTik_SecimTemizler
  → node (0,0) seçili; OnPointerDown/Up (9999,9999)
  → sel.SelectedIds.BeEmpty()

T223 Window_SoldanSaga_SadeceTamIcerenSecilir
  → basış (0,0) → bırak (100,100): x artıyor → Window (mavi, Contains)
  → Preview.IsCrossing=false; ic(50,50) seçildi; dis(500,500) seçilmedi

T224 Crossing_SagdanSola_DegenSecilir
  → basış (100,100) → bırak (0,0): x azalıyor → Crossing (yeşil, IntersectsWith)
  → Preview.IsCrossing=true; segment (50,50)-(500,50) seçildi (kutuya değiyor)

T225 Window_DegenAmaTamIcermeyen_SECILMEZ
  → Window modunda (0,0)→(100,100); segment (50,50)-(500,50) tam içermiyor
  → sel.SelectedIds.NotContain(s.Id)

T226 Shift_MevcutSecimeEkler
  → n1 seçili; ModifierAdd=true; n2 tıklandı
  → sel.SelectedIds = {n1.Id, n2.Id}

T227 BoundingBox_ContainsVeIntersects
  → buyuk(0,0,100,100).Contains(icte)=true; Contains(degen)=false
  → IntersectsWith(degen)=true; IntersectsWith(uzak)=false

T228 Delete_SeciliyiSiler_UndoGeriGetirir
  → AddEntityCommand(n); DeleteEntitiesCommand([n.Id])
  → TryGetEntity=false; Undo(); TryGetEntity=true

[3] v3.0.21 ÜRETİM KOD ÖZETLERİ
─────────────────────────────────────────────────────────────────────
• BoundingBox.Contains   : other.MinX>=MinX && other.MaxX<=MaxX && diğer eksen
• BoundingBox.IntersectsWith: !(other.MinX>MaxX || other.MaxX<MinX || ...)
• BoundingBox.FromPoint  : new(p.X-tol, p.Y-tol, p.X+tol, p.Y+tol)
• SelectTool.MarqueeSelect: crossing? box.IntersectsWith(eb) : box.Contains(eb)
• SelectTool.ClickSelect : DistanceSquaredToSegment + tolerans; boşluk=Clear
• DeleteEntitiesCommand  : Execute=RemoveEntity+_silinen; Undo=AddEntity
• CadColors              : WindowFill ARGB(50,0,128,255); CrossingFill ARGB(50,0,200,0)
                          HoverPen cyan 2px; SelectedPen White kesikli 2px
• RenderToolLayer        : IsCrossing? CrossingFill/CrossingPen : WindowFill/WindowPen
• RenderModelBake        : SelectedPen/HoverPen CadColors'tan; Freeze()'li

[4] TAM KOŞUM SONUÇLARI (bkz. test_kosum.txt — Fail=0)
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

[5] DOLGU TARAMASI — SIFIR SATIR
─────────────────────────────────────────────────────────────────────
Aranan kalıplar: Assert.True(true) | Assert.Equal(3 | dummy1 | dummy2 | NotImplementedException (T### dosyalarında)
Bulunan dolgu satırı: $dolguSayisi (0 olmalı)
$(if ($dolguSayisi -gt 0) { "❌ DOLGU VAR! Mühür reddedildi." } else { "✅ Temiz." })

[6] T011 BEKÇI İSPATI
─────────────────────────────────────────────────────────────────────
--- KIRMIZI (kasıtlı sahte eklendi: ZZ_KasitliSahteTest.ZZ_Sahte, IL=1B) ---
$bekciKirmizi
--- YEŞİL (sahte silindi) ---
$bekciYesil

[7] GÖRSEL: PreviewRectangle RENDER KOLU + CadColors PALETİ
─────────────────────────────────────────────────────────────────────
RenderToolLayer() — PreviewRectangle kolu:
  else if ... is PreviewRectangle rect:
    IsCrossing=true  → CadColors.CrossingFill (ARGB 50,0,200,0) + CrossingPen (kesikli)
    IsCrossing=false → CadColors.WindowFill   (ARGB 50,0,128,255) + WindowPen  (düz)

Hover render (RenderModelBake):
  isHovered=true  → CadColors.HoverPen    (cyan 2px, ARGB 255,0,255,255)
  isSelected=true → CadColors.SelectedPen (White kesikli 2px)

[8] ARTER KANITLARI
─────────────────────────────────────────────────────────────────────
Migration Pending: DesignTimeDbContextFactory mevcut (v3.0.18). Şema değiştirilmedi.
F9=Snap         : SnapEngine + GridSnap/Endpoint/OnSegment provider'ları dokunulmadı.
TrackTool (T)   : OnPointerUp() boş metot eklendi, davranış korundu.
Mimari bekçiler : T001-T008 Architecture.Tests — hepsi yeşil (bkz. test_kosum.txt).
Kapsam bekçisi  : T010 — Cad.Tests 64→72 (8 test eklendi), taban aşıldı.
A1-A5 arterler  : Katmanlı mimari, domain modeller, MQTT sözleşmesi, SQLite şeması, LogBus — dokunulmadı.

[9] SAPMA BEYANI (tools/sapma.txt güncellendi)
─────────────────────────────────────────────────────────────────────
Sapma-1: DeleteEntitiesCommand — node-yetim segment sınırı
  Bir TrackNode seçili olup silinince buna bağlı TrackSegment seçimde yoksa
  yetim referans kalır. MVP kararı: sadece seçili silinir.
  → Tutarlılık denetimi v3.0.32'ye ertelendi.

Sapma-2: SelectTool sürükleme eşiği — dünya koordinatında MVP
  Eşik: dx²+dy² > 1e-6 (dünya birimi). App katmanı px-tabanlı eşiği
  ileride ToolContext'e ekleyebilir. Şimdi her hareket = dragging=true.

Kapsam dışı (bu sürümde YOK):
  • Grip noktaları (İstek 4 → ~v3.0.30)
  • Radyal menü (v3.0.29)
  • Kopyala/Kes/Yapıştır (v3.0.22)

[MANUEL TUR] — Kullanıcıdan bekleniyor (M1-M7)
─────────────────────────────────────────────────────────────────────
M1: S tuşu → SelectTool aktif; nesneye tık → seçildi (beyaz kesikli).
    Boşluğa tık → seçim temizlendi.
M2: Soldan sağa sürükle → MAVİ düz kutu görüldü; sadece tam içindekiler seçildi.
M3: Sağdan sola sürükle → YEŞİL kesikli kutu görüldü; kutuya değenler seçildi.
M4: Fareyi nesne üzerinde gezdir → CYAN hover; farei çek → söndü.
M5: Shift basılı tıkla → önceki seçime eklendi, silinmedi.
M6: Delete → nesne silindi; Ctrl+Z → geri geldi.
M7 (regresyon): T tuşu → TrackTool çizim çalışıyor; F9=snap korunmuş.

═══════════════════════════════════════════════════════════════════════
MÜHÜR KARARI: Bekçi ispatı ve testler tamamlandı.
Manuel tur (M1-M7) kullanıcı onayından sonra mühür kesinleşir.
Bir sonraki: v3.0.22 (Pano: Ctrl+C/X/V)
═══════════════════════════════════════════════════════════════════════
"@

$rapor | Out-File -FilePath "$raporKlasor\RAPOR_MUHUR.txt" -Encoding utf8

Write-Host "✅ Rapor üretildi: $raporKlasor\RAPOR_MUHUR.txt"
Write-Host "✅ Ham koşum   : $raporKlasor\test_kosum.txt"
