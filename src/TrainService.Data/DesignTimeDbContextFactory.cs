using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TrainService.Data;

/// <summary>
/// EF Core araçlarının (Add-Migration, Update-Database, migrations list) tasarım-zamanında
/// TrainDbContext'i kurabilmesi için factory. YALNIZCA tasarım-zamanında kullanılır;
/// üretimde DI'daki gerçek yapılandırma geçerlidir. Buradaki yol geçici bir tasarım DB'sidir,
/// migration ŞEMASINI üretmek için yeterlidir (veri taşımaz).
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TrainDbContext>
{
    public TrainDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<TrainDbContext>()
            .UseSqlite("Data Source=designtime_migrations.db")   // geçici, sadece şema üretimi için
            .Options;
        return new TrainDbContext(options);
    }
}
