using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using TrainService.Core.Abstractions;

namespace TrainService.Messaging.Health;

public class PingService : IPingService, IDisposable
{
    private readonly ILogBus _logBus;
    private readonly IDeviceRegistry _deviceRegistry;
    
    // Dinamik cihaz IP listesi
    private readonly HashSet<string> _ipList = new(); 
    private CancellationTokenSource? _cts;

    // Önceki durumları tutarak log kirliliğini önleyelim
    private readonly Dictionary<string, bool> _previousStatus = new();

    public PingService(ILogBus logBus, IDeviceRegistry deviceRegistry)
    {
        _logBus = logBus;
        _deviceRegistry = deviceRegistry;
    }

    public void SetIpList(IEnumerable<string> ips)
    {
        _ipList.Clear();
        foreach (var ip in ips)
        {
            if (!string.IsNullOrWhiteSpace(ip))
                _ipList.Add(ip);
        }
    }

    public void StartPinging()
    {
        if (_cts != null) return;
        _cts = new CancellationTokenSource();

        Task.Run(async () => await PingLoopAsync(_cts.Token), _cts.Token);
        _logBus.Info("PingService", "Ağ cihazları için 5 saniyelik ICMP dinleme döngüsü başlatıldı.");
    }

    public void StopPinging()
    {
        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }
    }

    private async Task PingLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                // Superpowers: Task.WhenAll ile non-blocking, paralel ICMP paketleri yolluyoruz.
                var tasks = _ipList.Select(ip => PingIpAsync(ip, token));
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                _logBus.Error("PingService", $"Ping döngüsünde hata: {ex.Message}");
            }

            // 5 saniye bekle
            await Task.Delay(5000, token);
        }
    }

    private async Task PingIpAsync(string ip, CancellationToken token)
    {
        bool isSuccess = false;
        try
        {
            using var ping = new Ping();
            // Timeout 1 saniye. Token iptal edildiyse işlemi abort edemiyoruz Ping eski API ama
            // en fazla 1 saniye bekler.
            var reply = await ping.SendPingAsync(ip, 1000);
            isSuccess = reply.Status == IPStatus.Success;

            LogIfStatusChanged(ip, isSuccess, isSuccess ? $"Ping başarılı: {ip} ({reply.RoundtripTime}ms)" : $"Cihaza ulaşılamadı (Zaman aşımı): {ip}");
        }
        catch (PingException)
        {
            isSuccess = false;
            LogIfStatusChanged(ip, isSuccess, $"Cihaza ulaşılamadı (Ping hatası): {ip}");
        }
        catch (Exception ex)
        {
            isSuccess = false;
            LogIfStatusChanged(ip, isSuccess, $"Beklenmeyen Ping hatası [{ip}]: {ex.Message}");
        }
    }

    public event Action<string, bool>? OnPingStatusChanged;

    private void LogIfStatusChanged(string ip, bool currentStatus, string message)
    {
        _previousStatus.TryGetValue(ip, out var previousStatus);
        
        // Sadece durum değiştiğinde log bas (veya ilk kez ping atılıyorsa)
        if (!_previousStatus.ContainsKey(ip) || previousStatus != currentStatus)
        {
            _previousStatus[ip] = currentStatus;
            
            if (currentStatus)
                _logBus.Success("PingService", message);
            else
                _logBus.Error("PingService", message);
                
            OnPingStatusChanged?.Invoke(ip, currentStatus);
        }
    }

    public void Dispose()
    {
        StopPinging();
    }
}
