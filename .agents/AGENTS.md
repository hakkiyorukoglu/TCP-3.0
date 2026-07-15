# TCP 3.0 — Agent Kuralları (Superpowers Framework)
# Kaynak: https://github.com/obra/superpowers
# Son güncelleme: 2026-07-15

---

## Proje Bağlamı

Bu proje **Restoran Otonom Tren Servis Otomasyonu (TCP) v3.0** projesidir.
Tüm çalışma `Roadmap hy.md` dosyasına **tamamen bağlı** kalarak yürütülür.
Roadmap'teki versiyon sırası, kabul kriterleri ve mimari kararlar **değiştirilemez**.

**Referans Doküman:** `C:\Users\yoruk\Desktop\TCP 3.0\Roadmap hy.md`

---

## 1. Verification Before Completion (Tamamlanma Öncesi Doğrulama)

### Demir Kural
```
TAZE DOĞRULAMA KANITI OLMADAN HİÇBİR TAMAMLANMA İDDİASINDA BULUNMA
```

### Kapı Fonksiyonu
```
HERHANGİ BİR DURUM İDDİASI VEYA MEMNUNİYET İFADE ETMEDEN ÖNCE:

1. TESPİT ET: Hangi komut bu iddiayı kanıtlar?
2. ÇALIŞTIR: TAM komutu çalıştır (taze, eksiksiz)
3. OKU: Tam çıktıyı oku, çıkış kodunu kontrol et, hata sayısını say
4. DOĞRULA: Çıktı iddiayı doğruluyor mu?
   - HAYIR ise: Kanıtla birlikte gerçek durumu bildir
   - EVET ise: Kanıtla birlikte iddiada bulun
5. ANCAK O ZAMAN: İddiada bulun

Herhangi bir adımı atlamak = doğrulama değil, yalan
```

### Yaygın Hatalar
| İddia | Gerekli | Yeterli Değil |
|-------|---------|---------------|
| Testler geçiyor | Test komutu çıktısı: 0 hata | Önceki çalıştırma, "geçmeli" |
| Build başarılı | Build komutu: exit 0 | Kısmi kontrol, tahmin |
| Bug düzeltildi | Orijinal semptomu test et: geçer | Kod değişti, düzeltildi varsay |

### Kırmızı Bayraklar — DUR
- "Olmalı", "muhtemelen", "görünüyor" kullanmak
- Doğrulamadan önce memnuniyet ifade etmek ("Harika!", "Mükemmel!", "Bitti!")
- Doğrulama olmadan commit/push yapmak

---

## 2. Test-Driven Development (Test Güdümlü Geliştirme)

### Demir Kural
```
ÖNCE BAŞARISIZ TEST OLMADAN HİÇBİR ÜRETİM KODU YAZMA
```

### Red-Green-Refactor Döngüsü

1. **RED — Başarısız Test Yaz**
   - Bir minimal test yaz, ne olması gerektiğini göster
   - Tek davranış, net isim, gerçek kod (mock kaçınılmazsa hariç)

2. **RED Doğrula — Başarısız Olduğunu İzle**
   - ZORUNLU. Asla atlama.
   - Test başarısız oluyor mu (hata değil)?
   - Başarısızlık mesajı beklenen mi?

3. **GREEN — Minimal Kod**
   - Testi geçirmek için en basit kodu yaz
   - Özellik ekleme, başka kod refactor etme

4. **GREEN Doğrula — Geçtiğini İzle**
   - ZORUNLU.
   - Test geçiyor mu? Diğer testler hâlâ geçiyor mu?

5. **REFACTOR — Temizle**
   - Sadece yeşilden sonra
   - Tekrarı kaldır, isimleri iyileştir
   - Testleri yeşil tut, davranış ekleme

### İstisna (TCP Projesi İçin)
- v3.0.0 gibi iskelet kurulum versiyonlarında TDD uygulanmaz (test edilecek iş mantığı yok)
- v3.0.4'ten itibaren (domain modelleri) TDD zorunludur

---

## 3. Systematic Debugging (Sistematik Hata Ayıklama)

### Demir Kural
```
ÖNCE KÖK NEDEN ARAŞTIRMASI OLMADAN HİÇBİR DÜZELTME YAPMA
```

### Dört Faz

**Faz 1: Kök Neden Araştırması** (Düzeltme denemeden ÖNCE)
1. Hata mesajlarını dikkatlice oku — atlama
2. Tutarlı şekilde yeniden üret
3. Son değişiklikleri kontrol et
4. Çok bileşenli sistemlerde her bileşen sınırında kanıt topla
5. Veri akışını izle

**Faz 2: Kalıp Analizi**
1. Çalışan örnekleri bul
2. Referanslarla karşılaştır
3. Farkları tespit et

