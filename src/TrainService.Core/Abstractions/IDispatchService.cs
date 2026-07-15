using System.Threading;
using System.Threading.Tasks;
using TrainService.Core.Messaging.Contracts;

namespace TrainService.Core.Abstractions;

/// <summary>
/// Sistemdeki trenlere ve donanımlara komut gönderip güvenli şekilde onaylanmasını bekler.
/// </summary>
public interface IDispatchService
{
    void StartListening();
    
    /// <summary>
    /// Komutu gönderir ve belirlenen süre içinde donanımdan ACK gelmezse Exception veya false döner.
    /// </summary>
    Task<bool> SendCommandAndWaitAckAsync(CommandDto command, int timeoutMs = 3000, CancellationToken cancellationToken = default);
}
