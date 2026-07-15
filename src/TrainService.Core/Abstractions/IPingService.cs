namespace TrainService.Core.Abstractions;

/// <summary>
/// Sistemdeki ağ cihazlarına periyodik olarak ICMP (Ping) paketi gönderip canlılıklarını teyit eder.
/// </summary>
public interface IPingService
{
    /// <summary>
    /// Ping atma işlemini bir arka plan görevinde (Task) sürekli olarak başlatır.
    /// </summary>
    void StartPinging();
    
    /// <summary>
    /// Ping atma işlemini güvenli bir şekilde durdurur.
    /// </summary>
    void StopPinging();
}
