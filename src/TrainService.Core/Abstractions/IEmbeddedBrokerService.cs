using System.Threading.Tasks;

namespace TrainService.Core.Abstractions;

public interface IEmbeddedBrokerService
{
    Task StartAsync(int port = 1883);
    Task StopAsync();
}
