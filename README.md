# TCP 3.0 (Train Control Platform)

![Platform](https://img.shields.io/badge/Platform-Windows-blue)
![Framework](https://img.shields.io/badge/Framework-.NET%208.0-512BD4)
![UI](https://img.shields.io/badge/UI-WPF%20%2B%20Fluent-success)

TCP (Train Control Platform) 3.0, trenlerin otonom hareketlerini simüle eden, uzaktan komuta sağlayan ve MQTT üzerinden IoT tabanlı haberleşme ile gerçek zamanlı veri akışını yöneten yeni nesil bir masaüstü kontrol merkezidir.

## 🚀 Özellikler

- **Modern Kullanıcı Arayüzü**: WPF ve Wpf.Ui kütüphaneleriyle hazırlanmış karanlık mod ve Mica destekli Fluent tasarım.
- **Modüler Mimari**: Bağımlılık Enjeksiyonu (Dependency Injection) ile ayrıştırılmış; Core, App, Cad, Data, Firmware, Messaging ve Simulation katmanlarından oluşan temiz yapı.
- **Gerçek Zamanlı Haberleşme**: MQTTnet tabanlı Hub yapısıyla cihazlardan ve istasyonlardan gelen verilerin anlık işlenmesi.
- **Merkezi Veritabanı ve Repository**: Entity Framework Core 8 ve SQLite kullanılarak, sistemdeki tüm trenlerin, cihazların ve ray geometrilerinin kalıcı olarak depolanması.
- **Gelişmiş Log Otobüsü (LogBus)**: Uygulama içi terminaline ek olarak, alınan tüm önemli olayların asenkron bir biçimde veritabanındaki `EventLogs` tablosuna aynalanması.

## 🏗️ Mimari Yapı

Proje toplamda **7 katmanlı** bir Clean Architecture (Temiz Mimari) yaklaşımını benimser:
- `TrainService.Core`: Arayüzler (Interfaces), modeller, enumlar ve çekirdek iş kuralları.
- `TrainService.App`: Uygulama sunumu (WPF), MVVM mimarisi ve arayüz servisleri.
- `TrainService.Messaging`: İletişim (MQTT) altyapısı.
- `TrainService.Data`: Veritabanı (EF Core) bağlantıları.
- `TrainService.Simulation`: Tren davranışları ve simülasyon algoritmaları.
- `TrainService.Cad`: CAD dosyalarının okunması, harita motorları.
- `TrainService.Firmware`: Gömülü sistemlere (ESP vb.) dair kod/yardımcı dosyalar.

## ⚙️ Kurulum ve Çalıştırma

### Gereksinimler
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Windows 10/11 (WPF arayüzü nedeniyle)
- MQTT Broker (Örn: Mosquitto - Varsayılan `127.0.0.1:1883`)

### Çalıştırma
Projeyi derlemek ve başlatmak için terminalden aşağıdaki komutları kullanabilirsiniz:

```bash
# Bağımlılıkları yükle ve projeyi derle
dotnet build

# Uygulamayı başlat
dotnet run --project src/TrainService.App/TrainService.App.csproj
```

> **Not:** Windows masaüstündeki `TrainService_Baslat.bat` dosyasını çalıştırarak uygulamayı hızlıca başlatabilirsiniz.

---
*Geliştirme aşamasındadır (v3.0.x).*
