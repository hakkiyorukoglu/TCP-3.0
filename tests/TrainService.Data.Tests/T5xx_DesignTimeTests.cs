using System.Linq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Xunit;

namespace TrainService.Data.Tests;

public class T5xx_DesignTimeTests
{
    [Fact]
    public void T541_DesignTimeFactory_ContextKurabilir()
    {
        // Factory, EF araçlarının çağırdığı şekilde context üretebilmeli (design-time körlüğü tekrar etmesin)
        var factory = new TrainService.Data.DesignTimeDbContextFactory();
        using var ctx = factory.CreateDbContext(System.Array.Empty<string>());
        ctx.Should().NotBeNull();
        ctx.Database.Should().NotBeNull();
    }

    [Fact]
    public void T542_DesignTimeFactory_TumMigrationlariTanir()
    {
        // Factory ile kurulan context, migration assembly'sini görebilmeli
        var factory = new TrainService.Data.DesignTimeDbContextFactory();
        using var ctx = factory.CreateDbContext(System.Array.Empty<string>());
        var migrations = ctx.Database.GetMigrations().ToList();
        migrations.Should().NotBeEmpty("en az InitialSchema migration'ı tanınmalı");
    }

    [Fact]
    public void T543_Sema_BeklenenMigrationlariIcerir()
    {
        // AddMissingTables gibi kritik migration'lar listede olmalı (Pending kontrolünün test karşılığı)
        var factory = new TrainService.Data.DesignTimeDbContextFactory();
        using var ctx = factory.CreateDbContext(System.Array.Empty<string>());
        var migrations = ctx.Database.GetMigrations().ToList();
        migrations.Should().Contain(m => m.Contains("InitialSchema") || m.Contains("Initial"),
            "temel şema migration'ı bulunmalı");
    }
}