**Faz 3: Hipotez ve Test**
1. Tek hipotez oluştur: "X'in kök neden olduğunu düşünüyorum çünkü Y"
2. Mümkün olan EN KÜÇÜK değişiklikle test et

**Faz 4: Düzeltme**
- Sadece doğrulanmış kök nedene yönelik düzelt

---

## 4. Writing Plans (Plan Yazma)

### Genel Kurallar
- Kod yazan mühendis sıfır bağlam biliyormuş gibi kapsamlı plan yaz
- Her görevi bite-sized (2-5 dakika) adımlara böl
- DRY, YAGNI, TDD ilkelerine uy
- TCP projesi için plan her zaman Roadmap'teki sürüm numarasına bağlı olmalı

### Görev Boyutlandırma
Bir görev, kendi test döngüsünü taşıyan en küçük birimdir:
- "Başarısız testi yaz" — adım
- "Çalıştır, başarısız olduğunu gör" — adım
- "Testi geçirmek için minimal kodu yaz" — adım
- "Testleri çalıştır, geçtiğini gör" — adım

---

## 5. Executing Plans (Plan Uygulama)

### Süreç
1. **Planı Yükle ve İncele**
   - Planı oku, eleştirel incele
   - Endişeler varsa: Devam etmeden önce kullanıcıya bildir
   - Endişe yoksa: Todo listesi oluştur ve devam et

2. **Görevleri Uygula**
   - Her görev için: in_progress → adımları takip et → doğrula → completed

3. **Geliştirmeyi Tamamla**
   - Tüm görevler tamamlandığında son doğrulama

---

## 6. TCP 3.0 Projesine Özel Kurallar

### Roadmap Bağlılığı
- Her versiyon (v3.0.x) Roadmap'teki sıraya göre uygulanır
- Hiçbir versiyonun kabul kriteri atlanamaz
- Önceki versiyonun public API'si kırılamaz; sadece ekleme yapılır

### Ana Arter Koruması
- 5 Ana Arter (A1-A5) ilk fazlarda eksiksiz kurulur, proje boyunca **asla** yeniden yazılmaz
- `Core` projesi hiçbir projeye referans vermez
- UI projesi asla Data/Messaging'e doğrudan referans vermez

### Bağımlılık Yönü (Asla Ters Akmaz)
```
App → (Cad, Messaging, Data, Firmware, Simulation) → Core
```

### Simülasyon Sözleşmesi (Digital Twin)
- Simülasyon motoru sanal MQTT istemcileri olarak bağlanır
- Gerçek ESP32 ile aynı konuları kullanır
- Simülasyonun en sona bırakılması mimari borç yaratmaz

### MQTT Konu Sözleşmesi
- v3.0.4'te dondurulur
- Sadece yeni konu **eklenebilir**, mevcut konu **değiştirilemez**

### SQLite Şema Disiplini
- Kolon silinmez/yeniden adlandırılmaz
- Her değişiklik EF Core migration'ı olarak eklenir
- WAL modu zorunlu

### Loglama
- Tüm loglama `ILogBus` arayüzü ile yapılır
- Hiçbir servis `Console.WriteLine` veya doğrudan UI'ya yazamaz

### Git Versiyon Kontrolü
- **Repo:** https://github.com/hakkiyorukoglu/TCP-3.0
- **Git path:** `C:\Users\yoruk\AppData\Local\GitHubDesktop\app-3.6.2\resources\app\git\cmd\git.exe`
- Her versiyon (v3.0.x) tamamlandığında ve kabul kriterleri doğrulandığında:
  1. `dotnet run` komutu ile uygulamayı çalıştır (`run_command` arka plan görevi olarak).
  2. Kullanıcıya programı incelemesini söyle ve "pushla" diyene kadar BEKLE.
  3. `git add -A`
  4. `git commit -m "v3.0.x: <versiyon açıklaması>"`
  5. `git tag v3.0.x`
  6. `git push origin main --tags`
- Commit mesajları her zaman versiyon numarasıyla başlar
- PATH ayarı: `$env:PATH = "C:\Users\yoruk\AppData\Local\GitHubDesktop\app-3.6.2\resources\app\git\cmd;$env:PATH"`

---

## 7. Skill Öncelik Sırası

İlgili skill'leri yanıt veya eylemden ÖNCE çağır:
- "Bir şey inşa edelim" → Önce plan yaz, sonra uygula
- "Bu hatayı düzelt" → Önce sistematik hata ayıklama
- "Bu tamamlandı" → Önce doğrulama

---

> **Kaynak:** https://github.com/obra/superpowers
> **Uyarlama:** TCP 3.0 Restoran Otonom Tren Servis Otomasyonu projesi için özelleştirilmiştir.
