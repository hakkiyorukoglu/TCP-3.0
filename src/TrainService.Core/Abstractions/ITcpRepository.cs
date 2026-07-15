using System.Collections.Generic;
using System.Threading.Tasks;
using TrainService.Core.Entities;

namespace TrainService.Core.Abstractions;

public interface ITcpRepository
{
    Task<List<Device>> GetAllDevicesAsync();
    Task AddDeviceAsync(Device device);
    
    Task AddLogAsync(EventLog log);
    Task<List<EventLog>> GetLogsAsync(int count = 100);

    Task SaveChangesAsync();
}
