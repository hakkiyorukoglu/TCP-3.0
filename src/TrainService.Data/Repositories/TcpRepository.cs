using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TrainService.Core.Abstractions;
using TrainService.Core.Entities;

namespace TrainService.Data.Repositories;

public class TcpRepository : ITcpRepository
{
    private readonly TrainDbContext _context;

    public TcpRepository(TrainDbContext context)
    {
        _context = context;
    }

    public async Task<List<Device>> GetAllDevicesAsync()
    {
        return await _context.Devices.ToListAsync();
    }

    public async Task AddDeviceAsync(Device device)
    {
        await _context.Devices.AddAsync(device);
    }

    public async Task AddLogAsync(EventLog log)
    {
        await _context.EventLogs.AddAsync(log);
    }

    public async Task<List<EventLog>> GetLogsAsync(int count = 100)
    {
        return await _context.EventLogs
            .OrderByDescending(x => x.Timestamp)
            .Take(count)
            .ToListAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
