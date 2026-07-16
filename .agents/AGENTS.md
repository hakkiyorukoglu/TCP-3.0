# AGENTS.md — TrainService (TCP) Proje Anayasası

> Bu dosya projenin TEK otoritesidir. Roadmap, kurallar, Superpowers metodolojisi, test/rapor
> disiplini — hepsi buradadır. Ayrı `Roadmap hy.md` dosyasına ARTIK gerek yoktur; roadmap Bölüm 8'de
> gömülüdür. Bir çelişkide bu dosya geçerlidir. Kurallar, 19 sürümlük gerçek geliştirme deneyiminden
> (dolgu-assert, kimlik-gaspı, JSON sapması, F9 regresyonu gibi gerçek yaralardan) türetilmiştir.

---

## BÖLÜM 0 — TEMEL İLKELER (her şeyin üstünde)

**İlke 1 — Dürüstlük > Yeşil.** Kırmızı ama dürüst bir test, yeşil ama sahte bir testten sonsuz kez iyidir.
Testi geçirmek için gövdeyi zayıflatmak, doldurmak veya bekçiyi kandırmak PROJEYE İHANETTİR.

**İlke 2 — Kanıt > Beyan.** "Yaptım, çalışıyor, hepsi yeşil" kanıt DEĞİLDİR. Kanıt; ham komut çıktısı,
kaynak kod gövdesi ve script tarafından üretilmiş rapordur. Ajan asla kendi başarısının tek şahidi olamaz.

**İlke 3 — Sapmayı gizleme, bildir.** Plandan/talimattan sapman gerekiyorsa DUR, `tools/sapma.txt`'ye yaz,
kullanıcı onayını bekle. Kendi çözümünü uydurma. (JSON-kayıt ve dummy-assert felaketleri bu ilkenin ihlalinden doğdu.)

**İlke 4 — Ana arterler kutsaldır.** 5 arter (A1 Katmanlı Mimari+DI, A2 Domain Modelleri, A3 MQTT Sözleşmesi,
A4 SQLite Şeması, A5 Log Otobüsü) asla yeniden yazılmaz, sadece genişletilir.

