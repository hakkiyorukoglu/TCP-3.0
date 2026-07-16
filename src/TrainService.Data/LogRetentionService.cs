using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace TrainService.Data;

public class LogRetentionService
{
    public async Task TrimAsync(TrainDbContext db, int maxCount)
    {
        var count = await db.EventLogs.CountAsync();
        if (count > maxCount)
        {
            var toDelete = count - maxCount;
            // Get oldest records based on Ts
            var oldLogs = await db.EventLogs
                .OrderBy(e => e.Ts)
                .Take(toDelete)
                .ToListAsync();

            db.EventLogs.RemoveRange(oldLogs);
            await db.SaveChangesAsync();
        }
    }
}
