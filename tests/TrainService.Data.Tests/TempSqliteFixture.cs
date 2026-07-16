using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using TrainService.Data;

namespace TrainService.Data.Tests;

public class TempSqliteFixture : IDisposable
{
    private readonly string _dbPath;

    public TempSqliteFixture()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"TrainService_Test_{Guid.NewGuid():N}.db");
        using var context = CreateContext();
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
    }

    public TrainDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TrainDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;
            
        var ctx = new TrainDbContext(options);
        // WAL modunu aktif et
        ctx.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
        return ctx;
    }

    public void Dispose()
    {
        try
        {
            if (File.Exists(_dbPath)) File.Delete(_dbPath);
            var shm = _dbPath + "-shm";
            if (File.Exists(shm)) File.Delete(shm);
            var wal = _dbPath + "-wal";
            if (File.Exists(wal)) File.Delete(wal);
        }
        catch { }
    }
}