**İlke 5 — Kimlik değişmezdir.** Bir test kimliği (T###) yalnızca kendi tanımlı davranışına aittir.
Kimliğin altına başka davranış yazmak (kimlik gaspı) yasaktır. Davranış değişecekse yeni kimlik açılır.

---

## BÖLÜM 1 — SUPERPOWERS METODOLOJİSİ (obra/superpowers'tan gömülü)

Kaynak: https://github.com/obra/superpowers (v6.1.1). Aşağıdaki kurallar o metodolojinin bu projeye
uyarlanmış, BAĞLAYICI halidir. Ajan her görevden önce ilgili beceriyi (skill) kontrol eder — bunlar
öneri değil, zorunlu iş akışlarıdır.

### 1.1 — Çekirdek Felsefe
- **Test-Driven Development (TDD):** Önce test yaz, HER ZAMAN. RED-GREEN-REFACTOR döngüsü:
  (1) başarısız test yaz, (2) başarısız olduğunu GÖR, (3) minimum kodu yaz, (4) geçtiğini GÖR, (5) commit.
  Testten ÖNCE yazılmış kod silinir. ("Watch it fail" adımı atlanamaz — test gerçekten kırmızı olmalı.)
- **Systematic over ad-hoc:** Tahmin değil, süreç. Hata ayıklamada 4-fazlı kök-neden analizi (semptomu
  yamamak yerine kökü bul).
- **Complexity reduction / YAGNI:** İhtiyacın olmayanı yazma. Basitlik birincil hedeftir. DRY (kendini tekrarlama).
- **Evidence over claims:** Başarıyı ilan etmeden önce DOĞRULA (verification-before-completion).

### 1.2 — 7 Adımlı İş Akışı (Superpowers çekirdeği)
1. **brainstorming** — Kod yazmadan ÖNCE. Fikri sorularla netleştir, alternatifleri araştır, tasarımı
   okunabilir parçalar halinde sun, onay al. Tasarım belgesini kaydet.
2. **worktree/branch** — Tasarım onayından sonra izole çalışma alanı; temiz test tabanını doğrula.
3. **writing-plans** — Onaylı tasarımdan, "yargısı olmayan hevesli bir junior mühendisin" izleyebileceği
   kadar net plan. Her görev 2-5 dakikalık, tam dosya yolu + tam kod + doğrulama adımı içerir.
4. **executing-plans** — Planı görev görev uygula; her görev arası inceleme.
5. **test-driven-development** — Uygulama sırasında RED-GREEN-REFACTOR zorla.
6. **requesting-code-review** — Görevler arası plana göre incele; kritik sorunlar ilerlemeyi bloke eder.
7. **finishing** — Görevler bitince testleri doğrula, seçenekleri sun (merge/PR/tut), temizle.

### 1.3 — Testing Anti-Pattern'leri (Superpowers TDD becerisinden)
- Testi geçirmek için üretim kodunu değil, önce testi yazıp KIRMIZI gör.
- "Her zaman geçen" test, test değildir (bkz. Bölüm 3.2).
- Mock'a değil gerçek davranışa test et (mümkün olduğunca).

---

## BÖLÜM 2 — İŞ AKIŞI (STRICT WORKFLOW)

Her özellik geliştirmede bu sıra ZORUNLUDUR; adım atlanamaz:

1. **Ön Kontrol:** Bu AGENTS.md'nin Bölüm 8'indeki ROADMAP okunur; tam olarak nerede olduğumuz ve
   sıradaki sürümün ADI+KAPSAMI belirlenir. Sürüm numarası UYDURULMAZ.
2. **Kural Tazeleme:** Bu AGENTS.md yeniden okunur (özellikle Bölüm 3, 4, 8).
3. **Planlama:** `implementation_plan.md` yazılır ve **DURULUR**. Plan; kapsam sınırı (VAR/YOK listeleri),
   dosya-dosya değişiklik, yazılacak testlerin KİMLİK+DAVRANIŞ tablosu, kabul kriteri, manuel test
   maddelerini içerir. Kullanıcı onayı beklenir.
4. **Uygulama:** SADECE onaydan sonra kod yazılır. Kod ve testleri BİRLİKTE yazılır (TDD: test önce).
5. **Uygulama Doğrulama:** İş bitince `dotnet run` ile uygulama açılır ve **DURULUR**. Kullanıcıdan
   ekranda manuel test istenir (M-serisi, plandan gelir).
6. **Kesinleştirme & Push:** SADECE kullanıcı "pushla" dedikten sonra README yazılır ve push edilir.
   Onaydan önce push YASAK.
   - **Git Push Yöntemi:** Global `git` yok. GitHub Desktop gömülü git kullanılır:
     ```powershell
     $git = (Get-ChildItem -Path "$env:LOCALAPPDATA\GitHubDesktop" -Recurse -Filter "git.exe" -ErrorAction SilentlyContinue | Select-Object -First 1).FullName; & $git add .; & $git commit -m "feat(v3.0.X): mesaj"; & $git push
     ```
   - **Commit disiplini:** Her mühürlenen sürüm KENDİ commit'i olur (`feat(v3.0.X): ...`). Birden çok
     sürümü tek commit'te yığma — git geçmişi denetim aracıdır, sürüm sürüm okunabilmeli.

---

## BÖLÜM 3 — TEST YAZMA ANAYASASI (EN KRİTİK BÖLÜM)

### 3.1 — Kesinlikle YASAK testler (tespit = mühür reddi)
- `Assert.True(true)`, `Assert.Equal(1, 1)`, `Assert.Equal(3, dummy1 + dummy2)` ve benzeri kendine-referanslı assert.
- Üretim kodunu HİÇ çağırmayan `[Fact]`/`[Theory]` (sadece yerel değişkenle oynayan).
- `int dummy1 = ...;` gibi yalnızca IL-byte şişirmek için var olan satırlar.
- İskelet/placeholder testler (`Test1`, `UnitTest1`) — SİLİNİR, dürüst boşluk bırakılır.

### 3.2 — Gerçek testin tanımı
Bir test GERÇEKTİR ancak: (a) en az bir üretim tipini/metodunu çağırıyorsa, (b) sonucu üzerinde anlamlı
assert yapıyorsa, (c) başarısız olabilecek gerçek bir koşulu sınıyorsa. "Her zaman geçen" test, test değildir.

### 3.3 — Testi geçirme kuralı (altın kural)
Kırmızı test için İKİ meşru seçenek: (1) üretim kodu hatalıdır → düzelt (+ sapma.txt'ye yaz);
(2) test yanlış yazılmıştır → DUR, kullanıcıya sor. Testi geçirmek için gövdeyi zayıflatmak, assert
sulandırmak, gerekçesiz `Skip`, veya dolgu eklemek YASAKTIR. Teste değil, KODA dokunulur.

### 3.4 — Kimlik disiplini
T### kimliği kendi davranışını uygular. Metodun davranışı kimliğiyle uyuşmuyorsa doğru kimliğe TAŞINIR
(silinmez), gerçek davranış yeni yazılır. CRUD/smoke gibi kimliksiz testler T### öneki TAŞIYAMAZ.

### 3.5 — Gerçek altyapı zorunluluğu
- SQLite testleri gerçek geçici DOSYA (`TempSqliteFixture`); in-memory provider YASAK.
- MQTT testleri gerçek gömülü broker + gerçek MQTTnet istemcisi (rastgele boş port).
- `Task.Delay(sabit)` YASAK → `TaskCompletionSource` + `WaitAsync(timeout)`.
- Kopuş simülasyonu: `DisconnectAsync` DEĞİL, soketi `Dispose` ile kaba kesme.

### 3.6 — Kapsam bekçileri (kalıcı, silinemez)
- **T010 (Kapsam Geriletme Bekçisi):** Test sayısı kalibre tabanın altına DÜŞEMEZ. Bilinçli düşüş
  (konsolidasyon/silme) → taban gerekçesiyle güncellenir + sapma.txt.
- **T011 (Sahte Test Bekçisi):** IL-byte eşiği (≤12B) + dolgu-desen taraması. Kendi gövdesi gerçek olmalı.
- **Mimari Bekçiler (T001–T008):** NetArchTest ile GERÇEK bağımlılık/katman/WPF-sızıntı kontrolü.

### 3.7 — Bekçinin bekçiliği (her mühürde ZORUNLU ritüel)
Kasıtlı ihlal eklenir → bekçi KIRMIZI yanar (çıktı rapora) → ihlal geri alınır → yeşile döner.
İspat edilmemiş bekçi, çalıştığı varsayılamaz. (ZZ sahte-test kalıbı GEÇERLİ C# olmalı — attribute METODA konur.)

---

## BÖLÜM 4 — RAPORLAMA VE MÜHÜR PROTOKOLÜ

### 4.1 — Rapor klasör yapısı (YENİ — masaüstünde sürüm-adlı klasör)
Her sürüm için Masaüstünde `TrainService_Raporlar\v3.0.X\` klasörü oluşturulur. Rapor script'i
çıktısını BU KLASÖRE yazar:
- `v3.0.X\RAPOR_MUHUR.txt` — o sürümün mühür kanıt raporu (9 bölüm, aşağıda).
- `v3.0.X\test_kosum.txt` — ham tam koşum çıktısı.
- (5 sürümde bir) `v3.0.X\VERSIYON_KONTROL_DENETIMI.txt` — bkz. 4.6.

Rapor script'i (`tools/muhur-v3.0.X.ps1`) çıktıyı elle DEĞİL script ile üretir. Encoding: `utf8BOM`
(Türkçe karakter bozulmasın). Script çıktısını elle düzenlemek/kısaltmak = MÜHÜR REDDİ.

### 4.2 — Mühür raporu ZORUNLU bölümleri (9 bölüm)
1. **Sözleşme/diff kanıtı:** kritik dosyalar (SnapEngine gibi) değişmedi mi (git diff).
2. **Kimlikli test gövdeleri:** kritik T### testlerinin KAYNAK KODU (denetçi gözle okur).
3. **Snap/provider/altyapı kodları** (o sürüme özgü üretim kodu).
4. **Tam koşum:** `dotnet test -c Release --logger "console;verbosity=detailed"` — ADLARIYLA, Fail=0.
5. **Dolgu taraması:** `dummy`, `Assert.True(true)`, `Assert.Equal(3,` → **SIFIR satır**.
6. **Bekçi ispatı:** sahte test eklendiğinde T011 KIRMIZI, silinince yeşil (iki çıktı da rapor içinde).
7. **Render/görsel kod teyidi** (varsa).
8. **Arter kanıtları:** JSON söküm, F9=snap, migration listesi (Pending YOK).
9. **Sapma beyanı:** bu turun GERÇEK sapmaları. Bayat/kopyala-yapıştır YASAK — her rapor sıfırdan.

### 4.3 — Bayat içerik yasağı
Önceki raporlardan bölüm/sapma kopyalamak yasaktır. Raporun içinde çelişki (örn. "T302 dummy kaldı"
derken aynı turda T302'yi gerçek yazmak) = mühür reddi.

### 4.4 — Mühür kabul kriterleri (hepsi zorunlu)
- [ ] Dolgu taraması sıfır satır. Tüm testler yeşil, Skip=0.
- [ ] Bekçi ispatı geçerli (kırmızı→yeşil gösterildi).
- [ ] Kritik test gövdeleri davranışla eşleşiyor (sulandırma yok, süreler gerçekçi).
- [ ] Arter kanıtları temiz (JSON=0, F9=snap, migration Pending yok, mimari bekçiler gerçek).
- [ ] Sapma beyanı güncel ve tutarlı.
- [ ] Kullanıcının manuel turu (M-serisi) tamamlandı — test adı referansı manuel madde için kanıt SAYILMAZ;
      her M-maddesi için somut gözlem cümlesi gerekir.

### 4.5 — Migration disiplini
Şema sadece ileri gider; kolon silinmez/yeniden adlandırılmaz. Yanlış tablonun düzeltmesi bile ileri
migration'dır. Her migration eklendiğinde `dotnet ef database update`; rapor anında hiçbir migration
"Pending" olamaz. `IDesignTimeDbContextFactory` mevcut olmalı (araçlar tasarım-zamanında context kurabilsin).

### 4.6 — 5 SÜRÜMDE BİR VERSİYON KONTROL DENETİMİ (YENİ)
Her 5 sürümde bir (v3.0.20, v3.0.25, v3.0.30, ...) o sürümün mühür raporuna EK olarak, tam bir
geriye-dönük denetim yapılır ve `v3.0.X\VERSIYON_KONTROL_DENETIMI.txt` dosyasına yazılır. İçeriği:
- Son 5 sürümün roadmap sözü ↔ gerçek kod karşılaştırması (her biri ✅/⚠️/❌ + dosya:satır).
- 5 arterin (A1–A5) bütünlük kontrolü.
- Tüm testlerin gerçeklik taraması (dolgu, kimlik-gaspı) — sadece son sürüm değil, TÜM proje.
- Teknik borç envanteri güncellemesi.
Bu denetim, projenin "sessizce çürümediğini" 5 sürümde bir kanıtlar. Roadmap'te (Bölüm 8) bu duraklar
🔍 işaretiyle gösterilmiştir — ajan o sürüme gelince denetimi UNUTMAZ.

---

## BÖLÜM 5 — 10x MÜHENDİS DAVRANIŞI

1. **Önce oku, sonra yaz.** Değiştirmeden önce mevcut dosyayı oku, deseni koru.
2. **Küçük, atomik commit.** Her mikro-sürüm tek başına derlenir ve testleri yeşildir.
3. **Arayüzü bugün doğru kur.** "Şimdi basit, sonra düzeltiriz" YASAK — genişleyecek arayüz bugünden
   nihai kurulur, sonra sadece implementasyon EKLENİR. (Snap provider zinciri bu ilkenin zaferidir.)
4. **Matematik tuzakları:** `Math.Round` varsayılanı ToEven'dır → grid/snap için `AwayFromZero`.
   Sıfıra bölme (GradePercent, Normalize, DistanceToSegment) her zaman guard'lanır (NaN/Infinity üretme).
5. **Hot-path'te tahsis yok.** MouseMove/render/fizik döngüsünde LINQ, liste tahsisi, string yasak.
6. **UI'sız çekirdek.** Cad/Core/Snap/Tool mantığı WPF tanımaz (headless test edilebilir).
7. **Emin değilsen SOR.** Belirsizlikte uydurma; dur, sapma.txt'ye yaz veya kullanıcıya sor.

---

## BÖLÜM 6 — YASAKLAR LİSTESİ (geçmiş yaralardan; tekrarı = mühür reddi)

| # | Yasak | Kaynak yara |
|---|-------|-------------|
| Y1 | Dolgu assert (`dummy1+dummy2`, `Assert.True(true)`) | 90 sahte test vakası |
| Y2 | Kimlik gaspı (T### altına başka davranış) | T5xx→CRUD, T204→sahte |
| Y3 | Bekçiyi kandırma (IL şişirme, tarama-baypas) | T011 dolgu-aşımı |
| Y4 | Arteri bozan pratik çözüm (geometriyi JSON'a serileştirme) | CadProjectEntity vakası |
| Y5 | Kabul edilmiş davranışı sessiz ezme (F9 snap→araç) | 3 kez tekrarlanan F9 regresyonu |
| Y6 | Bayat rapor/sapma kopyalama | çelişkili "T302 dummy kaldı" beyanı |
| Y7 | Elle "hepsi yeşil" raporu (script çıktısı yerine) | sayıya-inan turları |
| Y8 | Migration'ı Pending bırakma | AddMissingTables vakası |
| Y9 | Onaysız push / plan onayı almadan kod yazma | workflow ihlali |
| Y10 | Belirsizlikte kendi çözümünü uydurma (sormak yerine) | tüm yaraların ortak kökü |
| Y11 | Mühürlenmemiş sürümü atlayıp sonrakine geçmek | v3.0.18 atlanıp v3.0.19'a geçilmesi |

---

## BÖLÜM 7 — HER SÜRÜM İÇİN ÇIKTI KONTROL LİSTESİ

Planı sunmadan önce: Roadmap'teki sürüm adı+kapsamı doğru mu? Kapsam sınırı (VAR/YOK) yazıldı mı?
Test kimlik+davranış tablosu var mı? Hangi arteri genişletiyor, hiçbirini kırmıyor mu?

Kodu bitirdikten sonra: Her yeni test gerçek mi (3.2)? Dolgu taraması sıfır mı? Kırmızı test için
kodu mu düzelttim testi mi zayıflattım (cevap: kod)? Bekçiler gerçek + ispat ritüeli yapıldı mı?
Migration Pending kaldı mı? Rapor doğru klasöre (v3.0.X\) yazıldı mı? Kabul edilmiş davranışları (F9) bozdum mu?
Bu listede tek "hayır" varsa sürüm mühürlenmez.

---

## BÖLÜM 8 — ROADMAP (gömülü — ayrı Roadmap dosyasına gerek yok)

**Sürümleme:** Her v3.0.X bağımsız, derlenebilir, geri dönüşsüz mikro-sürüm. Sıra atlanmaz (Y11).
**Mühür işareti:** `(MÜHÜRLENDİ)` = denetimden + manuel turdan geçti. 🔍 = 5-sürümlük denetim durağı.

### FAZ 0 — Temel İskelet ve Ana Arterler I
- v3.0.0 Solution + 7 proje + tests **(MÜHÜRLENDİ)**
- v3.0.1 Generic Host + DI **(MÜHÜRLENDİ)**
- v3.0.2 Shell: NavigationView, Dark Mica, 6 sayfa **(MÜHÜRLENDİ)**
- v3.0.3 A5 — ILogBus + TerminalPanel **(MÜHÜRLENDİ)**

### FAZ A — Ana Arterler II (Domain + SQLite)
- v3.0.4 A2+A3: Geometry, Entity'ler, Topic Contract **(MÜHÜRLENDİ)**
- v3.0.5 A4: TrainDbContext + 19 tablo + WAL **(MÜHÜRLENDİ)**
- v3.0.6 Repository katmanı **(MÜHÜRLENDİ)**
- v3.0.7 SettingsView **(MÜHÜRLENDİ)**

### FAZ B — Gömülü MQTT + Cihaz Kaydı
- v3.0.8 EmbeddedBrokerService + MqttHub **(MÜHÜRLENDİ)**
- v3.0.9 DeviceRegistry (LWT + heartbeat) **(MÜHÜRLENDİ)**
- v3.0.10 PingService + DeviceHealth **(MÜHÜRLENDİ)**
- v3.0.11 DispatchService (komut+ack) **(MÜHÜRLENDİ)**

### FAZ C — ElectronicsView
- v3.0.12 Ağ modeli CRUD **(MÜHÜRLENDİ)**
- v3.0.13 Node-based şema tuvali **(MÜHÜRLENDİ)**
- v3.0.14 Canlı durum bağlama (LED) **(MÜHÜRLENDİ)**

### FAZ D — CAD Editörü
- v3.0.15 CadViewportControl (DrawingVisual, pan/zoom) **(MÜHÜRLENDİ)**
- v3.0.16 CadDocument + CommandStack + SelectionService **(MÜHÜRLENDİ)**
- v3.0.17 SnapEngine v1: GridSnap + işaretçi **(MÜHÜRLENDİ)**
- v3.0.18 TrackTool: tıkla-tıkla ray + Ctrl+S ilişkisel kayıt **(MÜHÜRLENDİ)**
- v3.0.19 SnapEngine v2: Endpoint+OnSegment+SpatialHash **(MÜHÜRLENDİ)**
- v3.0.20 TrackGraph: topoloji grafı, komşuluk, rota doğrulama, blok bölme 🔍 **(MÜHÜRLENDİ)**
- **v3.0.21 SelectTool + Marquee seçim (Window/Crossing, Hover, Delete)** **(MÜHÜRLENDİ)**
- **v3.0.22 Pano: Ctrl+C/X/V** ← SIRADAKİ
- v3.0.23 Katmanlar (Zemin/Alt/Üst)
- v3.0.24 RouteTool (Hat çizimi + yön okları)
- v3.0.25 HybridTool (eşzamanlı Track+Route) 🔍
- v3.0.26 Makas nesnesi
- v3.0.27 Rampa nesnesi
- v3.0.28 Feature Tree (unsur ağacı)
- v3.0.29 Sağ tık Radyal Menü

### FAZ E — Donanım-Ray Eşleme
- v3.0.30 HardwareEndpoint + elastik lastik çizgiler 🔍
- v3.0.31 BindTool (sürükle-bırak + HardwareBindings)
- v3.0.32 Tutarlılık denetçisi

### FAZ F — Firmware + OTA
- v3.0.33 PlatformIO workspace + non-blocking C++ çekirdek
- v3.0.34 FirmwareGenerator (Scriban → config.h/main.cpp) 🔍
- v3.0.35 BuildService (pio CLI)
- v3.0.36 OtaUploader + Verify

### FAZ G — Senaryolar, Mutfak, State Recovery
- v3.0.37 Senaryo CRUD
- v3.0.38 KitchenView 🔍
- v3.0.39 HomeView + E-Stop
- v3.0.40 State Recovery (auto-load + makas senkron)
- v3.0.41 BlockSignalController (gerçek mod)

### FAZ H — SİMÜLASYON MOTORU (FINAL)
- v3.0.42 SimulationLoop (sabit Δt, accumulator) 🔍
- v3.0.43 ArcLengthPath (s-parametrizasyon)
- v3.0.44 TrainDynamics (ivme/fren/sürtünme/rampa)
- v3.0.45 Virtual ESP32/Train (C++ SM eşleniği)
- v3.0.46 Lazer engel + BlockSignal sim 🔍
- v3.0.47 Senaryo oynatıcı (Digital Twin tam)
- v3.0.48 Sertleştirme + test finali → **v3.1.0 Release**

**5-sürüm denetim durakları (🔍):** v3.0.20, .25, .30, .34/.38 civarı, .42, .46. Bu sürümlere gelince,
mühür raporuna EK olarak Bölüm 4.6'daki tam geriye-dönük denetim yapılır ve `v3.0.X\VERSIYON_KONTROL_DENETIMI.txt`
dosyasına yazılır. UNUTMA: 🔍 gördüğün sürümde, normal mühür + 5-sürüm denetimi İKİSİNİ birden yaparsın.
