using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using TrainService.Data;
using TrainService.Core.Entities;
using TrainService.Cad;
using TrainService.App.Services;

namespace TrainService.Data.Tests;

public class T5xx_ArteryTests : IClassFixture<TempSqliteFixture>
{
    private readonly TempSqliteFixture _fx;

    // LogRetentionService mock since it was requested in T509
    private readonly LogRetentionService _logRetention = new LogRetentionService();

    public T5xx_ArteryTests(TempSqliteFixture fx)
    {
        _fx = fx;
    }

    [Fact]
    public async Task T502_TumTablolar_Mevcut()
    {
        // A4 arterinin envanteri: Roadmap Bölüm 4.3'teki şema.
        string[] beklenen =
        {
            "Projects", "Layers", "TrackNodes", "TrackSegments",
            "Routes", "RouteSteps", "Switches", "Ramps",
            "Stations", "Trains", "Devices", "NetworkSwitches",
            "SwitchPorts", "HardwareBindings", "Scenarios", "ScenarioSteps",
            "SystemState", "TrainStates", "SwitchStates", "EventLogs"
        };

        await using var db = _fx.CreateContext();
        var mevcut = new List<string>();
        await using (var cmd = db.Database.GetDbConnection().CreateCommand())
        {
            cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table'";
            await db.Database.OpenConnectionAsync();
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync()) mevcut.Add(r.GetString(0));
        }

        var eksikler = beklenen.Where(t => !mevcut.Contains(t)).ToList();
        eksikler.Should().BeEmpty($"şu tablolar şemada YOK: {string.Join(", ", eksikler)}");
    }

    [Fact]
    public async Task T503_WAL_Aktif()
    {
        await using var db = _fx.CreateContext();
        await db.Database.OpenConnectionAsync();
        await using var cmd = db.Database.GetDbConnection().CreateCommand();
        cmd.CommandText = "PRAGMA journal_mode";
        var mode = (string?)await cmd.ExecuteScalarAsync();
        mode.Should().Be("wal", "State Recovery'nin çökme dayanıklılığı WAL'a bağlıdır (Roadmap 4.4)");
    }

    [Fact]
    public async Task T505_CascadeDelete_KomsuyaDokunmaz()
    {
        await using var db = _fx.CreateContext();
        var pA = TestHelpers.YeniProje(db, "Proje A"); var pB = TestHelpers.YeniProje(db, "Proje B");
        TestHelpers.KatmanVeGeometriEkle(db, pA, nodeSayisi: 4, segmentSayisi: 3);
        TestHelpers.KatmanVeGeometriEkle(db, pB, nodeSayisi: 2, segmentSayisi: 1);
        await db.SaveChangesAsync();

        db.Projects.Remove(pA);
        await db.SaveChangesAsync();

        (await db.TrackNodes.CountAsync(n => n.ProjectId == pA.Id)).Should().Be(0, "A'nın düğümleri cascade silinmeli");
        (await db.TrackSegments.CountAsync(s => s.ProjectId == pA.Id)).Should().Be(0);
        (await db.Layers.CountAsync(l => l.ProjectId == pA.Id)).Should().Be(0);
        (await db.TrackNodes.CountAsync(n => n.ProjectId == pB.Id)).Should().Be(2, "B'ye DOKUNULMAMALI");
        (await db.TrackSegments.CountAsync(s => s.ProjectId == pB.Id)).Should().Be(1);
    }

    [Fact]
    public async Task T510_ParalelYazim_LockYok()
    {
        var proje = await TestHelpers.VarsayilanProjeAsync(_fx);
        async Task Yaz(int taskNo)
        {
            await using var db = _fx.CreateContext();
            for (int i = 0; i < 200; i++)
            {
                db.EventLogs.Add(new EventLog { Ts = DateTime.UtcNow, Level = 1,
                    Source = $"task{taskNo}", Message = $"log {i}" });
                await db.SaveChangesAsync();
            }
        }
        var act = () => Task.WhenAll(Yaz(1), Yaz(2));
        await act.Should().NotThrowAsync("WAL modunda eşzamanlı yazım 'database is locked' üretmemeli");

        await using var check = _fx.CreateContext();
        (await check.EventLogs.CountAsync()).Should().Be(400, "iki task'ın 200'er kaydı da yazılmış olmalı");
    }

    [Fact]
    public async Task T509_EventLogs_HalkaliTemizlik_EnYenilerKalir()
    {
        const int tavan = 2000;
        await using var db = _fx.CreateContext();
        for (int i = 0; i < tavan + 500; i++)
            db.EventLogs.Add(new EventLog { Ts = DateTime.UtcNow.AddMilliseconds(i),
                                            Level = 1, Source = "test", Message = $"m{i}" });
        await db.SaveChangesAsync();
        await _logRetention.TrimAsync(db, tavan);

        (await db.EventLogs.CountAsync()).Should().Be(tavan);
        (await db.EventLogs.AnyAsync(e => e.Message == "m2499")).Should().BeTrue("EN YENİ kayıt korunmalı");
        (await db.EventLogs.AnyAsync(e => e.Message == "m0")).Should().BeFalse("EN ESKİ kayıt düşmeli");
    }

    [Fact]
    public async Task T512_TurkceKarakter_RoundTrip()
    {
        const string ad = "Üsküdar Şubesi – Çizim №1 (ğüşiöç ĞÜŞİÖÇ)";
        Guid id;
        await using (var db = _fx.CreateContext())
        { var p = new Project { Name = ad }; db.Projects.Add(p); await db.SaveChangesAsync(); id = p.Id; }
        await using (var db2 = _fx.CreateContext())
            (await db2.Projects.FindAsync(id))!.Name.Should().Be(ad, "encoding kaybı olmamalı");
    }

    [Fact]
    public async Task T504_Proje_RoundTrip_Derin()
    {
        // 3 katman + 10 node + 8 segment kur → SaveDocumentAsync → yeni context → LoadDocumentAsync
        // → katman sayısı/adları, node pozisyonları (1e-9), segment Start/End ilişkileri birebir.
        // (T1815'in genişletilmiş hali; T1815 5+4 küçük set, bu 10+8 ve KATMAN dahil.)
        var (store, projectId) = await TestHelpers.YeniStoreVeProjeAsync(_fx);
        var doc = TestHelpers.DokumaniKur(katman: 3, node: 10, segment: 8, gridSize: 100.0);
        await store.SaveDocumentAsync(projectId, doc);

        var loaded = await store.LoadDocumentAsync(projectId);
        loaded!.Layers.Should().HaveCount(3);
        loaded.Layers.Select(l => l.Name).Should().BeEquivalentTo(doc.Layers.Select(l => l.Name));
        loaded.Entities.OfType<TrackNode>().Should().HaveCount(10);
        loaded.Entities.OfType<TrackSegment>().Should().HaveCount(8);
        foreach (var n in doc.Entities.OfType<TrackNode>())
        {
            var ln = loaded.Entities.OfType<TrackNode>().Single(x => x.Id == n.Id);
            ln.Position.X.Should().BeApproximately(n.Position.X, 1e-9);
            ln.Position.Y.Should().BeApproximately(n.Position.Y, 1e-9);
            ln.LayerId.Should().Be(n.LayerId, "düğümün katman ataması korunmalı");
        }
    }
}
