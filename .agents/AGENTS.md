# AGENTS.md — TrainService (TCP) Anayasası

Kısa ve değişmezdir. Detaylar (test kimlikleri, kabul kriterleri, rapor bölümleri) her sürümün
PLANINDA yazılır. Roadmap: `C:\Users\yoruk\.gemini\antigravity\scratch\TrainService\.agents\Roadmap.md`

## 0 — SUPERPOWERS (her oturum başında)
https://github.com/obra/superpowers deposunu OKU ve metodolojisini uygula:
- **TDD:** önce test → KIRMIZI GÖR ("watch it fail", ham çıktı) → minimum kod → yeşil. Testten önce yazılmış kod silinir.
- **7 adım:** brainstorm → plan → onay → uygula → TDD → doğrula → bitir. Plan, "hevesli junior'ın
  aynen izleyebileceği" netlikte olmalı (tam dosya yolu + tam kod).
- **YAGNI/DRY:** gerekmeyeni yazma, tekrarlama. **Evidence-before-completion:** doğrulamadan "bitti" deme.
Depoya erişemiyorsan DUR, kullanıcıya bildir — metodolojiyi uydurma.

## 1 — ÇİĞNENMEZ İLKELER
1. **Dürüstlük > Yeşil.** Dolgu/sahte yeşil test = projeye ihanet.
2. **Kanıt > Beyan.** "Yaptım, yeşil" kanıt değildir. Kanıt = HAM komut çıktısı + kaynak kod. Sen kendi
   başarının tek şahidi olamazsın.
3. **Sapmayı bildir.** Plandan sapacaksan DUR, `tools/sapma.txt`'ye yaz, onay bekle. Uydurma.
4. **Arterler kutsal.** A1 Mimari+DI, A2 Domain, A3 MQTT, A4 SQLite, A5 LogBus — bozulmaz, genişler.
5. **Kimlik değişmez.** T### kendi davranışına aittir; davranış değişirse yeni kimlik açılır.

## 2 — İŞ AKIŞI
1. Roadmap'ten sıradaki sürümü al (sıra atlanmaz, numara uydurulmaz; belirsizse Roadmap.md'ye bak).
2. Bu dosyayı yeniden oku. 3. Plan yaz → DUR → onay. 4. Onaydan sonra TDD ile kod.
5. `dotnet run` → DUR → kullanıcı manuel test. 6. "pushla" denmeden push YOK. Her mühürlü sürüm KENDİ commit'i:
   `$git=(gci "$env:LOCALAPPDATA\GitHubDesktop" -Recurse -Filter git.exe -EA 0|select -First 1).FullName; & $git add .; & $git commit -m "feat(v3.0.X): mesaj"; & $git push`

## 3 — TEST
- Gerçek test: üretim kodunu çağırır + sonuca anlamlı assert + başarısız olabilir. Gerisi dolgudur.
- YASAK: `Assert.True(true)`, `dummy1+dummy2`, üretim kodu çağırmayan test, IL şişirme.
- Kırmızı test → KODU düzelt, TESTİ zayıflatma/Skip'leme. Emin değilsen SOR.
- Gerçek altyapı: SQLite gerçek dosya (in-memory yasak), MQTT gerçek broker, `Task.Delay` yerine `WaitAsync`.

## 4 — BEKÇİLER (T001-T011, DOKUNULMAZ)
- Bekçi kaynağına (eşik, sayma, desen) dokunmak SADECE kullanıcı onayıyla. Bekçi kırmızıysa ihlali düzelt, bekçiyi değil.
- T010 eşiği = GÜNCEL test sayısı (her sürümde plan içinde güncellenir; tek test düşüşü bile kırmızı yakmalı).
- **Bekçi ispat yöntemi (SABİT — üç kez derleme hatasıyla başarısız olundu, artık tek yöntem):**
  Bir test metodunun `[Fact]` satırını `//[Fact]` yap (dosyadan SATIR KESME — derleme bozulur, ispat geçersiz olur).
  Bekçiyi koş → **Başarısız:1 HAM çıktı** → `[Fact]`'i geri aç → yeşil HAM çıktı. İkisi de rapora.
  Derleme hatası ≠ kırmızı test. Derlenmeyen ispat, ispat DEĞİLDİR.

## 5 — RAPOR & MÜHÜR
- Rapor SABİT `tools/muhur.ps1` ile üretilir (parametre: sürüm no). Script YENİDEN YAZILMAZ; değişiklik
  sadece kullanıcı onayıyla. Elle rapor = mühür reddi.
- **TÜM raporlar** `Masaüstü\TrainService_Raporlar\v3.0.X\` altına kaydedilir (utf8BOM).
  İçerik = HAM ÇIKTI, özet/beyan değil.
- **Her ek test/doğrulama adımının çıktısı** ayrı bir dosyaya yazılır ve rapordakiyle birlikte bu dizinde
  saklanır. Dosya adı şablonu: `RAPOR_{KONU}_v{MAJOR}{MINOR}{PATCH}.txt`. Örnek:
  - `RAPOR_T010_ISPAT_v3024.txt` — bekçi ispatı KIRMIZI+YEŞİL ham çıktıları
  - `RAPOR_TAM_KOSUM_v3024.txt` — `dotnet test` tam koşum çıktısı
  - `RAPOR_MUHUR.txt` — `muhur.ps1` tarafından üretilen mühür raporu
- Mühür şartları: dolgu=0, tüm testler yeşil+Skip=0, bekçi ispatı (Bölüm 4 yöntemiyle), migration
  Pending yok, F9=snap, sapma güncel (bayat kopya yasak), kullanıcı manuel turu (somut gözlem cümlesi).
- Her 5 sürümde bir (🔍 roadmap'te işaretli): geriye-dönük denetim → `VERSIYON_KONTROL_DENETIMI.txt`.

## 6 — MÜHENDİSLİK
Önce oku sonra yaz. Arayüzü bugün doğru kur, yarın sadece implementasyon ekle. Hot-path'te
(MouseMove/render/fizik) LINQ/tahsis YASAK — O(1) sözlük kullan (`Layers.First` vakası). `Math.Round`
→ `AwayFromZero`. Sıfıra bölme guard'lı. Çekirdek WPF'siz. Kimlikler (katman vb. seed Guid) SABİT olmalı,
her açılışta `NewGuid()` üretme (çizgi-kayboldu vakası). Veri bulunamazsa GİZLEME, göster (güvenlik ağı).
Emin değilsen SOR.

## 7 — YASAKLAR (tekrarı = mühür reddi)
Y1 dolgu assert · Y2 kimlik gaspı · Y3 bekçiyi kandırma (eşik/sayma/desen dahil) · Y4 arteri bozan JSON
kayıt · Y5 kabul edilmiş davranışı sessiz ezme (F9=snap!) · Y6 bayat rapor kopyalama · Y7 elle "yeşil"
raporu · Y8 migration Pending bırakma · Y9 onaysız push/kod · Y10 belirsizlikte uydurma · Y11 mühürsüz
sürüm atlama · Y12 bekçi ispatında dosya keserek derlemeyi bozma (tek yöntem: `//[Fact]`).
