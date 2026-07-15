using System;

namespace TrainService.Core.Abstractions;

/// <summary>
/// Sistemdeki tüm uç donanımların (ESP32, PC vs.) ağ durumlarını izler.
/// LWT (Last Will and Testament) ve Retained mesajlara dayalıdır.
/// </summary>
public interface IDeviceRegistry
{
    /// <summary>
    /// Servisi başlatır ve MQTT dinleyicilerini kaydeder.
    /// </summary>
    void StartListening();

    /// <summary>
    /// Herhangi bir cihazın Online/Offline durumu değiştiğinde tetiklenir. (Cihaz Id, IsOnline)
    /// </summary>
    event Action<string, bool>? OnDeviceStatusChanged;

    /// <summary>
    /// Belirtilen cihazın anlık durumunu döndürür. Bilinmiyorsa false döner.
    /// </summary>
    bool IsDeviceOnline(string deviceId);
}
