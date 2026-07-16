using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using TrainService.Data;
using TrainService.Core.Entities;

namespace TrainService.Data.Tests;

public class T18xx_PersistenceTests : IClassFixture<TempSqliteFixture>
{
    private readonly TempSqliteFixture _fx;

    public T18xx_PersistenceTests(TempSqliteFixture fx)
    {
        _fx = fx;
    }

    [Fact]
    public async Task T1815_SaveLoad_TrackRoundTrip()
    {
        var (store, projectId) = await TestHelpers.YeniStoreVeProjeAsync(_fx);
        var doc = TestHelpers.DokumaniKur(katman: 1, node: 5, segment: 4, gridSize: 50.0);   // zincir: n1-n2-n3-n4-n5

        await store.SaveDocumentAsync(projectId, doc);
        var loaded = await store.LoadDocumentAsync(projectId);

        loaded.Should().NotBeNull();
        loaded!.GridSizeMm.Should().Be(50.0, "GridSizeMm proje satırında saklanmalı (AddGridSizeToProject)");
        loaded.Entities.OfType<TrackNode>().Should().HaveCount(5);
        loaded.Entities.OfType<TrackSegment>().Should().HaveCount(4);

        foreach (var s in doc.Entities.OfType<TrackSegment>())
        {
            var ls = loaded.Entities.OfType<TrackSegment>().Single(x => x.Id == s.Id);   // ★ Id AYNEN korunur
            ls.StartNodeId.Should().Be(s.StartNodeId);
            ls.EndNodeId.Should().Be(s.EndNodeId);
            loaded.TryGetEntity(ls.StartNodeId, out _).Should().BeTrue("segmentin başladığı düğüm yüklü olmalı");
            loaded.TryGetEntity(ls.EndNodeId, out _).Should().BeTrue();
            ls.LengthMm.Should().BeApproximately(s.LengthMm, 1e-9);
        }
        loaded.IsDirty.Should().BeFalse("depodan yüklenen belge temizdir");
    }

    [Fact]
    public async Task T1816_Save_BaskaProjeninVerisineDokunmaz()
    {
        var (store, pA) = await TestHelpers.YeniStoreVeProjeAsync(_fx, "A");
        var pB = (await TestHelpers.YeniProjeAsync(_fx, "B")).Id;
        await store.SaveDocumentAsync(pA, TestHelpers.DokumaniKur(1, 5, 4, 100));
        await store.SaveDocumentAsync(pB, TestHelpers.DokumaniKur(1, 3, 2, 100));

        // A'yı DEĞİŞTİR ve TEKRAR kaydet (delete-and-insert tam bu anda B'yi silme riskini taşır):
        var docA2 = TestHelpers.DokumaniKur(1, 7, 6, 100);
        await store.SaveDocumentAsync(pA, docA2);

        var loadedB = await store.LoadDocumentAsync(pB);
        loadedB!.Entities.OfType<TrackNode>().Should().HaveCount(3, "B'nin düğümleri A kaydından etkilenmemeli");
        loadedB.Entities.OfType<TrackSegment>().Should().HaveCount(2);
        var loadedA = await store.LoadDocumentAsync(pA);
        loadedA!.Entities.OfType<TrackNode>().Should().HaveCount(7, "A güncel halini yansıtmalı");
    }

    [Fact]
    public async Task T1817_IkinciKayit_SatirCogaltmaz()
    {
        var (store, projectId) = await TestHelpers.YeniStoreVeProjeAsync(_fx);
        var doc = TestHelpers.DokumaniKur(1, 5, 4, 100);

        await store.SaveDocumentAsync(projectId, doc);
        await store.SaveDocumentAsync(projectId, doc);   // aynı belge, ikinci kez

        await using var db = _fx.CreateContext();
        async Task<long> SayAsync(string tablo)
        {
            await db.Database.OpenConnectionAsync();
            await using var cmd = db.Database.GetDbConnection().CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM {tablo} WHERE ProjectId = @p";
            var prm = cmd.CreateParameter(); prm.ParameterName = "@p"; prm.Value = projectId.ToString();
            cmd.Parameters.Add(prm);
            return (long)(await cmd.ExecuteScalarAsync())!;
        }
        (await SayAsync("TrackNodes")).Should().Be(5, "delete-and-insert çoğaltma yapmamalı");
        (await SayAsync("TrackSegments")).Should().Be(4);
    }
}
